using System;
using System.Collections.Generic;
using AnFake.Api;

namespace AnFake.Core.Integration.Tests
{
	public class TestResult
	{
		private List<Hyperlink> _links;

		public TestResult(string name, TestStatus status, TimeSpan runTime)
		{
			Name = name;
			Status = status;
			RunTime = runTime;
		}

		public string Name { get; private set; }

		public TestStatus Status { get; private set; }

		public TimeSpan RunTime { get; private set; }

		public string Suite { get; set; }

		public string Category { get; set; }

		public string ErrorMessage { get; set; }

		public string ErrorStackTrace { get; set; }

		public string Output { get; set; }

		public List<Hyperlink> Links
		{
			get { return _links ?? (_links = new List<Hyperlink>()); }
		}
	}
}