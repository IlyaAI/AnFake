using System.Collections.Generic;
using AnFake.Api;
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

			MyBuildTesting.Initialize(
				new Dictionary<string, string>
				{
					{"Tfs.Uri", TfsUri},
					{"Tfs.BuildUri", Build.Uri.ToString()},
					{"Tfs.ActivityInstanceId", "0001"}
				});
		}

		[TestCleanup]
		public void Cleanup()
		{
			MyBuildTesting.Reset();

			Trace.Set(PrevTracer);

			CleanupTestBuild(Build);
			Build = null;
		}

		[TestCategory("Integration")]
		[TestMethod]
		public void TfsPlugin_should_track_messages()
		{
			// arrange			
			Build.Information
				.AddActivityTracking("0001", "Sequence", "General");
			Build.Information
				.Save();

			// ReSharper disable once ObjectCreationAsStatement
			new TfsPlugin();

			// act
			Trace.Message(new TraceMessage(TraceMessageLevel.Debug, "Debug"));
			Trace.Message(new TraceMessage(TraceMessageLevel.Info, "Info"));
			Trace.Message(new TraceMessage(TraceMessageLevel.Warning, "Warning"));
			Trace.Message(new TraceMessage(TraceMessageLevel.Error, "Error"));			

			// assert
			Build.Refresh(new[] {"*"}, QueryOptions.All);
			Assert.AreEqual(1, Build.Information.Nodes.Length);

			Assert.AreEqual("ActivityTracking", Build.Information.Nodes[0].Type);
			Assert.AreEqual(4, Build.Information.Nodes[0].Children.Nodes.Length);			
		}
	}
}