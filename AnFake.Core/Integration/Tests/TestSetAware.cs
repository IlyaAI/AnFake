using System;

namespace AnFake.Core.Integration.Tests
{
	/// <summary>
	///		Represents extension point for plugins interesting in test-set processing.
	/// </summary>
	/// <seealso cref="TestResultAware"/>	
	public static class TestSetAware
	{
		/// <summary>
		///		Fired when test-set processed by ITestPostProcessor.
		/// </summary>
		public static event EventHandler<TestSet> Processed;

		/// <summary>
		///		Calls 'Processed' event handler for given test-set.
		/// </summary>
		/// <param name="testSet"></param>
		internal static void Notify(TestSet testSet)
		{
			if (Processed != null)
			{
				Processed.Invoke(null, testSet);
			}			
		}
	}
}