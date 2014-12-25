using System;

namespace AnFake.Core.Integration.Tests
{
	public static class TestResultAware
	{
		public static event EventHandler<TestResult> Received;
		public static event EventHandler<TestResult> Passed;
		public static event EventHandler<TestResult> Skipped;
		public static event EventHandler<TestResult> Failed;
		public static event EventHandler<TestResult> Unknown;

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