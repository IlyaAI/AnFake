#r "AnFake/bin/Debug/AnFake.Api.dll"
#r "AnFake/bin/Debug/AnFake.Core.dll"
#r "AnFake/bin/Debug/AnFake.Fsx.dll"

open System.Linq
open AnFake.Core
open AnFake.Fsx.Dsl

let solution = "AnFake.sln".AsFile()
let out = ".out".AsPath()

let tests = 
    !!"AnFake.Api.Test/bin/Debug/AnFake.Api.Test.dll" 
    + "AnFake.Core.Test/bin/Debug/AnFake.Core.Test.dll"

//let fileset = !!"path/*.doc" + "*.txt"
//let folderset = !!!"path/**"
//let filesetWithBase = "path" %% "*.txt"

"Compile" => (fun _ ->    
    MsBuild.Build(solution, (fun p -> 
        p.Properties.Set
            [
                "Configuration", "Debug"
                "Platform", "Any CPU"        
            ]
        )) 
    |> ignore
)

"Test.Unit" => (fun _ -> 
    for t in tests do
        Logger.Debug t.RelPath

    MsTest.Run(tests, fun p -> p.NoIsolation <- true) 
        |> ignore
)

"Package" => (fun _ -> 
    let bins = 
        "AnFake/bin/Debug" %% "*.exe"
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
        .ToArray();    

    NuGet.Pack(nuspec, out.AsFolder(), fun p -> p.NoPackageAnalysis <- true)
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