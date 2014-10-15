using System.Collections.Generic;

namespace AnFake.Core.Tests
{
	public sealed class TestSuiteResult
	{
		private readonly IList<TestResult> _tests = new List<TestResult>();
		private readonly string _name;
		private readonly string _resultsPath;

		public TestSuiteResult(string name)
		{
			_name = name;
		}

		public string Name
		{
			get { return _name; }
		}

		public IList<TestResult> Tests
		{
			get { return _tests; }
		}

		public string ResultsPath
		{
			get { return _resultsPath; }
		}
	}
}