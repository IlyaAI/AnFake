using System;

namespace AnFake.Core.Tests
{
	public sealed class TestResult
	{
		public string Name { get; private set; }
		
		public TestStatus Status { get; private set; }
		
		public TimeSpan RunTime { get; private set; }
		
		public string Details { get; set; }

		public TestResult(string name, TestStatus status, TimeSpan runTime)
		{
			Name = name;
			Status = status;
			RunTime = runTime;
		}

		public TestResult(string name, TestStatus status, TimeSpan runTime, string details)
		{
			Name = name;
			Status = status;
			RunTime = runTime;
			Details = details;
		}
	}
}