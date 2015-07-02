using AnFake.Core;

namespace AnFake.Plugins.TeamCity
{
	/// <summary>
	///     Represents tools related to Team City.
	/// </summary>
	public static class TeamCity
	{
		/// <summary>
		///     Activates <c>TeamCity</c> plugin.
		/// </summary>
		public static void PlugIn()
		{			
			Plugin.Register<TeamCityPlugin>()				
				//.As<Core.Integration.IBuildServer>()
				.AsSelf();
		}		
	}
}