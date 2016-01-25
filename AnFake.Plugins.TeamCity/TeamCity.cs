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

		/// <summary>
		///     Activates <c>TeamCity</c> plugin.
		/// </summary>
		public static void PlugIn()
		{			
			Plugin.Register<TeamCityPlugin>()				
				.As<Core.Integration.IBuildServer>()
				.AsSelf();
		}		
	}
}