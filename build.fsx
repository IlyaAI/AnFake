#r ".AnFake/AnFake.Api.dll"
#r ".AnFake/AnFake.Core.dll"
#r ".AnFake/AnFake.Fsx.dll"
#r ".AnFake/Plugins/AnFake.Plugins.Tfs2012.dll"
#r ".AnFake/Plugins/AnFake.Plugins.HtmlSummary.dll"

open System
open System.Linq
open AnFake.Api
open AnFake.Core
open AnFake.Fsx.Dsl
open AnFake.Plugins.Tfs2012
open AnFake.Plugins.HtmlSummary

//Tfs.UseIt()
HtmlSummary.UseIt()

let out = ~~".out"
let productOut = out / "product"
let pluginsOut = productOut / "Plugins"
let extrasOut = productOut / "Extras"
let testsOut = out / "tests"
let product = !!"AnFake/*.csproj"
let plugins = 
    !!"AnFake.Plugins.Tfs2012/*.csproj"
    + "AnFake.Plugins.HtmlSummary/*.csproj"
let extras = ~~".AnFake/Extras" % "*"
let cmds = ~~".AnFake" % "*.cmd"
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
    + "AnFake.*.xml"
    + "FSharp.Core.optdata"
    + "FSharp.Core.sigdata"
    + "Extras/*"
    + "Plugins/AnFake.Integration.Tfs2012.dll"
    + "Plugins/AnFake.Plugins.Tfs2012.dll"
    + "Plugins/AnFake.Plugins.HtmlSummary.dll"
    + "Plugins/AnFake.Plugins.HtmlSummary.zip"
let version = "0.9".AsVersion()

"Clean" => (fun _ ->    
    let obj = !!!"*/obj"
    let bin = !!!"*/bin"

    Folders.Clean obj
    Folders.Clean bin
    Folders.Clean out
)

"EmbedAssemblyInfo" => (fun _ ->
    AssemblyInfo.Embed(
        !!"*/Properties/AssemblyInfo.cs",
        fun p -> 
            p.Title <- "AnFake /Another F# Make/ runtime component"
            p.Product <- "AnFake"
            p.Description <- "AnFake: Another F# Make"
            p.Copyright <- String.Format("Ilya A. Ivanov {0}", DateTime.Now.Year)
            p.Version <- version
        ) |> ignore    
)

"Compile" => (fun _ ->
    MsBuild.BuildRelease(product, productOut)

    Files.Copy(cmds, productOut, true)
    Files.Copy(fsharp, productOut, true)

    MsBuild.BuildRelease(plugins, pluginsOut)

    Files.Copy(extras, extrasOut, true)

    MsBuild.BuildRelease(tests, testsOut)
)

"Custom.ZipHtmlSummary" => (fun _ ->
    let htmlSummary = 
        ~~"AnFake.Plugins.HtmlSummary/Html" % "**/*"
        - "build.summary.js"

    let zip = pluginsOut / "AnFake.Plugins.HtmlSummary.zip"

    Zip.Pack(htmlSummary, zip)
    Files.Copy(zip, ~~".AnFake/Plugins" / zip.LastName, true)
)

"Test.Unit" => (fun _ -> 
    MsTest.Run(
        testsOut % "*.Test.dll",
        fun p -> p.NoIsolation <- true)
) |> skipErrors

"Package" => (fun _ -> 
    let fsharpCore = 
        productOut % "FSharp.Core.dll"
        + "FSharp.Core.optdata"
        + "FSharp.Core.sigdata"

    if fsharpCore.Count() <> 3 then
        MyBuild.Failed("There are FSharp.Core.dll, FSharp.Core.optdata and FSharp.Core.sigdata files must present in .out/product")

    let nuspec = NuGet.Spec25(fun meta -> 
        meta.Id <- "AnFake"
        meta.Version <- version
        meta.Authors <- "Ilya A. Ivanov"
        meta.Description <- "AnFake: Another F# Make"
    )

    nuspec.AddFiles(nugetFiles, "")

    NuGet.Pack(nuspec, out, fun p -> 
        p.NoPackageAnalysis <- true
        p.NoDefaultExcludes <- true)
        |> ignore
)

"Compile" <== ["EmbedAssemblyInfo"]

"Build" <== ["Compile"; "Custom.ZipHtmlSummary"; "Test.Unit"]