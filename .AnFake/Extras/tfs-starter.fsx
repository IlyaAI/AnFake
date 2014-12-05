#r "../AnFake.Api.dll"
#r "../AnFake.Core.dll"
#r "../AnFake.Fsx.dll"
#r "../Plugins/AnFake.Plugins.Tfs2012.dll"

open System
open System.Linq
open AnFake.Api
open AnFake.Core
open AnFake.Fsx.Dsl
open AnFake.Plugins.Tfs2012

// Necessary for Clipboard accessing
//#r "System.Windows.Forms.dll"
//open System.Windows.Forms
////////////////////////////////////

let curDir = Folders.Current.Path;

"Build" => (fun _ ->
    Trace.Info "Usage: AnFake.Tf[.cmd] <command> [<param>] ..."
    Trace.Info "Supported commands:"
    Trace.Info " {Checkout|co} <server-path> [<local-path> [<workspace-name>]]"
    Trace.Info " {SyncLocal|syncl} [<local-path>]"
    Trace.Info " {Sync|sync} [<local-path>]"

    MyBuild.Failed "Command is missed."
)

"SetUpCollection" => (fun _ ->
    if not <| MyBuild.HasProp("Tfs.Uri") then        
        MyBuild.SetProp(
            "Tfs.Uri",
            UserInterop.Prompt(
                "TeamFoundation Collection Uri", 
                "Please, enter an URI of Team Foundation Projects Collection, e.g. https://tf-server:8080/my-collection.\n" + 
                "Alternatively you can provide URI as command line parameter: \"Tfs.Uri=<your-uri>\"")
        )
    
    Tfs.UseIt()

    MyBuild.SaveProp("Tfs.Uri")
)

"DropAnFake" => (fun _ ->
    if not <| MyBuild.HasProp("AnFake.TfsPath") then
        MyBuild.SetProp(
            "AnFake.TfsPath",
            UserInterop.Prompt(
                "TFS Path to AnFake", 
                "Please, enter a TFS path where AnFake should be stored.\n" + 
                "Alternatively you can provide path as command line parameter: \"AnFake.TfsPath=<tfs-path-to-anfake>\"")
        )

    let anFakeServerPath = MyBuild.GetProp("AnFake.TfsPath").AsServerPath()
    let anFakeLocalPath = ~~".AnFake"

    let dstPath = curDir / anFakeLocalPath;
    if not <| dstPath.AsFolder().Exists() then
        TfsWorkspace.SaveLocal(curDir)

        let wsFile = (curDir / TfsWorkspace.Defaults.WorkspaceFile).AsFile()
        wsFile.Append("{0}: {1}\n", anFakeServerPath, anFakeLocalPath)

        //TfsWorkspace.SyncLocal(curDir) |> ignore

        if not <| anFakeLocalPath.AsFolder().Exists() then
            let myself = ~~"[AnFake]" % "**\*"
            Files.Copy(myself, curDir / anFakeLocalPath)
            TfsWorkspace.PendAdd(curDir / anFakeLocalPath % "**\*")
    
    MyBuild.SaveProp("AnFake.TfsPath")
)

"DropAnFake" <== ["SetUpCollection"]