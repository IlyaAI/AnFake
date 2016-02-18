using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Core.Test
{
	[DeploymentItem("NuGet.exe")]
	[TestClass]
	public class NuGetTest
	{
		[TestMethod]
		public void NuGet_pack_spec_20()
		{
			// arrange
			var nuspec = NuGet.Spec20(meta =>
			{
				meta.Id = "TestId";
				meta.Version = "1.2.3.0".AsVersion();
				meta.Authors = "Author";
				meta.Description = "Description";
				meta.Summary = "Summary";
			});

			nuspec.AddFiles("AnFake.Core.Test.dll".AsFileSet(), "lib");
    
			// act
			NuGet.Pack(nuspec, "".AsPath(), p =>
			{
				p.NoPackageAnalysis = true;
				p.ToolPath = "NuGet.exe".AsPath();
			});

			// assert
			Assert.IsTrue("TestId.1.2.3.0.nupkg".AsFile().Exists());
		}

		[TestMethod]
		public void NuGet_pack_spec_25()
		{
			// arrange
			var nuspec = NuGet.Spec25(meta =>
			{
				meta.Id = "TestId";
				meta.Version = "1.2.3.5".AsVersion();
				meta.Authors = "Author";
				meta.Description = "Description";
				meta.Summary = "Summary";
			});

			nuspec.AddFiles("AnFake.Core.Test.dll".AsFileSet(), "lib");

			// act
			NuGet.Pack(nuspec, "".AsPath(), p =>
			{
				p.NoPackageAnalysis = true;
				p.ToolPath = "NuGet.exe".AsPath();
			});

			// assert
			Assert.IsTrue("TestId.1.2.3.5.nupkg".AsFile().Exists());
		}
	}
}