using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Core.Test
{
	[DeploymentItem("Data/archive.tar.gz", "Data")]	
	[TestClass]
	public class TarGzTest
	{
		[TestCategory("Functional")]
		[TestMethod]
		public void Unpack_should_extract_recursively()
		{
			// arrange
			var archive = "Data/archive.tar.gz".AsPath();
			var tmp = "[Temp]/tar.gz".MakeUnique().AsPath();

			// act
			try
			{
				TarGz.Unpack(archive, tmp);

				// assert
				Assert.AreEqual(1, (tmp % "doc/library_usage.xml").Count());
			}
			finally
			{
				Folders.Delete(tmp);
			}
		}
		
		[TestCategory("Functional")]
		[TestMethod]
		public void List_should_scna_recursively()
		{
			// arrange
			var archive = "Data/archive.tar.gz".AsPath();

			// act			
			var list = TarGz.List(archive);

			// assert
			Assert.AreEqual(1, list.Count);
			Assert.AreEqual("doc/library_usage.xml", list[0].Path);
		}		
	}
}