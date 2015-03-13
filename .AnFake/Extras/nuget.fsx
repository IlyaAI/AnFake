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

let curDir = Folders.Current.Path;

Tfs.PlugInDeferred()

"InitiateSolution" => (fun _ ->
    let slnRoot = 
        (~~"[AnFake]")  // <sln-root>/packages/AnFake.x.y.z/bin
            .Parent     // <sln-root>/packages/bin
            .Parent     // <sln-root>/packages
            .Parent     // <sln-root>

    let sln = (slnRoot % "*.sln").FirstOrDefault()
    if sln = null then
        MyBuild.Failed("Expecting solution file in '{0}' but no one was found.", slnRoot)

    Log.InfoFormat("Initiating solution '{0}'...", sln.NameWithoutExt)

    let anfCmd = (slnRoot / "anf.cmd").AsFile()
    if not <| anfCmd.Exists() then
        Files.Copy(~~"[AnFake]/anf.cmd", anfCmd.Path)
        Log.Info("  'anf.cmd' generated.")
    else
        Log.Info("  'anf.cmd' Ok.")

    let buildFsx = (slnRoot / "build.fsx").AsFile()
    if not <| buildFsx.Exists() then            
        Files.Copy(~~"[AnFake]/build.tmpl.fsx", buildFsx.Path)
        Log.Info("  'build.fsx' generated.")
    else
        Log.Info("  'build.fsx' Ok.")

    let symlink = slnRoot / ".AnFake"
    if not <| symlink.AsFolder().Exists() then
        SymLink.Create(symlink, "[AnFake]".AsFolder())
        Log.Info("  '.AnFake' symlink created.")
    else
        Log.Info("  '.AnFake' Ok.")
    
    let wsFile = (slnRoot / TfsWorkspace.Defaults.WorkspaceFile).AsFile()
    if not <| wsFile.Exists() then
        let tfsUriRx = @"SccTeamFoundationServer[\s\t]*=[\s\t]*(.*)" // SccTeamFoundationServer = https://server.com/path
        let tfsUriLine = sln.AsTextDoc().MatchedLines(tfsUriRx).FirstOrDefault()
        if tfsUriLine <> null then
            let tfsUri = Text.Parse1Group(tfsUriLine.Text, tfsUriRx)
            MyBuild.SetProp("Tfs.Uri", tfsUri)

            Log.InfoFormat("  TFS detected: '{0}'.", tfsUri)
            
            Tfs.PlugIn()

            TfsWorkspace.SaveLocal(slnRoot)
            Log.InfoFormat("  '{0}' generated.", wsFile.Name)

            TfsWorkspace.PendAdd([wsFile])
            TfsWorkspace.PendAdd([buildFsx])
    else
        Log.InfoFormat("  '{0}' Ok.", wsFile.Name)
        
    Log.InfoFormat("Solution '{0}' is ready to use AnFake.", sln.NameWithoutExt)
)