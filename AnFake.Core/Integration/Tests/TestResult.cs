using System;

namespace AnFake.Core.Integration.Tests
{
	public class TestResult
	{
		public string Name { get; private set; }		
		
		public TestStatus Status { get; private set; }

		public TimeSpan RunTime { get; private set; }

		public string Suite { get; set; }

		public string Category { get; set; }

		public string ErrorMessage { get; set; }

		public string ErrorDetails { get; set; }

		public TestResult(string name, TestStatus status, TimeSpan runTime)
		{
			Name = name;
			Status = status;
			RunTime = runTime;
		}		
	}
}