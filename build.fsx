#r ".AnFake/Bin/AnFake.Api.dll"
#r ".AnFake/Bin/AnFake.Core.dll"
#r ".AnFake/Bin/AnFake.Fsx.dll"

open System.Linq
open AnFake.Core
open AnFake.Fsx.Dsl

let solution = "AnFake.sln".AsFile()
let out = ".out".AsPath()
let outBin = out / "bin"
let outPkg = out / "pkg"

let tests = outBin.Spec %% "*.Test.dll"

"Clean" => (fun _ ->    
    let obj = !!!"*/obj"
    let bin = !!!"*/bin" - ".AnFake/Bin"

    Folders.Clean obj
    Folders.Clean bin
    Folders.Clean out
)

"Compile" => (fun _ ->    
    MsBuild.Build(solution, (fun p -> 
        p.Properties.Set
            [
                "Configuration", "Debug"
                "Platform", "Any CPU"
                "OutDir", outBin.Full                
            ]
        )) 
    |> ignore
)

"Test.Unit" => (fun _ -> 
    MsTest.Run(tests, fun p -> p.NoIsolation <- true) 
        |> ignore
)

"Package" => (fun _ -> 
    let bins = 
        out.Spec %% "bin/AnFake.exe"
        + "bin/AnFake.exe.config"
        + "bin/*.dll"
        + "bin/*.xml"

    let nuspec = NuGet.Spec25(fun meta -> 
        meta.Id <- "AnFake"
        meta.Version <- "1.0.0.0"
        meta.Authors <- "Ilya A. Ivanov"
        meta.Description <- "AnFake (Another F# Make) ..."
    )

    nuspec.Files <- bins
        .Select(fun f -> new NuSpec.v25.File(f.RelPath.Spec, "Bin"))
        .ToArray()

    NuGet.Pack(nuspec, out.AsFolder(), outPkg.AsFolder(), fun p -> p.NoPackageAnalysis <- true)
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

//"Compile".AsTarget().Run();