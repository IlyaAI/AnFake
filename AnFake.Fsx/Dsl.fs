[<AutoOpen>]
module AnFake.Fsx.Dsl

open System
open System.IO
open System.Collections.Generic
open AnFake.Core
open System.Runtime.CompilerServices

let inline (~~) (path: string) = FileSystem.AsPath(path)

let inline (!!) (wildcardedPath: string) = FileSystem.AsFileSet(wildcardedPath)

let inline (%%) (basePath: string, wildcardedPath: string) = FileSystem.AsFileSetFrom(wildcardedPath, basePath)

let inline (!!!) (wildcardedPath: string) = FileSystem.AsFolderSet(wildcardedPath)

let inline (=>) target (action: unit -> unit) = Targets.AsTarget(target).Do(fun _ -> action ())

let inline (&=>) (target: Target) (action: unit -> unit) = target.OnFailure(action)

let inline (|=>) (target: Target) (action: unit -> unit) = target.Finally(action)

let inline (<==) target (dependencies: IEnumerable<string>) = Targets.AsTarget(target).DependsOn(dependencies)

[<Extension>]
type FsxHelper () =
    [<Extension>]
    static member inline Set(props: IDictionary<System.String, System.String>, nameValue: (string * string) list) = 
        for (name, value) in nameValue do
            props.Item(name) <- value