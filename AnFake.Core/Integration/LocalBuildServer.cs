namespace AnFake.Core.Integration
{
	internal sealed class LocalBuildServer : IBuildServer
	{
		public LocalBuildServer()
		{
			DropLocation = ".out".AsPath();
			LogsLocation = DropLocation/"logs";
		}

		public bool IsLocal
		{
			get { return true; }
		}

		public bool HasDropLocation
		{
			get { return true; }
		}

		public FileSystemPath DropLocation { get; set; }

		public bool HasLogsLocation
		{
			get { return true; }
		}

		public FileSystemPath LogsLocation { get; set; }
	}
}