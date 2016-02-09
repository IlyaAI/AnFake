using System;
using AnFake.Core;

namespace AnFake.Plugins.TeamCity
{
	/// <summary>
	///     Represents tools related to Team City.
	/// </summary>
	public static class TeamCity
	{
		public static TimeSpan WaitAfterImport = TimeSpan.FromSeconds(1.5);

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