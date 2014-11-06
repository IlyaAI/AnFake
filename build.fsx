#r "AnFake/bin/Debug/AnFake.Api.dll"
#r "AnFake/bin/Debug/AnFake.Core.dll"
#r "AnFake/bin/Debug/AnFake.Fsx.dll"

open AnFake.Core
open AnFake.Fsx.Dsl

let solution = "AnFake.sln".AsFile()

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

//"TargetC" => (fun _ ->
//    Logger.Debug "Target C"
//) &=> (fun _ ->
//    Logger.Debug "Failure C"
//) |=> (fun _ -> 
//    Logger.Debug "Final C"
//)

"Build" <== ["Compile"; "Test.Unit"]

//"Compile".AsTarget().Run();