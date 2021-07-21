using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Owin.Hosting;

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

            var job1 = BackgroundJob.Enqueue<Services>(x => x.WriteIndex(0));
            var job2 = BackgroundJob.ContinueJobWith<Services>(job1, x => x.WriteIndex(null));
            var job3 = BackgroundJob.ContinueJobWith<Services>(job2, x => x.WriteIndex(null));
            var job4 = BackgroundJob.ContinueJobWith<Services>(job3, x => x.WriteIndex(null));
            var job5 = BackgroundJob.ContinueJobWith<Services>(job4, x => x.WriteIndex(null));

            RecurringJob.AddOrUpdate("seconds", () => Console.WriteLine("Collabra, seconds!"), "*/15 * * * * *");
            RecurringJob.AddOrUpdate(() => Console.WriteLine("Collabra, pipeline!"), Cron.Minutely);
            RecurringJob.AddOrUpdate("hourly", () => Console.WriteLine("Collabra"), "25 15 * * *");
            RecurringJob.AddOrUpdate("neverfires", () => Console.WriteLine("Can only be triggered"), "0 0 31 2 *");

            RecurringJob.AddOrUpdate("Brazilian", () => Console.WriteLine("Brazilian"), "15 08 * * *", TimeZoneInfo.FindSystemTimeZoneById("Central Brazilian Standard Time"));
            RecurringJob.AddOrUpdate("UTC", () => Console.WriteLine("UTC"), "15 10 * * *");
            RecurringJob.AddOrUpdate("Brazil", () => Console.WriteLine("Brazil"), "15 11 * * *", TimeZoneInfo.Local);

            using (WebApp.Start<Startup>("http://localhost:5010"))
            {
                var count = 1;

                while (true)
                {
                    var command = Console.ReadLine();

                    if (command == null || command.Equals("stop", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    if (command.StartsWith("add", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var workCount = int.Parse(command.Substring(4));
                            for (var i = 0; i < workCount; i++)
                            {
                                var number = i;
                                BackgroundJob.Enqueue<Services>(x => x.Random(number));
                            }
                            Console.WriteLine("Jobs enqueued.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }

                    if (command.StartsWith("async", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var workCount = int.Parse(command.Substring(6));
                            for (var i = 0; i < workCount; i++)
                            {
                                BackgroundJob.Enqueue<Services>(x => x.Async(CancellationToken.None));
                            }
                            Console.WriteLine("Jobs enqueued.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }

                    if (command.StartsWith("static", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var workCount = int.Parse(command.Substring(7));
                            for (var i = 0; i < workCount; i++)
                            {
                                BackgroundJob.Enqueue(() => Console.WriteLine("Collabra, {0}!", "pipeline"));
                            }
                            Console.WriteLine("Jobs enqueued.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }

                    if (command.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                    {
                        var workCount = int.Parse(command.Substring(6));
                        for (var i = 0; i < workCount; i++)
                        {
                            BackgroundJob.Enqueue<Services>(x => x.Error());
                        }
                    }

                    if (command.StartsWith("args", StringComparison.OrdinalIgnoreCase))
                    {
                        var workCount = int.Parse(command.Substring(5));
                        for (var i = 0; i < workCount; i++)
                        {
                            BackgroundJob.Enqueue<Services>(x => x.Args(Guid.NewGuid().ToString(), 14442, DateTime.UtcNow));
                        }
                    }

                    if (command.StartsWith("custom", StringComparison.OrdinalIgnoreCase))
                    {
                        var workCount = int.Parse(command.Substring(7));
                        for (var i = 0; i < workCount; i++)
                        {
                            BackgroundJob.Enqueue<Services>(x => x.Custom(
                                new Random().Next(),
                                new[] { "Collabra", "pipeline!" },
                                new Services.CustomObject { Id = 123 },
                                DayOfWeek.Friday
                                ));
                        }
                    }

                    if (command.StartsWith("fullargs", StringComparison.OrdinalIgnoreCase))
                    {
                        var workCount = int.Parse(command.Substring(9));
                        for (var i = 0; i < workCount; i++)
                        {
                            BackgroundJob.Enqueue<Services>(x => x.FullArgs(
                                false,
                                123,
                                'c',
                                DayOfWeek.Monday,
                                "Collabra",
                                new TimeSpan(0, 1, 10),
                                new DateTime(2021, 07, 21),
                                new Services.CustomObject { Id = 123 },
                                new[] { "1", "2", "3" },
                                new[] { 4, 5, 6 },
                                new long[0],
                                null,
                                new List<string> { "7", "8", "9" }));
                        }
                    }

                    if (command.StartsWith("in", StringComparison.OrdinalIgnoreCase))
                    {
                        var seconds = int.Parse(command.Substring(2));
                        var number = count++;
                        BackgroundJob.Schedule<Services>(x => x.Random(number), TimeSpan.FromSeconds(seconds));
                    }

                    if (command.StartsWith("cancelable", StringComparison.OrdinalIgnoreCase))
                    {
                        var iterations = int.Parse(command.Substring(11));
                        BackgroundJob.Enqueue<Services>(x => x.Cancelable(iterations, JobCancellationToken.Null));
                    }

                    if (command.StartsWith("delete", StringComparison.OrdinalIgnoreCase))
                    {
                        var workCount = int.Parse(command.Substring(7));
                        for (var i = 0; i < workCount; i++)
                        {
                            var jobId = BackgroundJob.Enqueue<Services>(x => x.EmptyDefault());
                            BackgroundJob.Delete(jobId);
                        }
                    }

                    if (command.StartsWith("fast", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var workCount = int.Parse(command.Substring(5));
                            Parallel.For(0, workCount, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, i =>
                            {
                                if (i % 2 == 0)
                                {
                                    BackgroundJob.Enqueue<Services>(x => x.EmptyCritical());
                                }
                                else
                                {
                                    BackgroundJob.Enqueue<Services>(x => x.EmptyDefault());
                                }
                            });
                            Console.WriteLine("Jobs enqueued.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }

                    if (command.StartsWith("generic", StringComparison.OrdinalIgnoreCase))
                    {
                        BackgroundJob.Enqueue<GenericServices<string>>(x => x.Method("Collabra", 1));
                    }

                    if (command.StartsWith("continuations", StringComparison.OrdinalIgnoreCase))
                    {
                        WriteString("Collabra, Hangfire continuations!");
                    }
                }
            }

            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }

        public static void WriteString(string value)
        {
            var lastId = BackgroundJob.Enqueue<Services>(x => x.Write(value[0]));

            for (var i = 1; i < value.Length; i++)
            {
                lastId = BackgroundJob.ContinueJobWith<Services>(lastId, x => x.Write(value[i]));
            }

            BackgroundJob.ContinueJobWith<Services>(lastId, x => x.WriteBlankLine());
        }
    }
}
