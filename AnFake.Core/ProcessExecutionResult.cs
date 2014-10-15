using AnFake.Api;

namespace AnFake.Core
{
	public sealed class ProcessExecutionResult : IToolExecutionResult
	{
		public ProcessExecutionResult(int exitCode, int errorsCount, int warningsCount)
		{
			ExitCode = exitCode;
			ErrorsCount = errorsCount;
			WarningsCount = warningsCount;
		}

		public int ExitCode { get; private set; }

		public int ErrorsCount { get; private set; }

		public int WarningsCount { get; private set; }
	}
}