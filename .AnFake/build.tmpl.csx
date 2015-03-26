using System;
using System.Linq;
using AnFake.Api;
using AnFake.Core;
using AnFake.Csx;
using AnFake.Plugins.Tfs2012;

public sealed class BuildScript : BuildScriptSkeleton
{
	public override void Configure()
	{
		Tfs.PlugIn();

		var productVersion = "1.0.0".AsVersion();

		var outDir = ".out".AsPath();
		var productOut = outDir/"product";
		var testsOut = outDir/"tests";

		var tests = "*/*.Test.csproj".AsFileSet();

		// TODO: The better solution is to explicitly enumerate here top level projects which forms you product.
		var product =
			"*/*.csproj".AsFileSet()
			- tests;

		"Clean".AsTarget().Do(() =>
		{
			var obj = "*/obj".AsFolderSet();
			var bin = "*/bin".AsFolderSet();

			Folders.Clean(obj);
			Folders.Clean(bin);
			Folders.Clean(outDir);
		});

		"NuGetRestore".AsTarget().Do(() =>
		{
			NuGet.Restore();
		});

		"Compile".AsTarget().Do(() =>
		{
			// Embeds product version into AssemblyInfo files.
			// 'Temporary' means that after build all changes will be reverted to prevent committing of version number to VCS.
			AssemblyInfo.EmbedTemporary(
				"*/Properties/AssemblyInfo.cs".AsFileSet(),
				p =>
				{
					p.Version = VersionControl.GetFullVersion(productVersion);
				});

			MsBuild.BuildRelease(product, productOut);

			MsBuild.BuildRelease(tests, testsOut);
		});

		"Test.Unit".AsTarget().Do(() =>
		{
			// Run tests using VSTest.Console.exe runner
			VsTest.Run(testsOut%"*.Test.dll");
		});

		// 'Drop' target is requested when DropLocation or PrivateDropLocation is specified in TFS build definition.
		"Drop".AsTarget().Do(() =>
		{
			// TODO: expose your final artifacts via BuildServer.ExposeArtifacts
		});

		"Test".AsTarget().DependsOn("Test.Unit");

		"Build".AsTarget().DependsOn("NuGetRestore", "Compile", "Test");
	}
}