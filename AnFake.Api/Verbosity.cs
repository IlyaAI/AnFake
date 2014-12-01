namespace AnFake.Api
{
	/// <summary>
	///     Represents build trace verbosity. <see cref="Trace" />
	/// </summary>
	public enum Verbosity
	{
		/// <summary>
		///     Error and warning messages only visible.
		/// </summary>
		Quiet,

		/// <summary>
		///     Errors, warnings and summary messages visible.
		/// </summary>
		Minimal,

		/// <summary>
		///     Errors, warnings, summary and info messages visible.
		/// </summary>
		Normal,

		/// <summary>
		///     All messages visible.
		/// </summary>
		Detailed,

		/// <summary>
		///     All messages visible.
		/// </summary>
		/// <remarks>
		///     The same as Detailed but provided for compatibility with Microsoft.Build.Framework.LoggerVerbosity
		/// </remarks>
		Diagnostic
	}
}