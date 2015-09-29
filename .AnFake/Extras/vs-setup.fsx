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

let vsSupportedVersions =
    [
        "11.0".AsVersion()
        "12.0".AsVersion()
        "14.0".AsVersion()
    ]

let vsExternalTools = 
    [
        "AnFake Checkout",     "anf-tf.cmd", "Checkout",          null,             ExternalTool.OptionNone
        "AnFake Get Specific", "anf-tf.cmd", "GetSpecific Cnnnn", "$(SolutionDir)", ExternalTool.OptionPromptArgs + ExternalTool.OptionUseOutWindow
        "AnFake Get Latest",   "anf-tf.cmd", "GetLatest",         "$(SolutionDir)", ExternalTool.OptionUseOutWindow
        "AnFake Build",        "anf.cmd",    "Build",             "$(SolutionDir)", ExternalTool.OptionPromptArgs + ExternalTool.OptionUseOutWindow
    ]

let getProjectsHome () =
    let projHome = 
        if MyBuild.HasProp("__1") then
            MyBuild.GetProp("__1")
        else if MyBuild.HasProp("TfExtension.ProjectsHome") then
            MyBuild.GetProp("TfExtension.ProjectsHome")
        else
            @"C:\Projects"

    if not <| projHome.AsFolder().Exists() then
        MyBuild.Failed("Projects home folder '{0}' doesn't exist.", projHome)

    ~~projHome // return


let getTeamProject () =
    if not <| MyBuild.HasProp("__1") then
        MyBuild.Failed("Required argument <team-project-name> is missed.")

    let teamProj = MyBuild.GetProp("__1")
    if not <| Tfs.HasTeamProject(teamProj) then
        MyBuild.Failed("Team Project '{0}' doesn't exist.", teamProj)
        
    teamProj // return;        


let configureVsTools (anfHome:FileSystemPath) (projHome:FileSystemPath) =
    let versions = 
        VisualStudio
            .GetInstalledVersions()
            .Intersect(vsSupportedVersions)

    for version in versions do
        Trace.InfoFormat("Setting up external tools in VisualStudio {0}...", version)

        let tools = VisualStudio.GetExternalTools(version)

        for (title, cmd, args, dir, opt) in vsExternalTools do
            if cmd <> "anf-tf.cmd" || MyBuild.HasProp("Tfs.Uri") then                
                let mutable tool = tools.FirstOrDefault(fun x -> x.Title = title)
                if tool = null then
                    tool <- new ExternalTool()
                    tool.Title <- title
                    tools.Insert(0, tool)
            
                tool.Command <- if cmd = "anf-tf.cmd" then (anfHome / cmd).Full else cmd
                tool.Arguments <- args
                tool.InitialDirectory <- if dir <> null then dir else projHome.Full
                tool.Options <- tool.Options ||| opt            
    
        VisualStudio.SetExternalTools(version, tools)
        Trace.SummaryFormat("VisualStudio {0}: external tools configured.", version)


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
    
    let anfHome = ~~"[LocalApplicationData]/AnFake";
    let tfConvFsx = ~~"Extras/tf-conv.fsx";
    Trace.InfoFormat("Copying AnFake to '{0}'...", anfHome)    
    Files.Copy(~~"[AnFake]" % "**/*", anfHome, true);
    if not <| (anfHome / tfConvFsx).AsFile().Exists() then
        Files.Copy(~~"[AnFakeExtras]/tf-conv-flat.fsx", anfHome / tfConvFsx, false);

    configureVsTools anfHome projHome
)

"Wizard" => (fun _ ->
    if not <| MyBuild.HasProp("Tfs.Uri") then
        MyBuild.SetProp(
            "Tfs.Uri",
            UserInterop.Prompt(
                "Team Foundation Collection Uri", 
                "Please, enter an URI of Team Foundation projects collection, e.g. 'https://tf-server:8080/my-collection'.",
                fun uri ->
                    Tfs.CheckConnection(uri)
                )
        )
        MyBuild.SaveProp("Tfs.Uri")
        Trace.Summary("Team Foundation collection uri verified and saved.")
    else
        Trace.Summary("Team Foundation collection uri already configured.")

    if not <| MyBuild.HasProp("TfExtension.ProjectsHome") then
        MyBuild.SetProp(
            "TfExtension.ProjectsHome",
            UserInterop.Prompt(
                "Projects Home", 
                "Please, enter a full path to your projects home folder, e.g. 'C:\\Projects'.\n" +
                "(AnFake will checkout sources into this folder by default)",
                fun projHome ->
                    if not <| projHome.AsFolder().Exists() then
                        MyBuild.Failed("Folder '{0}' doesn't exist.", projHome)
                ).AsPath().Full
            )
        MyBuild.SaveProp("TfExtension.ProjectsHome")
        Trace.Summary("Projects home verified and saved.")
    else
        Trace.Summary("Projects home already configured.")

    let projHome = MyBuild.GetProp("TfExtension.ProjectsHome")
    
    let tfConvFsx = ~~"[AnFakeExtras]/tf-conv.fsx"
    if not <| tfConvFsx.AsFile().Exists() then
        let layout = 
            UserInterop.Prompt(
                "Projects Layout", 
                "Choose a default projects layout:\n" +
                String.Format(" (F)lat    $/TeamProject/Module/branch => {0}\\Module.branch\n", projHome) +            
                String.Format(" (T)tree   $/TeamProject/Module/branch => {0}\\Module\n", projHome) + 
                new String(' ', projHome.Length + 49) + "\\branch\n" + 
                "(AnFake will checkout sources with given layout by default)",
                fun lt ->
                    if lt <> "f" && lt <> "F" && lt <> "t" && lt <> "T" then
                        MyBuild.Failed("Please, pick 'F' or 'T'.")
                )
        if layout = "f" || layout = "F" then
            Files.Copy(~~"[AnFakeExtras]/tf-conv-flat.fsx", tfConvFsx, false)
            Trace.Summary("Projects layout configured as FLAT.")
        else
            Files.Copy(~~"[AnFakeExtras]/tf-conv-tree.fsx", tfConvFsx, false)
            Trace.Summary("Projects layout configured as TREE.")
    else
        Trace.Summary("Projects layout already configured.")

    configureVsTools (~~"[AnFake]") (~~projHome)
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