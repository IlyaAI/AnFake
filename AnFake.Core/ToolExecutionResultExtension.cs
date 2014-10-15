using System;
using AnFake.Api;

namespace AnFake.Core
{
	public static class ToolExecutionResultExtension
	{
		public static IToolExecutionResult FailIf(this IToolExecutionResult result, Predicate<IToolExecutionResult> predicate, string message)
		{
			if (predicate(result))
				throw new TargetFailureException(message);

			return result;
		}

		public static IToolExecutionResult FailIfAnyError(this IToolExecutionResult result, string message)
		{
			if (result.ErrorsCount > 0)
				throw new TerminateTargetException(message);

			return result;
		}

		public static IToolExecutionResult FailIfAnyErrorOrWarning(this IToolExecutionResult result, string message)
		{
			if (result.WarningsCount > 0 || result.ErrorsCount > 0)
				throw new TerminateTargetException(message);

			return result;
		}

		public static ProcessExecutionResult FailIf(this ProcessExecutionResult result, Predicate<ProcessExecutionResult> predicate, string message)
		{
			if (predicate(result))
				throw new TargetFailureException(message);

			return result;
		}

		public static ProcessExecutionResult FailIfExitCodeNonZero(this ProcessExecutionResult result, string message)
		{
			if (result.ExitCode > 0)
				throw new TargetFailureException(message);

			return result;
		}
	}
}