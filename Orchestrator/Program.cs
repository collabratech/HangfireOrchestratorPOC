using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Hangfire;
using Hangfire.Pipeline;
using Hangfire.Pipeline.SqlServer;
using Hangfire.Pipeline.Windsor;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using PipelineTasks;
using PipelineTasks.Tasks;
using System;
using System.Threading;

namespace Orchestrator
{
	public class Program
	{
		public static void Main(string[] args)
		{
			CreateHostBuilder(args).Build().Run();
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder =>
				{
					webBuilder.UseStartup<Startup>();
				});
	}
}
