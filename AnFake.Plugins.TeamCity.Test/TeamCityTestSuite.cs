using System;
using AnFake.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Plugins.TeamCity.Test
{
	public abstract class TeamCityTestSuite
	{
		public Settings TcSettings;

		public Uri TcUri
		{
			get { return new Uri(TcSettings.Get("TcUri")); }
		}

		public string TcUser
		{
			get { return TcSettings.Get("TcUser"); }
		}

		public string TcPassword
		{
			get { return TcSettings.Get("TcPassword"); }
		}

		public string TcTestBuildConfiguration
		{
			get { return TcSettings.Get("TcTestBuildConfiguration"); }
		}

		public virtual void Initialize()
		{
			TcSettings = new Settings("[ApplicationData]/AnFake/teamcity-test.json".AsPath());

			if (!TcSettings.Has("TcUri"))
				Assert.Inconclusive("There is no settings provided for integration test. Put appropriate values into '[ApplicationData]/AnFake/teamcity-test.json' file.");
		}		
	}
}