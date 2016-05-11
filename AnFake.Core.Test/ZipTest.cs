using System.IO;
using System.Linq;
using AnFake.Api;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace AnFake.Core.Test
{
	[DeploymentItem("Data/Files", "Data/Files")]
	[DeploymentItem("Data/archive.tar", "Data")]
	[DeploymentItem("Data/archive.tar.gz", "Data")]
	[DeploymentItem("Data/archive.zip", "Data")]
	[TestClass]
	public class ZipTest
	{
		public ITracer PrevTracer;
		public ITracer Tracer;

		[TestInitialize]
		public void Initialize()
		{
			Tracer = MockRepository.GenerateMock<ITracer>();
			PrevTracer = Trace.Set(Tracer);
		}

		[TestCleanup]
		public void Cleanup()
		{
			Trace.Set(PrevTracer);
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void Pack_should_emit_warning_if_duplicated_entries()
		{
			// arrange
			var files = "Data/Files/dir-A".AsPath() % "file.txt"
				+ "Data/Files/dir-B".AsPath() % "file.txt";
			var zipPath = "[Temp]".AsPath() / "pack".MakeUnique(".zip");
			
			// act
			try
			{
				Zip.Pack(files, zipPath);
			}
			finally
			{
				Files.Delete(zipPath);
			}
			
			// assert
			Tracer.AssertWasCalled(x => x.Write(Arg<TraceMessage>.Matches(y => y.Level == TraceMessageLevel.Warning)));
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void Pack_should_convert_back_slashes()
		{
			// arrange
			var files = "Data/Files".AsPath() % "dir-A/file.txt";
			var zipPath = "[Temp]".AsPath() / "pack".MakeUnique(".zip");

			// act
			try
			{
				Zip.Pack(files, zipPath);

				// assert
				using (var zip = new ZipInputStream(new FileStream(zipPath.Full, FileMode.Open, FileAccess.Read)))
				{
					ZipEntry entry;
					while ((entry = zip.GetNextEntry()) != null)
					{
						Assert.IsFalse(entry.Name.Contains("\\"), "Zip entry should not contain back slashes");						
					}
				}
			}
			finally
			{
				Files.Delete(zipPath);
			}
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void Unpack_should_support_zip()
		{
			// arrange
			var archive = "Data/archive.zip".AsPath();
			var tmp = "[Temp]/zip".MakeUnique().AsPath();

			// act
			try
			{
				Zip.Unpack(archive, tmp);

				// assert
				Assert.AreEqual(1, (tmp % "readme.txt").Count());
			}
			finally
			{
				Folders.Delete(tmp);
			}
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void Unpack_should_support_tar()
		{
			// arrange
			var archive = "Data/archive.tar".AsPath();
			var tmp = "[Temp]/tar".MakeUnique().AsPath();

			// act
			try
			{
				Zip.Unpack(archive, tmp);

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
		public void Unpack_should_support_gz()
		{
			// arrange
			var archive = "Data/archive.tar.gz".AsPath();
			var tmp = "[Temp]/gz".MakeUnique().AsPath();

			// act
			try
			{
				Zip.Unpack(archive, tmp);

				// assert
				Assert.AreEqual(1, (tmp % "archive.tar").Count());
			}
			finally
			{
				Folders.Delete(tmp);
			}
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void List_should_support_zip()
		{
			// arrange
			var archive = "Data/archive.zip".AsPath();			

			// act			
			var list = Zip.List(archive);

			// assert
			Assert.AreEqual(1, list.Count);
			Assert.AreEqual("readme.txt", list[0].Name);			
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void List_should_support_tar()
		{
			// arrange
			var archive = "Data/archive.tar".AsPath();

			// act			
			var list = Zip.List(archive);

			// assert
			Assert.AreEqual(1, list.Count);
			Assert.AreEqual("doc/library_usage.xml", list[0].Path);
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void List_should_support_gz()
		{
			// arrange
			var archive = "Data/archive.tar.gz".AsPath();

			// act			
			var list = Zip.List(archive);

			// assert
			Assert.AreEqual(1, list.Count);
			Assert.AreEqual("archive.tar", list[0].Name);
		}
	}
}