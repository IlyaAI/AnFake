#r "../Bin/AnFake.Api.dll"
#r "../Bin/AnFake.Core.dll"
#r "../Bin/AnFake.Fsx.dll"
#r "../Plugins/AnFake.Plugins.Tfs2012.dll"

open System
open System.Linq
open AnFake.Api
open AnFake.Core
open AnFake.Fsx.Dsl
open AnFake.Plugins.Tfs2012

let curDir = Folders.Current.Path;
let serviceNames = 
    [
        "release"
        "releases"
        "branch"
        "branches"
        "feature"
        "features"
    ]

Tfs.UseIt()

"Build" => (fun _ ->
    Logger.Debug "Usage: AnFake.Tf[.cmd] <command> [<param>] ..."
    Logger.Debug "Supported commands:"
    Logger.Debug " {Checkout|co} <server-path> [<local-path> [<workspace-name>]]"
    Logger.Debug " {SyncLocal|syncl} [<local-path>]"
    Logger.Debug " {Sync|sync} [<local-path>]"

    MyBuild.Failed "Command is missed."
)

"co" => (fun _ ->
    if not <| MyBuild.HasProp("Arg1") then
        MyBuild.Failed "Required parameter <server-path> is missed."

    let serverPath = MyBuild.GetProp("Arg1").AsServerPath();
    let productName = 
        serverPath
            .Split()
            .Reverse()
            .Skip(1)
            .Except(serviceNames, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault()

    let localPath = 
        if MyBuild.HasProp("Arg2") then
            curDir / MyBuild.GetProp("Arg2")
        else
            curDir / productName / serverPath.LastName

    let workspaceName = 
        TfsWorkspace.GenerateUniqueName(
            if MyBuild.HasProp("Arg3") then
                MyBuild.GetProp("Arg3")
            else
                String.Format("{0}.{1}", productName, localPath.LastName)
        )

    TfsWorkspace.Checkout(serverPath, localPath, workspaceName) |> ignore
)

"Checkout" <== ["co"]

"syncl" => (fun _ ->
    let localPath = 
        if MyBuild.HasProp("Arg1") then
            curDir / MyBuild.GetProp("Arg1");
        else
            curDir
    
    TfsWorkspace.SyncLocal(localPath) |> ignore
)

"SyncLocal" <== ["syncl"]

"Sync" => (fun _ ->
    let localPath = 
        if MyBuild.HasProp("Arg1") then
            curDir / MyBuild.GetProp("Arg1");
        else
            curDir
    
    TfsWorkspace.Sync(localPath) |> ignore
)