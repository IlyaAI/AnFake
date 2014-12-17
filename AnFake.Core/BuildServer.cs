using AnFake.Core.Integration;

namespace AnFake.Core
{
	public static class BuildServer
	{
		public sealed class LocalBuildServer : IBuildServer
		{
			public LocalBuildServer()
			{
				DropLocation = ".out".AsPath();
				LogsLocation = DropLocation/"logs";
			}

			// ReSharper disable once MemberHidesStaticFromOuterClass
			public FileSystemPath DropLocation { get; set; }

			// ReSharper disable once MemberHidesStaticFromOuterClass
			public FileSystemPath LogsLocation { get; set; }
		}

		public static LocalBuildServer Local = new LocalBuildServer();

		private static IBuildServer _instance;
		private static IBuildServer Instance
		{
			get
			{
				if (_instance != null)
					return _instance;

				return (_instance = Plugin.Find<IBuildServer>() ?? Local);
			}
		}

		public static FileSystemPath DropLocation
		{
			get { return Instance.DropLocation; }
		}

		public static FileSystemPath LogsLocation
		{
			get { return Instance.LogsLocation; }
		}
	}
}