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
	public class TfsWorkItemTest : TfsTestSuite
	{
		public ITracer PrevTracer;
		public IBuildDetail Build;

		[TestInitialize]
		public override void Initialize()
		{
			base.Initialize();

			MyBuildTesting.Initialize(
				new Dictionary<string, string>
				{
					{"Tfs.Uri", TfsUri},
					{"Tfs.TeamProject", TeamProject}
				});

			Plugin.Register<TfsPlugin>().AsSelf();
			Plugin.Configure();
		}

		[TestCleanup]
		public void Cleanup()
		{
			Plugin.Reset();
			MyBuildTesting.Reset();
		}

		[TestCategory("Integration")]
		[TestMethod]
		public void TfsWorkItem_should_exec_named_query()
		{
			// arrange

			// act
			var items = TfsWorkItem.ExecNamedQuery("Shared Queries/My Work/My Tasks", "me", "Ilya Ivanov").ToArray();
			
			// assert
			Assert.IsTrue(items.Length > 0);
			Assert.IsTrue(items[0].NativeId > 0);
			Assert.AreEqual("Task", items[0].Type);
		}
	}
}