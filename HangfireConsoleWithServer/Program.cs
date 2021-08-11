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
		// Setup your data connection
		private const string SqlConnectionString = @"Data Source=DESKTOP-P0P0RVI\SQLEXPRESS;Initial Catalog=Hangfire;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
		private const string SqlDataTableName = "MyDataTable";
		private const string SqlDataPrimaryKeyColumn = "Id";
		private const string SqlDataValueColumn = "Data";
		private const string SqlDataCollabraColumn = "ProcessResult";

		private static IPipelineServer _pipelineServer;
		private static BackgroundJobServer _hangfireServer;

		public static void Main(string[] args)
		{
			var cts = new CancellationTokenSource();
			var ct = cts.Token;
			try
			{
				var pipelineStorage = GetPipelineStorage();
				var hangfireStorage = new SqlServerStorage(SqlConnectionString);
				Console.WriteLine("Start Server");
				StartServer(pipelineStorage, hangfireStorage);

				var client = GetClient(pipelineStorage, hangfireStorage);
				
				var jobContext = new PipelineJobContext
				{
					Id = Guid.NewGuid().ToString()
				};

				var orderapi = "https://localhost:5010/api/Orders/327eaa5d-c5b4-ad9d-ada7-089aaed0786f1";

				jobContext.AddEnvironment("Order", orderapi);

				jobContext.QueueTask(new PipelineTaskContext()
				{
					Task = "GetReturnString",
					Id = Guid.NewGuid().ToString(),
					Priority = 100
				});

				jobContext.QueueTask(new PipelineTaskContext()
				{
					Task = "GetStatusTaskResult",
					Id = Guid.NewGuid().ToString(),
					Priority = 200,
				});

				jobContext.QueueTask(new PipelineTaskContext()
				{
					Task = "LogResult",
					Id = Guid.NewGuid().ToString(),
					Priority = 400
				});

				client.Storage.CreateJobContextAsync(jobContext, ct).Wait();

				var enqueuedJobContext = client.EnqueueAsync(jobContext).Result;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message, ex);
			}
			Console.ReadKey();

			CloseServer();
			cts.Cancel();
		}

		public static IPipelineStorage GetPipelineStorage()
		{
			var pipelineStorageOptions = new SqlPipelineStorageOptions
			{
				Table = SqlDataTableName,
				KeyColumn = SqlDataPrimaryKeyColumn,
				ValueColumn = SqlDataValueColumn,
				CollabraColumn = SqlDataCollabraColumn,

				ConnectionFactory = new SqlConnectionFactory(SqlConnectionString),
			};

			var pipelineStorage = new SqlPipelineStorage(pipelineStorageOptions);
			return pipelineStorage;
		}

		public static void StartServer(IPipelineStorage pipelineStorage, JobStorage hangfireStorage)
		{
			var container = new WindsorContainer();

			container.Register(
				Component.For<IPipelineStorage>().Instance(pipelineStorage),
				Component.For<IPipelineTaskFactory>().Instance(new WindsorPipelineTaskFactory(container)),
				Component.For<IPipelineServer>().ImplementedBy<CustomPipelineServer>(),
				Component.For<GetReturnString>().Named("GetReturnString").LifestyleScoped(),
				Component.For<GetStatusTaskResult>().Named("GetStatusTaskResult").LifestyleScoped(),
				Component.For<LogResultTask>().Named("LogResult").LifestyleScoped());

			_pipelineServer = container.Resolve<IPipelineServer>();

			var hangfireServerOptions = new BackgroundJobServerOptions
			{
				Activator = new PipelineJobActivator(_pipelineServer)
			};
			_hangfireServer = new BackgroundJobServer(hangfireServerOptions, hangfireStorage);
		}

		public static PipelineClient GetClient(IPipelineStorage pipelineStorage, JobStorage hangfireStorage)
		{
			var hangfireClient = new BackgroundJobClient(hangfireStorage);
			var client = new PipelineClient(pipelineStorage, hangfireClient);
			return client;
		}

		public static void CloseServer()
		{
			Console.WriteLine("Closing server");
			_pipelineServer.Dispose();
			_hangfireServer.Dispose();
		}
	}
}
