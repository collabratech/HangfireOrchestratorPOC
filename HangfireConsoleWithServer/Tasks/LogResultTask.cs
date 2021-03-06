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
            var countWordTasks = jobContext.Result.Where(result => result.Key.Contains(CountWordsTask.Suffix));
            var serialized = JsonConvert.SerializeObject(countWordTasks);
			Console.WriteLine("Step 3 - Finalizando para gravar : "+ serialized);
			return Task.FromResult(taskContext);
        }

        public void Dispose()
        {
        }
    }
}
