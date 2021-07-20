using HangfireJobFlow.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Hangfire;
using System;

namespace HangfireJobFlow.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class OrchestratorController : ControllerBase
	{
		private readonly IOrchestratorService _OrchestratorService;
		private readonly IBackgroundJobClient _backgroundJobClient;
		private readonly IRecurringJobManager _recurringJobManager;

		public OrchestratorController(IOrchestratorService OrchestratorService, IBackgroundJobClient backgroundJobClient, IRecurringJobManager recurringJobManager)
		{
			_OrchestratorService = OrchestratorService;
			_backgroundJobClient = backgroundJobClient;
			_recurringJobManager = recurringJobManager;
		}

		/// <summary>
		/// Create Fire And Forget Job in Hangfire Services .
		/// </summary>
		[HttpGet("/FireAndForgetJob")]
		public ActionResult CreateFireAndForgetJob()
		{
			_backgroundJobClient.Enqueue(() => _OrchestratorService.FireAndForgetJob());
			return Ok();
		}

		/// <summary>
		/// Update recurring Job in Hangfire Services with CRON.
		/// </summary>
		[HttpGet("/ReccuringUpdateJob")]
		public ActionResult CreateReccuringJob()
		{
			_recurringJobManager.AddOrUpdate("jobId", () => _OrchestratorService.ReccuringJob(), Cron.Minutely);
			return Ok();
		}

		/// <summary>
		/// Create a scheduled or delayed Job in Hangfire Services with timespan.
		/// </summary>
		[HttpGet("/ScheduleJob")]
		public ActionResult CreateDelayedJob()
		{
			_backgroundJobClient.Schedule(() => _OrchestratorService.DelayedJob(), TimeSpan.FromSeconds(60));
			return Ok();
		}

		/// <summary>
		/// Create a Continuation Job in Hangfire Services jobID.
		/// </summary>
		[HttpGet("/CreateContinuationJob")]
		public ActionResult CreateContinuationJob()
		{
			var parentJobId = _backgroundJobClient.Enqueue(() => _OrchestratorService.FireAndForgetJob());
			_backgroundJobClient.ContinueJobWith(parentJobId, () => _OrchestratorService.ContinuationJob());
			
			return Ok();
		}

		/// <summary>
		/// Requeue a existing Job in Hangfire Services with jobID.
		/// </summary>
		[HttpGet("/RequeueJob")]
		public ActionResult RequeueJob(string parentJobId)
		{
			//var parentJobId = _backgroundJobClient.Enqueue(() => _OrchestratorService.FireAndForgetJob());
			_backgroundJobClient.Requeue(parentJobId);
			return Ok();
		}
	}
}
