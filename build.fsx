#r "paket:
nuget Fake.DotNet.Cli
nuget Fake.IO.FileSystem
nuget Fake.Core.Target //"

#load ".fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators

[<AutoOpen>]
module Utils =
  let shell cmd args =
    Shell.Exec(cmd, args) |> function
    | 0 -> Trace.tracefn "Success '%s %s'" cmd args
    | code -> failwithf "Failed '%s %s', Exit Code: %d" cmd args code


  let dotnet cmd arg =
    let res = DotNet.exec id cmd arg

    let msg = $"dotnet %s{cmd} %s{arg}"

    if res.OK then
      Trace.tracefn "Success '%s'" msg
    else
      failwithf "Failed '%s'" msg

  let (|LowerCase|_|) (x: string) (s: string) =
    if x.ToLower() = s.ToLower() then Some LowerCase else None


Target.initEnvironment ()

let args = Target.getArguments()

Target.create "Format" (fun _ ->
  !! "src/**/*.csproj"
  ++ "tests/**/*.csproj"
  |> Seq.iter (fun proj ->
    dotnet "format" $"{proj} -v diag"
  )
)

Target.create "Format.Check" (fun _ ->
  !! "src/**/*.csproj"
  ++ "tests/**/*.csproj"
  |> Seq.iter (fun proj ->
    dotnet "format" $"{proj} --check -v diag"
  )
)

Target.create "Test" (fun _ ->
  dotnet "test" "--logger:\"console;verbosity=detailed\""
)

Target.create "Clean" (fun _ ->
  !! "src/**/bin"
  ++ "src/**/obj"
  |> Shell.cleanDirs
)

Target.create "Build" (fun _ ->
  let configuration =
    args
    |> Option.bind Array.tryHead
    |> function
    | Some (LowerCase("debug")) -> DotNet.BuildConfiguration.Debug
    | Some (LowerCase("release")) -> DotNet.BuildConfiguration.Release
    | Some (c) -> failwithf "Invalid configuration '%s'" c
    | _ -> DotNet.BuildConfiguration.Debug

  !! "src/**/*.*proj"
  |> Seq.iter (DotNet.build (fun p ->
    { p with
        Configuration = configuration
    }))
)

Target.create "PreCommit" (fun _ ->
  Target.runSimple "Format" [] |> ignore
  Target.runSimple "Test" [] |> ignore
)

Target.create "Default" ignore

"Build" ==> "Default"

Target.runOrDefaultWithArguments "Default"
