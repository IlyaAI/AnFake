#r "../AnFake.Api.v1.dll"
#r "../AnFake.Core.dll"
#r "../AnFake.Fsx.dll"
#r "../Plugins/AnFake.Plugins.Tfs2012.dll"
#r "../Plugins/AnFake.Integration.Vs2012.dll"

open System
open System.Linq
open AnFake.Api
open AnFake.Core
open AnFake.Fsx.Dsl
open AnFake.Plugins.Tfs2012
open AnFake.Integration.Vs2012;

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

let getProjectsHome () =    
    UserInterop.Prompt(
        "Projects Home", 
        "Please, enter a full path to your projects home folder, e.g. 'C:\\Projects'.",
        fun projHome ->
            if not <| projHome.AsFolder().Exists() then
                MyBuild.Failed("Folder '{0}' doesn't exist.", projHome)
        ).AsPath()
        .Full

let getTeamProject () =
    UserInterop.Prompt(
        "Team Project", 
        "Please, enter a TFS team project name, e.g. 'MY-PROJ'.",
        fun teamProj ->
            if not <| Tfs.HasTeamProject(teamProj) then
                MyBuild.Failed("Team project '{0}' doesn't exist.", teamProj)
        )

let vsSupportedVersions =
    [
        "11.0".AsVersion()
        "12.0".AsVersion()
    ]

let vsExternalTools = 
    [
        "AnFake Checkout",   "[AnFake]/anf-tf.cmd", "Checkout",  null,             ExternalTool.OptionNone
        "AnFake Get Latest", "[AnFake]/anf-tf.cmd", "GetLatest", "$(SolutionDir)", ExternalTool.OptionNone
        "AnFake Build",      "anf.cmd",             "Build",     "$(SolutionDir)", ExternalTool.OptionPromptArgs        
    ]

"Tools" => (fun _ ->
    plugInTfs()

    let projHome = getProjectsHome()
    let versions = 
        VisualStudio
            .GetInstalledVersions()
            .Intersect(vsSupportedVersions)

    for version in versions do
        Trace.InfoFormat("Setting up external tools in VisualStudio {0}...", version)

        let tools = VisualStudio.GetExternalTools(version)

        for (title, cmd, args, dir, opt) in vsExternalTools do
            if not <| tools.Any(fun x -> x.Title = title) then
                let tool = new ExternalTool()
                tool.Title <- title            
                tool.Command <- (~~cmd).Full
                tool.Arguments <- args
                tool.InitialDirectory <- if dir <> null then dir else projHome
                tool.Options <- tool.Options ||| opt
            
                tools.Insert(0, tool)
    
        VisualStudio.SetExternalTools(version, tools)

        Trace.SummaryFormat("VisualStudio {0}: external tools configured.", version)
)

"BuildTemplate" => (fun _ ->
    plugInTfs()

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
    
    Files.Copy(~~"[AnFakePlugins]" % "*.xaml", localPath, true)
    Files.Copy(
        ~~"[AnFake]" % "AnFake.Api.*.dll" 
        + "Antlr4.Runtime.dll"
        + "NuGet.exe",
        activitiesLocalPath, 
        true)
    Files.Copy(~~"[AnFakePlugins]" % "AnFake.Integration.Tfs2012.*.dll", activitiesLocalPath, true)    

    TfsWorkspace.PendAdd(localPath % "*.xaml")
    TfsWorkspace.PendAdd(
        activitiesLocalPath % "AnFake.Api.*.dll"
        + "Antlr4.Runtime.dll"
        + "NuGet.exe"
        + "AnFake.Integration.Tfs2012.*.dll"
    )

    Trace.SummaryFormat("AnFakeTemplate is ready to be checked-in. Review all pending changes in '{0}' workspace and commit them.", workspaceName)
)