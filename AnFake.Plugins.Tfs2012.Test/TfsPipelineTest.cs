using System;
using System.Linq;
using System.Threading;
using AnFake.Integration.Tfs2012.Pipeline;
using AnFake.Api.Pipeline;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Plugins.Tfs2012.Test
{
	[TestClass]
	public class TfsPipelineTest : TfsTestSuite
	{
		public IBuildDetail Build;
		public TfsPipeline Pipeline;

		[TestInitialize]
		public override void Initialize()
		{
			base.Initialize();

			Build = CreateTestBuild();

			var tracking = Build
				.Information
				.AddActivityTracking("0001", "Sequence", "General");

			Build
				.Information
				.Save();

			Pipeline = new TfsPipeline(Build, tracking);
		}

		[TestCleanup]
		public void Cleanup()
		{			
			Build.FinalizeStatus(BuildStatus.Succeeded);
			Build = null;
			Pipeline = null;
		}

		[TestCategory("Integration")]
		[TestMethod]
		public void TfsPipeline_should_start_builds()
		{
			// arrange

			// act
			var aId = Pipeline.StartBuild(TfsSettings.Get("PipelineBuildRunA"), null, "a", null);
			Wait(aId);

			var bId = Pipeline.StartBuild(TfsSettings.Get("PipelineBuildRunB"), "a", null, null);
			Wait(bId);

			// assert
			Build.Refresh(new[] {"*"}, QueryOptions.All);

			var trackingNode = Build.Information.Nodes.First(x => x.Type == "ActivityTracking");
			Assert.IsTrue(trackingNode.Children.Nodes.Length >= 6, "At least 6 messages must be tracked.");
		}

		private void Wait(object id)
		{
			while (true)
			{
				var status = Pipeline.GetBuildStatus(id);
				if (status != PipelineStepStatus.InProgress)
					break;

				Thread.Sleep(TimeSpan.FromSeconds(1));
			}
		}
	}
}