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
				new[] {"Build"},
				properties);
		}		

		public static void Reset()
		{
			Plugin.Reset();
			MyBuild.Reset();
		}

		public static void ConfigurePlugins(Action registrator)
		{
			Plugin.Reset();

			registrator();

			Plugin.Configure();
		}		
	}
}