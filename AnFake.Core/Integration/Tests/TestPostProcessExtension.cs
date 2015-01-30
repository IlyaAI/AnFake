using System;
using System.Collections.Generic;
using System.Text;
using AnFake.Api;

namespace AnFake.Core.Integration.Tests
{
	/// <summary>
	///		Provides set of extension methods for <c>TestResult</c> sequences.
	/// </summary>
	public static class TestPostProcessExtension
	{
		/// <summary>
		///		Generates and writes appropriate <c>TraceMessage</c> for each given <c>TestResult</c>.
		/// </summary>
		/// <param name="testSet">set of tests to be traced</param>		
		/// <returns>the same sequence which was passed as 'tests' argument</returns>
		public static IEnumerable<TestResult> Trace(this TestSet testSet)
		{
			const int ident = 2;

			var trxUri = (Uri) null;
			if (BuildServer.CanExposeArtifacts)
			{
				trxUri = BuildServer.ExposeArtifact(testSet.TraceFile, ArtifactType.TestResults);
				
				if (testSet.AttachmentsFolder != null && testSet.AttachmentsFolder.Exists())
				{
					BuildServer.ExposeArtifact(testSet.AttachmentsFolder, ArtifactType.TestResults);
				}
			}						

			var msg = (TraceMessage) null;
			var total = 0;
			var passed = 0;
			var skipped = 0;
			var failed = 0;

			foreach (var test in testSet.Tests)
			{
				TestResultAware.Notify(test);

				var report = new StringBuilder();
				report.AppendFormat("{0} {1,-8} {2,-80} @ {3}",
					test.RunTime, test.Status.ToString().ToUpperInvariant(), test.Name, test.Suite);
				
				switch (test.Status)
				{
					case TestStatus.Passed:
						Api.Trace.Info(report.ToString());
						passed++;
						break;

					case TestStatus.Unknown:
					case TestStatus.Skipped:
						report
							.AppendLine().Append(' ', ident)
							.Append(test.ErrorMessage);

						Api.Trace.Warn(report.ToString());
						skipped++;
						break;

					case TestStatus.Failed:
						report
							.AppendLine().Append(' ', ident)
							.Append(test.ErrorMessage);

						msg = new TraceMessage(TraceMessageLevel.Error, report.ToString())
						{
							Details = test.ErrorStackTrace							
						};
						msg.Links.AddRange(test.Links);

						if (trxUri != null)
						{
							msg.Links.Add(new Hyperlink(trxUri, "Trace"));
						}
						
						Api.Trace.Message(msg);
						failed++;
						break;
				}

				total++;				
				
				yield return test;
			}

			var summary = new StringBuilder(128)
				.AppendFormat("{0}: {1} total / {2} passed", testSet.Name, total, passed);

			if (skipped > 0)
			{
				summary.AppendFormat(" / {0} skipped", skipped);
			}

			if (failed > 0)
			{
				summary.AppendFormat(" / {0} FAILED", failed);
			}

			msg = new TraceMessage(TraceMessageLevel.Summary, summary.ToString());
			if (trxUri != null)
			{
				msg.Links.Add(new Hyperlink(trxUri, "Trace"));
			}

			Api.Trace.Message(msg);
		}

		/// <summary>
		///		Generates and writes test-run summary <c>TraceMessage</c>.
		/// </summary>
		/// <param name="tests">set of tests to be summarized</param>
		public static void TraceSummary(this IEnumerable<TestResult> tests)
		{
			var runTime = TimeSpan.Zero;
			var passed = 0;
			var errors = 0;
			var warnings = 0;
			var total = 0;
			
			foreach (var test in tests)
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
				total++;
			}

			var summary = new StringBuilder(128)
				.AppendFormat("TEST-RUN SUMMARY: {0}, {1} total / {2} passed", runTime, total, passed);

			if (warnings > 0)
			{
				summary.AppendFormat(" / {0} skipped", warnings);
			}

			if (errors > 0)
			{
				summary.AppendFormat(" / {0} FAILED", errors);
			}			

			Api.Trace.SummaryFormat(summary.ToString());
		}
	}
}