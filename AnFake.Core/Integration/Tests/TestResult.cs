using System;
using System.Collections.Generic;
using AnFake.Api;

namespace AnFake.Core.Integration.Tests
{
	/// <summary>
	///		Represents single test execution result.
	/// </summary>
	public class TestResult
	{
		private List<Hyperlink> _links;

		/// <summary>
		///		Constructs new <c>TestResult</c> instance.
		/// </summary>
		/// <param name="name">test name (not null or empty)</param>
		/// <param name="status">test outcome status</param>
		/// <param name="runTime">test run time</param>
		public TestResult(string name, TestStatus status, TimeSpan runTime)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentException("TestResult(name, status, runTime): name must not be null or empty");

			Name = name;
			Status = status;
			RunTime = runTime;
		}

		/// <summary>
		///		Test name. Usually this is name of test method. Not null.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		///		Test outcome status.
		/// </summary>
		/// <seealso cref="TestStatus"/>
		public TestStatus Status { get; private set; }

		/// <summary>
		///		Test run time.
		/// </summary>
		public TimeSpan RunTime { get; private set; }

		/// <summary>
		///		Test suite name. Usually this is name of test class.
		/// </summary>
		public string Suite { get; set; }

		/// <summary>
		///		Test categories separated by coma.
		/// </summary>
		public string Category { get; set; }

		/// <summary>
		///		Error message if test failed or skipped.
		/// </summary>
		public string ErrorMessage { get; set; }

		/// <summary>
		///		Error stack trace if test failed.
		/// </summary>
		public string ErrorStackTrace { get; set; }

		/// <summary>
		///		Fragment of test output if test failed.
		/// </summary>
		/// <remarks>
		///		<para>Output might be truncated (depends on ITestPostProcessor implementation).</para>
		///		<para>Output might include stdout, stderr and Diagnostics.Trace messages.</para>
		/// </remarks>
		public string Output { get; set; }

		/// <summary>
		///		Related hyperlinks.
		/// </summary>
		/// <remarks>
		///		Links might contains reference to test-run trace file and any other files produced during test-run.
		/// </remarks>
		public List<Hyperlink> Links
		{
			get { return _links ?? (_links = new List<Hyperlink>()); }
		}
	}
}