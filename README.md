# SquirrelayServer

SquirrelayServer は、リアルタイム通信のためのリレーサーバーです。
設定ファイルを記述するだけで色々な種類のゲームで利用できることを目指しています。
特に理由はないですが、名前は動物のリス（Squirrel）と掛けています。

SquirrelayServer is a relay server for real-time communication.
The goal is to be able to use it in various kinds of games by simply writing a configuration file.
There is no particular reason for this, but the name is a reference to the animal "Squirrel".


## Compilation

### Setup

```sh
$ git submodule update --init
$ dotnet tool restore
```

### Build

```sh
$ dotnet fake build [-- <DEBUG|RELEASE>]
```

Default configuration is DEBUG

## Format

```sh
$ dotnet fake build -t format
```

## Test

```sh
$ dotnet fake build -t test [-- <DEBUG|RELEASE>]
```

## Dependencies
- [LiteNetLib](https://github.com/RevenantX/LiteNetLib)
- [MessagePack-CSharp](https://github.com/neuecc/MessagePack-CSharp)
