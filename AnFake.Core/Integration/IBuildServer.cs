namespace AnFake.Core.Integration
{
	/// <summary>
	///		Represents extension point for build server integration.
	/// </summary>
	public interface IBuildServer
	{
		/// <summary>
		///		Is this local build?
		/// </summary>
		bool IsLocal { get; }

		/// <summary>
		///		Whether this build has drop location?
		/// </summary>
		bool HasDropLocation { get; }

		/// <summary>
		///		Drop folder location. Throws if not specified.
		/// </summary>
		FileSystemPath DropLocation { get; }

		/// <summary>
		///		Whether this build has logs folder?
		/// </summary>
		bool HasLogsLocation { get; }

		/// <summary>
		///		Logs folder location. Throws if not specified.
		/// </summary>
		FileSystemPath LogsLocation { get; }
	}
}