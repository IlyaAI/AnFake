using System;
using AnFake.Core.Integration;

namespace AnFake.Core
{
	public static class BuildServer
	{
		private readonly static Lazy<IBuildServer> Instance 
			= new Lazy<IBuildServer>(Plugin.Get<IBuildServer>);

		public static bool IsLocal
		{
			get { return Instance.Value is LocalBuildServer; }
		}
		
		public static FileSystemPath DropLocation
		{
			get { return Instance.Value.DropLocation; }
		}

		public static FileSystemPath LogsLocation
		{
			get { return Instance.Value.LogsLocation; }
		}
	}
}