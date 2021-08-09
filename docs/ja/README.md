# SquirrelayServer ドキュメント

## 目次

- [SquirrelayServer ドキュメント](#squirrelayserver-ドキュメント)
  - [目次](#目次)
  - [SquireelayServerの概要](#squireelayserverの概要)
    - [Serverの使い方](#serverの使い方)
    - [Clientの使い方](#clientの使い方)
    - [GameMessageの扱い方](#gamemessageの扱い方)
    - [Configの書き方](#configの書き方)


## SquireelayServerの概要

SquireelayServerは、RUDPを使ったリアルタイム通信のためのサーバー及びそれに接続するためのクライアントの実装です。

### Serverの使い方

`config/config.json`という相対パスに設定ファイルを用意して`SquireelayServer.App`を実行するだけで使用可能です。

プログラムに組み込みたい場合は、[`SquirrelayServer.Server.Server`](/src/SquirrelayServer/Server/Server.cs)クラスを利用します。
[SquirrelayServer.App/Program.cs](/src/SquirrelayServer.App/Program.cs)を参考にしてください。


### Clientの使い方

3つの型`PlayerStatus`、`RoomMessage`, `GameMessage`を用意します。
それぞれ`MessagePack.MessagePackObject`属性をつけて、MessagePackを利用可能にしてください。

`PlayerStatus`クラスは、ルーム内の各プレイヤーの持つ値です。
`RoomMessage`クラスは、ルームに設定される値です。この値はルームリスト取得時に公開されます。（`roomMessageEnabled`が`true`のときのみ利用可能です）
`GameMessage`型は、ゲームにおける各プレイヤーの操作を表します。ルームがプレイ中の状態になっているとき、ルームに送信されたメッセージは各プレイヤーにブロードキャストされます。

`SquireelayServer.Client.Client<TPlayerStatus, TRoomMessage, TMsg>`クラスのインスタンスを作成して、`client.Start("host")`で接続します。

`Client`クラスには、コンストラクタで`IClientListener`インターフェースのインスタンスを渡します。
このインターフェースでは、サーバーからの色々なメッセージを受け取ったときに実行するメソッドを記述できます。
デフォルトでは[`EventBasedClientListener`](/src/SquirrelayServer/Client/Listener.cs)が用意されています。

### GameMessageの扱い方

1. 各クライアントは、自身のゲーム入力を`GameMessage`としてサーバーに送信します。
1. サーバーの`Room`はゲームメッセージを受け取ると、そのゲームメッセージを送信したクライアントの`Id`と受け取った時のゲーム経過秒数をセットにします。
1. サーバーのアップデート処理が行われると、前回のアップデートから`Room`に送信されたゲームメッセージのリストを、その`Room`に接続されているすべてのクライアントに送信します。
1. クライアントでは、各メッセージに対して`IClientListener.OnGameMessageReceived(ulong clientId, float elapsedSeconds, TMsg gameMessage)`メソッドが実行されるので、これを利用してゲームの更新処理を行います。
2. すべてのクライアントが（自身のゲーム入力も含めて）サーバーで受信した時刻をと併せて`GameMessage`を基にステートの更新を行うことで、ゲームの同期ズレが起きない、多分。

### Configの書き方

サンプルは[config.json](/config/config.json)を確認してください。

- `netConfig`: ネットワーク（LiteNetLib）で利用する設定
  - `connectionKey`: 接続時に照合するキーの文字列
  - `port`: サーバーのポート
  - `maxClientsCount`: サーバーに同時接続可能なクライアント数の最大
  - `updateTime`: 更新間隔(msec)
  - `natPunchEnabled`: NAT Punchingを有効にするかどうか
  - `pingInterval`: 遅延検出と接続確認のインターバル(msec)
  - `disconnectTimeout`: Disconnectのtimeout(msec)
  - `debugOnly`: DEBUGビルドでのみ有効な設定
    - `simulationPacketLossChance`: パケットロスシミュレーションの確率(%)
    - `simulateLatencyRange`: レイテンシシミュレーションの範囲(msec)
- `roomConfig`: ゲームのルーム作成に利用される設定
  - `invisibleEnabled`: ルーム一覧に表示されないルームを作成可能にする
  - `roomMessageEnabled`: ルームのメッセージを設定可能にする
  - `passwordEnabled`: ルームにパスワードを設定可能にする
  - `enterWhenPlaying`: ゲームがスタートされた後に新しくプレイヤーが入室可能にするかどうか
  - `disposeSecondsWhenNoMember`: ルーム内のメンバーが全員退室後にルームを破棄するまでの時間(sec)
  - `updatingDisposeStatusIntervalSeconds`: ルームが破棄対象か確認して破棄する処理の感覚(sec)
  - `numberOfPlayersRange`: ルームに入室可能なプレイヤー数の範囲
  - `generatedRoomIdRange`: ルームIdの範囲

