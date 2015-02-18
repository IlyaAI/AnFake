using System;
using System.Collections.Generic;
using System.IO;
using AnFake.Api;

namespace AnFake.Core.Internal
{
	public static class MyBuildTesting
	{
		public static void Initialize(IDictionary<string, string> properties)
		{
			var buildPath = Directory.GetCurrentDirectory().AsPath();

			MyBuild.Initialize(
				buildPath,
				new FileItem(buildPath/"build.log", buildPath),
				new FileItem(buildPath/"build.fsx", buildPath),
				Verbosity.Normal,
				new[] {"Test"},
				properties);
		}

		public static void Initialize()
		{
			Initialize(new Dictionary<string, string>());
		}

		public static void Finalise()
		{			
			MyBuild.Finalise();
		}

		public static void ConfigurePlugins(Action registrator)
		{
			Plugin.Finalise();

			registrator();

			Plugin.Configure();
		}

		public static void RunTarget(string name)
		{
			name.AsTarget().Run();
		}
	}
}