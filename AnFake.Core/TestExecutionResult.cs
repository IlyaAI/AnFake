using System.Collections.Generic;
using AnFake.Api;
using AnFake.Core.Tests;

namespace AnFake.Core
{
	public sealed class TestExecutionResult : IToolExecutionResult
	{
		private readonly TestResult[] _tests;
		private readonly int _errorsCount;
		private readonly int _warningsCount;

		public TestExecutionResult(int errorsCount, int warningsCount, TestResult[] tests)
		{
			_errorsCount = errorsCount;
			_warningsCount = warningsCount;
			_tests = tests;
		}

		public int ErrorsCount
		{
			get { return _errorsCount; }
		}

		public int WarningsCount
		{
			get { return _warningsCount; }
		}

		public IEnumerable<TestResult> Tests
		{
			get { return _tests; }
		}
	}
}