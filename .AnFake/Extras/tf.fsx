#r "../AnFake.Api.v1.dll"
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

// Get text from Clipboard ASAP because some targets have interactive points and 
// user can re-use clipboard to provide requested values, so we've lost initial text.
let clipboardText =
    if Clipboard.ContainsText() then
        Clipboard.GetText()
    else
        null
//

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

let anfakeFiles =
    ~~"[AnFake]" % "**/*" 
    - "anf.cmd" 
    - "*.fsx" 
    - "*.csx"
    - "*.nupkg"
    - "Plugins/*.xaml"

let getProductName (serverPath: ServerPath) =
    serverPath
        .Split()
        .Reverse()
        .Skip(1)
        .Except(serviceNames, StringComparer.OrdinalIgnoreCase)
        .FirstOrDefault()

let printInstallXamlUsage () =
    Log.Info "InstallXaml <build-process-templates-path> [<build-process-custom-activities-path>]"
    Log.Info "  Creates TFS workspace and pends AnFakeTemplate.xaml and related DLLs for addition."
    Log.Info "    <build-process-templates-path>         Server path to BuildProcessTemplates folder."
    Log.Info "    <build-process-custom-activities-path> Server path to CustomActivities folder."
    Log.Info "                                           If omitted then will be evaluated as <build-process-templates-path> / 'CustomActivities'."
    Log.Info ""

let printGetStartedUsage () =
    Log.Info "{GetStarted|gs} [<local-path>]"
    Log.Info "  Prepares module to start using AnFake."
    Log.Info "    <local-path>      Local path of module which is getting started."
    Log.Info "                      If omitted current directory is used."    
    Log.Info ""

let printCheckoutUsage () =
    Log.Info "{Checkout|co} [<server-path> [<local-path> [<workspace-name>]]]"
    Log.Info "  Creates TFS workspace from '.workspace' file and downloads specified branch into local folder."
    Log.Info "    <server-path>     Full TFS path of branch to be checked out."
    Log.Info "                      If omitted AnFake tries to use value from clipboard."
    Log.Info "    <local-path>      Local path of branch to be checked out."
    Log.Info "                      If omitted AnFake tries to evaluate it by convention."
    Log.Info "    <workspace-name>  Workspace name to be created for branch to be checked out."
    Log.Info "                      If omitted AnFake tries to evaluate it by convention."
    Log.Info ""

let printSyncUsage () =
    Log.Info "{Sync|sync} [<local-path>]"
    Log.Info "  Updates TFS workspace using latest version of '.workspace' file from source control and gets latest items."
    Log.Info "    <local-path>      Local path of '.workspace' file."
    Log.Info "                      If omitted AnFake tries to locate '.workspace' file in current directory."
    Log.Info ""

let printSyncLocalUsage () =
    Log.Info "{SyncLocal|syncl} [<local-path>]"
    Log.Info "  Updates TFS workspace using local '.workspace' file and gets latest items from source control."
    Log.Info "    <local-path>      Local path of '.workspace' file."
    Log.Info "                      If omitted AnFake tries to locate '.workspace' file in current directory."
    Log.Info ""

let printCheckinUsage () =
    Log.Info "{Checkin|ci} <server-path> [<local-path> [<workspace-name>]]"
    Log.Info "  Creates TFS workspace with single mapping '<server-path>: <local-path>' and pends all source files for addition."
    Log.Info "    <server-path>     Full TFS path of branch to be checked in."    
    Log.Info "    <local-path>      Local path of branch to be checked in."
    Log.Info "                      If omitted current directory is used."
    Log.Info "    <workspace-name>  Workspace name to be created for branch to be checked out."
    Log.Info "                      If omitted AnFake tries to evaluate it by convention."
    Log.Info ""

Tfs.PlugInDeferred()

"Build" => (fun _ ->
    Log.Info ""
    Log.Info "Usage: anf-tf[.cmd] <command> [<param>] ..."
    Log.Info "COMMANDS:"
    printInstallXamlUsage()
    printGetStartedUsage()
    printCheckoutUsage()
    printSyncUsage()
    printSyncLocalUsage()
    printCheckinUsage()    

    MyBuild.Failed "Command is missed."
)

