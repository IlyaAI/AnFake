using System.Collections.Generic;
using System.IO;
using AnFake.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Plugins.Tfs2012.Test
{
	//[Ignore]
	[TestClass]
	public class TfsExTest : TfsTestSuite
	{
		[TestInitialize]
		public void Initialize()
		{
			var p = MyBuildTesting.CreateParams(new Dictionary<string, string> {{"Tfs.Uri", TfsUri}});

			Plugin.Register(new TfsPlugin(p));
		}

		[TestCleanup]
		public void Cleanup()
		{
			Plugin.Reset();
		}

		[TestCategory("Integration")]
		[TestMethod]
		public void TfsEx_should_create_workspace_from_file_and_get_it()
		{
			// arrange

			// act
			/*TfsWorkspace.Checkout(
				"$/DLPR_Internals/Dlp.Build/Fake.Plugins/1.0.0", 
				"[Temp]/Fake.Plugins.1.0.0_2".AsPath(), 
				"TfsExTest2",
				p => p.WorkspaceFile = "vcs.txt");*/

			// assert			
		}
	}
}