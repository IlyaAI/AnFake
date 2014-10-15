#r "bin/Debug/AnFake.Api.dll"
#r "bin/Debug/AnFake.Core.dll"
#r "bin/Debug/AnFake.Fsx.dll"

open AnFake.Core
open AnFake.Fsx.Dsl

let path = ~~"path" / "sub-dir"
let fileset = !!"path/*.doc" + "*.txt"
let folderset = !!!"path/**"
let filesetWithBase = "path" %% "*.txt"

Logger.Debug path.Full

"TargetA" => (fun _ -> 
    Logger.Debug "Target A"
)

"TargetB" => (fun _ ->
    Logger.Debug "Target B"
) |=> (fun _ -> 
    Logger.Debug "Final B"
)

"TargetC" => (fun _ ->
    Logger.Debug "Target C"
) &=> (fun _ ->
    Logger.Debug "Failure C"
) |=> (fun _ -> 
    Logger.Debug "Final C"
)

"TargetD" => (fun _ ->
    Logger.Debug "Target D"
    failwith "ERROR"
)

"TargetB" <== ["TargetA"; "TargetC"; "TargetD"]
"TargetC" <== ["TargetD"];

"TargetB".AsTarget().Run();