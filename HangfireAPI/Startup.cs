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
using System;
using System.IO;
using System.Reflection;

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
				c.SwaggerDoc("v1", new OpenApiInfo
				{
					Title = "Hangfire Orchestrator application - POC",
					Version = "v1",
					Description = "Hangfire Orchestrator application is a POC - prove of concept - to Collabra Orchestrator",
					License = new OpenApiLicense()
					{
						Url = new Uri("https://www.collabratechnology.com/collabra-api-license"),
						Name = "Proprietary License"
					},
					TermsOfService = new Uri("https://www.collabratechnology.com/terms-of-service/"),
					Contact = new OpenApiContact()
					{
						Url = new Uri("https://www.collabratechnology.com/#contact"),
						Name = "Contact Us"
					}
				});
				c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
				//c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{typeof(OrderingMongoDbContext).Assembly.GetName().Name}.xml"));

			});

			services.AddScoped<IOrchestratorService, OrchestratorService>();

			services.AddHangfire(x =>
			{
				x.UseSqlServerStorage(Configuration.GetConnectionString("DBConnection"));
			});

			services.AddHangfireServer();
		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			app.UseStaticFiles();
			app.UseSwagger();
			app.UseSwaggerUI(config =>
			{
				config.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
				config.DocumentTitle = "Hangfire Orchestrator API";
				//config.RoutePrefix = "";
				config.InjectStylesheet("/swagger-ui/custom.css");
				config.InjectJavascript("/swagger-ui/custom.js");
			});

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
