using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AnFake.Api;

namespace AnFake.Core.Tests
{
	public static class TestPostProcessExtension
	{
		public static IEnumerable<TestResult> PostProcess(this ITestPostProcessor postProcessor, FileSystemPath resultPath)
		{
			using (var stream = new FileStream(resultPath.Full, FileMode.Open, FileAccess.Read))
			{
				return postProcessor.PostProcess(stream);
			}
		}

		public static IEnumerable<TestResult> Trace(this IEnumerable<TestResult> tests)
		{
			const int ident = 2;

			foreach (var test in tests)
			{
				var report = new StringBuilder();
				report.AppendFormat("{0} {1,-8} {2,-80} @ {3}",
					test.RunTime, test.Status.ToString().ToUpperInvariant(), test.Name, test.Suite);
				
				switch (test.Status)
				{
					case TestStatus.Passed:
						Api.Trace.Info(report.ToString());
						break;

					case TestStatus.Unknown:
					case TestStatus.Skipped:
						report
							.AppendLine().Append(' ', ident)
							.Append(test.ErrorMessage);

						Api.Trace.Warn(report.ToString());
						break;

					case TestStatus.Failed:
						report
							.AppendLine().Append(' ', ident)
							.Append(test.ErrorMessage);

						Api.Trace.Message(
							new TraceMessage(TraceMessageLevel.Error, report.ToString())
							{
								Details = test.ErrorDetails
							});						
						break;
				}

				yield return test;
			}
		}

		public static TestExecutionResult TraceSummary(this IEnumerable<TestResult> tests)
		{
			var runTime = TimeSpan.Zero;
			var passed = 0;
			var errors = 0;
			var warnings = 0;

			var array = tests.ToArray();
			foreach (var test in array)
			{
				switch (test.Status)
				{
					case TestStatus.Passed:
						passed++;
						break;
					case TestStatus.Unknown:
					case TestStatus.Skipped:
						warnings++;							
						break;
					case TestStatus.Failed:
						errors++;							
						break;
				}
				
				runTime += test.RunTime;
			}

			Api.Trace.SummaryFormat("Test Run Summary: {0}, {1} failed, {2} skipped, {3} passed, {4} total.",
					runTime, errors, warnings, passed, array.Length);
			
			return new TestExecutionResult(errors, warnings, array);
		}
	}
}