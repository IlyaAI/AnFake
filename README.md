# AnFake = Another F# Make

AnFake provides alternative to TFS build process templates. 
Using AnFake you can define your build steps with F# or C# script instead of XAML. 
TFS 2012 and 2013 is supported out-of-box.

## How Is This Look Like?

Lets look at a Demo solution:
```
\Demo
  \Demo.App
  \Demo.Lib
  \Demo.Lib.Test
  - Demo.sln
  - build.fsx      
```

The build.fsx defines build steps:
```fsharp
Tfs.PlugIn()

let out = ~~".out"
let productOut = out / "product"
let testsOut = out / "tests"

let tests = !!"*/*.Test.csproj"
let product = !!"*/*.csproj" - tests

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
    VsTest.Run(testsOut % "*.Test.dll")
)

"Test" <== ["Test.Unit"]
"Build" <== ["Compile"; "Test"]
```

The same using C#:
```csharp
Tfs.PlugIn();

var outDir = ".out".AsPath();
var productOut = out / "product";
var testsOut = out / "tests";

var tests = "*/*.Test.csproj".AsFileSet();
var product = "*/*.csproj".AsFileSet() - tests;

"Clean".AsTarget().Do(() => 
{
    var obj = "*/obj".AsFolderSet();
    var bin = "*/bin".AsFolderSet();

    Folders.Clean(obj);
    Folders.Clean(bin);
    Folders.Clean(out);
});

"Compile".AsTarget().Do(() => 
{
    MsBuild.BuildRelease(product, productOut);
    MsBuild.BuildRelease(tests, testsOut);
});

"Test.Unit".AsTarget().Do(() => 
{
    VsTest.Run(testsOut % "*.Test.dll");
});

"Test".AsTarget().DependsOn("Test.Unit");
"Build".AsTarget().DependsOn("Compile", "Test");
```

You can run build either locally...

```
\Projects\Demo\dev> anf Clean Compile
```

![AnFake Local Run](https://github.com/IlyaAI/AnFake/blob/assets/Images/ConsoleSample.png)

...or on Team Build server

![AnFake Team Build Run](https://github.com/IlyaAI/AnFake/blob/assets/Images/TeamBuildSample.png)

### Benefits

* build.fsx is under version control and will be branched with a solution (this is especially important when build steps change from branch to branch);
* it's possible to test build locally before commit;
* for most developers it is easier to edit script (in usual editor with IntelliSense working!) instead of build process template;
* it's significantly easy to debug local script than build workflow on remote machine.


[![Bitdeli Badge](https://d2weczhvl823v0.cloudfront.net/IlyaAI/anfake/trend.png)](https://bitdeli.com/free "Bitdeli Badge")

