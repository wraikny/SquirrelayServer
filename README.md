# SquirrelayServer

SquirrelayServer は、リアルタイム通信のためのリレーサーバーです。
設定ファイルを記述するだけで複数のゲームで利用できることを目指しています。
特に理由はないですが、名前は動物のリス（Squirrel）と掛けています。

SquirrelayServer is a relay server for real-time communication.
The goal is to be able to use it in multiple games by simply writing a configuration file.
For no particular reason, the name is a play on the animal Squirrel.


## Compilation

### Setup

```sh
$ git submodule update --init
$ dotnet tool restore
```

### Build
```sh
$ dotnet fake build
```

## Dependencies
- [LiteNetLib](https://github.com/RevenantX/LiteNetLib)
- [MessagePack-CSharp](https://github.com/neuecc/MessagePack-CSharp)
