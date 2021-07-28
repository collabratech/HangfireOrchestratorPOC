using Hangfire.Logging;
using Hangfire.Pipeline;
using HtmlAgilityPack;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace PipelineTasks.Tasks
{
    public sealed class GetWebpageTextTask : IPipelineTask
    {
        public const string Suffix = "_text";

        public GetWebpageTextTask()
        {
        }

        public Task<IPipelineTaskContext> ExecuteTaskAsync(IPipelineTaskContext taskContext, IPipelineJobContext jobContext, IPipelineStorage pipelineStorage, CancellationToken ct)
        {
            var urls = jobContext.GetUrlsFromEnvironment();
            var parallelOptions = new ParallelOptions
            {
                CancellationToken = ct
            };
            Parallel.ForEach(urls, url =>
            {
                Console.WriteLine("Step 2 - Stripping tags from '{0}'", url);
               
                var html = (string)jobContext.Result[url];
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);
                var text = htmlDoc.DocumentNode?.SelectSingleNode("//body")?.InnerText;

                jobContext.AddResult(url + Suffix, text);
            });
            return Task.FromResult(taskContext);
        }

        public void Dispose()
        {
        }
    }
}
