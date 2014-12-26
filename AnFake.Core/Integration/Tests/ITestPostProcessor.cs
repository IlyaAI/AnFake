using System.Collections.Generic;
using System.IO;

namespace AnFake.Core.Integration.Tests
{
	/// <summary>
	///		Represents extension point for test-run trace parsers.
	/// </summary>
	public interface ITestPostProcessor
	{
		/// <summary>
		///		Processes given stream and returns <c>TestResult</c>-s.
		/// </summary>
		/// <param name="stream">stream to be processed</param>
		/// <returns>set of test results</returns>
		IEnumerable<TestResult> PostProcess(Stream stream);
	}
}