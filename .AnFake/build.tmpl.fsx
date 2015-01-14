#r ".AnFake/AnFake.Api.dll"
#r ".AnFake/AnFake.Core.dll"
#r ".AnFake/AnFake.Fsx.dll"
#r ".AnFake/Plugins/AnFake.Plugins.Tfs2012.dll"

open System
open System.Linq
open AnFake.Api
open AnFake.Core
open AnFake.Fsx.Dsl
open AnFake.Plugins.Tfs2012

Tfs.UseIt()

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
    MsBuild.BuildRelease(product, productOut)

    MsBuild.BuildRelease(tests, testsOut)
)

"Test.Unit" => (fun _ -> 
    VsTest.Run(
        testsOut % "*.Test.dll")
)

"Test" <== ["Test.Unit"]

"Build" <== ["Compile"; "Test"]