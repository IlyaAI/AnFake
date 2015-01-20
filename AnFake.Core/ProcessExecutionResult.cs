using System;
using AnFake.Core.Exceptions;

namespace AnFake.Core
{
	/// <summary>
	///		Represents external process execution result.
	/// </summary>
	public sealed class ProcessExecutionResult
	{
		internal ProcessExecutionResult(int exitCode, int errorsCount, int warningsCount, string lastOutput)
		{
			ExitCode = exitCode;
			ErrorsCount = errorsCount;
			WarningsCount = warningsCount;
			LastOutput = lastOutput;
		}

		/// <summary>
		///		Process exit code.
		/// </summary>
		public int ExitCode { get; private set; }

		/// <summary>
		///		Number of errors.
		/// </summary>
		/// <remarks>
		///		Errors might be tracked eigther via stderr (default) or via <c>ITracer</c>.
		/// </remarks>
		public int ErrorsCount { get; private set; }

		/// <summary>
		///		Number of warnings.
		/// </summary>
		/// <remarks>
		///		Warnings might be tracked via <c>ITracer</c> only.
		/// </remarks>
		public int WarningsCount { get; private set; }

		/// <summary>
		///		A several last lines from process output.
		/// </summary>
		/// <remarks>
		///		The amount of lines to be tracked is defined by <see cref="Process.Params.OutputBufferCapacity"/>.
		/// </remarks>
		public string LastOutput { get; private set; }

		/// <summary>
		///		Throws a <c>TargetFailureException</c> with specified message if predicate returns true.
		/// </summary>
		/// <param name="predicate">predicate to be checked</param>
		/// <param name="message">error message</param>
		/// <returns>this</returns>
		public ProcessExecutionResult FailIf(Predicate<ProcessExecutionResult> predicate, string message)			
		{
			if (predicate(this))
				throw new TargetFailureException(message);

			return this;
		}

		/// <summary>
		///		Throws a <c>TerminateTargetException</c> with specified message if ErrorsCount > 0.
		/// </summary>
		/// <param name="message">error message</param>
		/// <returns>this</returns>
		public ProcessExecutionResult FailIfAnyError(string message)			
		{
			if (ErrorsCount > 0)
				throw new TerminateTargetException(message);

			return this;
		}

		/// <summary>
		///		Throws a <c>TerminateTargetException</c> if ErrorsCount > 0.
		/// </summary>		
		/// <returns>this</returns>
		public ProcessExecutionResult FailIfAnyError()
		{
			if (ErrorsCount > 0)
				throw new TerminateTargetException();

			return this;
		}

		/// <summary>
		/// 	Throws a <c>TerminateTargetException</c> with specified message if ErrorsCount > 0 or WarningsCount > 0.
		/// </summary>
		/// <param name="message">error message</param>
		/// <returns>this</returns>
		public ProcessExecutionResult FailIfAnyErrorOrWarning(string message)			
		{
			if (WarningsCount > 0 || ErrorsCount > 0)
				throw new TerminateTargetException(message);

			return this;
		}

		/// <summary>
		/// 	Throws a <c>TerminateTargetException</c> if ErrorsCount > 0 or WarningsCount > 0.
		/// </summary>		
		/// <returns>this</returns>
		public ProcessExecutionResult FailIfAnyErrorOrWarning()
		{
			if (WarningsCount > 0 || ErrorsCount > 0)
				throw new TerminateTargetException();

			return this;
		}

		/// <summary>
		/// 	Throws a <c>TargetFailureException</c> with specified message if ExitCode != 0.
		/// </summary>
		/// <remarks>
		///		Exception to be thrown will include LastOutput as Details property.
		/// </remarks>
		/// <param name="message">error message</param>
		/// <returns>this</returns>
		public ProcessExecutionResult FailIfExitCodeNonZero(string message)
		{
			if (ExitCode != 0)
				throw new TargetFailureException(message, LastOutput);

			return this;
		}
	}
}