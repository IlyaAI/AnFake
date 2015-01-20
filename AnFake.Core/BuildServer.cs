using System;
using AnFake.Core.Integration;

namespace AnFake.Core
{
	/// <summary>
	///		Represents access point to build server integration.
	/// </summary>
	public static class BuildServer
	{
		private readonly static Lazy<IBuildServer> Instance 
			= new Lazy<IBuildServer>(Plugin.Get<IBuildServer>);

		private static IBuildServer _local;

		/// <summary>
		///		Instance of local build server.
		/// </summary>
		/// <remarks>
		///		The local instance is always available even build is ran on server. 
		///		This instance might be used for various fallback cases.
		/// </remarks>
		public static IBuildServer Local
		{
			get { return _local ?? (_local = new LocalBuildServer()); }
		}

		/// <summary>
		///		Is this build local?
		/// </summary>
		/// <remarks>
		///		Returns true is current build is ran locally (i.e. not under build server) and false otherwise.
		/// </remarks>
		public static bool IsLocal
		{
			get { return Instance.Value.IsLocal; }
		}
		
		/// <summary>
		///		Drop folder location.
		/// </summary>
		/// <remarks>
		///		Drop folder is used for "publishing" build's final artifacts. Default for local builds is ".out".
		/// </remarks>
		public static FileSystemPath DropLocation
		{
			get { return Instance.Value.DropLocation; }
		}

		/// <summary>
		///		Build and test logs location.
		/// </summary>
		/// <remarks>
		///		Logs folder is used for "publishing" build and test logs. Default for local builds is ".out/logs".
		/// </remarks>
		public static FileSystemPath LogsLocation
		{
			get { return Instance.Value.LogsLocation; }
		}
	}
}