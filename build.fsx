#r ".AnFake/Bin/AnFake.Api.dll"
#r ".AnFake/Bin/AnFake.Core.dll"
#r ".AnFake/Bin/AnFake.Fsx.dll"
#r ".AnFake/Plugins/AnFake.Plugins.Tfs2012.dll"

open System
open System.Linq
open AnFake.Core
open AnFake.Fsx.Dsl
open AnFake.Plugins.Tfs2012
open AnFake.Api

//Tfs.UseIt()

let out = ~~".out"
let productOut = out / "product"
let pluginsOut = out / "plugins"
let testsOut = out / "tests"
let product = !!"AnFake/*.csproj"
let plugins = !!"AnFake.Plugins.Tfs2012/*.csproj"
let tests = !!"*/*.Test.csproj"

"Clean" => (fun _ ->    
    let obj = !!!"*/obj"
    let bin = !!!"*/bin" - ".AnFake/Bin"

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
        ) |> ignore

    MsBuild.BuildRelease(product, productOut) |> ignore

    MsBuild.BuildRelease(plugins, pluginsOut) |> ignore

    MsBuild.BuildRelease(tests, testsOut) |> ignore
)

"Test.Unit" => (fun _ -> 
    MsTest.Run(
        testsOut % "*.Test.dll",
        fun p -> p.NoIsolation <- true) 
        |> ignore
) |> skipErrors

"Package" => (fun _ -> 
    let bins = 
        productOut % "AnFake.exe"
        + "AnFake.exe.config"
        + "*.dll"
        + "*.xml"
    let plugins = 
        pluginsOut % "*.dll" + "*.xml"

    let nuspec = NuGet.Spec25(fun meta -> 
        meta.Id <- "AnFake"
        meta.Version <- "1.0.0.0"
        meta.Authors <- "Ilya A. Ivanov"
        meta.Description <- "AnFake (Another F# Make) ..."
    )

    nuspec.AddFiles(bins, "Bin")
    nuspec.AddFiles(plugins, "Plugins")

    NuGet.Pack(nuspec, out, fun p -> 
        p.NoPackageAnalysis <- true
        p.NoDefaultExcludes <- true)
        |> ignore
)

//"TargetC" => (fun _ ->
//    Logger.Debug "Target C"
//) &=> (fun _ ->
//    Logger.Debug "Failure C"
//) |=> (fun _ -> 
//    Logger.Debug "Final C"
//)

"Build" <== ["Compile"; "Test.Unit"]