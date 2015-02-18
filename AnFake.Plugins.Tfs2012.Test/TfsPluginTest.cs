using System.Collections.Generic;
using System.Linq;
using AnFake.Api;
using AnFake.Core;
using AnFake.Core.Internal;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Plugins.Tfs2012.Test
{
	[TestClass]
	public class TfsPluginTest : TfsTestSuite
	{
		public ITracer PrevTracer;
		public IBuildDetail Build;

		[TestInitialize]
		public override void Initialize()
		{
			base.Initialize();

			PrevTracer = Trace.Set(new BypassTracer());

			Build = CreateTestBuild();
			Build.Information
				.AddActivityTracking("0001", "Sequence", "General");
			Build.Information
				.Save();

			MyBuildTesting.Initialize(
				new Dictionary<string, string>
				{
					{"Tfs.Uri", TfsUri},
					{"Tfs.BuildUri", Build.Uri.ToString()},
					{"Tfs.ActivityInstanceId", "0001"}
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

			Trace.Set(PrevTracer);

			CleanupTestBuild(Build);
			Build = null;
		}

		[TestCategory("Integration")]
		[TestMethod]
		public void TfsPlugin_should_track_messages()
		{
			// arrange			
			"Test".AsTarget().Do(() =>
			{
				Trace.Message(new TraceMessage(TraceMessageLevel.Debug, "Debug"));
				Trace.Message(new TraceMessage(TraceMessageLevel.Info, "Info"));
				Trace.Message(new TraceMessage(TraceMessageLevel.Warning, "Warning"));
				Trace.Message(new TraceMessage(TraceMessageLevel.Error, "Error"));				
			}).SkipErrors();

			// act
			MyBuildTesting.RunTarget("Test");

			// assert
			Build.Refresh(new[] { "*" }, QueryOptions.All);

			var trackingNode = Build.Information.Nodes.First(x => x.Type == "ActivityTracking");
			Assert.IsTrue(trackingNode.Children.Nodes.Length >= 4, "At least 4 messages must be tracked.");
		}
	}
}