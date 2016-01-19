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

Tfs.PlugIn()

"GetSpecific" => (fun _ ->
    // USAGE: GetSpecific -p <local-path> [<version-spec>]

    TfsWorkspace.UpdateInfoCache()
    
    let localPath = MyBuild.GetProp("__1").AsPath()
    let versionSpec = MyBuild.GetProp("__2", "T")

    TfsWorkspace.Get(localPath, fun p -> (p.VersionSpec <- versionSpec))
)
