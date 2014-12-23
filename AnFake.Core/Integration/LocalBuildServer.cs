namespace AnFake.Core.Integration
{
	internal sealed class LocalBuildServer : IBuildServer
	{
		public LocalBuildServer()
		{
			DropLocation = ".out".AsPath();
			LogsLocation = DropLocation/"logs";
		}

		public FileSystemPath DropLocation { get; set; }

		public FileSystemPath LogsLocation { get; set; }
	}
}