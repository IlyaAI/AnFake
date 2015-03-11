#r "../.AnFake/AnFake.Api.v1.dll"
#r "../.AnFake/AnFake.Core.dll"
#r "../.AnFake/AnFake.Fsx.dll"
#r "../.AnFake/Plugins/AnFake.Plugins.Tfs2012.dll"

open System
open System.Linq
open AnFake.Api
open AnFake.Core
open AnFake.Fsx.Dsl
open AnFake.Plugins.Tfs2012

Tfs.PlugIn()

let productVersion = "1.0.0".AsVersion()

let out = ~~".out"
let productOut = out / "product"
let testsOut = out / "tests"

let tests = !!"*/*.Test.csproj"

// TODO: The better solution is to explicitly enumerate here top level projects which forms you product.
let product = 
    !!"*/*.csproj"
    - tests

"Clean" => (fun _ ->    
    let obj = !!!"*/obj"
    let bin = !!!"*/bin"

    Folders.Clean obj
    Folders.Clean bin
    Folders.Clean out
)

"Compile" => (fun _ ->
    // Embeds product version into AssemblyInfo files.
    // 'Temporary' means that after build all changes will be reverted to prevent committing of version number to VCS.
    AssemblyInfo.EmbedTemporary(
        !!"*/Properties/AssemblyInfo.cs",
        fun p ->             
            p.Version <- VersionControl.GetFullVersion(productVersion)
        )
    
    MsBuild.BuildRelease(product, productOut)

    MsBuild.BuildRelease(tests, testsOut)
)

"Test.Unit" => (fun _ -> 
    // Run tests using VSTest.Console.exe runner
    VsTest.Run(
        testsOut % "*.Test.dll")
)

//
// 'Drop' target is requested when DropLocation or PrivateDropLocation is specified in TFS build definition.
// TODO: expose your final artifacts via BuildServer.ExposeArtifacts
//
"Drop" => (fun _ -> ())

"Test" <== ["Test.Unit"]            // 'Test' consists of 'Test.Unit'

"Build" <== ["Compile"; "Test"]     // 'Build' consists of 'Compile' and 'Test'