# SquirrelayServer ドキュメント

## 目次

- [SquirrelayServer ドキュメント](#squirrelayserver-ドキュメント)
  - [目次](#目次)
  - [SquireelayServerの概要](#squireelayserverの概要)
  - [Serverの概要](#serverの概要)
  - [Clientの概要](#clientの概要)
  - [GameMessageの扱い方](#gamemessageの扱い方)
  - [Configの書き方](#configの書き方)
  - [Clientの詳細](#clientの詳細)
    - [コンストラクタ](#コンストラクタ)
    - [プロパティ](#プロパティ)
    - [メソッド](#メソッド)
  - [IClientListenerについて](#iclientlistenerについて)
  - [各IServerMsg.~Responseクラスについて](#各iservermsgresponseクラスについて)


## SquireelayServerの概要

SquireelayServerは、RUDP（LiteNetLib）とMessagePackを使った、リアルタイム同期サーバーです。

特徴

* ルーム機能と、ルーム内でのメッセージブロードキャストを提供
* サーバー側のプログラム不要（組み込むことも可能）で、設定ファイルの記述のみ
* クライアント側は、送受信されるメッセージを表す型をいくつか定義するだけで利用可能
* async/await対応


## Serverの概要

`config/config.json`という相対パスに設定ファイルを用意して`SquireelayServer.App`を実行するだけで使用可能です。

プログラムに組み込みたい場合は、[`SquirrelayServer.Server.Server`](/src/SquirrelayServer/Server/Server.cs)クラスを利用します。
[SquirrelayServer.App/Program.cs](/src/SquirrelayServer.App/Program.cs)を参考にしてください。


## Clientの概要

3つの型`PlayerStatus`、`RoomMessage`, `GameMessage`を用意します。
それぞれ`MessagePack.MessagePackObject`属性をつけてください。

* `PlayerStatus`クラス
  * ルーム内の各プレイヤーが持つ値です。プレイヤー名などの利用を想定しています。
* `RoomMessage`クラス
  * ルームに設定される値です。この値はルームリスト取得時に公開されます。ゲームルールの設定や募集メッセージなどの利用を想定しています（`roomMessageEnabled`が`true`のときのみ利用可能です）
* `GameMessage`型
  * ゲームにおける各プレイヤーの操作を表します。ルームがプレイ中の状態になっているとき、ルームに送信されたメッセージは各プレイヤーにブロードキャストされます

`SquireelayServer.Client.Client<TPlayerStatus, TRoomMessage, TMsg>`クラスのインスタンスを作成して、`client.Start("http://example.com")`などとして接続します。

`Client`クラスには、コンストラクタで[`IClientListener`](/src/SquirrelayServer/Client/Listener.cs)インターフェースのインスタンスを渡します。
このインターフェースでは、サーバーからの色々なメッセージを受け取ったときに実行するメソッドを記述できます。
デフォルトでは[`EventBasedClientListener`](/src/SquirrelayServer/Client/Listener.cs)が用意されています。

## GameMessageの扱い方

1. 各クライアントは、自身のゲーム入力を`GameMessage`としてサーバーに送信します。
1. サーバーの`Room`はゲームメッセージを受け取ると、そのゲームメッセージを送信したクライアントの`Id`と受け取った時のゲーム経過秒数をセットにします。
1. サーバーのアップデート処理が行われると、前回のアップデートから`Room`に送信されたゲームメッセージのリストを、その`Room`に接続されているすべてのクライアントに送信します。
1. クライアントでは、各メッセージに対して`IClientListener.OnGameMessageReceived(ulong clientId, float elapsedSeconds, TMsg gameMessage)`メソッドが実行されるので、これを利用してゲームの更新処理を行います。
1. すべてのクライアントが（自身のゲーム入力も含めて）サーバーで受信した時刻をと併せて`GameMessage`を基にステートの更新を行うことで、ゲームの同期ズレが起きないでほしいですね。
1. ラグとか知らないですが、同期の正確性を引き換えに`Latency`プロパティを使ってごまかせると思います。

`tickMessageEnabled`を有効にすることで、サーバーのアップデート処理に`Tick`メッセージをクライアントに送信して、`IClientListener.OnTicked(float elapsedSeconds)`メソッドで処理を行うことができます。

## Configの書き方

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
  - `tickMessageEnabled`: ルーム更新毎に`Tick`メッセージをクライアントにブロードキャストするかどうか。
  - `disposeSecondsWhenNoMember`: ルーム内のメンバーが全員退室後にルームを破棄するまでの時間(sec)
  - `updatingDisposeStatusIntervalSeconds`: ルームが破棄対象か確認して破棄する処理の感覚(sec)
  - `numberOfPlayersRange`: ルームに入室可能なプレイヤー数の範囲（両端含む）
  - `generatedRoomIdRange`: ルームIdの範囲（両端含む）

## Clientの詳細

### コンストラクタ

| 引数名 | 型 | 説明 |
|--|--|--|
| `netConfig` | `NetConfig` | ネットワークの設定 |
| `serverSerializerOptions` | `MessagePackSerializerOptions` | サーバーとクライアント共通のオプション |
| `clientsSerializerOptions` | `MessagePackSerializerOptions` | クライアント間のみで利用されるオプション |
| `listener` | `IClientListener<TPlayerSt, TRoomMsg, TMsg>` | クライアントの各イベントに対するリスナー |

### プロパティ

| プロパティ名 | 型 | 説明 |
|--|--|--|
| `Id` | `ulong?` | クライアントのIdを取得。未接続時は`null` |
| `RoomConfig` | `RoomConfig` | サーバーのルーム設定を取得。未接続時は`null` |
| `Latency` | `int` | 現在のレイテンシを取得 |
| `IsStarted` | `bool` | クライアントがスタートされたかを取得 |
| `IsConnected` | `bool` | クライアントが接続されたかを取得 |
| `IsOwner` | `bool` | 自身がルームのオーナーかを取得 |
| `CurrentRoom` | `CurrentRoomInfo<TPlayerStatus, TRoomMessage>` | 現在のルームの情報を取得 |

### メソッド

* `Start`
  * 指定したホストに接続を行う
  * 返り値の型: `Task`
    * 接続が完了するまで待機するタスク
  * 引数
    * `string host`: 接続先のアドレス
* `Stop`
  * サーバーとの接続を停止する
  * 返り値の型: `void`
* `Update`
  * 各イベントの処理を行う。ゲームの毎フレームの更新時に呼び出す
  * 返り値の型： `void`
* `RequestGetClientsCountAsync`
  * 現在サーバーに接続中のクライアント数を取得するリクエストを行う
  * 返り値の型: `Task<int>`
* `RequestGetRoomListAsync`
  * ルームリストを取得するリクエストを行う。`invisible`指定されているルームは含まれない。
  * 返り値の型: `Task<IReadOnlyCollection<RoomInfo<TRoomMessage>>>`
* `RequestCreateRoomAsync`
  * 新規のルームを作成しそこに入室するリクエストを行う。自身がルームオーナーとなることに注意。
  * 返り値の型: `Task<IServerMsg.CreateRoomResponse>`
  * 引数
    * `bool isVisible = true`: ルームリストに表示するかどうか
    * `string password = null`: ルームのパスワード
    * `int? maxNumberOfPlayers = null`: クライアントの最大入室可能数
    * `TPlayerStatus playerStatus = null`: 自身のステータスを指定する
    * `TRoomMessage roomMessage = null`: ルームのメッセージを指定する
* `RequestEnterRoomAsync`
  * 既存のルームに入室するリクエストを行う
  * 返り値の型: `Task<IServerMsg.EnterRoomResponse<TRoomMessage>>`
  * 引数
    * `int roomId`: 入室するルームのID
    * `string password = null`: ルームのパスワード
    * `TPlayerStatus playerStatus = null`: 自身のステータスを指定する
* `RequestExitRoomAsync`
  * 現在のルームを退室するリクエストを行う
  * 返り値の型: `Task<IServerMsg.ExitRoomResponse>`
* `RequestStartPlayingAsync`
  * 現在のルームのゲームプレイを開始するリクエストを行う。このリクエストはルームのオーナーのみ行える。
  * 返り値の型: `Task<IServerMsg.OperateRoomResponse>`
* `RequestFinishPlayingAsync`
  * 現在のルームのゲームプレイを終了するリクエストを行う。このリクエストはルームのオーナーのみ行える。
  * 返り値の型: `Task<IServerMsg.OperateRoomResponse>`
* `RequestSetPlayerStatusAsync`
  * 自身のステータスをセットするリクエストを行う
  * 返り値の型: `Task<IServerMsg.SetPlayerStatusResponse>`
  * 引数
    * `TPlayerStatus status`: 指定する自身のステータス
* `RequestSetRoomMessageAsync`
  * ルームのメッセージをセットするリクエストを行う
  * 返り値の型: `Task<IServerMsg.SetRoomMessageResponse>`
  * 引数
    * `TRoomMessage roomMessage`: 指定するルームのメッセージ
* `SendGameMessageAsync`
  * ゲームメッセージを送信するリクエストを行う
  * 戻り値の型: `Task<IServerMsg.SendGameMessageResponse>`
  * 引数
    * `TMsg message`: 送信するゲームメッセージ

## IClientListenerについて

* `OnGameStarted`
  * ゲーム開始の通知を受け取ると実行される
* `OnGameFinished`
  * ゲーム終了の通知を受け取ると実行される
* `OnOwnerChanged`
  * ルームのオーナーが変更された通知を受け取ると実行される。新規にルームを作成して入室した際には通知されない。
  * 引数
    * `ulong? id`: 新しいオーナーのID
* `OnPlayerEntered`
  * ルームにプレイヤーが入室した通知を受け取ると実行される
  * 引数
    * `ulong id`: 入室したプレイヤーのID
    * `TPlayerStatus status`: 入室したプレイヤーのステータス
* `OnPlayerExited`
  * ルームからプレイヤーが退室した通知を受け取ると実行される
* `OnPlayerStatusUpdated`
  * プレイヤーのステータスが更新された通知を受け取ると実行される
  * 引数
    * `ulong id`: プレイヤーのID
    * `TPlayerStatus status`: プレイヤーの新しいステータス
* `OnRoomMessageUpdated`
  * ルームメッセージが更新された通知を受け取ると実行される
  * 引数
    * `TRoomMessage roomMessage`: 新しいルームメッセージ
* `OnGameMesageReceievd`
  * 各クライアントから送信されたゲームメッセージを受け取ったときに実行される。自分が送信したメッセージも含まれる。
  * 引数
    * `ulong clientId`: メッセージを送信したプレイヤーのID
    * `float elapsedSeconds`: サーバーがメッセージを受け取ったときの、ゲームスタートからのサーバー内経過時間
    * `TMsg message`: 送信されたゲームメッセージ
* `OnTicked`
  * サーバーの`NetConfig.UpdateTime`毎の更新のたびに送信される通知を受け取ると実行される。この通知は`RoomConfig.tickMessageEnabled`が有効のときのみ送信される。
  * 引数
    * `float elapsedSeconds`: ゲームスタートからのサーバー内経過時間

## 各IServerMsg.~Responseクラスについて

共通事項

* `bool IsSuccess`プロパティで、リクエストが成功したかどうかを取得可能
* `Result`プロパティで、リクエストに対する結果を表す列挙型を取得できる

各レスポンスのプロパティ一覧

* `CreateRoomResponse`
  * `int Id`: 作成したルームのID
  * `Result`
    * `Success`: 成功
    * `AlreadyEntered`: すでにルームに入室済み
* `EnterRoomResponse<TRoomMessage>`
  * `ulong? OwnerId`: ルームオーナーのID
  * `IReadOnlyDictionary<ulong, RoomPlayerStatus> Statuses`: 各プレイヤーのステータス
  * `TRoomMessage RoomMessage`: ルームメッセージ
  * `Result`
    * `Success`: 成功
    * `RoomNotFound`: 指定したルームIDが見つからなかった
    * `InvalidPassword`: パスワードが正しくない
    * `NumberOfPlayersLimitation`: ルームのプレイヤー人数が上限に達している
    * `AlreadyEntered`: すでにルームに入室済み
    * `InvalidRoomStatus`: 現在ルームに入室できない状態
* `ExitRoomResponse`
  * `Result`
    * `Success`: 成功
    * `PlayerOutOfRoom`: ルームに入室していない
* `OperateRoomResponse`
  * `Result`
    * `Success`: 成功
    * `PlayerOutOfRoom`: ルームに入室していない
    * `PlayerIsNotOwner`: ルームオーナーではない
    * `InvalidRoomStatus`: 現在のルームステータスでは実行不可能
    * `NotEnoughPeople`: ルームの人数が最低人数未満
* `SetPlayerStatusResponse`
  * `Result`
    * `Success`: 成功
    * `PlayerOutOfRoom`: ルームに入室していない
* `SetRoomMessageResponse`
  * `Result`
    * `Success`: 成功
    * `PlayerOutOfRoom`: ルームに入室していない
    * `PlayerIsNotOwner`: ルームオーナーではない
* `SendGameMessageResponse`
  * `Result`
    * `Success`: 成功
    * `PlayerOutOfRoom`: ルームに入室していない
    * `InvalidRoomStatus`: 現在のルームステータスでは実行不可能
