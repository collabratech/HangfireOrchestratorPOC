using Hangfire;
using Hangfire.Dashboard;
using HangfireJobFlow.Services;
using HangfireJobFlow.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace HangfireJobFlow
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		//==================================================================
		//TYPES OF JOBS - background jobs using Hangfire: 
		//	fire-and-forget(to offload the method invocation), 
		//	delayed(to perform the call after some time) 
		//	and recurring(to perform methods hourly, daily and so on).

		// HANGFIRE SERVER  - https://localhost:5001/hangfire 
		//   IMPORTANT - Server should be always on to perform scheduling and processing logic
		//   Hangfire Server consist of different components that are doing different work:
		//				workers listen to queue and process jobs,
		//				recurring scheduler enqueues recurring jobs,
		//				schedule poller enqueues delayed jobs,
		//				expire manager removes obsolete jobs and
		//				keeps the storage as clean as possible, etc.
		//==================================================================
		// STATES ==	Enqueued,
		//				Scheduled,
		//				Awaiting,
		//				Processing,
		//				Failed, - expira 24hrs (ou method WithJobExpirationTimeout)
		//				Succeedede Deleted

		//ENQUEUE method does not call the target method immediately, it runs the following steps instead:
		//				Serialize a method information and all its arguments.
		//				Create a new background job based on the serialized information.
		//				Save background job to a persistent storage.
		//				Enqueue background job to its queue.

		//After run SERVER BackgroundJob.
		//				Enqueue method immediately returns to a caller.Another Hangfire component,
		//				called Hangfire Server, checks the persistent storage for enqueued background jobs
		//				and performs them in a reliable way.

		//SERVER ENQUEUED jobs are handled by a dedicated pool of worker threads
		//				Fetch next job and hide it from other workers.
		//				Perform the job and all its extension filters.
		//				Remove the job from the queue.

		//SCHEDULE -	send an email to newly registered users a day after their registration
		//				BackgroundJob.Schedule(TimeSpan.FromDays(1));
		//				HF Server periodically checks the schedule to enqueue scheduled jobs to their queues,

		//RECURRENT TASKS - JOBS
		//				RecurringJob.AddOrUpdate("easyjob", () => Console.Write("Easy!"), Cron.Daily);
		//				overloads to run jobs on a minute, hourly, daily, weekly, monthly

		//ARGUMENTS
		//				use JSON to serialize some adicional information

		//TOKENS - cancellation tokens
		//				background jobs that have a cancellation token parameter in a method and polls the storage to watch their current states


		//EXCEPTIONS
		//	 AutomaticRetryAttribute class - if failed job to be automatically retried after increasing delay.


		// HANGFIRE PRO - is a set of extension packages that boost the performance and simplify
		// the maintenance of background job processing in large applications
		//https://www.hangfire.io/pro/

		//=======================================================================

		public IConfiguration Configuration { get; }

		public void ConfigureServices(IServiceCollection services)
		{
			services.AddControllers();

			services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc("v1", new OpenApiInfo { Title = "HangfireJobFlow", Version = "v1" });
			});
			
			services.AddScoped<IJobTestService, JobTestService>();

			services.AddHangfire(x =>
			{
				x.UseSqlServerStorage(Configuration.GetConnectionString("DBConnection"));
			});

			services.AddHangfireServer();
		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
				app.UseSwagger();
				app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "HangfireJobFlow v1"));
			}

			app.UseHttpsRedirection();

			app.UseRouting();

			app.UseAuthorization();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
				endpoints.MapHangfireDashboard();
			});

			app.UseHangfireDashboard();
			//app.UseHangfireDashboard("/hangfire", new DashboardOptions
			//{
			//	Authorization = new[] { new MyAuthorizationFilter() },
			//	IsReadOnlyFunc = (DashboardContext context) => true
			//});
		}

	}
	public class MyAuthorizationFilter : IDashboardAuthorizationFilter
	{
		public bool Authorize(DashboardContext context)
		{
			var httpContext = context.GetHttpContext();

			return httpContext.User.Identity.IsAuthenticated;
		}
	}
}
