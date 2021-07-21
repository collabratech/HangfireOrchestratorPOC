using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Hangfire;
using Hangfire.Logging;
using Hangfire.Pipeline;
using Hangfire.Pipeline.SqlServer;
using Hangfire.Pipeline.Windsor;
using Hangfire.SqlServer;
using log4net.Config;
using PipelineTasks.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;

namespace PipelineTasks
{
    public class Program
    {
        private const string SqlConnectionString = @"Data Source=DESKTOP-P0P0RVI\SQLEXPRESS;Initial Catalog=hangfire;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
        private const string SqlDataTableName = "MyDataTable";
        private const string SqlDataPrimaryKeyColumn = "Id";
        private const string SqlDataValueColumn = "Data";

        private static readonly ILog Log = LogProvider.GetLogger(typeof(Program));
        private static IPipelineServer _pipelineServer;
        private static BackgroundJobServer _hangfireServer;


        public static void Main(string[] args)
        {
            var cts = new CancellationTokenSource();
            var ct = cts.Token;
			var server = string.Empty;


			try
            {
                Console.WriteLine("Get the pipeline SQL Server storage connection");
                var pipelineStorage = GetPipelineStorage();

                Console.WriteLine("Get Hangfire storage connection");
                var hangfireStorage = new SqlServerStorage(SqlConnectionString);

				if (hangfireStorage.GetMonitoringApi().Servers() != null && hangfireStorage.GetMonitoringApi().Servers().Count <= 1)
				{
					server = hangfireStorage.GetMonitoringApi().Servers()[0].Name;
				}
				else
				{
					StartServer(pipelineStorage, hangfireStorage);
				}
				Console.WriteLine("Start the pipeline server: "+ server); //this manages pipeline jobs that will run on Hangfire


				Console.WriteLine("Get a pipeline client"); // this is a wrapper over the Hangfire job client
                var client = GetClient(pipelineStorage, hangfireStorage);

                Console.WriteLine("Create a new pipeline job");
                var jobContext = new PipelineJobContext
                {
                    Id = Guid.NewGuid().ToString()
                };

                Console.WriteLine("Add Flashcards URLs");
                //collabrafcsales-functions-stg.azurewebsites.net/api/ReceiveWebhook?code=3mL2MoJaWuNiM35AoLnE[…]EkPPECZ8SP3upUgwmjv2A==&unpausedat=1599769627 ",

                var urls = new[] {
                    "http://www.cnn.com",
                    "http://www.apache.org"
                };
                jobContext.AddEnvironment("urls", string.Join(",", urls));

                Console.WriteLine("GetWebpage");
                jobContext.QueueTask(new PipelineTaskContext()
                {
                    Task = "GetWebpage",
                    Id = Guid.NewGuid().ToString(),
                    RunParallel = true,
                    Priority = 100
                });

                Console.WriteLine("Strip all the HTML tags..");
                jobContext.QueueTask(new PipelineTaskContext()
                {
                    Task = "GetWebpageText",
                    Id = Guid.NewGuid().ToString(),
                    RunParallel = false,
                    Priority = 200
                });

                Console.WriteLine("Store the job in the pipeline SQL");
                client.Storage.CreateJobContextAsync(jobContext, ct).Wait();

                var enqueuedJobContext = client.EnqueueAsync(jobContext).Result;
                Console.WriteLine("Execute the job in Hangfire ID: " + enqueuedJobContext.HangfireId);

                var requeuedJobContext = client.ScheduleAsync(enqueuedJobContext, DateTimeOffset.Now.AddMinutes(1)).Result;
                Console.WriteLine("Requeue the job in Hangfire ID: " + requeuedJobContext.HangfireId);
            }
            catch (Exception ex)
            {
                Log.ErrorException(ex.Message, ex);
            }
			Console.WriteLine("Pressione uma tecla para sair!!");
			Console.ReadKey();

            //CloseServer();
            cts.Cancel();
        }

        public static IPipelineStorage GetPipelineStorage()
        {
            Console.WriteLine("Building pipeline storage");

            var pipelineStorageOptions = new SqlPipelineStorageOptions
            {
                Table = SqlDataTableName,
                KeyColumn = SqlDataPrimaryKeyColumn,
                ValueColumn = SqlDataValueColumn,

                ConnectionFactory = new SqlConnectionFactory(SqlConnectionString),

                //Serializer = new JsonPipelineSerializer()
            };

            Console.WriteLine("Create the pipeline storage instance");
            var pipelineStorage = new SqlPipelineStorage(pipelineStorageOptions);
            return pipelineStorage;
        }

        public static void StartServer(IPipelineStorage pipelineStorage, JobStorage hangfireStorage)
        {
            Console.WriteLine("Create a new Windsor container");
            var container = new WindsorContainer();

            // Register the all the dependencies for the pipeline server and register tasks, for
            // Windsor it is recommenderd to name your tasks and use LifestyleScoped
            Console.WriteLine("Register pipeline server and tasks");
            container.Register(
                Component.For<IPipelineStorage>().Instance(pipelineStorage),
                Component.For<IPipelineTaskFactory>().Instance(new WindsorPipelineTaskFactory(container)),
                Component.For<IPipelineServer>().ImplementedBy<CustomPipelineServer>(),
                Component.For<GetWebpageTask>().Named("GetWebpage").LifestyleScoped(),
                Component.For<GetWebpageTextTask>().Named("GetWebpageText").LifestyleScoped());

            _pipelineServer = container.Resolve<IPipelineServer>();

            var hangfireServerOptions = new BackgroundJobServerOptions
            {
                // will route all Hangfire executions to the pipeline server
                Activator = new PipelineJobActivator(_pipelineServer),
                WorkerCount = Environment.ProcessorCount * 5 //degree of parallelism
            };
            
            Console.WriteLine("Building Hangfire server");
            _hangfireServer = new BackgroundJobServer(hangfireServerOptions, hangfireStorage);
        }

        public static PipelineClient GetClient(IPipelineStorage pipelineStorage, JobStorage hangfireStorage)
        {
            Console.WriteLine("Building Hangfire client");

            var hangfireClient = new BackgroundJobClient(hangfireStorage);
            var client = new PipelineClient(pipelineStorage, hangfireClient);

            return client;
        }

        public static void CloseServer()
        {
            Log.Info("Closing server");
            _pipelineServer.Dispose();
            _hangfireServer.Dispose();
        }
    }
}
