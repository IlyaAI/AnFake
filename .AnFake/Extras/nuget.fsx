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

Tfs.PlugInDeferred()

let getSolutionRoot() =
    (~~"[AnFake]")  // <sln-root>/packages/AnFake.x.y.z/bin
        .Parent     // <sln-root>/packages/bin
        .Parent     // <sln-root>/packages
        .Parent     // <sln-root>

"InitiateSolution" => (fun _ ->
    let slnRoot = getSolutionRoot()

    let sln = (slnRoot % "*.sln").FirstOrDefault()
    if sln = null then
        MyBuild.Failed("Expecting solution file in '{0}' but no one was found.", slnRoot)

    Log.InfoFormat("Initiating solution '{0}'...", sln.NameWithoutExt)

    let anfakePath = (~~"[AnFake]").ToRelative(slnRoot).Spec

    let mutable undoNeeded = false
    let nugetConfig = (slnRoot / ".nuget/NuGet.config").AsFile()
    if not <| nugetConfig.Exists() then
        undoNeeded <- true

        let nugetConfigBody = "<configuration/>".AsXmlDoc()
        let node = 
            nugetConfigBody.Root
                .Append("solution")
                .Append("add")
        node.SetAttr("key", "disableSourceControlIntegration")
        node.SetAttr("value", "true")

        nugetConfigBody.SaveTo(nugetConfig)
        Log.Info("  NuGet.config generated.")
    else
        Log.Info("  NuGet.config Ok.")

    let anfCmd = (slnRoot / "anf.cmd").AsFile()    
    let anfCmdBody = "[AnFake]/anf.cmd".AsFile().AsTextDoc()
    anfCmdBody.Replace(@"\[\.AnFake\]", anfakePath)
    anfCmdBody.SaveTo(anfCmd, Text.Encoding.ASCII)
    Log.Info("  anf.cmd generated.")    
    
    for sx in ["fsx"; "csx"] do
        let buildSx = (slnRoot / "build." + sx).AsFile()
        if not <| buildSx.Exists() then
            let buildSxBody = ("[AnFake]/build.tmpl." + sx).AsFile().AsTextDoc()
            buildSxBody.Replace(@"\[\.AnFake\]", anfakePath)
            buildSxBody.SaveTo(buildSx)
            Log.InfoFormat("  build.{0} generated.", sx)
        else
            Log.InfoFormat("  build.{0} Ok.", sx)

    let wsFile = (slnRoot / TfsWorkspace.Defaults.WorkspaceFile).AsFile()
    if not <| wsFile.Exists() then
        let tfsUriRx = @"SccTeamFoundationServer[\s\t]*=[\s\t]*(.*)" // SccTeamFoundationServer = https://server.com/path
        let tfsUriLine = sln.AsTextDoc().MatchedLines(tfsUriRx).FirstOrDefault()
        if tfsUriLine <> null then
            let tfsUri = Text.Parse1Group(tfsUriLine.Text, tfsUriRx)
            MyBuild.SetProp("Tfs.Uri", tfsUri)
            MyBuild.SaveProp("Tfs.Uri")

            Log.InfoFormat("  TFS detected: {0}.", tfsUri)
            
            Tfs.PlugIn()

            TfsWorkspace.SaveLocal(slnRoot)
            Log.InfoFormat("  {0} generated.", wsFile.Name)

            TfsWorkspace.PendAdd([wsFile; anfCmd; nugetConfig])
            TfsWorkspace.PendAdd(
                ["fsx"; "csx"].Select(
                    fun sx -> (slnRoot / "build." + sx).AsFile()
                )
            )
            
            if undoNeeded then
                TfsWorkspace.Undo(!!!MyBuild.GetProp("__1"))
    else
        Log.InfoFormat("  {0} Ok.", wsFile.Name)
        
    Log.InfoFormat("Solution '{0}' is ready to use AnFake.", sln.NameWithoutExt)
)