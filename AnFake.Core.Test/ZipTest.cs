using System.IO;
using AnFake.Api;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace AnFake.Core.Test
{
	[DeploymentItem("Data/Files", "Data/Files")]
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
	}
}