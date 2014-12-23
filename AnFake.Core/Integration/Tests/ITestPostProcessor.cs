using System.Collections.Generic;
using System.IO;

namespace AnFake.Core.Integration.Tests
{
	public interface ITestPostProcessor
	{
		IEnumerable<TestResult> PostProcess(Stream stream);
	}
}