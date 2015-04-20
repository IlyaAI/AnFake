module TfsConvention

#r "../AnFake.Api.v1.dll"
#r "../AnFake.Core.dll"
#r "../AnFake.Plugins.Tfs2012.dll"

open System
open System.Linq
open AnFake.Core
open AnFake.Plugins.Tfs2012

let private serviceNames = 
    [
        "release"
        "releases"
        "branch"
        "branches"
        "feature"
        "features"
    ]

let getProductNameByConvention (serverPath: ServerPath) =
    serverPath
        .Split()
        .Reverse()
        .Skip(1)
        .Except(serviceNames, StringComparer.OrdinalIgnoreCase)
        .FirstOrDefault()

let getBranchNameByConvention (serverPath: ServerPath) =
    serverPath.LastName

let getWorkspaceNameByConvention productName branchName =
    NameGen.Generate(
        String.Format("{0}.{1}", productName, branchName),
        TfsWorkspace.UniqueName
    )

let getLocalPathByConvention (basePath: FileSystemPath) (productName) (branchName) =
    basePath / NameGen.Generate(
        String.Format("{0}.{1}", productName, branchName),
        fun n -> (basePath / n).AsFolder().IsEmpty()
    )