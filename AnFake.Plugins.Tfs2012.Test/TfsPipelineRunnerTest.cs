using System;
using System.Linq;
using AnFake.Integration.Tfs2012.Pipeline;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Plugins.Tfs2012.Test
{
	[TestClass]
	public class TfsPipelineRunnerTest : TfsTestSuite
	{
		internal IBuildDetail Build;
		internal TfsPipelineRunner PipelineRunner;

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

			PipelineRunner = new TfsPipelineRunner(Build, tracking);
		}

		[TestCleanup]
		public void Cleanup()
		{
			Build.FinalizeStatus(BuildStatus.Succeeded);
			Build = null;

			PipelineRunner.Dispose();
			PipelineRunner = null;
		}

		[TestCategory("Integration")]
		[TestMethod]
		public void TfsPipelineRunner_should_exec_build_sequence()
		{
			// arrange
			var pipelineDef = String.Format(
				"'{0}' as a -> '{1}'(a)",
				TfsSettings.Get("PipelineBuildRunA"),
				TfsSettings.Get("PipelineBuildRunB"));

			// act
			PipelineRunner.Run(pipelineDef, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(5));

			// assert
			Build.Refresh(new[] {"*"}, QueryOptions.All);

			var trackingNode = Build.Information.Nodes.First(x => x.Type == "ActivityTracking");
			Assert.IsTrue(trackingNode.Children.Nodes.Length >= 6, "At least 6 messages must be tracked.");
		}
	}
}