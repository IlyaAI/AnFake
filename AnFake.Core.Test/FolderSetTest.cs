using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Core.Test
{
	[DeploymentItem("Data/FileSet", "Data/FileSet")]
	[TestClass]
	public class FolderSetTest
	{		
		[TestCategory("Functional")]
		[TestMethod]
		public void FolderSet_should_preserve_order_1()
		{
			// arrange
			var fs = "Data/FileSet/dir-A".AsFolderSet()
				.Include("Data/FileSet/dir-B");

			// act
			var folders = fs.ToList();

			// assert
			Assert.AreEqual(2, folders.Count);
			Assert.AreEqual("dir-A", folders[0].Name);
			Assert.AreEqual("dir-B", folders[1].Name);			
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void FolderSet_should_preserve_order_2()
		{
			// arrange
			var fs = "Data/FileSet/dir-B".AsFolderSet()
				.Include("Data/FileSet/dir-A");

			// act
			var folders = fs.ToList();

			// assert
			Assert.AreEqual(2, folders.Count);
			Assert.AreEqual("dir-B", folders[0].Name);			
			Assert.AreEqual("dir-A", folders[1].Name);			
		}
	}
}