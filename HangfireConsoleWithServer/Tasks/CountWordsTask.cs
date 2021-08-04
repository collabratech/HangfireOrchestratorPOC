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
		public static string Result { get; set; }

		public CountWordsTask()
        {
			_cancellationTokenSource = new CancellationTokenSource();
		}

		public Task<IPipelineTaskContext> ExecuteTaskAsync(IPipelineTaskContext taskContext, IPipelineJobContext jobContext, IPipelineStorage pipelineStorage, CancellationToken ct)
		{
			var urls = jobContext.GetEnvironment<string>("urls").Split(',');
			var pattern = new Regex(taskContext.GetArg<string>("pattern"));

			var parallelOptions = new ParallelOptions
			{
				CancellationToken = ct
			};

			Parallel.ForEach(urls, url =>
			{
				for (int iCount = 1; iCount <= 5; iCount++)
				{
					var stateName = SimulateChangeState(iCount);
					Result = stateName;
					Console.WriteLine("Step 2 - Read state........ '{0}'", stateName);
				}
				
				var tokens = pattern.Matches(jobContext.GetResult<string>(url));
				jobContext.AddResult(url + Suffix, tokens.Count);
			});
			return Task.FromResult(taskContext);
		}

		public string SimulateChangeState(int iCount)
		{
			while (iCount<5)
			{
				Thread.Sleep(500);
				return ProcessingState.StateName;
			}

			if (iCount % 2 == 0)
				return SucceededState.StateName;
			else
				return FailedState.StateName;
		}
		public void Dispose()
        {
        }
    }
}
