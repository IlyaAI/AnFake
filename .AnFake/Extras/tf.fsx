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
#r "System.Windows.Forms.dll"
open System.Windows.Forms
////////////////////////////////////

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

"Build" => (fun _ ->
    Trace.Info "Usage: AnFake.Tf[.cmd] <command> [<param>] ..."
    Trace.Info "Supported commands:"
    Trace.Info " {Checkout|co} <server-path> [<local-path> [<workspace-name>]]"
    Trace.Info " {SyncLocal|syncl} [<local-path>]"
    Trace.Info " {Sync|sync} [<local-path>]"

    MyBuild.Failed "Command is missed."
)

"SetUpTeamProjects" => (fun _ ->
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

"GetStarted" => (fun _ ->
    if not <| MyBuild.HasProp("AnFake.TfsPath") then
        MyBuild.SetProp(
            "AnFake.TfsPath",
            UserInterop.Prompt(
                "TFS Path to AnFake", 
                "Please, enter a TFS path where AnFake should be stored.\n" + 
                "Alternatively, you can provide path as command line parameter: \"AnFake.TfsPath=<tfs-path-to-anfake>\"")
        )

    let dstPath = 
        if MyBuild.HasProp("__1") then
            curDir / MyBuild.GetProp("__1")
        else
            curDir

    let anfDstPath = dstPath  / ".AnFake";

    if not <| anfDstPath.AsFolder().Exists() then
        TfsWorkspace.SaveLocal(dstPath)

        let myselfServerPath = MyBuild.GetProp("AnFake.TfsPath").AsServerPath()        
        let wsFile = (dstPath / TfsWorkspace.Defaults.WorkspaceFile).AsFile()
        let wsDef = wsFile.AsTextDoc();

        if not <| wsDef.HasLine("\\.AnFake") then
            wsDef.LastLine().InsertAfter("{0}: .AnFake", myselfServerPath)
            wsDef.Save()
        else
            MyBuild.Failed("Workspace already contains AnFake mapping.")
                
        TfsWorkspace.SyncLocal(dstPath)
        TfsWorkspace.PendAdd([wsFile])

        if not <| anfDstPath.AsFolder().Exists() then
            let myself = ~~"[AnFake]" % "**\*"
            Files.Copy(myself, anfDstPath)
            TfsWorkspace.PendAdd(anfDstPath % "**\*")
    
        MyBuild.SaveProp("AnFake.TfsPath")

        Trace.InfoFormat("Folder '{0}' is ready to use AnFake. Carefully review all pended chages before commit!", dstPath)
    else
        MyBuild.Failed("AnFake already exists: '{0}'", dstPath)   
)

"SetUpTeamProjects" ==> "GetStarted" ==> "gs"

"Checkout" => (fun _ ->
    let mutable needConfirmation = false

    let serverPath =
        if not <| MyBuild.HasProp("__1") then
            if Clipboard.ContainsText() then                
                let text = Clipboard.GetText()
                if text.StartsWith("$") && not <| text.Contains("\n") && not <| text.Contains("\r") then
                    needConfirmation <- true
                    text.AsServerPath()
                else
                    null
            else
                null                
        else
            MyBuild.GetProp("__1").AsServerPath()

    if serverPath = null then
        MyBuild.Failed("Required parameter <server-path> is missed.\nHint: you can pass it via clipboard, simply do 'Copy' on desired value.")

    UserInterop.Highlight(
        "TFS Path", 
        serverPath.Spec, 
        "If you see something strange above then you probably didn't copy TFS path to clipboard.\n" +
        "Simply select project root in Source Control Explorer, click on 'Source location' and press Ctrl+C then re-run command."
    )

    let productName = 
        serverPath
            .Split()
            .Reverse()
            .Skip(1)
            .Except(serviceNames, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault()

    if productName = null && not <| MyBuild.HasProp("__2") && not <| MyBuild.HasProp("__3") then
        MyBuild.Failed("Unable to auto-detect product name. Please, specify <local-path> and <workspace-name> explicitly.")

    let localPath = 
        if MyBuild.HasProp("__2") then
            curDir / MyBuild.GetProp("__2")
        else
            needConfirmation <- true
            curDir / productName / NameGen.Generate(
                serverPath.LastName,
                fun n -> (curDir / productName / n).AsFolder().IsEmpty()
            )

    let workspaceName =
        if MyBuild.HasProp("__3") then
            MyBuild.GetProp("__3")
        else
            needConfirmation <- true
            NameGen.Generate(
                String.Format("{0}.{1}", productName, localPath.LastName),
                TfsWorkspace.UniqueName
            )

    if needConfirmation then
        let confirmed = 
            UserInterop.Confirm(
                String.Format("Checkout \"{0}\" \"{1}\" \"{2}\"", serverPath, localPath, workspaceName)
            )
        if not confirmed then
            MyBuild.Failed("Operation cancelled.")
    
    TfsWorkspace.Checkout(serverPath, localPath, workspaceName)
)

"SetUpTeamProjects" ==> "Checkout" ==> "co"

"SyncLocal" => (fun _ ->
    let localPath = 
        if MyBuild.HasProp("__1") then
            curDir / MyBuild.GetProp("__1");
        else
            curDir
    
    TfsWorkspace.SyncLocal(localPath)
)

"SetUpTeamProjects" ==> "SyncLocal" ==> "syncl"

"Sync" => (fun _ ->
    let localPath = 
        if MyBuild.HasProp("__1") then
            curDir / MyBuild.GetProp("__1");
        else
            curDir
    
    TfsWorkspace.Sync(localPath)
)

"SetUpTeamProjects" ==> "Sync"
