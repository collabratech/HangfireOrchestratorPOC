using Hangfire.Logging;
using Hangfire.Pipeline;
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
						ForceExecuteFailed("......waiting previous task, new attempt");
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
		public void Dispose()
        {
        }
    }
}
