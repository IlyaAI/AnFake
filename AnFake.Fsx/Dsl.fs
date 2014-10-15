[<AutoOpen>]
module AnFake.Fsx.Dsl

open System
open System.IO
open System.Collections.Generic
open AnFake.Core

let inline (~~) (path: string) = FileSystem.AsPath(path)

let inline (!!) (wildcardedPath: string) = FileSystem.AsFileSet(wildcardedPath)

let inline (%%) (basePath: string) (wildcardedPath: string) = FileSystem.AsFileSetFrom(wildcardedPath, basePath)

let inline (!!!) (wildcardedPath: string) = FileSystem.AsFolderSet(wildcardedPath)

let inline (=>) target (action: unit -> unit) = Targets.AsTarget(target).Do(fun _ -> action ())

let inline (&=>) (target: Target) (action: unit -> unit) = target.OnFailure(fun _ -> action ())

let inline (|=>) (target: Target) (action: unit -> unit) = target.Finally(fun _ -> action ())

let inline (<==) target (dependencies: IEnumerable<string>) = Targets.AsTarget(target).DependsOn(dependencies)



