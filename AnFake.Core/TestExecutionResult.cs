using System.Collections.Generic;
using System.Collections.ObjectModel;
using AnFake.Api;
using AnFake.Core.Tests;

namespace AnFake.Core
{
	public sealed class TestExecutionResult : IToolExecutionResult
	{
		private readonly IList<TestSuiteResult> _suites;
		private readonly int _errorsCount;

		public TestExecutionResult(int errorsCount, IList<TestSuiteResult> suites)
		{
			_errorsCount = errorsCount;
			_suites = suites;
		}

		public int ErrorsCount
		{
			get { return _errorsCount; }
		}

		public int WarningsCount
		{
			get { return 0; }
		}

		public IList<TestSuiteResult> Suites
		{
			get { return new ReadOnlyCollection<TestSuiteResult>(_suites); }
		}
	}
}