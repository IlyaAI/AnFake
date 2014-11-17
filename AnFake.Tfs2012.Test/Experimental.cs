using System;
using System.Linq;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Tfs2012.Test
{
	//[Ignore]
	[TestClass]
	public class Experimental
	{
		[TestMethod]
		public void Test()
		{
			var teamProjectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri("https://nsk-tfs.avp.ru:8081/tfs/dlpr"));
			var buildService = (IBuildServer)teamProjectCollection.GetService(typeof(IBuildServer));

			//IBuildDefinition buildDefinition = buildService.GetBuildDefinition("DLP_PDK", "BuildDefinitionName");
			var buildDetail = buildService.QueryBuildsByUri(
				new[] { new Uri("vstfs:///Build/Build/32717") }, 
				new[] { "*" }, 
				QueryOptions.All).Single();

			//var node = Find(buildDetail.Information.Nodes[0], "DisplayText", "package must be empty");
			var node = Find(buildDetail.Information.Nodes[0], "Test Run Inconclusive");			
		}

		[TestMethod]
		public void Test2()
		{
			var teamProjectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri("https://nsk-tfs.avp.ru:8081/tfs/dlpr"));
			var buildServer = (IBuildServer)teamProjectCollection.GetService(typeof(IBuildServer));

			var definition = buildServer.GetBuildDefinition("DLP_PDK", "FAKE-test");
			var detail = definition.CreateManualBuild("0005", @"\\nsk-fs\Inbox\Ivanov Ilya");

			detail.Information
				.AddActivityTracking("0001", "Sequence", "General")/*.Node.Children
				.AddBuildError("FileSet_should_support_up_steps_and_wildcard_steps: Assert.IsTrue failed.", DateTime.Now)*/;

			/*detail.Information
				.AddActivityTracking("01", "Sequence", "General").Node.Children				
				.AddBuildError("Test failure", DateTime.Now).ErrorType = "Test.Unit";

			detail.Information
				.AddConfigurationSummary("Configuration", "Platform").Node.Children
				.AddBuildError("Compilation failure", DateTime.Now).ErrorType = "Compilation";*/

			/*detail.Information
				.AddCustomSummaryInformation("Compile: 0 error(s) 0 warning(s) SUCCESSED", "AnFake", "AnFake 'Build' Summary", 170);
			detail.Information
				.AddCustomSummaryInformation("Test.Unit: 1 error(s) 0 warning(s) FAILED", "AnFake", "AnFake 'Build' Summary", 170);*/
			//detail.Information
			//	.AddCustomSummaryInformation("[0001] FileSet_should_support_up_steps_and_wildcard_steps: Assert.IsTrue failed.", "AnFake", "'Build' Summary", 170);
			/*detail.Information
				.AddCustomSummaryInformation("====================", "AnFake", "AnFake 'Build' Summary", 170);
			detail.Information
				.AddCustomSummaryInformation("'Build' Failed. See the section below for error/warning details...", "AnFake", "AnFake 'Build' Summary", 170);*/

			/*var node = detail.Information.AddBuildProjectNode(DateTime.Now, "Debug", "MySolution.sln", "x86", "$/project/MySolution.sln", DateTime.Now, "Default");
			node.CompilationErrors = 1;
			node.CompilationWarnings = 1;			

			node.Node.Children.AddBuildError("Compilation", "File1.cs", 12, 5, "", "Syntax error", DateTime.Now);
			node.Node.Children.AddBuildWarning("File2.cs", 3, 1, "", "Some warning", DateTime.Now, "Compilation");
			
			node.Node.Children.AddBuildError("Test failure", DateTime.Now).ErrorType = "Test";

			node.Node.Children.AddExternalLink("Log File", new Uri(@"\\server\share\logfiledebug.txt"));			
			node.Save();*/
			
			/*buildProjectNode = detail.Information.AddBuildProjectNode(DateTime.Now, "Release", "MySolution.sln", "x86", "$/project/MySolution.sln", DateTime.Now, "Default");
			buildProjectNode.CompilationErrors = 0;
			buildProjectNode.CompilationWarnings = 0;

			buildProjectNode.Node.Children.AddExternalLink("Log File", new Uri(@"\\server\share\logfilerelease.txt"));
			buildProjectNode.Save();*/

			detail.Information.Save();
			detail.FinalizeStatus(BuildStatus.Failed);

			var uri = detail.Uri;
			//detail.Save();
		}

		[TestMethod]
		public void Test3()
		{
			var teamProjectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri("https://nsk-tfs.avp.ru:8081/tfs/dlpr"));
			var buildService = (IBuildServer)teamProjectCollection.GetService(typeof(IBuildServer));

			var buildDefinition = buildService.GetBuildDefinition("DLP_PDK", "compliance-dev.fake");
			var buildDetails = buildDefinition.QueryBuilds();

			/*var buildDetail = buildService.QueryBuildsByUri(
				new[] { new Uri("vstfs:///Build/Build/32764") },
				new[] { "*" },
				QueryOptions.All).Single();

			buildDetail.FinalizeStatus(BuildStatus.Failed);
			buildDetail.Delete(DeleteOptions.All & ~DeleteOptions.DropLocation);*/
		}

		private static IBuildInformationNode Find(IBuildInformationNode node, string fieldName, string fieldValue)
		{
			if (node.Fields.ContainsKey(fieldName) && node.Fields[fieldName].Contains(fieldValue))
				return node;

			return node.Children.Nodes
				.Select(child => Find(child, fieldName, fieldValue))
				.FirstOrDefault(ret => ret != null);
		}

		private static IBuildInformationNode Find(IBuildInformationNode node, string fieldValue)
		{
			if (node.Fields.Values.Any(x => x.Contains(fieldValue)))
				return node;

			return node.Children.Nodes
				.Select(child => Find(child, fieldValue))
				.FirstOrDefault(ret => ret != null);
		}
	}
}
