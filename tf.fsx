#r ".AnFake/Bin/AnFake.Api.dll"
#r ".AnFake/Bin/AnFake.Core.dll"
#r ".AnFake/Bin/AnFake.Fsx.dll"
#r ".AnFake/Plugins/AnFake.Plugins.Tfs2012.dll"

open AnFake.Api
open AnFake.Core
open AnFake.Fsx.Dsl
open AnFake.Plugins.Tfs2012

Tfs.UseIt()

"dw" => (fun _ ->
    let curDir = Folders.Current.Path;

    let serverPath = MyBuild.Defaults.Properties.Get("ServerPath");
    let localPath = curDir / MyBuild.Defaults.Properties.Get("LocalPath");
    let workspaceName = MyBuild.Defaults.Properties.Get("WorkspaceName")

    TfsEx.DownloadWorkspace(serverPath, localPath, workspaceName) |> ignore
)

"DownloadWorkspace" <== ["dw"]