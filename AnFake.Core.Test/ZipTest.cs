using AnFake.Api;
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
	}
}