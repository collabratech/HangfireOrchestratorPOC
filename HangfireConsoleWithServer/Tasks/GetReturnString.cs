using Hangfire.Logging;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using Hangfire.Pipeline;
using System;

namespace PipelineTasks.Tasks
{
    public class GetReturnString : IPipelineTask
    {
		public static string ResultTask1 { get; set; }
		public GetReturnString()
        {
        }

        public Task<IPipelineTaskContext> ExecuteTaskAsync(IPipelineTaskContext taskContext, IPipelineJobContext jobContext, IPipelineStorage pipelineStorage, CancellationToken ct)
        {
			ResultTask1 = jobContext.GetEnvironment<string>("Order");

			jobContext.AddResult(taskContext.Id, ResultTask1);

			ExecuteThis(taskContext.Id);

			return Task.FromResult(taskContext);
        }

		public static void ExecuteThis(string message)
		{
			Console.WriteLine("Step 1 - Request some string: " + message);
		}

		public void Dispose()
        {
        }
    }
}
