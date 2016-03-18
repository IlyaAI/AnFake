using System;
using System.Collections.Generic;
using System.Linq;
using AnFake.Core;
using AnFake.Core.Integration.Builds;
using AnFake.Core.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Plugins.TeamCity.Test
{
	[TestClass]
	public class TeamCityPluginTest : TeamCityTestSuite
	{
		[TestInitialize]
		public override void Initialize()
		{
			base.Initialize();

			MyBuildTesting.Initialize(
				new Dictionary<string, string>
				{
					{"TeamCity.Uri", TcUri.ToString()},
					{"TeamCity.User", TcUser},
					{"TeamCity.Password", TcPassword}
				});

			MyBuildTesting.ConfigurePlugins(TeamCity.PlugIn);
		}
		
		[TestCleanup]
		public void Cleanup()
		{
			MyBuildTesting.Finalise();			
		}

		[TestCategory("Integration")]
		[TestMethod]
		public void TeamCityPlugin_should_return_last_good_build()
		{
			// arrange			

			// act
			var build = Plugin.Get<IBuildServer2>().FindLastGoodBuild(TcTestBuildConfiguration);

			// assert
			Assert.IsNotNull(build);
			Assert.IsNotNull(build.Uri);
			Assert.IsNotNull(build.NativeUri);
			Assert.IsTrue(build.Started > DateTime.MinValue);
			Assert.IsTrue(build.Finished > DateTime.MinValue);
			Assert.IsNotNull(build.ChangesetHash);
			Assert.IsTrue(build.Counter > 0);
		}

		[TestCategory("Integration")]
		[TestMethod]
		public void TeamCityPlugin_should_return_last_tagged_build()
		{
			// arrange
			var build = Plugin.Get<IBuildServer2>().FindLastGoodBuild(TcTestBuildConfiguration);
			build.AddTag("TEST");

			try
			{
				// act
				build = Plugin.Get<IBuildServer2>().FindLastTaggedBuild(TcTestBuildConfiguration, new[] { "TEST" });

				// assert
				Assert.IsNotNull(build);
				Assert.IsTrue(build.Tags.Contains("TEST"));
			}
			finally
			{
				SafeOp.Try(() => build.RemoveTag("TEST"));
			}			
		}

		[TestCategory("Integration")]
		[TestMethod]
		public void TeamCityPlugin_should_download_artifacts()
		{
			// arrange
			var build = Plugin.Get<IBuildServer2>().FindLastGoodBuild(TcTestBuildConfiguration);			
			
			var dstPath = ".artifacts".AsPath();
			Folders.Clean(dstPath);

			// act
			build.DownloadArtifacts(".teamcity", dstPath, "**");
				
			// assert
			Assert.IsTrue((dstPath / "logs/buildLog.msg5").AsFile().Exists());
			Assert.IsTrue((dstPath / "settings/digest.txt").AsFile().Exists());			
		}
	}
}