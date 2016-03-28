using System;
using AnFake.Core;

namespace AnFake.Plugins.TeamCity
{
	/// <summary>
	///     Represents tools related to Team City.
	/// </summary>
	public static class TeamCity
	{
		/// <summary>
		///		Turns off import of whole test trace file into TeamCity. Instead each test will be reported separately by AnFake.
		///		This mode might be used if you would like to use VsTest.Params.TestSetPrefix for example.
		/// </summary>		
		public static bool DoNotImportTestTrace = false;
		
		/// <summary>
		///		Specifies delay after emitting TeamCity service message 'importData'.
		///		TeamCity process data and writes messages to build log in parallel with build script which might clog the output.
		///		This delay takes TeamCity a bit time to do it work.
		/// </summary>
		public static TimeSpan WaitAfterTestTraceImport = TimeSpan.FromSeconds(1.5);

		private static TeamCityPlugin _impl;
		private static TeamCityPlugin Impl
		{
			get
			{
				if (_impl != null)
					return _impl;

				_impl = Plugin.Get<TeamCityPlugin>();
				_impl.Disposed += () => _impl = null;

				return _impl;
			}
		}

		/// <summary>
		///     Activates <c>TeamCity</c> plugin.
		/// </summary>
		public static void PlugIn()
		{			
			Plugin.Register<TeamCityPlugin>()
				.As<Core.Integration.IBuildServer>()
				.As<Core.Integration.Builds.IBuildServer2>()
				.AsSelf();
		}

		/// <summary>
		///     Activates <c>TeamCity</c> plugin on-demand.
		/// </summary>
		public static void PlugInOnDemand()
		{
			Plugin.RegisterOnDemand<TeamCityPlugin>()
				.As<Core.Integration.IBuildServer>()
				.As<Core.Integration.Builds.IBuildServer2>()
				.AsSelf();
		}

		/// <summary>
		///		Writes TeamCity service message to open new block in build log.
		/// </summary>
		/// <param name="name"></param>
		public static void LogOpenBlock(string name)
		{
			Impl.WriteBlockOpened(name);
		}

		/// <summary>
		///		Writes TeamCity service message to close block in build log.
		/// </summary>
		/// <param name="name"></param>
		public static void LogCloseBlock(string name)
		{
			Impl.WriteBlockClosed(name);
		}
	}
}