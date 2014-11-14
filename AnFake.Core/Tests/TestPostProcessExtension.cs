using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
			foreach (var test in tests)
			{
				var report = String.Format("{0} {1,-8} {2,-80} @ {3}",
					test.RunTime, test.Status.ToString().ToUpperInvariant(), test.Name, test.Suite);
				
				switch (test.Status)
				{
					case TestStatus.Passed:
						Logger.Info(report);
						break;
					case TestStatus.Unknown:
					case TestStatus.Skipped:
						Logger.Warn(report);
						Logger.DebugFormat("  {0}", test.ErrorMessage);
						Tracer.Write(new TraceMessage(TraceMessageLevel.Warning, String.Format("{0}: {1}", test.Name, test.ErrorMessage)));
						break;
					case TestStatus.Failed:
						Logger.Error(report);
						Logger.DebugFormat("  {0}\n  {1}", test.ErrorMessage, test.ErrorDetails);
						Tracer.Write(new TraceMessage(TraceMessageLevel.Error, String.Format("{0}: {1}", test.Name, test.ErrorMessage)) { Details = test.ErrorDetails });
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

			var summary = String.Format("Test Run Summary: {0} {1} failed, {2} skipped, {3} passed, {4} total.",
					runTime, errors, warnings, passed, array.Length);

			Logger.Debug(summary);
			Tracer.Write(new TraceMessage(TraceMessageLevel.Info, summary));

			return new TestExecutionResult(errors, warnings, array);
		}
	}
}