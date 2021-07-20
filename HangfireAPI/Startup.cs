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
