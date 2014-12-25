using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AnFake.Core.Integration.Tests
{
	public class MsTrxPostProcessor : IMsTrxPostProcessor
	{
		public IEnumerable<TestResult> PostProcess(Stream stream)
		{
			var xdoc = stream.AsXmlDoc();
			xdoc.Ns("t", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010");

			var defs = xdoc.Select("/t:TestRun/t:TestDefinitions/t:UnitTest")
				.Select(x => new
				{
					Id = x.Attr("id"),
					Category = String.Join(",",
						x.Select("t:TestCategory/t:TestCategoryItem")							
							.Select(y => y.Attr("TestCategory"))),
					Suite = x.ValueOf("t:TestMethod/@className")
				}).ToDictionary(x => x.Id);

			var tests = new List<TestResult>();
			var results = xdoc.Select("/t:TestRun/t:Results/t:UnitTestResult");

			foreach (var result in results)
			{
				var test = new TestResult(
					result.Attr("testName"),
					ParseTestStatus(result.Attr("outcome")),
					TimeSpan.Zero);

				switch (test.Status)
				{
					case TestStatus.Skipped:
						ExtractErrorMessage(result, test);
						break;

					case TestStatus.Failed:
						ExtractErrorMessage(result, test);
						ExtractErrorDetails(result, test);
						break;
				}

				var id = result.Attr("testId");
				if (defs.ContainsKey(id))
				{
					var def = defs[id];

					test.Suite = def.Suite.Split(',').First();
					test.Category = def.Category;
				}
				
				tests.Add(test);				
			}

			return tests;
		}

		private static void ExtractErrorMessage(Xml.XNode result, TestResult test)
		{
			test.ErrorMessage = result.ValueOf("t:Output/t:ErrorInfo/t:Message", "UNKNOWN REASON (there is no 'ErrorInfo/Message' node in .trx)");
		}

		private static void ExtractErrorDetails(Xml.XNode result, TestResult test)
		{
			test.ErrorStackTrace = result.ValueOf("t:Output/t:ErrorInfo/t:StackTrace");

			var stdErr = result.ValueOf("t:Output/t:StdErr");
			var stdOut = result.ValueOf("t:Output/t:StdOut");
			var trace = result.ValueOf("t:Output/t:Trace");

			var output = new StringBuilder(stdErr.Length + stdOut.Length + trace.Length + 256);
			if (stdErr.Length > 0)
			{
				output
					.AppendLine("======== Standard Error ========")
					.AppendLine(stdErr);
			}
			if (stdOut.Length > 0)
			{
				output
					.AppendLine("======== Standard Output ========")
					.AppendLine(stdOut);
			}
			if (trace.Length > 0)
			{
				output
					.AppendLine("======== Diagnostics Trace ========")
					.AppendLine(trace);
			}

			test.Output = output.ToString();
		}

		private static TestStatus ParseTestStatus(string outcome)
		{
			if ("passed".Equals(outcome, StringComparison.OrdinalIgnoreCase))
				return TestStatus.Passed;

			if ("inconclusive".Equals(outcome, StringComparison.OrdinalIgnoreCase) ||
				"ignored".Equals(outcome, StringComparison.OrdinalIgnoreCase))
				return TestStatus.Skipped;

			if ("failed".Equals(outcome, StringComparison.OrdinalIgnoreCase))
				return TestStatus.Failed;

			return TestStatus.Unknown;
		}
	}
}