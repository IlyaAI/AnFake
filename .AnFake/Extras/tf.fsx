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
    let mutable needConfirmation = false

    let serverPath =
        if not <| MyBuild.HasProp("Arg1") then
            if not <| Clipboard.ContainsText() then
                MyBuild.Failed("Required parameter <server-path> is missed.\nHint: you can pass it via clipboard, simply do 'Copy' on desired value.")
                null
            else
                needConfirmation <- true
                Clipboard.GetText().AsServerPath()                
        else
            MyBuild.GetProp("Arg1").AsServerPath()

    Console.ForegroundColor <- ConsoleColor.White
    Console.WriteLine()
    Console.WriteLine("TFS Path: {0}", serverPath)
    
    Console.ForegroundColor <- ConsoleColor.Gray
    Console.WriteLine("If you see something strange above then you probably didn't copy TFS path to clipboard.")
    Console.WriteLine("Simply select project root in Source Control Explorer, click on 'Source location' and press Ctrl+C then re-run command.")

    let productName = 
        serverPath
            .Split()
            .Reverse()
            .Skip(1)
            .Except(serviceNames, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault()

    if productName = null && not <| MyBuild.HasProp("Arg2") && not <| MyBuild.HasProp("Arg3") then
        MyBuild.Failed("Unable to auto-detect product name. Please, specify <local-path> and <workspace-name> explicitly.")

    let localPath = 
        if MyBuild.HasProp("Arg2") then
            curDir / MyBuild.GetProp("Arg2")
        else
            needConfirmation <- true
            curDir / productName / NameGen.Generate(
                serverPath.LastName,
                fun n -> (curDir / productName / n).AsFolder().IsEmpty()
            )

    let workspaceName =
        if MyBuild.HasProp("Arg3") then
            MyBuild.GetProp("Arg3")
        else
            needConfirmation <- true
            NameGen.Generate(
                String.Format("{0}.{1}", productName, localPath.LastName),
                TfsWorkspace.UniqueName
            )

    if needConfirmation then
        Console.ForegroundColor <- ConsoleColor.White
        Console.WriteLine()
        Console.WriteLine("Checkout \"{0}\" \"{1}\" \"{2}\"", serverPath, localPath, workspaceName)
    
        Console.ForegroundColor <- ConsoleColor.Gray
        Console.Write("Enter/Space = OK, Esc = CANCEL?");
        let consoleInfo = Console.ReadKey()    
        Console.WriteLine()
        if consoleInfo.Key = ConsoleKey.Escape then
            MyBuild.Failed("Operation cancelled.")
    
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