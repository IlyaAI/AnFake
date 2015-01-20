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
		///		Drop folder location.
		/// </summary>
		FileSystemPath DropLocation { get; }

		/// <summary>
		///		Logs folder location.
		/// </summary>
		FileSystemPath LogsLocation { get; }
	}
}