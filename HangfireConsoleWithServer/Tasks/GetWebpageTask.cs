using Hangfire.Logging;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using Hangfire.Pipeline;
using System;

namespace PipelineTasks.Tasks
{
    public class GetWebpageTask : IPipelineTask
    {

        public GetWebpageTask()
        {
        }

        public Task<IPipelineTaskContext> ExecuteTaskAsync(IPipelineTaskContext taskContext, IPipelineJobContext jobContext, IPipelineStorage pipelineStorage, CancellationToken ct)
        {
			var urls = jobContext.GetEnvironment<string>("urls").Split(',');
			var httpClient = new HttpClient();

            var parallelOptions = new ParallelOptions
            {
                CancellationToken = ct
            };

            Parallel.ForEach(urls, url =>
            {
                var req = new HttpRequestMessage(HttpMethod.Get, url);
                var res = httpClient.SendAsync(req).Result;

				Console.WriteLine("Step 1 - Read task: "+ taskContext.Id);

				jobContext.AddResult(url, res.Content.ReadAsStringAsync().Result);
            });
            return Task.FromResult(taskContext);
        }

        public void Dispose()
        {
        }
    }
}
