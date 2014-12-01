using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using AnFake.Api;
using AnFake.Core;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Plugins.Tfs2012.Test
{
	[Ignore]
	[TestClass]
	public class TfsPluginTest
	{
		public const string TfsUri = "https://nsk-tfs.avp.ru:8081/tfs/dlpr";
		public const string TeamProject = "DLP_PDK";
		public const string BuildDefinition = "FAKE-test";
		public const string DropLocation = @"\\nsk-fs\Inbox\Ivanov Ilya";

		public ITracer PrevTracer;
		public IBuildDetail Build;

		[TestInitialize]
		public void Initialize()
		{
			PrevTracer = Trace.Set(new BypassTracer());

			var teamProjectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(TfsUri));
			var buildServer = (IBuildServer) teamProjectCollection.GetService(typeof (IBuildServer));

			var definition = buildServer.GetBuildDefinition(TeamProject, BuildDefinition);
			Build = definition.CreateManualBuild(
				new Random().Next().ToString(CultureInfo.InvariantCulture),
				DropLocation);
		}

		[TestCleanup]
		public void Cleanup()
		{
			Trace.Set(PrevTracer);

			if (Build != null)
			{
				Build.FinalizeStatus(BuildStatus.Failed);
				Build.Delete(DeleteOptions.All & ~DeleteOptions.DropLocation);
				Build = null;
			}
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

			var p = MyBuildTesting.CreateParams(
				new Dictionary<string, string>
				{					
					{"Tfs.Uri", TfsUri},
					{"Tfs.BuildUri", Build.Uri.ToString()},
					{"Tfs.ActivityInstanceId", "0001"}
				});

			var tfs = new TfsPlugin(p);			

			// act
			Trace.Message(new TraceMessage(TraceMessageLevel.Debug, "Debug"));
			Trace.Message(new TraceMessage(TraceMessageLevel.Info, "Info"));
			Trace.Message(new TraceMessage(TraceMessageLevel.Warning, "Warning"));
			Trace.Message(new TraceMessage(TraceMessageLevel.Error, "Error"));

			// assert
			Build.Refresh(new[] {"*"}, QueryOptions.All);
			Assert.AreEqual(2, Build.Information.Nodes.Length);

			Assert.AreEqual("ActivityTracking", Build.Information.Nodes[0].Type);
			Assert.AreEqual(4, Build.Information.Nodes[0].Children.Nodes.Length);

			Assert.AreEqual("CustomSummaryInformation", Build.Information.Nodes[1].Type);
			Assert.AreEqual(1, Build.Information.Nodes[1].Children.Nodes.Length);
		}
	}
}