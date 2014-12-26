using System;

namespace AnFake.Core.Integration.Tests
{
	/// <summary>
	///		Represents extension point for plugins interesting in test results.
	/// </summary>
	/// <remarks>
	///		Test results are retrivied by post-processing of test run trace file (e.g. '.trx' for MsTest/VsTest). 
	///		Thus if no implementation of ITestPostProcessor registered for some particular test runner then results will be anavailable.
	/// </remarks>
	public static class TestResultAware
	{
		/// <summary>
		///		Fired when any test result received.
		/// </summary>
		public static event EventHandler<TestResult> Received;

		/// <summary>
		///		Fired when successful test result received.
		/// </summary>
		public static event EventHandler<TestResult> Passed;

		/// <summary>
		///		Fired when skipped test result received.
		/// </summary>
		public static event EventHandler<TestResult> Skipped;

		/// <summary>
		///		Fired when failed test result received.
		/// </summary>
		public static event EventHandler<TestResult> Failed;

		/// <summary>
		///		Fired when unknown test result received.
		/// </summary>
		public static event EventHandler<TestResult> Unknown;

		/// <summary>
		///		Calls appropriate event handlers for given test result.
		/// </summary>
		/// <param name="test"></param>
		internal static void Notify(TestResult test)
		{
			if (Received != null)
			{
				Received.Invoke(null, test);
			}

			switch (test.Status)
			{
				case TestStatus.Unknown:
					if (Unknown != null)
					{
						Unknown.Invoke(null, test);
					}
					break;

				case TestStatus.Passed:
					if (Passed != null)
					{
						Passed.Invoke(null, test);
					}
					break;

				case TestStatus.Skipped:
					if (Skipped != null)
					{
						Skipped.Invoke(null, test);
					}
					break;

				case TestStatus.Failed:
					if (Failed != null)
					{
						Failed.Invoke(null, test);
					}
					break;
			}
		}
	}
}