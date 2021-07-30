using Hangfire;
using Hangfire.Logging;
using Hangfire.Pipeline;
using Hangfire.SqlServer;
using Hangfire.States;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PipelineTasks.Tasks
{
    public sealed class CountWordsTask : IPipelineTask
    {
        public const string Suffix = "_count";
		private readonly CancellationTokenSource _cancellationTokenSource;

		public CountWordsTask()
        {
			_cancellationTokenSource = new CancellationTokenSource();
		}

		public Task<IPipelineTaskContext> ExecuteTaskAsync(IPipelineTaskContext taskContext, IPipelineJobContext jobContext, IPipelineStorage pipelineStorage, CancellationToken ct)
		{
			var urls = jobContext.GetUrlsFromEnvironment();
			var patternArg = taskContext.GetArg<string>("pattern");

			if (string.IsNullOrEmpty(patternArg))
				throw new ArgumentNullException("pattern");

			var pattern = new Regex(patternArg);
			var parallelOptions = new ParallelOptions
			{
				CancellationToken = ct
			};

			Parallel.ForEach(urls, url =>
			{
				Console.WriteLine("Step 2 - Counting words from '{0}'", url);

				Task.Run(async () =>
					{
						ChangeToEnqueuedState(jobContext.Id);
						await Task.Delay(3000, _cancellationTokenSource.Token);
					}, _cancellationTokenSource.Token);

				var text = jobContext.GetResult<string>(url + GetWebpageTextTask.Suffix);
				if (string.IsNullOrEmpty(text))
					return;

				var tokens = pattern.Matches(text);
				jobContext.AddResult(url + Suffix, tokens.Count);
			});
			return Task.FromResult(taskContext);
		}

		public static void ForceExecuteFailed(string message)
		{
			Console.WriteLine("Executing Failed" + message);
		}
		public void ChangeToEnqueuedState(string jobId)
		{
			var hangfireStorage = new SqlServerStorage("Data Source = DESKTOP-P0P0RVI\\SQLEXPRESS; Initial Catalog = Hangfire; Integrated Security = True; Connect Timeout = 30; Encrypt = False; TrustServerCertificate = True; ApplicationIntent = ReadWrite; MultiSubnetFailover = False");
			var client = new BackgroundJobClient(hangfireStorage);
			var state = new EnqueuedState(); // Use the default queue

			client.ChangeState(jobId, state, FailedState.StateName);
			Console.WriteLine("Check State - " + FailedState.StateName + "- JobId: '{0}': ", jobId);
		}
		public void Dispose()
        {
        }
    }
}
