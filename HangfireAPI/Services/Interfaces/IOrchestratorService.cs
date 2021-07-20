namespace HangfireJobFlow.Services.Interfaces
{
	public interface IOrchestratorService
	{
		void FireAndForgetJob();

		void ReccuringJob();

		void DelayedJob();

		void ContinuationJob();

		public void RequeueJob();
	}
}
