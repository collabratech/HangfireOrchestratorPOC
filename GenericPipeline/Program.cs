using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.SqlServer;

namespace GenericPipeline
{
	public static class Program
	{
		public static void Main()
		{
			GlobalConfiguration.Configuration
				.UseColouredConsoleLogProvider()
				.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
				.UseSimpleAssemblyNameTypeSerializer()
				.UseRecommendedSerializerSettings()
				.UseResultsInContinuations()
				.UseSqlServerStorage(@"Data Source = DESKTOP-P0P0RVI\SQLEXPRESS; Initial Catalog = Hangfire; Integrated Security = True; Connect Timeout = 30; Encrypt = False; TrustServerCertificate = True; ApplicationIntent = ReadWrite; MultiSubnetFailover = False", new SqlServerStorageOptions
				{
					CommandBatchMaxTimeout = TimeSpan.FromMinutes(1),
					QueuePollInterval = TimeSpan.Zero,
					SlidingInvisibilityTimeout = TimeSpan.FromMinutes(1),
					UseRecommendedIsolationLevel = true,
					UsePageLocksOnDequeue = true,
					DisableGlobalLocks = true,
					EnableHeavyMigrations = true
				});

			var backgroundJobs = new BackgroundJobClient
			{
				RetryAttempts = 5
			};

			var cts = new CancellationTokenSource();

			//for (int i = 1; i <= 5; i++)
			//{
			var parentJobId = BackgroundJob.Enqueue(() => FireAndForget());
			var jobId = BackgroundJob.Schedule(() => Delayed(), TimeSpan.FromSeconds(30));
			BackgroundJob.ContinueJobWith(jobId, () => ContinueWith(jobId), JobContinuationOptions.OnlyOnSucceededState);

			var id1 = BackgroundJob.Enqueue(() => ExecuteThis("initial"));

			var id2 = BackgroundJob.ContinueJobWith(id1, () => ExecuteThis(id1), JobContinuationOptions.OnlyOnSucceededState);

			var id3 = BackgroundJob.ContinueJobWith(id2, () => ExecuteThis(id2), JobContinuationOptions.OnlyOnSucceededState);

			BackgroundJob.ContinueJobWith(id3, () => ExecuteThis(id3), JobContinuationOptions.OnlyOnSucceededState);

			RecurringJob.AddOrUpdate(parentJobId, () => Recurring(jobId), "*/1 * * * *");
			//}

			using (var server = new BackgroundJobServer())
			{
				Console.WriteLine("Press to exit...");
				Console.ReadKey();
				server.Dispose();
			}
		}

		[DisableConcurrentExecution(15)]
		public static void ExecuteThis(string Id)
		{
			Console.WriteLine("Executing " + Id);
		}

		public static void FireAndForget()
		{
			Console.WriteLine("Executing some Task -> FireForget!");
		}

		//Delayed
		//This type of Job is executed only once and is triggered after a TimeSpan (specified interval of time).
		public static void Delayed()
		{
			Console.WriteLine("Executing some Task -> Delayed !");
		}

		//Continuations
		//This type of Job is executed when its specified parent job has finished its execution.
		public static void ContinueWith(string Id)
		{
			Console.WriteLine("Executing some Task -> Continue/Child: " + Id);
		}


		//Recurring
		//This type of Job is executed multiple times and is triggered based upon the recurrence pattern (Hangfire.Cron type).
		public static void Recurring(string Id)
		{
			Console.WriteLine("Executing some Task -> Recurring: " + Id);
		}
	}
}
