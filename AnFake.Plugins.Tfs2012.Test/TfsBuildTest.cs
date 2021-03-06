﻿using System;
using System.Collections.Generic;
using System.Linq;
using AnFake.Core;
using AnFake.Core.Internal;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Plugins.Tfs2012.Test
{
	[TestClass]
	public class TfsBuildTest : TfsTestSuite
	{
		public IBuildDetail Build;

		[TestInitialize]
		public override void Initialize()
		{
			base.Initialize();

			Build = CreateTestBuild();

			MyBuildTesting.Initialize(
				new Dictionary<string, string>
				{
					{"Tfs.Uri", TfsUri},
					{"Tfs.BuildUri", Build.Uri.ToString()}
				});

			MyBuildTesting.ConfigurePlugins(PluginsRegistrator);
		}

		private static void PluginsRegistrator()
		{
			Plugin.Register<TfsPlugin>().AsSelf();
		}

		[TestCleanup]
		public void Cleanup()
		{
			MyBuildTesting.Finalise();

			CleanupTestBuild(Build);
			Build = null;
		}

		[TestCategory("Integration")]
		[TestMethod]
		public void TfsBuild_should_save_custom_fields()
		{
			// arrange			

			// act I
			TfsBuild.Current.SetCustomField("MyField", "Custom-Value");
			TfsBuild.Current.Save();

			// re-initiate			
			MyBuildTesting.ConfigurePlugins(PluginsRegistrator);

			// act II
			var myField = TfsBuild.Current.GetCustomField("MyField");

			// assert
			Assert.AreEqual("Custom-Value", myField);
		}

		[TestCategory("Integration")]
		[TestMethod]
		public void TfsBuild_should_return_custom_fields_in_queried_build()
		{
			// arrange
			var build = TfsBuild.QueryAll(1).First();
			build.SetCustomField("MyField", "Custom-Value");
			build.Save();

			// act
			var myField = TfsBuild.QueryAll(1)
				.First()
				.GetCustomField("MyField");

			// assert
			Assert.AreEqual("Custom-Value", myField);
		}

		[TestCategory("Integration")]
		[TestMethod]
		public void TfsBuild_should_return_http_uri()
		{
			// arrange

			// act
			var build = TfsBuild.QueryAll(BuildDefinition, 1).First();

			// assert			
			Assert.IsTrue(build.Uri.ToString().StartsWith("http", StringComparison.OrdinalIgnoreCase));
		}

		[TestCategory("Integration")]
		[TestMethod]
		public void TfsBuild_should_return_requested_for_on_current()
		{
			// arrange

			// act
			var requestedFor = TfsBuild.Current.RequestedFor;

			// assert			
			Assert.IsFalse(String.IsNullOrEmpty(requestedFor));
		}

		[TestCategory("Integration")]
		[TestMethod]
		public void TfsBuild_should_return_requested_for_on_any()
		{
			// arrange

			// act
			var requestedFor = TfsBuild.QueryAll(1)
				.First()
				.RequestedFor;

			// assert			
			Assert.IsFalse(String.IsNullOrEmpty(requestedFor));
		}
	}
}