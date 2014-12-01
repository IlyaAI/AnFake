using System.Collections.Generic;
using System.IO;
using AnFake.Api;

namespace AnFake.Core
{
	public static class MyBuildTesting
	{
		public static MyBuild.Params CreateParams(IDictionary<string, string> properties)
		{
			var buildPath = Directory.GetCurrentDirectory().AsPath();

			return new MyBuild.Params(
				buildPath,
				new FileItem(buildPath/"build.log", buildPath),
				new FileItem(buildPath/"build.fsx", buildPath),
				Verbosity.Normal,
				new[] {"Build"},
				properties);
		}
	}
}