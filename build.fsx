#r ".AnFake/Bin/AnFake.Api.dll"
#r ".AnFake/Bin/AnFake.Core.dll"
#r ".AnFake/Bin/AnFake.Fsx.dll"
#r ".AnFake/Plugins/AnFake.Plugins.Tfs2012.dll"

open System.Linq
open AnFake.Core
open AnFake.Fsx.Dsl
open AnFake.Plugins.Tfs2012
open AnFake.Api

Tfs.UseIt()

let out = ~~".out"
let productOut = out / "product"
let testsOut = out / "tests"
let product = !!"AnFake/AnFake.csproj"
let tests = !!"*/*.Test.csproj"

"Clean" => (fun _ ->    
    let obj = !!!"*/obj"
    let bin = !!!"*/bin" - ".AnFake/Bin"

    Folders.Clean obj
    Folders.Clean bin
    Folders.Clean out
)

"Compile" => (fun _ ->    
    Tracer.Info "some info"

    MsBuild.BuildRelease(product, productOut) |> ignore

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

    let nuspec = NuGet.Spec25(fun meta -> 
        meta.Id <- "AnFake"
        meta.Version <- "1.0.0.0"
        meta.Authors <- "Ilya A. Ivanov"
        meta.Description <- "AnFake (Another F# Make) ..."
    )

    nuspec.Files <- bins
        .Select(fun f -> new NuSpec.v25.File(f.Path.Full, "Bin"))
        .ToArray()

    NuGet.Pack(nuspec, out.AsFolder(), out.AsFolder(), fun p -> 
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