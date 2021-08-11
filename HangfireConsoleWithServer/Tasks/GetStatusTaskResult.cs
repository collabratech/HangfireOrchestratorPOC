using Hangfire;
using Hangfire.Logging;
using Hangfire.Pipeline;
using Hangfire.SqlServer;
using Hangfire.States;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PipelineTasks.Tasks
{
	public class GetStatusTaskResult : IPipelineTask
	{
		public static string ResultStateName{ get; set; }

		public GetStatusTaskResult()
        {
		}

		
		public async Task<IPipelineTaskContext> ExecuteTaskAsync(IPipelineTaskContext taskContext, IPipelineJobContext jobContext, IPipelineStorage pipelineStorage, CancellationToken ct)
		{
			try
			{
				DateTimeOffset startTime = DateTimeOffset.Now;

				var result = await Task.Run(() => ReadAll());

				if (result != string.Empty)
				{
					if (result.Contains("Succeeded"))
						ResultStateName = SucceededState.StateName;
						
					if (result.Contains("errors"))
						ResultStateName = FailedState.StateName;
					
					jobContext.AddResult(ResultStateName + taskContext.Id, result);
				}
				Console.WriteLine("Step 2 - Executed Task with " + ResultStateName);

				if (ResultStateName == FailedState.StateName)
				{
					CancellationTokenSource source = new CancellationTokenSource(1000);

					while (!source.IsCancellationRequested)
					{
						Console.WriteLine("Step 2 - waiting...:"+ DateTimeOffset.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"));
					}
				}
			}
			catch (TaskCanceledException ex)
			{
				Console.WriteLine("Error...:" + ex.Message);
			}
			return await Task.FromResult(taskContext);
		}

		private async Task<string> ReadAll()
		{
			var urlOrder = GetReturnString.ResultTask1;
			var httpClient = new HttpClient();

			if (urlOrder == string.Empty)
				return "errors";

			var req = new HttpRequestMessage(HttpMethod.Get, urlOrder);
			var res = httpClient.SendAsync(req).Result;

			for (int i = 0; i < 4; i++)
			{
				Console.WriteLine("Step 2 - Trigger Attempt...:" + i + " State: " + ProcessingState.StateName);
			}

			return res.Content.ReadAsStringAsync().Result;
		}

		public void Dispose()
		{
		}






		//public bool TimeBetween()
		//{
		//	DateTime time = DateTime.Now;
		//	DateTime startDateTime = DateTime.Now;
		//	DateTime endDateTime = DateTime.Now.AddSeconds(10); 

		//	TimeSpan start = new TimeSpan(startDateTime.Hour, startDateTime.Minute, 0);
		//	TimeSpan end = new TimeSpan(endDateTime.Hour, endDateTime.Minute, endDateTime.Second);

		//	TimeSpan now = time.TimeOfDay;

		//	if (start < end)
		//		return start <= now && now <= end;
		//	// start is after end, so do the inverse comparison
		//	return !(end < now && now < start);
		//}

		//DateTime time = DateTime.Now;
		//DateTime startDateTime = DateTime.Now;
		//DateTime endDateTime = DateTime.Now.AddSeconds(10);

		//	return DateTime.Compare(startDateTime, endDateTime);

		//	//// get TimeSpan
		//	//TimeSpan start = new TimeSpan(startDateTime.Hour, startDateTime.Minute, 0);
		//	//TimeSpan end = new TimeSpan(endDateTime.Hour, endDateTime.Minute, 0);

		//	//// convert datetime to a TimeSpan
		//	//TimeSpan now = time.TimeOfDay;
		//	//// see if start comes before end
		//	//if (start < end)
		//	//	return start <= now && now <= end;
		//	//// start is after end, so do the inverse comparison
		//	//return !(end < now && now < start);
		//var ReadAllTask = Task.Run(() => {
		//	var stateName = SimulateChangeState();

		//}, cts.Token);

		//if (await Task.WhenAny(ReadAllTask, Task.Delay(timeout)) == ReadAllTask)
		//{
		//	//DateTime start = DateTime.UtcNow;
		//	//Thread.Sleep(2000);
		//	//DateTime end = DateTime.UtcNow;
		//	//TimeSpan timeDiff = end - start;
		//	//Console.WriteLine(Convert.ToInt32(timeDiff.TotalMilliseconds));
		//public string SimulateChangeState(int iCount)
		//{
		//	while (iCount < 5)
		//	{
		//		return ProcessingState.StateName;
		//	}

		//	if (iCount % 2 == 0)
		//		return SucceededState.StateName;
		//	else
		//		return FailedState.StateName;
		//}


	}
}
