﻿using Castle.MicroKernel.Registration;
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

		private static IPipelineServer _pipelineServer;
		private static BackgroundJobServer _hangfireServer;

		public static void Main(string[] args)
		{

			// Get a new cancellation token
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

				var urls = new[] {
					"http://www.nyse.com",
					"http://www.cnn.com"
				};
				jobContext.AddEnvironment("urls", string.Join(",", urls));


				jobContext.QueueTask(new PipelineTaskContext()
				{
					Task = "GetWebpage",
					Id = Guid.NewGuid().ToString(),
					RunParallel = true,
					Priority = 100
				});

				jobContext.QueueTask(new PipelineTaskContext()
				{
					Task = "GetWebpageText",
					Id = Guid.NewGuid().ToString(),
					RunParallel = false,
					Priority = 200
				});

				client.Storage.CreateJobContextAsync(jobContext, ct).Wait();

				var enqueuedJobContext = client.EnqueueAsync(jobContext).Result;

				var continueJobContext = client.ContinueJobWithAsync(jobContext).Result;

			
				//---TODO -------------------------------------------------------
				//var parentJobId = BackgroundJob.Enqueue(() => FireAndForget());
				//var jobId = BackgroundJob.Schedule(() => Delayed(), TimeSpan.FromSeconds(30));
				//BackgroundJob.ContinueJobWith(jobId, () => ContinueWith(jobId), JobContinuationOptions.OnlyOnSucceededState);

				//var id1 = BackgroundJob.Enqueue(() => ExecuteThis("initial"));

				//var id2 = BackgroundJob.ContinueJobWith(id1, () => ExecuteThis(id1), JobContinuationOptions.OnlyOnSucceededState);

				//for (int i = 1; i <= 5; i++)
				//{
				//	id0 = BackgroundJob.ContinueJobWith(id2, () => SomeJobWithFailed(id2, i), JobContinuationOptions.OnlyOnSucceededState);
				//}
				//RecurringJob.AddOrUpdate(id0, () => Recurring(id0), "*/1 * * * *");

				//var id3 = BackgroundJob.ContinueJobWith(id2, () => ExecuteThis(id2), JobContinuationOptions.OnlyOnSucceededState);
				//BackgroundJob.ContinueJobWith(id3, () => ExecuteThis(id3), JobContinuationOptions.OnlyOnSucceededState);

				//RecurringJob.AddOrUpdate(parentJobId, () => Recurring(jobId), "*/1 * * * *");

				//-----------------------------------------------------

				Console.WriteLine("Enqueued job with Hangfire ID '{0}'", enqueuedJobContext.HangfireId);

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
			Console.WriteLine("Building pipeline storage");

			var pipelineStorageOptions = new SqlPipelineStorageOptions
			{
				Table = SqlDataTableName,
				KeyColumn = SqlDataPrimaryKeyColumn,
				ValueColumn = SqlDataValueColumn,

				ConnectionFactory = new SqlConnectionFactory(SqlConnectionString),
			};

			var pipelineStorage = new SqlPipelineStorage(pipelineStorageOptions);
			return pipelineStorage;
		}

		public static void StartServer(IPipelineStorage pipelineStorage, JobStorage hangfireStorage)
		{
			Console.WriteLine("Building the DI/IoC container");
			var container = new WindsorContainer();

			container.Register(
				Component.For<IPipelineStorage>().Instance(pipelineStorage),
				Component.For<IPipelineTaskFactory>().Instance(new WindsorPipelineTaskFactory(container)),
				Component.For<IPipelineServer>().ImplementedBy<CustomPipelineServer>(),
				Component.For<GetWebpageTask>().Named("GetWebpage").LifestyleScoped(),
				Component.For<GetWebpageTextTask>().Named("GetWebpageText").LifestyleScoped());

			Console.WriteLine("Resolving pipeline server from container");
			_pipelineServer = container.Resolve<IPipelineServer>();

			var hangfireServerOptions = new BackgroundJobServerOptions
			{
				Activator = new PipelineJobActivator(_pipelineServer)
			};
			Console.WriteLine("Building Hangfire server");
			_hangfireServer = new BackgroundJobServer(hangfireServerOptions, hangfireStorage);
		}

		public static PipelineClient GetClient(IPipelineStorage pipelineStorage, JobStorage hangfireStorage)
		{
			Console.WriteLine("Building Hangfire client");
			var hangfireClient = new BackgroundJobClient(hangfireStorage);
			
			Console.WriteLine("Building pipeline client");
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
