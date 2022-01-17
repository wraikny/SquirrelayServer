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

  let getConfiguration = function
    | Some (LowerCase("debug")) -> DotNet.BuildConfiguration.Debug
    | Some (LowerCase("release")) -> DotNet.BuildConfiguration.Release
    | Some (c) -> failwithf "Invalid configuration '%s'" c
    | _ -> DotNet.BuildConfiguration.Debug


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
    dotnet "format" $"{proj} --verify-no-changes -v diag"
  )
)

Target.create "Test" (fun _ ->
  !! "tests/**/*.*proj"
  |> Seq.iter(fun proj ->
    dotnet "test" $"%s{proj} -l \"console;verbosity=detailed\""
  )
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
    |> getConfiguration

  !! "src/**/*.*proj"
  |> Seq.iter (DotNet.build (fun p ->
    { p with
        Configuration = configuration
    }))
)

Target.create "Publish" (fun _ ->
  [
    "linux-x64"
    "win10-x64"
  ]
  |> Seq.iter (fun runtime ->
    let dir = @$"output/SquirrelayServer.{runtime}"

    @"src/SquirrelayServer.App/SquirrelayServer.App.csproj"
    |> DotNet.publish (fun p ->
      { p with
          Runtime = Some runtime
          Configuration = DotNet.BuildConfiguration.Release
          SelfContained = Some true
          OutputPath = Some dir
          MSBuildParams = {
            p.MSBuildParams with
              Properties =
                ("PublishSingleFile", "true")
                :: ("PublishTrimmed", "true")
                :: p.MSBuildParams.Properties
          }
      }
    )

    Directory.ensure $"{dir}/config"

    @"config/config.json" |> Shell.copyFile @$"{dir}/config/config.json"

    !! $"{dir}/**.pdb" |> Seq.iter Shell.rm
  )
)

Target.create "PreCommit" (fun _ ->
  Target.runSimple "Format" [] |> ignore
  Target.runSimple "Build" [ "debug" ] |> ignore
  Target.runSimple "Build" [ "release" ] |> ignore
  Target.runSimple "Test" [ ] |> ignore
)

Target.create "UpdatePackages" (fun _ ->
  dotnet "add" "src/SquirrelayServer package MessagePack"
  dotnet "add" "src/SquirrelayServer package MessagePackAnalyzer"
  dotnet "add" "tests/SquirrelayServer.Tests package Microsoft.NET.Test.Sdk"
  dotnet "add" "tests/SquirrelayServer.Tests package Moq"
  dotnet "add" "tests/SquirrelayServer.Tests package xunit"
  dotnet "add" "tests/SquirrelayServer.Tests package xunit.runner.visualstudio"
  dotnet "add" "tests/SquirrelayServer.Tests package coverlet.collector"
)

Target.create "None" ignore
Target.create "Default" ignore

"Build" ==> "Default"

Target.runOrDefaultWithArguments "Default"
