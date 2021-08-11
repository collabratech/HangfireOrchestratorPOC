using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace TaskTimeout
{
	public class Program
	{
		public static void Main(string[] args)
		{

			var teste = new WebClientWithTimeout();
			teste.DownloadStringTaskAsync("https://orderapi-sandbox.azurewebsites.net/api/Orders");
			//var processors = new Processor[]
			//{
			//	new Processor1(),
			//	new Processor2(),
			//	new Processor3(),
			//};

			//var token = new CancellationTokenSource();
			//var timer = new Timer(
			//	callback: state => token.Cancel(),
			//	state: null,
			//	dueTime: TimeSpan.FromSeconds(5),
			//	period: TimeSpan.FromMilliseconds(-1));

			//try
			//{
			//	var tasks = processors.Select(p => p.Start(token.Token)).ToArray();
			//	if (!Task.WaitAll(tasks, TimeSpan.FromSeconds(5)))
			//	{
			//		token.Cancel();
			//	}
			//}
			//catch (AggregateException ex)
			//{
			//	Console.WriteLine(ex);
			//}
			//finally
			//{
			//	timer.Dispose();
			//	token.Dispose();
			//}
		}

		public class WebClientWithTimeout : WebClient
		{
			public int Timeout { get; set; } = 10000; //10 secs default

			//for sync requests
			protected override WebRequest GetWebRequest(Uri uri)
			{
				var w = base.GetWebRequest(uri);
				w.Timeout = Timeout; //10 seconds timeout
				return w;
			}

			//the above does not work for async requests, lets override the method
			public new async Task<string> DownloadStringTaskAsync(Uri address)
			{
				return await RunWithTimeout(base.DownloadStringTaskAsync(address));
			}

			public new async Task<string> UploadStringTaskAsync(string address, string data)
			{
				return await RunWithTimeout(base.UploadStringTaskAsync(address, data));
			}

			private async Task<T> RunWithTimeout<T>(Task<T> task)
			{
				if (task == await Task.WhenAny(task, Task.Delay(Timeout)))
					return await task;
				else
				{
					this.CancelAsync();
					throw new TimeoutException();
				}
			}
		}
		abstract class Processor
		{
			public Task Start(CancellationToken cancellationToken)
			{
				try
				{
					return Task.Run(async () => await StartCore(cancellationToken));
				}
				catch (Exception ex)
				{
					// logging
					return Task.FromException(ex);
				}
			}

			protected abstract Task StartCore(CancellationToken cancellationToken);
		}

		class Processor1 : Processor
		{
			protected override Task StartCore(CancellationToken cancellationToken)
			{
				Console.WriteLine(GetType().Name + " started.");
				for (int i = 0; i < 6; i++)
				{
					cancellationToken.ThrowIfCancellationRequested();
					Thread.Sleep(TimeSpan.FromSeconds(1));
				}
				Console.WriteLine(GetType().Name + " finished.");
				return Task.FromResult<object>(null);
			}
		}

		class Processor2 : Processor
		{
			protected override Task StartCore(CancellationToken cancellationToken)
			{
				Console.WriteLine(GetType().Name + " started.");
				for (int i = 0; i < 2; i++)
				{
					cancellationToken.ThrowIfCancellationRequested();
					Thread.Sleep(TimeSpan.FromSeconds(1));
				}
				Console.WriteLine(GetType().Name + " finished.");
				return Task.FromResult<object>(null);
			}
		}

		class Processor3 : Processor
		{
			protected override Task StartCore(CancellationToken cancellationToken)
			{
				Console.WriteLine(GetType().Name + " started.");
				for (int i = 0; i < 4; i++)
				{
					cancellationToken.ThrowIfCancellationRequested();
					Thread.Sleep(TimeSpan.FromSeconds(1));
				}
				Console.WriteLine(GetType().Name + " finished.");
				return Task.FromResult<object>(null);
			}
		}
	}
}