#r ".AnFake/AnFake.Api.v1.dll"
#r ".AnFake/AnFake.Core.dll"
#r ".AnFake/AnFake.Fsx.dll"

open System
open System.Linq
open AnFake.Api
open AnFake.Core
open AnFake.Fsx.Dsl

let out = ~~".out"
let productOut = out / "product"
let extrasOut = productOut / "Extras"
let testsOut = out / "tests"
let product = 
    !!"AnFake/*.csproj"
    + "AnFake.Api.Pipeline/*.csproj"
    + "AnFake.Plugins.*/*.csproj" - "AnFake.Plugins.*.Test/*.csproj"
    + "AnFake.Integration.MsBuild/*.csproj"
    + "AnFake.Integration.Vs2012/*.csproj"
    + "AnFake.Integration.TfWorkspacer/*.csproj"
let extras = ~~".AnFake/Extras" % "*"
let cmds = ~~".AnFake" % "*.cmd"
let xaml = ~~"AnFake.Integration.Tfs2012.Template" % "*.xaml"
let buildTmpls = ~~".AnFake" % "*.tmpl.fsx" + "*.tmpl.csx"
let installer = 
    !!"AnFake.Installer/*.csproj"
let tests = !!"*/*.Test.csproj"
let nugetFiles = 
    productOut % "AnFake.exe"
    + "AnFake.exe.config"
    + "*.cmd"
    + "*.dll"
    + "*.xaml"
    + "*.tmpl.fsx"
    + "*.tmpl.csx"
    + "AnFake.*.xml"
    + "AnFake.Plugins.*.xml"
    + "FSharp.Core.optdata"
    + "FSharp.Core.sigdata"
    + "Extras/*"
    - "Microsoft.*"
    - "AnFake.Integration.TfWorkspacer.exe"
    + ~~"packages/NuGet.CommandLine.2.8.5/tools" % "NuGet.exe"

let productName = "AnFake"
let productTitle = "AnFake: Another F# Make. Use eighther F# or C# script for your build definitions in Ms Team Build."
let productDescription = 
    "AnFake: Another F# Make. " + 
    "Use eighther F# or C# script for your build definitions in Ms Team Build, " + 
    "forget about build process templates! " +
    "Integration with TFS 2012/2013 provided out-of-box."
let productAuthor = "IlyaAI"
let productVersion = "1.2.0".AsVersion()
let productHome = "http://ilyaai.github.io/AnFake"
let productTags = "team build f# c# tfs"

//
// IMPORTANT! 
// If apiVersion changed then xamlVersion MUST BE incremented too
//
let apiVersion = "1"
let xamlVersion = "2" 
/////////////////////////////////////////////////////////////////

"Clean" => (fun _ ->    
    Folders.Clean(!!!"*/obj")
    Folders.Clean(!!!"*/bin")
    Folders.Clean(out)
    Folders.Clean("TestResults")
)

"NuGetRestore" => (fun _ ->
    NuGet.Restore("AnFake.sln".AsFile())
)

"EmbedAssemblyInfo" => (fun _ ->
    AssemblyInfo.Embed(
        !!"*/Properties/AssemblyInfo.cs",
        fun p -> 
            p.Title <- productTitle
            p.Product <- productName
            p.Description <- ""
            p.Copyright <- String.Format("{0} {1}", productAuthor, DateTime.Now.Year)
            p.Version <- productVersion
        ) |> ignore    
)

"Compile" => (fun _ ->
    MsBuild.BuildRelease(product, productOut)

    Files.Copy(cmds, productOut, true)    
    Files.Copy(buildTmpls, productOut, true)

    Files.Copy(extras, extrasOut, true)
    for fx in xaml do
        Files.Copy(fx, productOut / String.Format("{0}.v{1}.xaml", fx.NameWithoutExt, xamlVersion), true)

    MsBuild.BuildRelease(tests, testsOut)
)

"Custom.ZipHtmlSummary" => (fun _ ->
    let htmlSummary = 
        ~~"AnFake.Plugins.HtmlSummary/Html" % "**/*"
        - "build.summary.js"

    let zip = productOut / "AnFake.Plugins.HtmlSummary.zip"

    Zip.Pack(htmlSummary, zip)    
)

"Test.Unit" => (fun _ -> 
    MsTest.Run(
        testsOut % "*.Test.dll",
        fun p -> p.NoIsolation <- true)
) |> skipErrors

"Package.Zip" => (fun _ ->    
    Zip.Pack(nugetFiles, out / String.Format("AnFake-{0}.zip", productVersion))
)

"Package.Pack" => (fun _ -> 
    let fsharpCore = 
        productOut % "FSharp.Core.dll"
        + "FSharp.Core.optdata"
        + "FSharp.Core.sigdata"

    if fsharpCore.Count() <> 3 then
        MyBuild.Failed("There are FSharp.Core.dll, FSharp.Core.optdata and FSharp.Core.sigdata files must present in .out/product")

    let nuspec = NuGet.Spec25(fun meta -> 
        meta.Id <- productName
        meta.Version <- productVersion
        meta.Authors <- productAuthor
        meta.Description <- productDescription
        meta.ProjectUrl <- productHome
        meta.Summary <- productTitle
        meta.Tags <- productTags
    )

    nuspec.AddFiles(nugetFiles, "bin")
    nuspec.AddFiles(!!"tools/*")

    NuGet.Pack(nuspec, out, fun p -> 
        p.NoPackageAnalysis <- true
        p.NoDefaultExcludes <- true)
        |> ignore
)

