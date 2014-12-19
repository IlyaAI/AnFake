namespace AnFake.Api
{
	/// <summary>
	///     Represents level of message written to build log.
	/// </summary>
	public enum LogMessageLevel
	{
		/// <summary>
		///     Denotes diagnostic details of current operation. E.g. 'Overwrite enabled => deleting target folder x'.
		/// </summary>
		/// <remarks>
		///     Debug messages are visible with verbosity Detailed or higher.
		///     They have GRAY color on console and [DEBUG] tag in log file.
		/// </remarks>
		Debug,

		/// <summary>
		///     Denotes information about current progress. E.g. 'Starting process x', 'Copying files x => y', etc.
		/// </summary>
		/// <remarks>
		///     Info messages are visible with verbosity Normal or higher.
		///     They have WHITE color on console and [INFO] tag in log file.
		/// </remarks>
		Info,

		/// <summary>
		///     Denotes summary of important operation. E.g. 'x failed / y total tests'.
		/// </summary>
		/// <remarks>
		///     Summary messages are visible with verbosity Minimal or higher.
		///     They have WHITE color on console and [INFO] tag in log file.
		/// </remarks>
		Summary,

		/// <summary>
		///     Denotes warning about operation's result. E.g. 'Obsolete API is used'.
		/// </summary>
		/// <remarks>
		///     Warning messages are visible with any verbosity.
		///     They have YELLOW color on console and [WARN] tag in log file.
		/// </remarks>
		Warning,

		/// <summary>
		///     Denotes failure of operation. E.g. 'Syntax error in ...'
		/// </summary>
		/// <remarks>
		///     Error messages are visible with any verbosity.
		///     They have RED color on console and [ERROR] tag in log file.
		/// </remarks>
		Error,

		/// <summary>
		///     Denotes success of operation or phase. E.g. 'Target Compile successfull'
		/// </summary>
		/// <remarks>
		///     <para>
		///         Success messages are visible with any verbosity.
		///         They have GREEN color on console and [SUCCESS] tag in log file.
		///     </para>
		///     <para>
		///         Normally success messages are used in global build summary only.
		///     </para>
		/// </remarks>
		Success,

		/// <summary>
		///     Denotes generic text message.
		/// </summary>
		/// <remarks>
		///     <para>
		///         Text messages are visible with any verbosity.
		///         They have WHITE color on console and [INFO] tag in log file.
		///     </para>
		///     <para>
		///         Normally text messages are used in global build summary only.
		///     </para>
		/// </remarks>
		Text,

		/// <summary>
		///     Denotes text message with details.
		/// </summary>
		/// <remarks>
		///     <para>
		///         Details messages are visible with any verbosity.
		///         They have GRAY color on console and [INFO] tag in log file.
		///     </para>
		///     <para>
		///         Normally details messages are used in global build summary only.
		///     </para>
		/// </remarks>
		Details
	}
}