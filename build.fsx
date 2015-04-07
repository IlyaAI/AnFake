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
    + "AnFake.Integration.Vs2012/*.csproj"
let extras = ~~".AnFake/Extras" % "*"
let cmds = ~~".AnFake" % "*.cmd"
let xaml = ~~"AnFake.Integration.Tfs2012.Template" % "*.xaml"
let buildTmpls = ~~".AnFake" % "*.tmpl.fsx" + "*.tmpl.csx"
let fsharp = 
    ~~"[ProgramFilesX86]/Reference Assemblies/Microsoft/FSharp/.NETFramework/v4.0/4.3.1.0" % "FSharp.Core.dll"
    + "FSharp.Core.optdata"
    + "FSharp.Core.sigdata"
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
    + ~~"packages/NuGet.CommandLine.2.8.3/tools" % "NuGet.exe"

let productName = "AnFake"
let productTitle = "AnFake: Another F# Make. Use eighther F# or C# script for your build definitions in Ms Team Build."
let productDescription = 
    "AnFake: Another F# Make. " + 
    "Use eighther F# or C# script for your build definitions in Ms Team Build, " + 
    "forget about build process templates! " +
    "Integration with TFS 2012/2013 provided out-of-box."
let productAuthor = "Ilya A. Ivanov"
let productVersion = "1.0.6".AsVersion()
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
    let obj = !!!"*/obj"
    let bin = !!!"*/bin"

    Folders.Clean obj
    Folders.Clean bin
    Folders.Clean out
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
    Files.Copy(fsharp, productOut, true)
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
                    @"<HintPath>\.\.\\\.AnFake\\Plugins\\AnFake.Integration\.Tfs2012\.v(\d+)\.dll</HintPath>"
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

        doc.Save()    
)

"Compile" <== ["EmbedAssemblyInfo"]

"Build" <== ["NuGetRestore"; "Compile"; "Custom.ZipHtmlSummary"; "Test.Unit"]

"Package" <== ["Package.Zip"; "Package.Pack"; "Package.Push"]