namespace AnFake.Api
{
	/// <summary>
	///		Verbosity extension methods.
	/// </summary>
	public static class VerbosityExtension
	{
		/// <summary>
		///		Converts <c>Verbosity</c> to threshold in term of <c>LogMessageLevel</c>.
		/// </summary>
		/// <param name="verbosity">log verbosity</param>
		/// <returns>log message level threshold</returns>
		public static LogMessageLevel AsLogLevelThreshold(this Verbosity verbosity)
		{
			switch (verbosity)
			{
				case Verbosity.Quiet:
					return LogMessageLevel.Warning;
					
				case Verbosity.Minimal:
					return LogMessageLevel.Summary;
					
				case Verbosity.Detailed:
				case Verbosity.Diagnostic:
					return LogMessageLevel.Debug;
					
				default:
					return LogMessageLevel.Info;					
			}
		}

		/// <summary>
		///		Converts <c>Verbosity</c> to threshold in term of <c>TraceMessageLevel</c>.
		/// </summary>
		/// <param name="verbosity">log verbosity</param>
		/// <returns>trace message level threshold</returns>
		public static TraceMessageLevel AsTraceLevelThreshold(this Verbosity verbosity)
		{
			switch (verbosity)
			{
				case Verbosity.Quiet:
					return TraceMessageLevel.Warning;

				case Verbosity.Minimal:
					return TraceMessageLevel.Summary;

				case Verbosity.Detailed:
				case Verbosity.Diagnostic:
					return TraceMessageLevel.Debug;

				default:
					return TraceMessageLevel.Info;
			}
		}
	}
}