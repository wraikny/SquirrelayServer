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

Target.initEnvironment ()

let shell cmd args =
  Shell.Exec(cmd, args) |> function
  | 0 -> Trace.tracefn "Success '%s %s'" cmd args
  | code -> failwithf "Failed '%s %s', Exit Code: %d" cmd args code


Target.create "Format" (fun _ ->
  !! "src/**/*.csproj"
  |> Seq.iter (fun proj ->
    shell "dotnet" $"format {proj} -v diag"
  )
)

Target.create "Format.Check" (fun _ ->
  !! "src/**/*.csproj"
  |> Seq.iter (fun proj ->
    shell "dotnet" $"format {proj} --check -v diag"
  )
)

Target.create "Clean" (fun _ ->
    !! "src/**/bin"
    ++ "src/**/obj"
    |> Shell.cleanDirs 
)

Target.create "Build" (fun _ ->
    !! "src/**/*.*proj"
    |> Seq.iter (DotNet.build id)
)

Target.create "All" ignore

"Clean"
  ==> "Format"
  ==> "Build"
  ==> "All"

Target.runOrDefault "All"
