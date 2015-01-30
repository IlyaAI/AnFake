namespace AnFake.Core.Integration.Tests
{
	/// <summary>
	///     Represents extension point for test-run trace parsers.
	/// </summary>
	public interface ITestPostProcessor
	{
		/// <summary>
		///     Processes given stream and returns <c>TestResult</c>-s.
		/// </summary>
		/// <param name="setName">test-set name</param>
		/// <param name="traceFile">trace file to be processed</param>
		/// <returns>set of test results</returns>
		TestSet PostProcess(string setName, FileItem traceFile);
	}
}