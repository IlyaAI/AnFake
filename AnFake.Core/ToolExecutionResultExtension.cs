using System;
using AnFake.Api;

namespace AnFake.Core
{
	public static class ToolExecutionResultExtension
	{
		public static T FailIf<T>(this T result, Predicate<T> predicate, string message)
			where T : IToolExecutionResult
		{
			if (predicate(result))
				throw new TargetFailureException(message);

			return result;
		}

		public static T FailIfAnyError<T>(this T result, string message)
			where T : IToolExecutionResult
		{
			if (result.ErrorsCount > 0)
				throw new TerminateTargetException(message);

			return result;
		}

		public static T FailIfAnyErrorOrWarning<T>(this T result, string message)
			where T : IToolExecutionResult
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