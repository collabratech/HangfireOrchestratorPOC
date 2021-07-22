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
		[Obsolete]
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

			//for (int i = 1; i <= 5; i++)
			//{
			var parentJobId = BackgroundJob.Enqueue(() => FireAndForget());
			var jobId = BackgroundJob.Schedule(() => Delayed(), TimeSpan.FromSeconds(30));
			BackgroundJob.ContinueWith(jobId, () => ContinueWith());

			var id1 = BackgroundJob.Enqueue(() => ExecuteThis(1));

			var id2 = BackgroundJob.ContinueWith(id1, () => ExecuteThis(2));

			var id3 = BackgroundJob.ContinueWith(id2, () => ExecuteThis(3));

			BackgroundJob.ContinueWith(id3, () => ExecuteThis(4));

			RecurringJob.AddOrUpdate(() => Recurring(), Cron.MinuteInterval(1));
			//}

			using (var server = new BackgroundJobServer())
			{
				Console.ReadLine();
			}

			Console.WriteLine("Press to exit...");
			Console.ReadKey();
			CloseServer();
		}

		[DisableConcurrentExecution(15)]
		public static void ExecuteThis(int number)
		{
			Console.WriteLine("Start " + number);
			Console.WriteLine("End " + number);
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
		public static void ContinueWith()
		{
			Console.WriteLine("Executing some Task -> Continue/Child!");
		}


		//Recurring
		//This type of Job is executed multiple times and is triggered based upon the recurrence pattern (Hangfire.Cron type).
		public static void Recurring()
		{
			Console.WriteLine("Executing some Task -> Recurring!");
		}
		public static void CloseServer()
		{
			Console.WriteLine("Closing server");
		}
	}
}
