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

let vsSupportedVersions =
    [
        "11.0".AsVersion()
        "12.0".AsVersion()
    ]

let vsExternalTools = 
    [
        "AnFake Get Latest", "GetLatest \"Tfs.Uri={0}\"", "$(SolutionDir)"
        "AnFake Checkout", "Checkout \"Tfs.Uri={0}\"", null        
    ]

"SetupTfsUri" => (fun _ ->
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
)

"SetupProjectsHome" => (fun _ ->
    if not <| MyBuild.HasProp("projectsHome") then
        MyBuild.SetProp(
            "projectsHome",
            UserInterop.Prompt(
                "Projects Home", 
                "Please, enter a full path to your projects home folder, e.g. 'C:\\Projects'.\n" + 
                "Alternatively you can provide projects home as command line parameter: \"projectsHome=<path>\"",
                fun projHome ->
                    if not <| projHome.AsFolder().Exists() then
                        MyBuild.Failed("Folder '{0}' doesn't exist.", projHome)    
                ).AsPath()
                .Full
        )    
)

"SetupTools" => (fun _ ->
    let tfsUri = MyBuild.GetProp("Tfs.Uri")
    let projHome = MyBuild.GetProp("projectsHome")
    let versions = 
        VisualStudio
            .GetInstalledVersions()
            .Intersect(vsSupportedVersions)

    for version in versions do
        Log.InfoFormat("Setting up external tools in VisualStudio {0}...", version)

        let tools = VisualStudio.GetExternalTools(version)

        for (title, args, dir) in vsExternalTools do
            if not <| tools.Any(fun x -> x.Title = title) then
                let tool = new ExternalTool()
                tool.Title <- title            
                tool.Arguments <- String.Format(args, tfsUri)
                tool.InitialDirectory <- if dir <> null then dir else projHome
                tool.Command <- (~~"[AnFake]/anf-tf.cmd").Full
            
                tools.Insert(0, tool)
    
        VisualStudio.SetExternalTools(version, tools)

        Log.Info("Tools successfuly configured.")
)

"SetupTfsUri" ==> "SetupProjectsHome" ==> "SetupTools"