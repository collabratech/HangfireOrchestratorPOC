using HangfireJobFlow.Services.Interfaces;
using System;

namespace HangfireJobFlow.Services
{
	public class JobTestService : IJobTestService
	{
		public void FireAndForgetJob()
		{
			Console.WriteLine("Fire and Forget job!");
		}

		public void RequeueJob()
		{
			Console.WriteLine("Requeue job!");
		}

		public void ReccuringJob()
		{
			Console.WriteLine("Scheduled job!");
		}

		public void DelayedJob()
		{
			Console.WriteLine("Delayed job!");
		}

		public void ContinuationJob()
		{
			Console.WriteLine("Continuation job!");
		}

	}
}
