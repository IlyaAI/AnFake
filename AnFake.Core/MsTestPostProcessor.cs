using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.XPath;
using AnFake.Core.Tests;
using AnFake.Core.Xml;

namespace AnFake.Core
{
	public class MsTestPostProcessor : ITestPostProcessor
	{
		public IEnumerable<TestResult> PostProcess(Stream stream)
		{
			var xdoc = new XPathDocument(stream);
			var navigator = xdoc.CreateNavigator();

			// ReSharper disable once AssignNullToNotNullAttribute
			var ns = new XmlNamespaceManager(navigator.NameTable);
			ns.AddNamespace("t", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010");

			var defs = navigator.Select("/t:TestRun/t:TestDefinitions/t:UnitTest", ns)
				.AsEnumerable().Select(x => new
				{
					Id = x.Attr("id"),
					Category = String.Join(",",
						x.Select("t:TestCategory/t:TestCategoryItem", ns)
							.AsEnumerable()
							.Select(y => y.Attr("TestCategory"))),
					Suite = x.Value("t:TestMethod/@className", ns)
				}).ToDictionary(x => x.Id);

			var tests = new List<TestResult>();
			var results = navigator.Select("/t:TestRun/t:Results/t:UnitTestResult", ns);

			foreach (var result in results.AsEnumerable())
			{
				var test = new TestResult(
					result.Attr("testName"),
					ParseTestStatus(result.Attr("outcome")),
					TimeSpan.Zero);

				switch (test.Status)
				{
					case TestStatus.Skipped:
						test.ErrorMessage = result.Value("t:Output/t:ErrorInfo/t:Message", ns);
						break;

					case TestStatus.Failed:
						test.ErrorMessage = result.Value("t:Output/t:ErrorInfo/t:Message", ns);
						test.ErrorDetails = result.Value("t:Output/t:ErrorInfo/t:StackTrace", ns);
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

		private static TestStatus ParseTestStatus(string outcome)
		{
			if ("passed".Equals(outcome, StringComparison.InvariantCultureIgnoreCase))
				return TestStatus.Passed;

			if ("inconclusive".Equals(outcome, StringComparison.InvariantCultureIgnoreCase) ||
				"ignored".Equals(outcome, StringComparison.InvariantCultureIgnoreCase))
				return TestStatus.Skipped;

			if ("failed".Equals(outcome, StringComparison.InvariantCultureIgnoreCase))
				return TestStatus.Failed;

			return TestStatus.Unknown;
		}
	}
}