"SetUpTeamProjects" => (fun _ ->
    if not <| MyBuild.HasProp("Tfs.Uri") then        
        MyBuild.SetProp(
            "Tfs.Uri",
            UserInterop.Prompt(
                "TeamFoundation Collection Uri", 
                "Please, enter an URI of Team Foundation Projects Collection, e.g. 'https://tf-server:8080/my-collection'.\n" + 
                "Alternatively you can provide URI as command line parameter: \"Tfs.Uri=<your-uri>\"")
        )
    
    Tfs.PlugIn()

    MyBuild.SaveProp("Tfs.Uri")
)

"InstallXaml" => (fun _ ->
    if not <| MyBuild.HasProp("__1") then
        printInstallXamlUsage()
        MyBuild.Failed("Required parameter <build-process-templates-path> is missed.")

    let processTmplPath = MyBuild.GetProp("__1").AsServerPath()
    let customActivitiesPath =
        if MyBuild.HasProp("__2") then
            MyBuild.GetProp("__2").AsServerPath()
        else
            processTmplPath / "CustomActivities"

    let workspaceName = "AnFake.Xaml".MakeUnique()
    
    let localPath = ~~"[Temp]" / workspaceName;
    let activitiesLocalPath = localPath / "CustomActivities"
    Folders.Clean(localPath)
    Folders.Create(activitiesLocalPath)

    let wsFile = (localPath / TfsWorkspace.Defaults.WorkspaceFile).AsFile()
    let wsDef = wsFile.AsTextDoc();    
    wsDef.LastLine().InsertAfter("{0}: CustomActivities", customActivitiesPath)    
    wsDef.Save()

    TfsWorkspace.Create(processTmplPath, localPath, workspaceName)
    
    Files.Copy(~~"[AnFakePlugins]" % "AnFakeTemplate.*.xaml", localPath, true)
    Files.Copy(~~"[AnFake]" % "AnFake.Api.*.dll", activitiesLocalPath, true)
    Files.Copy(~~"[AnFakePlugins]" % "AnFake.Integration.Tfs2012.*.dll", activitiesLocalPath, true)    

    TfsWorkspace.PendAdd(localPath % "AnFakeTemplate.*.xaml")
    TfsWorkspace.PendAdd(
        activitiesLocalPath % "AnFake.Api.*.dll"
        + "AnFake.Integration.Tfs2012.*.dll"
    )

    Log.InfoFormat("AnFakeTemplate is ready to be checked-in. Carefully review all pending changes in '{0}' workspace and commit them.", workspaceName)
)

"SetUpTeamProjects" ==> "InstallXaml"

