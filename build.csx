using System;
using AnFake.Core;
using AnFake.Csx;

public sealed class BuildScript : BuildScriptSkeleton
{	
	public override void Configure()
	{
		var outDir = ".out".AsPath();
		var productOut = outDir/"product";
		var pluginsOut = productOut/"Plugins";
		var extrasOut = productOut/"Extras";
		var testsOut = outDir/"tests";
		var product = "AnFake/*.csproj".AsFileSet();
		var plugins = "AnFake.Plugins.Tfs2012/*.csproj".AsFileSet()
					+ "AnFake.Plugins.HtmlSummary/*.csproj";
		var extras = "*".AsFileSetFrom(".AnFake/Extras");
		var cmds = "*.cmd".AsFileSetFrom(".AnFake");
		var tests = "*/*.Test.csproj".AsFileSet();
		var nugetFiles =
			productOut%"AnFake.exe"
			+ "AnFake.exe.config"
			+ "*.cmd"
			+ "*.dll"
			+ "AnFake.*.xml"
			- "FSharp.Core.dll"
			+ "Extras/*"
			+ "Plugins/AnFake.Integration.Tfs2012.dll"
			+ "Plugins/AnFake.Plugins.Tfs2012.dll"
			+ "Plugins/AnFake.Plugins.HtmlSummary.dll"
			+ "Plugins/AnFake.Plugins.HtmlSummary.zip";
		var version = "0.9".AsVersion();

		"Clean".AsTarget().Do(() =>
		{
			var obj = "*/obj".AsFolderSet();
			var bin = "*/bin".AsFolderSet();

			Folders.Clean(obj);
			Folders.Clean(bin);
			Folders.Clean(outDir);
		});

		"Compile".AsTarget().Do(() =>
		{
			AssemblyInfo.EmbedTemporary(
				"*/Properties/AssemblyInfo.cs".AsFileSet(),
				p =>
				{
					p.Title = "AnFake /Another F# Make/ runtime component";
					p.Product = "AnFake";
					p.Description = "AnFake: Another F# Make";
					p.Copyright = String.Format("Ilya A. Ivanov {0}", DateTime.Now.Year);
					p.Version = version;
				});

			MsBuild.BuildRelease(product, productOut);

			Files.Copy(cmds, productOut, true);

			MsBuild.BuildRelease(plugins, pluginsOut);

			Files.Copy(extras, extrasOut, true);

			MsBuild.BuildRelease(tests, testsOut);
		});

		"Custom.ZipHtmlSummary".AsTarget().Do(() =>
		{
			var htmlSummary = "**/*".AsFileSetFrom("AnFake.Plugins.HtmlSummary/Html")
							- "build.summary.js";

			var zip = pluginsOut/"AnFake.Plugins.HtmlSummary.zip";

			Zip.Pack(htmlSummary, zip);
			Files.Copy(zip, ".AnFake/Plugins".AsPath()/zip.LastName, true);
		});

		"Test.Unit".AsTarget().Do(() =>
		{
			MsTest.Run(
				testsOut%"*.Test.dll",
				p => { p.NoIsolation = true; });
		}).SkipErrors();

		"Package".AsTarget().Do(() =>
		{
			var nuspec = NuGet.Spec25(meta =>
			{
				meta.Id = "AnFake";
				meta.Version = version;
				meta.Authors = "Ilya A. Ivanov";
				meta.Description = "AnFake: Another F# Make";
			});

			nuspec.AddFiles(nugetFiles, "");

			NuGet.Pack(nuspec, outDir, p =>
			{
				p.NoPackageAnalysis = true;
				p.NoDefaultExcludes = true;
			});
		});
		
		"Build".AsTarget().DependsOn("Compile", "Custom.ZipHtmlSummary", "Test.Unit");
	}
}