using System.Runtime.Serialization;

namespace AnFake.Api
{
	/// <summary>
	///     Represents level of message written to build trace.
	/// </summary>
	[DataContract]
	public enum TraceMessageLevel
	{
		/// <summary>
		///     Denotes diagnostic details of current operation. E.g. 'Overwrite enabled => deleting target folder x'.
		/// </summary>
		/// <remarks>
		///     Debug messages are visible with verbosity Detailed or higher.
		///     They have GRAY color on console and [TRACE] tag in log file.
		/// </remarks>
		[EnumMember] Debug,

		/// <summary>
		///     Denotes information about current progress. E.g. 'Starting process x', 'Copying files x => y', etc.
		/// </summary>
		/// <remarks>
		///     Info meessages are visible with verbosity Normal or higher.
		///     They have WHITE color on console and [DEBUG] tag in log file.
		/// </remarks>
		[EnumMember] Info,

		/// <summary>
		///     Denotes summary of important operation. E.g. 'x failed / y total tests'.
		/// </summary>
		/// <remarks>
		///     <para>
		///         Summary meessages are visible with verbosity Minimal or higher.
		///         They have WHITE color on console and [DEBUG] tag in log file.
		///     </para>
		///     <para>
		///         The difference between Info and Summary is summary messages are duplicated in global build summary at the end
		///         of run.
		///     </para>
		/// </remarks>
		[EnumMember] Summary,

		/// <summary>
		///     Denotes warning about operation's result. E.g. 'Obsolete API is used'.
		/// </summary>
		/// <remarks>
		///     <para>
		///         Warning meessages are visible with any verbosity.
		///         They have YELLOW color on console and [WARN] tag in log file.
		///     </para>
		///     <para>
		///         Warning messages are duplicated in global build summary at the end of run.
		///     </para>
		/// </remarks>
		[EnumMember] Warning,

		/// <summary>
		///     Denotes failure of operation. E.g. 'Syntax error in ...'
		/// </summary>
		/// <remarks>
		///     <para>
		///         Error meessages are visible with any verbosity.
		///         They have RED color on console and [ERROR] tag in log file.
		///     </para>
		///     <para>
		///         Error messages are duplicated in global build summary at the end of run.
		///     </para>
		/// </remarks>
		[EnumMember] Error
	}
}