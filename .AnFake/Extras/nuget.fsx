#r "../AnFake.Api.v1.dll"
#r "../AnFake.Core.dll"
#r "../AnFake.Fsx.dll"
#r "../AnFake.Plugins.Tfs2012.dll"

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

    let anfCmd = (slnRoot / "anf.cmd").AsFile()    
    let anfCmdBody = "[AnFake]/anf.cmd".AsFile().AsTextDoc()
    anfCmdBody.Replace(@"\[\.AnFake\]", anfakePath)
    anfCmdBody.SaveTo(anfCmd, Text.Encoding.ASCII)        
    
    let buildFsx = (slnRoot / "build.fsx").AsFile()
    if not <| buildFsx.Exists() then
        let buildFsxBody = ("[AnFake]/build.tmpl.fsx").AsFile().AsTextDoc()
        buildFsxBody.Replace(@"\[\.AnFake\]", anfakePath)
        buildFsxBody.SaveTo(buildFsx)
        Log.Info("  build.fsx generated.")

    let wsFile = (slnRoot / TfsWorkspace.Defaults.WorkspaceFile).AsFile()
    if not <| wsFile.Exists() then
        let tfsUriRx = @"SccTeamFoundationServer[\s\t]*=[\s\t]*(.*)" // SccTeamFoundationServer = https://server.com/path
        let tfsUriLine = sln.AsTextDoc().MatchedLines(tfsUriRx).FirstOrDefault()
        if tfsUriLine <> null then
            let tfsUri = Text.Parse1Group(tfsUriLine.Text, tfsUriRx)
            MyBuild.SetProp("Tfs.Uri", tfsUri)
            MyBuild.SaveProp("Tfs.Uri")

            Log.InfoFormat("Team Foundation detected at '{0}'.", tfsUri)
            
            Tfs.PlugIn()

            let nugetConfig = (slnRoot / ".nuget/NuGet.config").AsFile()
            if not <| nugetConfig.Exists() then
                Log.Info("== Configuring NuGet to avoid checking-in binaries to version control...")

                let nugetConfigBody = "<configuration/>".AsXmlDoc()
                let node = 
                    nugetConfigBody.Root
                        .Append("solution")
                        .Append("add")
                node.SetAttr("key", "disableSourceControlIntegration")
                node.SetAttr("value", "true")
                nugetConfigBody.SaveTo(nugetConfig)

                TfsWorkspace.Undo(!!!MyBuild.GetProp("__1"))

                Log.Info("== NuGet configured.")

            Log.Info("== Preparing workspace...")
            TfsWorkspace.PendAdd([anfCmd; nugetConfig; buildFsx])

            if SafeOp.Try((fun x -> TfsWorkspace.SaveLocal(x)), slnRoot) then
                TfsWorkspace.PendAdd([wsFile])
                Log.Info("== Workspace ready.")
        
    Log.InfoFormat("Solution '{0}' is ready to use AnFake.", sln.NameWithoutExt)    
)