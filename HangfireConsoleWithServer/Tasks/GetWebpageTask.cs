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
            var urls = jobContext.GetUrlsFromEnvironment();

            var httpClient = new HttpClient();
            var parallelOptions = new ParallelOptions
            {
                CancellationToken = ct
            };
            Parallel.ForEach(urls, url =>
            {
                Console.WriteLine("Step 1 - Downloading content from URL '{0}'", url);
                
                var req = new HttpRequestMessage(HttpMethod.Get, url);
                var res = httpClient.SendAsync(req).Result;
                if (!res.IsSuccessStatusCode)
                    throw new HttpRequestException(res.ReasonPhrase);

                jobContext.AddResult(url, res.Content.ReadAsStringAsync().Result);
            });
            return Task.FromResult(taskContext);
        }

        public void Dispose()
        {
        }
    }
}
