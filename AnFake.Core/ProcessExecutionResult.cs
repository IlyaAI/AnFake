using System;
using AnFake.Core.Exceptions;

namespace AnFake.Core
{
	public sealed class ProcessExecutionResult
	{
		public ProcessExecutionResult(int exitCode, int errorsCount, int warningsCount, string lastOutput)
		{
			ExitCode = exitCode;
			ErrorsCount = errorsCount;
			WarningsCount = warningsCount;
			LastOutput = lastOutput;
		}

		public int ExitCode { get; private set; }

		public int ErrorsCount { get; private set; }

		public int WarningsCount { get; private set; }

		public string LastOutput { get; private set; }

		public ProcessExecutionResult FailIf(Predicate<ProcessExecutionResult> predicate, string message)			
		{
			if (predicate(this))
				throw new TargetFailureException(message);

			return this;
		}

		public ProcessExecutionResult FailIfAnyError(string message)			
		{
			if (ErrorsCount > 0)
				throw new TerminateTargetException(message);

			return this;
		}

		public ProcessExecutionResult FailIfAnyErrorOrWarning(string message)			
		{
			if (WarningsCount > 0 || ErrorsCount > 0)
				throw new TerminateTargetException(message);

			return this;
		}

		public ProcessExecutionResult FailIfExitCodeNonZero(string message)
		{
			if (ExitCode > 0)
				throw new TargetFailureException(message, LastOutput);

			return this;
		}
	}
}