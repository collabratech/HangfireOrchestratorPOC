using Hangfire.Logging;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;
using Hangfire.Pipeline;
using System;

namespace PipelineTasks.Tasks
{
    public class LogResultTask : IPipelineTask
    {

        public LogResultTask()
        {
        }

        public Task<IPipelineTaskContext> ExecuteTaskAsync(IPipelineTaskContext taskContext, IPipelineJobContext jobContext, IPipelineStorage pipelineStorage, CancellationToken ct)
        {
            var serialized = JsonConvert.SerializeObject(jobContext.Result.Where(result => result.Key.Contains("_count")));
			var ResultTask2 = CountWordsTask.Result;
			Console.WriteLine("Step 3 - Save SUCCESS.... "+ ResultTask2);
			return Task.FromResult(taskContext);
        }

        public void Dispose()
        {
        }
    }
}
