#r ".AnFake/AnFake.Api.dll"
#r ".AnFake/AnFake.Core.dll"
#r ".AnFake/AnFake.Fsx.dll"
#r ".AnFake/Plugins/AnFake.Plugins.Tfs2012.dll"

open System
open System.Linq
open AnFake.Api
open AnFake.Core
open AnFake.Fsx.Dsl
open AnFake.Plugins.Tfs2012

Tfs.UseIt()

let out = ~~".out"
let productOut = out / "product"
let pluginsOut = productOut / "Plugins"
let extrasOut = productOut / "Extras"
let testsOut = out / "tests"
let product = !!"AnFake/*.csproj"
let plugins = !!"AnFake.Plugins.Tfs2012/*.csproj"
let extras = ~~".AnFake/Extras" % "*"
let cmds = ~~".AnFake" % "*.cmd"
let tests = !!"*/*.Test.csproj"
let nugetFiles = 
    productOut % "AnFake.exe"
    + "AnFake.exe.config"
    + "*.cmd"
    + "*.dll"
    + "AnFake.*.xml"
    - "FSharp.Core.dll"
    + "Extras/*"
    + "Plugins/AnFake.Integration.Tfs2012.dll"
    + "Plugins/AnFake.Plugins.Tfs2012.dll"
let version = "0.9".AsVersion()

"Clean" => (fun _ ->    
    let obj = !!!"*/obj"
    let bin = !!!"*/bin"

    Folders.Clean obj
    Folders.Clean bin
    Folders.Clean out
)

"Compile" => (fun _ ->
    AssemblyInfo.EmbedTemporary(
        !!"*/Properties/AssemblyInfo.cs",
        fun p -> 
            p.Title <- "AnFake /Another F# Make/ runtime component"
            p.Product <- "AnFake"
            p.Description <- "AnFake: Another F# Make"
            p.Copyright <- String.Format("Ilya A. Ivanov {0}", DateTime.Now.Year)
            p.Version <- version
        ) |> ignore

    MsBuild.BuildRelease(product, productOut) |> ignore

    Files.Copy(cmds, productOut, true)

    MsBuild.BuildRelease(plugins, pluginsOut) |> ignore

    Files.Copy(extras, extrasOut, true)

    MsBuild.BuildRelease(tests, testsOut) |> ignore
)

"Test.Unit" => (fun _ -> 
    MsTest.Run(
        testsOut % "*.Test.dll",
        fun p -> p.NoIsolation <- true) 
        |> ignore
) |> skipErrors

"Package" => (fun _ -> 
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

"Build" <== ["Compile"; "Test.Unit"]