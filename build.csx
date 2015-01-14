using System;
using System.Linq;
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
		var plugins =
			"AnFake.Plugins.Tfs2012/*.csproj".AsFileSet()
			+ "AnFake.Plugins.HtmlSummary/*.csproj";
		var extras = "*".AsFileSetFrom(".AnFake/Extras");
		var cmds = "*.cmd".AsFileSetFrom(".AnFake");
		var xaml = "AnFakeTemplate.xaml".AsFileSetFrom("AnFake.Integration.Tfs2012.Template");
		var buildTmpls = "*.tmpl.fsx".AsFileSetFrom(".AnFake") + "*.tmpl.csx";
		var fsharp =
			"[ProgramFilesX86]/Reference Assemblies/Microsoft/FSharp/.NETFramework/v4.0/4.3.1.0".AsPath()
			%"FSharp.Core.dll"
			+ "FSharp.Core.optdata"
			+ "FSharp.Core.sigdata";
		var tests = "*/*.Test.csproj".AsFileSet();
		var nugetFiles =
			productOut%"AnFake.exe"
			+ "AnFake.exe.config"
			+ "*.cmd"
			+ "*.dll"
			+ "*.tmpl.fsx"
			+ "*.tmpl.csx"
			+ "AnFake.*.xml"			
			+ "FSharp.Core.optdata"
			+ "FSharp.Core.sigdata"
			+ "Extras/*"
			+ "Plugins/AnFake.Integration.Tfs2012.dll"
			+ "Plugins/AnFakeTemplate.xaml"
			+ "Plugins/AnFake.Plugins.Tfs2012.dll"
			+ "Plugins/AnFake.Plugins.HtmlSummary.dll"
			+ "Plugins/AnFake.Plugins.HtmlSummary.zip";

		var productName = "AnFake";
		var productTitle = "AnFake /Another F# Make/ runtime component";
		var productDescription = "AnFake: Another F# Make";
		var productAuthor = "Ilya A. Ivanov";
		var productVersion = "0.9".AsVersion();

		"Clean".AsTarget().Do(() =>
		{
			var obj = "*/obj".AsFolderSet();
			var bin = "*/bin".AsFolderSet();

			Folders.Clean(obj);
			Folders.Clean(bin);
			Folders.Clean(outDir);
		});

		"EmbedAssemblyInfo".AsTarget().Do(() =>
		{
			AssemblyInfo.Embed(
				"*/Properties/AssemblyInfo.cs".AsFileSet(),
				p =>
				{
					p.Title = productTitle;
					p.Product = productName;
					p.Description = productDescription;
					p.Copyright = String.Format("{0} {1}", productAuthor, DateTime.Now.Year);
					p.Version = productVersion;
				});
		});

		"Compile".AsTarget().Do(() =>
		{
			MsBuild.BuildRelease(product, productOut);

			Files.Copy(cmds, productOut, true);
			Files.Copy(fsharp, productOut, true);
			Files.Copy(buildTmpls, productOut, true);

			MsBuild.BuildRelease(plugins, pluginsOut);

			Files.Copy(extras, extrasOut, true);
			Files.Copy(xaml, pluginsOut, true);

			MsBuild.BuildRelease(tests, testsOut);
		});

		"Custom.ZipHtmlSummary".AsTarget().Do(() =>
		{
			var htmlSummary =
				"**/*".AsFileSetFrom("AnFake.Plugins.HtmlSummary/Html")
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
			var fsharpCore =
				productOut%"FSharp.Core.dll"
				+ "FSharp.Core.optdata"
				+ "FSharp.Core.sigdata";

			if (fsharpCore.Count() != 3)
				MyBuild.Failed("There are FSharp.Core.dll, FSharp.Core.optdata and FSharp.Core.sigdata files must present in .out/product");

			var nuspec = NuGet.Spec25(meta =>
			{
				meta.Id = productName;
				meta.Version = productVersion;
				meta.Authors = productAuthor;
				meta.Description = productDescription;
			});

			nuspec.AddFiles(nugetFiles, "");

			NuGet.Pack(nuspec, outDir, p =>
			{
				p.NoPackageAnalysis = true;
				p.NoDefaultExcludes = true;
			});

			//NuGet.Push(nupkg, fun p -> 
			//	p.AccessKey <- MyBuild.GetProp("NuGet.AccessKey")
			//	p.SourceUrl <- MyBuild.GetProp("NuGet.SourceUrl"))
		});

		"Compile".AsTarget()
			.DependsOn("EmbedAssemblyInfo");

		"Build".AsTarget()
			.DependsOn("Compile", "Custom.ZipHtmlSummary", "Test.Unit");
	}
}