"GetStarted" => (fun _ ->
    if not <| MyBuild.HasProp("AnFake.TfsPath") then
        MyBuild.SetProp(
            "AnFake.TfsPath",
            UserInterop.Prompt(
                "TFS Path to AnFake", 
                "Please, enter a TFS path where AnFake should be stored, e.g. '$/Infrastructure' (do not include 'AnFake' itself, this will be added automatically).\n" + 
                "Alternatively, you can provide path as command line parameter: \"AnFake.TfsPath=<tfs-path-to-anfake>\"")
        )

    if not <| MyBuild.HasProp("AnFake.SettingsTfsPath") then
        MyBuild.SetProp(
            "AnFake.SettingsTfsPath",
            UserInterop.Prompt(
                "TFS Path to AnFake.settings.json", 
                "Please, enter a TFS path where AnFake infrastructure settings should be stored, e.g. '$/Infrastructure/Settings/current'.\n" + 
                "Alternatively, you can provide path as command line parameter: \"AnFake.SettingsTfsPath=<tfs-path-to-anfake-settings>\"")
        )

    let dstPath = 
        if MyBuild.HasProp("__1") then
            curDir / MyBuild.GetProp("__1")
        else
            curDir

    let anfDstPath = dstPath  / ".AnFake";

    if not <| anfDstPath.AsFolder().Exists() then
        TfsWorkspace.SaveLocal(dstPath)

        let myselfServerPath = 
            MyBuild.GetProp("AnFake.TfsPath").AsServerPath() 
            / "AnFake" 
            / MyBuild.Current.AnFakeVersion.ToString()

        let settingsServerPath = 
            MyBuild.GetProp("AnFake.SettingsTfsPath").AsServerPath()

        let settingsFile = (dstPath / "AnFake.settings.json").AsFile()
        
        let wsFile = (dstPath / TfsWorkspace.Defaults.WorkspaceFile).AsFile()
        let wsDef = wsFile.AsTextDoc();

        if not <| wsDef.HasLine("\\.AnFake") then
            wsDef.LastLine().InsertAfter("{0}: .AnFake", myselfServerPath)
            wsDef.LastLine().InsertAfter("{0}/anf.cmd: anf.cmd", myselfServerPath)
            wsDef.LastLine().InsertAfter("{0}/{1}: {1}", settingsServerPath, settingsFile.Name)
            wsDef.Save()
        else
            MyBuild.Failed("Workspace already contains AnFake mapping.")

        TfsWorkspace.SyncLocal(dstPath)
        TfsWorkspace.PendAdd([wsFile])

        if not <| anfDstPath.AsFolder().Exists() then
            Files.Copy(anfakeFiles, anfDstPath)
            Files.Copy(~~"[AnFake]/anf.cmd", dstPath / "anf.cmd")
            TfsWorkspace.PendAdd(anfDstPath % "**/*")
            TfsWorkspace.PendAdd(dstPath % "anf.cmd")
        
        if not <| settingsFile.Exists() then
            "{}".AsTextDoc().SaveTo(settingsFile, new Text.UTF8Encoding(false))
            TfsWorkspace.PendAdd([settingsFile])

        let buildFsx = (dstPath / "build.fsx").AsFile()
        if not <| buildFsx.Exists() then            
            Files.Copy(~~"[AnFake]/build.tmpl.fsx", buildFsx.Path)
            TfsWorkspace.PendAdd([buildFsx])
    
        MyBuild.SaveProp("AnFake.TfsPath")
        MyBuild.SaveProp("AnFake.SettingsTfsPath")

        Log.InfoFormat("Folder '{0}' is ready to use AnFake. Carefully review all pending changes before commit!", dstPath)
    else
        MyBuild.Failed("AnFake already exists: '{0}'", dstPath)   
)

"SetUpTeamProjects" ==> "GetStarted" ==> "gs"

"Upgrade" => (fun _ ->
    let dstPath = 
        if MyBuild.HasProp("__1") then
            curDir / MyBuild.GetProp("__1")
        else
            curDir

    let anfDstPath = dstPath  / ".AnFake";

    if not <| anfDstPath.AsFolder().Exists() then
        MyBuild.Failed("AnFake doesn't exists. Use 'GetStarted' for initial setup.")

    Folders.Delete(anfDstPath)

    let myselfServerPath = 
        MyBuild.GetProp("AnFake.TfsPath").AsServerPath() 
        / "AnFake"
        / MyBuild.Current.AnFakeVersion.ToString()

    let wsFile = (dstPath / TfsWorkspace.Defaults.WorkspaceFile).AsFile()
    let wsDef = wsFile.AsTextDoc();

    wsDef.MatchedLine(@"/AnFake/\d+\.\d+(?:\.\d+)?[ ]*:[ ]*\.AnFake")
        .Replace("{0}: .AnFake", myselfServerPath)

    wsDef.MatchedLine(@"/AnFake/\d+\.\d+(?:\.\d+)?/anf\.cmd[ ]*:[ ]*anf\.cmd")
        .Replace("{0}/anf.cmd: anf.cmd", myselfServerPath)

    wsDef.Save()
    
    TfsWorkspace.SyncLocal(dstPath)
    
    if not <| (anfDstPath / "AnFake.exe").AsFile().Exists() then
        Files.Copy(anfakeFiles, anfDstPath)
        Files.Copy(~~"[AnFake]/anf.cmd", dstPath / "anf.cmd", true)
        TfsWorkspace.PendAdd(anfDstPath % "**/*")
        TfsWorkspace.PendAdd(dstPath % "anf.cmd")

    Log.InfoFormat("Folder '{0}' is upgraded to use AnFake v.{1}. Carefully review all pending changes and commit them.", dstPath, myselfServerPath.LastName)
)

