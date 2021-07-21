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
            // Get URLs from the job context environment -- see TaskExtensions.cs
            var urls = jobContext.GetUrlsFromEnvironment();

            var httpClient = new HttpClient();

            // Iterate over all of the URLs
            var parallelOptions = new ParallelOptions
            {
                CancellationToken = ct
            };
            Parallel.ForEach(urls, url =>
            {
                Console.WriteLine("Downloading content from URL '{0}'", url);
                
                // Build a new HTTP request
                var req = new HttpRequestMessage(HttpMethod.Get, url);

                // Make the HTTP call and check for errors
                var res = httpClient.SendAsync(req).Result;
                if (!res.IsSuccessStatusCode)
                    throw new HttpRequestException(res.ReasonPhrase);

                // Store the output in the job context
                jobContext.AddResult(url, res.Content.ReadAsStringAsync().Result);
            });
            return Task.FromResult(taskContext);
        }

        public void Dispose()
        {
        }
    }
}