"Package.Push" => (fun _ -> 
    let nupkg = (out / String.Format("{0}.{1}.nupkg", productName, productVersion)).AsFile()

    NuGet.Push(nupkg, fun p -> 
        p.AccessKey <- MyBuild.GetProp("NuGet.AccessKey")
        p.SourceUrl <- MyBuild.GetProp("NuGet.SourcePushUrl"))
)

"Package.Installer" => (fun _ ->
    let nupkg = (out / String.Format("{0}.{1}.nupkg", productName, productVersion)).AsFile()

    Files.Copy(nupkg, ~~"AnFake.Installer/AnFake.nupkg", true)
    
    MsBuild.BuildRelease(installer, out)
)

"Package.Alias" => (fun _ -> 
    let nuspec = NuGet.Spec25(fun meta -> 
        meta.Id <- "TeamBuild.ScriptTemplate"
        meta.Version <- productVersion
        meta.Authors <- productAuthor
        meta.Description <-             
            "Use eighther F# or C# script for your build definitions in Ms Team Build, " + 
            "forget about build process templates! " +
            "Integration with TFS 2012/2013 provided out-of-box.\n\n" +
            "This package is just an ALIAS for AnFake, which is installed as dependent one."
        meta.ProjectUrl <- productHome
        meta.Summary <- "Use eighther F# or C# script for your build definitions in Ms Team Build."
        meta.Tags <- productTags
    )    

    nuspec.Metadata.AddDependencies(productName, productVersion)

    nuspec.AddFiles(~~".AnFake" % "README.txt", "")
    
    let nupkg = NuGet.Pack(nuspec, out, fun p -> 
        p.NoPackageAnalysis <- true
        p.NoDefaultExcludes <- true)

    NuGet.Push(nupkg, fun p -> 
        p.AccessKey <- MyBuild.GetProp("NuGet.AccessKey")
        p.SourceUrl <- MyBuild.GetProp("NuGet.SourcePushUrl"))
)

"Package.TfsApi" => (fun _ -> 
    let src = ~~"[ProgramFilesX86]/Microsoft Visual Studio 11.0/Common7/IDE/ReferenceAssemblies"
    
    let ver = 
        System.Diagnostics.FileVersionInfo
            .GetVersionInfo((src / "v2.0/Microsoft.TeamFoundation.dll").Full)
            .ProductVersion
            .AsVersion()

    let nuspec = NuGet.Spec25(fun meta -> 
        meta.Id <- "Tfs2012.Api"
        meta.Version <- ver
        meta.Authors <- productAuthor
        meta.Description <- "Contains the reference assemblies for Microsoft Team Foundation 2012 client."
        meta.Summary <- "Team Foundation 2012 API"    
    )

    nuspec.AddFiles(src % "*/*.*", "ref-lib");
    
    let nupkg = NuGet.Pack(nuspec, out, fun p -> 
        p.NoPackageAnalysis <- true)

    NuGet.Push(nupkg, fun p -> 
        p.AccessKey <- MyBuild.GetProp("NuGet.AccessKey")
        p.SourceUrl <- MyBuild.GetProp("NuGet.SourcePushUrl"))
)

"SetApiVersion" => (fun _ ->
    let files =
        [
            "AnFake.Api/AnFake.Api.csproj", 
                [
                    @"<AssemblyName>AnFake\.Api\.v(\d+)</AssemblyName>"
                ]
            "AnFake.Integration.Tfs2012.Template/AnFake.Integration.Tfs2012.Template.csproj",
                [                    
                    @"<HintPath>\.\.\\\.AnFake\\AnFake.Api\.v(\d+)\.dll</HintPath>"
                ]
            "AnFake.Api.Pipeline/AnFake.Api.Pipeline.csproj", 
                [
                    @"<AssemblyName>AnFake\.Api\.Pipeline\.v(\d+)</AssemblyName>"
                ]            
        ]

    for (path, patterns) in files do
        let doc = path.AsFile().AsTextDoc()

        for pattern in patterns do
            doc.Replace(pattern, fun i v -> if i = 1 then apiVersion else null)
                |> ignore

        doc.Save()    
)

"SetXamlVersion" => (fun _ ->
    let files =
        [
            "AnFake.Integration.Tfs2012/AnFake.Integration.Tfs2012.csproj", 
                [
                    @"<AssemblyName>AnFake\.Integration\.Tfs2012\.v(\d+)</AssemblyName>"
                ]
            "AnFake.Integration.Tfs2012.Template/AnFake.Integration.Tfs2012.Template.csproj",
                [                    
                    @"<HintPath>\.\.\\\.AnFake\\AnFake.Integration\.Tfs2012\.v(\d+)\.dll</HintPath>"
                ]
            "AnFake.Integration.Tfs2012.Template/AnFakeTemplate.xaml",
                [
                    @"clr-namespace:AnFake\.Integration\.Tfs2012;assembly=AnFake\.Integration\.Tfs2012\.v(\d+)"
                ]
            "AnFake.Integration.Tfs2012.Template/AnFakePipelineTemplate.xaml",
                [
                    @"clr-namespace:AnFake\.Integration\.Tfs2012;assembly=AnFake\.Integration\.Tfs2012\.v(\d+)"
                ]
        ]

    for (path, patterns) in files do
        let doc = path.AsFile().AsTextDoc()

        for pattern in patterns do
            doc.Replace(pattern, fun i v -> if i = 1 then xamlVersion else null)
                |> ignore

        doc.Save()    
)

"Compile" <== ["EmbedAssemblyInfo"]

"Build" <== ["NuGetRestore"; "Compile"; "Custom.ZipHtmlSummary"; "Test.Unit"]

"Package" <== ["Package.Pack"; "Package.Installer"; "Package.Push"]

"Package.Installer" <== ["Package.Pack"]