"SetUpTeamProjects" ==> "Upgrade"

"Checkout" => (fun _ ->
    let mutable needConfirmation = false

    let serverPath =
        if not <| MyBuild.HasProp("__1") then
            if clipboardText <> null then
                if clipboardText.StartsWith("$") && not <| clipboardText.Contains("\n") && not <| clipboardText.Contains("\r") then
                    needConfirmation <- true
                    clipboardText.AsServerPath()
                else
                    null
            else
                null                
        else
            MyBuild.GetProp("__1").AsServerPath()

    if serverPath = null then
        printCheckoutUsage()
        MyBuild.Failed("Required parameter <server-path> is missed.\nHint: you can pass it via clipboard, simply do 'Copy' on desired value.")

    UserInterop.Highlight(
        "TFS Path", 
        serverPath.Spec, 
        "If you see something strange above then you probably didn't copy TFS path to clipboard.\n" +
        "Simply select project root in Source Control Explorer, click on 'Source location' and press Ctrl+C then re-run command."
    )

    let productName = getProductName(serverPath)

    if productName = null && not <| MyBuild.HasProp("__2") && not <| MyBuild.HasProp("__3") then
        printCheckoutUsage()
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

"Checkin" => (fun _ ->
    let mutable needConfirmation = false

    let serverPath = 
        if MyBuild.HasProp("__1") then
            MyBuild.GetProp("__1").AsServerPath()
        else
            printCheckinUsage()
            MyBuild.Failed("Required parameter <server-path> is missed.")
            null    

    let productName = getProductName(serverPath)
    if productName = null && not <| MyBuild.HasProp("__2") && not <| MyBuild.HasProp("__3") then
        printCheckinUsage()
        MyBuild.Failed("Unable to auto-detect product name. Please, specify <local-path> and <workspace-name> explicitly.")

    let localPath = 
        if MyBuild.HasProp("__2") then
            curDir / MyBuild.GetProp("__2")
        else            
            curDir

    let workspaceName =
        if MyBuild.HasProp("__3") then
            MyBuild.GetProp("__3")
        else
            needConfirmation <- true
            NameGen.Generate(
                String.Format("{0}.{1}", productName, serverPath.LastName),
                TfsWorkspace.UniqueName
            )

    if needConfirmation then
        let confirmed = 
            UserInterop.Confirm(
                String.Format("Checkin \"{0}\" \"{1}\" \"{2}\"", serverPath, localPath, workspaceName)
            )
        if not confirmed then
            MyBuild.Failed("Operation cancelled.")
    
    TfsWorkspace.Create(serverPath, localPath, workspaceName)

    let srcFiles =
        localPath % "**/*"
        - "**/bin/**/*"
        - "**/obj/**/*"
        - "*.suo"
        - "*.log"
        - "*.trace.jsx"

    TfsWorkspace.PendAdd(srcFiles)

    Log.InfoFormat("Folder '{0}' is ready to check-in. Carefully review all pending changes in '{1}' workspace and commit them.", localPath, workspaceName)
)

"SetUpTeamProjects" ==> "Checkin" ==> "ci"

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

"ReCache" => (fun _ ->
    Folders.Delete("[LocalApplicationData]/Microsoft/Team Foundation/4.0/Cache")
    Log.Info("Hint: if delete operation failed ensure all instances of Visual Studio are closed.")

    TfsWorkspace.UpdateInfoCache()
)

"SetUpTeamProjects" ==> "ReCache" ==> "rc"