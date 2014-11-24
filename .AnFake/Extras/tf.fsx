#r "../Bin/AnFake.Api.dll"
#r "../Bin/AnFake.Core.dll"
#r "../Bin/AnFake.Fsx.dll"
#r "../Plugins/AnFake.Plugins.Tfs2012.dll"

open System
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

"co" => (fun _ ->
    let serverPath = MyBuild.GetProp("Arg1").AsServerPath();
    let branchName = serverPath.LastName
    let productName = serverPath.Parent.LastName
    
    let localPath = 
        if MyBuild.HasProp("Arg2") then
            curDir / MyBuild.GetProp("Arg2")
        else
            curDir / productName / branchName

    let workspaceName = 
        TfsWorkspace.GenerateUniqueName(
            if MyBuild.HasProp("Arg3") then
                MyBuild.GetProp("Arg3")
            else
                String.Format("{0}.{1}", productName, branchName)
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