#r "../AnFake.Api.v1.dll"
#r "../AnFake.Core.dll"
#r "../AnFake.Fsx.dll"
#r "../AnFake.Plugins.Tfs2012.dll"
#load "tf-conv.fsx"

open System
open System.Linq
open AnFake.Api
open AnFake.Core
open AnFake.Fsx.Dsl
open AnFake.Plugins.Tfs2012
open TfsConvention

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

let printGetUsage () =
    Log.Info "{GetLatest|GetSpecific} [<version-spec>] [<local-path>]"
    Log.Info "  Updates TFS workspace using latest or specified version of '.workspace' file from source control and gets workspace items."
    Log.Info "    <version-spec>    Version specification in TFS style."
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

let plugInTfs () =
    if not <| MyBuild.HasProp("Tfs.Uri") then
        MyBuild.SetProp(
            "Tfs.Uri",
            UserInterop.Prompt(
                "TeamFoundation Collection Uri", 
                "Please, enter an URI of Team Foundation Projects Collection, e.g. 'https://tf-server:8080/my-collection'.\n" + 
                "Alternatively you can provide URI as command line parameter: \"Tfs.Uri=<your-uri>\"",
                fun uri ->
                    Tfs.CheckConnection(uri)
                )
        )
        MyBuild.SaveProp("Tfs.Uri")
    Tfs.PlugIn()

let restoreAnFake (localPath:FileSystemPath) =
    let packagesConfig = (localPath / ".nuget/packages.config").AsFile()
    if packagesConfig.Exists() then
        NuGet.Restore(
            packagesConfig,
            fun p -> 
                p.SolutionDirectory <- localPath
                p.OutputDirectory <- null)

"Help" => (fun _ ->
    Log.Info ""
    Log.Info "Usage: anf-tf[.cmd] <command> [<param>] ..."
    Log.Info "COMMANDS:"    
    printCheckoutUsage()
    printGetUsage()    
    printCheckinUsage()    

    MyBuild.Failed "Command is missed."
)

"Build" <== ["Help"]

"Checkout" => (fun _ ->
    plugInTfs()

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

    let productName = getProductNameByConvention(serverPath)
    let branchName = getBranchNameByConvention(serverPath)

    if productName = null && not <| MyBuild.HasProp("__2") && not <| MyBuild.HasProp("__3") then
        printCheckoutUsage()
        MyBuild.Failed("Unable to auto-detect product name. Please, specify <local-path> and <workspace-name> explicitly.")

    let localPath = 
        if MyBuild.HasProp("__2") then
            curDir / MyBuild.GetProp("__2")
        else
            needConfirmation <- true
            getLocalPathByConvention curDir productName branchName

    let workspaceName =
        if MyBuild.HasProp("__3") then
            MyBuild.GetProp("__3")
        else
            needConfirmation <- true
            getWorkspaceNameByConvention productName branchName

    if needConfirmation then
        let confirmed = 
            UserInterop.Confirm(
                String.Format("Checkout '{0}' to '{1}' under workspace '{2}'", serverPath, localPath, workspaceName)
            )
        if not confirmed then
            MyBuild.Failed("Operation cancelled.")
    
    TfsWorkspace.Checkout(serverPath, localPath, workspaceName)
    restoreAnFake(localPath)
)

"Checkout" ==> "co"

"GetLatest" => (fun _ ->
    plugInTfs()

    let localPath = 
        if MyBuild.HasProp("__1") then
            curDir / MyBuild.GetProp("__1");
        else
            curDir
    
    TfsWorkspace.Get(localPath)
    restoreAnFake(localPath)
)

"GetSpecific" => (fun _ ->
    plugInTfs()

    let versionSpec = MyBuild.GetProp("__1", "T")
    let localPath = 
        if MyBuild.HasProp("__2") then
            curDir / MyBuild.GetProp("__2");
        else
            curDir
    
    TfsWorkspace.Get(localPath, fun p -> (p.VersionSpec <- versionSpec))
    restoreAnFake(localPath)
)

"Checkin" => (fun _ ->
    plugInTfs()
    
    let mutable needConfirmation = false

    let serverPath = 
        if MyBuild.HasProp("__1") then
            MyBuild.GetProp("__1").AsServerPath()
        else
            printCheckinUsage()
            MyBuild.Failed("Required parameter <server-path> is missed.")
            null    

    let productName = getProductNameByConvention(serverPath)
    let branchName = getBranchNameByConvention(serverPath)

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
            getWorkspaceNameByConvention productName branchName

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

    Trace.InfoFormat("Folder '{0}' is ready to check-in. Carefully review all pending changes in '{1}' workspace and commit them.", localPath, workspaceName)
)

"Checkin" ==> "ci"

"ReCache" => (fun _ ->
    plugInTfs()

    Folders.Delete("[LocalApplicationData]/Microsoft/Team Foundation/4.0/Cache")
    Trace.Info("Hint: if delete operation failed ensure all instances of Visual Studio are closed.")

    TfsWorkspace.UpdateInfoCache()
)
