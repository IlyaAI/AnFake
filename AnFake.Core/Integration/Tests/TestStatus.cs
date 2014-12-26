namespace AnFake.Core.Integration.Tests
{
	/// <summary>
	///		Represents test outcome status.
	/// </summary>
	public enum TestStatus
	{
		/// <summary>
		///		Status wasn't parsed by <c>ITestPostProcessor</c>.
		/// </summary>
		Unknown,

		/// <summary>
		///		Test successfuly passed.
		/// </summary>
		Passed,

		/// <summary>
		///		Test skipped/ignored.
		/// </summary>
		Skipped,		

		/// <summary>
		///		Test failed.
		/// </summary>
		Failed
	}
}