#r "../AnFake.Api.v1.dll"
#r "../AnFake.Core.dll"
#r "../AnFake.Fsx.dll"
#r "../AnFake.Plugins.Tfs2012.dll"
#r "../AnFake.Integration.Vs2012.dll"

open System
open System.Linq
open AnFake.Api
open AnFake.Core
open AnFake.Fsx.Dsl
open AnFake.Plugins.Tfs2012
open AnFake.Integration.Vs2012;

Tfs.PlugInDeferred()

let getProjectsHome () =
    let projHome = MyBuild.GetProp("__1", @"C:\Projects")
    if not <| projHome.AsFolder().Exists() then
        MyBuild.Failed("Projects home folder '{0}' doesn't exist.", projHome)

    projHome.AsPath().Full // return

let getTeamProject () =
    if not <| MyBuild.HasProp("__1") then
        MyBuild.Failed("Required argument <team-project-name> is missed.")

    let teamProj = MyBuild.GetProp("__1")
    if not <| Tfs.HasTeamProject(teamProj) then
        MyBuild.Failed("Team Project '{0}' doesn't exist.", teamProj)
        
    teamProj // return;        

let vsSupportedVersions =
    [
        "11.0".AsVersion()
        "12.0".AsVersion()
    ]

let vsExternalTools = 
    [
        "AnFake Checkout",     "anf-tf.cmd", "Checkout",          null,             ExternalTool.OptionNone
        "AnFake Get Specific", "anf-tf.cmd", "GetSpecific Cnnnn", "$(SolutionDir)", ExternalTool.OptionPromptArgs
        "AnFake Get Latest",   "anf-tf.cmd", "GetLatest",         "$(SolutionDir)", ExternalTool.OptionNone
        "AnFake Build",        "anf.cmd",    "Build",             "$(SolutionDir)", ExternalTool.OptionPromptArgs
    ]

"Help" => (fun _ ->
    Log.Info("")
    Log.Info("Usage: <command> [<param>] ...")
    Log.Info("COMMANDS:")
    Log.Info("  Tools [-p <local-projects-home>]")
    Log.Info("    Setup external tools in VisualStudio.")
    Log.Info("    If <local-projects-home> is ommitted then 'C:\\Projects' is used.")
    Log.Info("  BuildTemplate -p <team-project-name>")
    Log.Info("    Setup build process template in Team Build.")
    
    MyBuild.Failed "Command is missed."
)

"Build" <== ["Help"]

"Tools" => (fun _ ->
    let projHome = getProjectsHome()
    let versions = 
        VisualStudio
            .GetInstalledVersions()
            .Intersect(vsSupportedVersions)

    let dstPath = ~~"[LocalApplicationData]/AnFake";
    Trace.InfoFormat("Copying AnFake to '{0}'...", dstPath)    
    Files.Copy(~~"[AnFake]" % "**/*", dstPath, true);

    for version in versions do
        Trace.InfoFormat("Setting up external tools in VisualStudio {0}...", version)

        let tools = VisualStudio.GetExternalTools(version)

        for (title, cmd, args, dir, opt) in vsExternalTools do
            if not <| tools.Any(fun x -> x.Title = title) && (cmd <> "anf-tf.cmd" || MyBuild.HasProp("Tfs.Uri")) then                
                let tool = new ExternalTool()
                tool.Title <- title            
                tool.Command <- if cmd = "anf-tf.cmd" then (dstPath / cmd).Full else cmd
                tool.Arguments <- args
                tool.InitialDirectory <- if dir <> null then dir else projHome
                tool.Options <- tool.Options ||| opt
            
                tools.Insert(0, tool)
    
        VisualStudio.SetExternalTools(version, tools)

        Trace.SummaryFormat("VisualStudio {0}: external tools configured.", version)
)

"BuildTemplate" => (fun _ ->
    Tfs.PlugIn()
    MyBuild.SaveProp("Tfs.Uri")

    let teamProj = getTeamProject()

    let processTmplPath = ("$/" + teamProj + "/BuildProcessTemplates").AsServerPath()
    let customActivitiesPath = processTmplPath / "CustomActivities"

    let workspaceName = "AnFake.BuildTemplate".MakeUnique()
    
    let localPath = ~~"[Temp]" / workspaceName;
    let activitiesLocalPath = localPath / "CustomActivities"
    Folders.Clean(localPath)
    Folders.Create(activitiesLocalPath)

    let wsFile = (localPath / TfsWorkspace.Defaults.WorkspaceFile).AsFile()
    let wsDef = wsFile.AsTextDoc();    
    wsDef.LastLine().InsertAfter("{0}: CustomActivities", customActivitiesPath)    
    wsDef.Save()

    TfsWorkspace.Create(processTmplPath, localPath, workspaceName)
    
    Files.Copy(~~"[AnFake]" % "*.xaml", localPath, true)
    Files.Copy(
        ~~"[AnFake]" % "AnFake.Api.*.dll" 
        + "AnFake.Integration.Tfs2012.*.dll"
        + "Antlr4.Runtime.dll"
        + "NuGet.exe",
        activitiesLocalPath, 
        true)    

    TfsWorkspace.PendAdd(localPath % "*.xaml")
    TfsWorkspace.PendAdd(
        activitiesLocalPath % "AnFake.Api.*.dll"
        + "Antlr4.Runtime.dll"
        + "NuGet.exe"
        + "AnFake.Integration.Tfs2012.*.dll"
    )

    Trace.SummaryFormat("AnFakeTemplate is ready to be checked-in. Review all pending changes in '{0}' workspace and commit them.", workspaceName)
)