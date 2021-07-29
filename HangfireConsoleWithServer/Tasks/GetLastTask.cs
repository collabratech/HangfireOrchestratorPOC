using Hangfire.Logging;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using Hangfire.Pipeline;
using System;

namespace PipelineTasks.Tasks
{
    public class GetLastTask : IPipelineTask
    {

        public GetLastTask()
        {
        }

        public Task<IPipelineTaskContext> ExecuteTaskAsync(IPipelineTaskContext taskContext, IPipelineJobContext jobContext, IPipelineStorage pipelineStorage, CancellationToken ct)
        {
			ExecuteThis(taskContext.Id);
			return Task.FromResult(taskContext);
        }

		public static void ExecuteThis(string Id)
		{
			Console.WriteLine("Step 4 - Executing the last task Id: " + Id);
		}

		public void Dispose()
        {
        }
    }
}
