using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

using LiteNetLib;

using MessagePack;

using SquirrelayServer.Common;

namespace SquirrelayServer.Client
{
    public sealed class Client<TPlayerStatus, TRoomMessage, TMsg>
        where TPlayerStatus : class
        where TRoomMessage : class
    {
        private readonly NetConfig _netConfig;
        private readonly MessagePackSerializerOptions _serverSerializerOptions;
        private readonly MessagePackSerializerOptions _clientsSerializerOptions;
        private readonly NetManager _manager;

        private readonly Queue<Action> _updateContext;

        private readonly List<RelayedGameMessage> _gameMessages;

        private MessageHandler<IClientMsg, IServerMsg> _messageHandler;
        private IClientListener<TPlayerStatus, TRoomMessage, TMsg> _listener;

        public CurrentRoomInfo<TPlayerStatus, TRoomMessage> CurrentRoom { get; private set; }

        public ulong? Id { get; private set; }

        public RoomConfig RoomConfig { get; private set; }

        public int Latency { get; private set; }

        private bool IsStarted { get; set; }

        public bool IsConnected { get; private set; }

        public bool IsOwner => CurrentRoom?.OwnerId == Id;

        public Client(NetConfig netConfig, MessagePackSerializerOptions serverSerializerOptions, MessagePackSerializerOptions clientsSerializerOptions)
        {
            _netConfig = netConfig;
            _serverSerializerOptions = serverSerializerOptions;
            _clientsSerializerOptions = clientsSerializerOptions;

            _gameMessages = new List<RelayedGameMessage>();

            Id = null;
            IsStarted = false;

            _updateContext = new Queue<Action>();

            _manager = new NetManager(new Listener(this))
            {
                NatPunchEnabled = netConfig.NatPunchEnabled,
                UpdateTime = netConfig.UpdateTime,
                PingInterval = netConfig.PingInterval,
                DisconnectTimeout = netConfig.DisconnectedTimeout
            };

#if DEBUG
            var debugOnly = netConfig.DebugOnly;
            if (debugOnly.SimulationPacketLossChance is int chance)
            {
                _manager.SimulatePacketLoss = true;
                _manager.SimulationPacketLossChance = chance;
            }

            if (debugOnly.SimulationLatencyRange is (int min, int max))
            {
                _manager.SimulateLatency = true;
                _manager.SimulationMinLatency = min;
                _manager.SimulationMaxLatency = max;
            }
#endif
        }

        /// <summary>
        /// Start the connection to the server
        /// </summary>
        /// <param name="host"></param>
        /// <param name="listener"></param>
        /// <returns></returns>
        public async Task Start(string host, IClientListener<TPlayerStatus, TRoomMessage, TMsg> listener)
        {
            if (IsStarted)
            {
                NetDebug.Logger?.WriteNet(NetLogLevel.Info, "Client has already been started.");
                return;
            }

            IsStarted = true;


            _manager.Start();
            _manager.Connect(host, _netConfig.Port, _netConfig.ConnectionKey);

            _listener = listener;

            NetDebug.Logger?.WriteNet(NetLogLevel.Info, "Client is waiting to be connected.");
            while (_messageHandler is null)
            {
                await Task.Delay(_netConfig.UpdateTime);
            }

            var hello = await _messageHandler.WaitMsgOfType<IServerMsg.Hello>();
            Id = hello.Id;
            RoomConfig = hello.RoomConfig;

            NetDebug.Logger?.WriteNet(NetLogLevel.Info, $"Hello from server, received self id({Id}).");

            IsConnected = true;
        }

        /// <summary>
        /// Stop the connection to the server
        /// </summary>
        public void Stop()
        {
            if (!IsStarted) return;

            NetDebug.Logger?.WriteNet(NetLogLevel.Info, "Client stop.");

            _manager.DisconnectAll();
            _manager.Stop(true);

            _updateContext.Clear();

            _listener = null;

            if (_messageHandler is { })
            {
                _messageHandler.Cancel();
                _messageHandler = null;
            }

            _gameMessages.Clear();

            Id = null;
            RoomConfig = null;

            IsStarted = false;
        }

        /// <summary>
        /// Update the client to handle messages from the server.
        /// </summary>
        public void Update()
        {
            if (_manager.IsRunning)
            {
                _manager?.PollEvents();
            }

            if (!IsStarted || !IsConnected) return;

            List<Exception> exceptions = null;

            while (_updateContext.TryDequeue(out var action))
            {
                NetDebug.Logger?.WriteNet(NetLogLevel.Info, $"Client updateContext");
                try
                {
                    action?.Invoke();
                }
                catch (Exception e)
                {
                    exceptions ??= new List<Exception>();
                    exceptions.Add(e);
                }
            }

            foreach (var m in _gameMessages)
            {
                try
                {
                    var message = MessagePackSerializer.Deserialize<TMsg>(m.Data, _clientsSerializerOptions);

                    try
                    {
                        _listener.OnGameMessageReceived(m.ClientId, m.ElapsedSecond, message);
                    }
                    catch (Exception e)
                    {
                        exceptions ??= new List<Exception>();
                        exceptions.Add(e);
                    }
                }
                catch
                {
                    NetDebug.Logger?.WriteNet(NetLogLevel.Error, $"Failed to deserialize gameMessage from client({m.ClientId}).");
                }
            }

            _gameMessages.Clear();

            if (exceptions is { })
            {
                throw new AggregateException(exceptions);
            }
        }

        /// <summary>
        /// Request to retrieve the number of clients currently connected to the server.
        /// </summary>
        /// <returns></returns>
        public async Task<int> RequestGetClientsCountAsync()
        {
            if (!IsConnected) throw new ClientNotConnectedException();

            _messageHandler.Send(IClientMsg.GetClientsCount.Instance);
            var res = await _messageHandler.WaitMsgOfType<IServerMsg.ClientsCountResponse>();
            return res.Count;
        }

        /// <summary>
        /// Request to retrieve a list of rooms. Rooms that are set to invisible cannot be retrieved.
        /// </summary>
        /// <returns></returns>
        public async Task<IReadOnlyCollection<RoomInfo<TRoomMessage>>> RequestGetRoomListAsync()
        {
            if (!IsConnected) throw new ClientNotConnectedException();

            _messageHandler.Send(IClientMsg.GetRoomList.Instance);
            var res = await _messageHandler.WaitMsgOfType<IServerMsg.RoomListResponse>();

            var infoList = new List<RoomInfo<TRoomMessage>>();
            foreach (var info in res.Info)
            {
                TRoomMessage message = null;
                try
                {
                    message = MessagePackSerializer.Deserialize<TRoomMessage>(info.Message, _clientsSerializerOptions);
                }
                catch
                {

                }

                infoList.Add(new RoomInfo<TRoomMessage>(info, message));
            }

            return infoList;
        }

        /// <summary>
        /// Request to create a new room and to enter it.
        /// </summary>
        /// <param name="isVisible"></param>
        /// <param name="password"></param>
        /// <param name="maxNumberOfPlayers"></param>
        /// <param name="playerStatus"></param>
        /// <param name="roomMessage"></param>
        /// <returns></returns>
        public async Task<IServerMsg.CreateRoomResponse> RequestCreateRoomAsync(bool isVisible = true, string password = null, int? maxNumberOfPlayers = null, TPlayerStatus playerStatus = null, TRoomMessage roomMessage = null)
        {
            if (!IsConnected) throw new ClientNotConnectedException();

            isVisible = RoomConfig.InvisibleEnabled ? isVisible : true;
            password = RoomConfig.PasswordEnabled ? password : null;
            var maxNum = maxNumberOfPlayers ?? RoomConfig.NumberOfPlayersRange.Item2;
            var playerStatusData = playerStatus is null ? null : MessagePackSerializer.Serialize(playerStatus, _clientsSerializerOptions);
            var roomMessageData = RoomConfig.RoomMessageEnabled ? MessagePackSerializer.Serialize(roomMessage, _clientsSerializerOptions) : null;


            _messageHandler.Send(new IClientMsg.CreateRoom(isVisible, password, maxNum, playerStatusData, roomMessageData));
            var res = await _messageHandler.WaitMsgOfType<IServerMsg.CreateRoomResponse>();

            if (res.IsSuccess)
            {
                CurrentRoom = CreateCurrentRoomInfo(res.Id, Id, null, null);
            }

            return res;
        }

        /// <summary>
        /// Request to enter the specified room.
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="password"></param>
        /// <param name="playerStatus"></param>
        /// <returns></returns>
        public async Task<IServerMsg.EnterRoomResponse> RequestEnterRoomAsync(int roomId, string password = null, TPlayerStatus playerStatus = null)
        {
            if (!IsConnected) throw new ClientNotConnectedException();

            var playerStatusData = playerStatus is null ? null : MessagePackSerializer.Serialize(playerStatus, _clientsSerializerOptions);

            _messageHandler.Send(new IClientMsg.EnterRoom(roomId, RoomConfig.PasswordEnabled ? password : null, playerStatusData));
            var res = await _messageHandler.WaitMsgOfType<IServerMsg.EnterRoomResponse>();

            if (res.IsSuccess)
            {
                CurrentRoom = CreateCurrentRoomInfo(roomId, res.OwnerId, res.Statuses, res.RoomMessage);
            }

            return res;
        }

        /// <summary>
        /// Request to leave the room.
        /// </summary>
        /// <returns></returns>
        public Task<IServerMsg.ExitRoomResponse> RequestExitRoomAsync()
        {
            if (!IsConnected) throw new ClientNotConnectedException();

            _messageHandler.Send(IClientMsg.ExitRoom.Instance);
            return _messageHandler.WaitMsgOfType<IServerMsg.ExitRoomResponse>();
        }

        /// <summary>
        /// Request to start a game in a room. This operation can only be performed by the owner.
        /// </summary>
        /// <returns></returns>
        public Task<IServerMsg.OperateRoomResponse> RequestStartPlayingAsync()
        {
            if (!IsConnected) throw new ClientNotConnectedException();

            _messageHandler.Send(IClientMsg.OperateRoom.StartPlaying);
            return _messageHandler.WaitMsgOfType<IServerMsg.OperateRoomResponse>();
        }

        /// <summary>
        /// Request to finish a game in a room. This operation can only be performed by the owner.
        /// </summary>
        /// <returns></returns>
        public Task<IServerMsg.OperateRoomResponse> RequestFinishPlayingAsync()
        {
            if (!IsConnected) throw new ClientNotConnectedException();

            _messageHandler.Send(IClientMsg.OperateRoom.FinishPlaying);
            return _messageHandler.WaitMsgOfType<IServerMsg.OperateRoomResponse>();
        }

        /// <summary>
        /// Request to set the client's own status in the room.
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public Task<IServerMsg.SetPlayerStatusResponse> RequestSetPlayerStatusAsync(TPlayerStatus status)
        {
            if (!IsConnected) throw new ClientNotConnectedException();

            var data = MessagePackSerializer.Serialize(status, _clientsSerializerOptions);
            _messageHandler.Send(new IClientMsg.SetPlayerStatus(new RoomPlayerStatus { Data = data }));
            return _messageHandler.WaitMsgOfType<IServerMsg.SetPlayerStatusResponse>();
        }

        /// <summary>
        /// Request to set a message for the room. This operation can only be performed by the owner.
        /// </summary>
        /// <param name="roomMessage"></param>
        /// <returns></returns>
        public Task<IServerMsg.SetRoomMessageResponse> RequestSetRoomMessageAsync(TRoomMessage roomMessage)
        {
            if (!IsConnected) throw new ClientNotConnectedException();

            var data = MessagePackSerializer.Serialize(roomMessage, _clientsSerializerOptions);
            _messageHandler.Send(new IClientMsg.SetRoomMessage(data));
            return _messageHandler.WaitMsgOfType<IServerMsg.SetRoomMessageResponse>();
        }

        /// <summary>
        /// Request to send a game message to be broadcasted.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public Task<IServerMsg.SendGameMessageResponse> RequestSendGameMessage(TMsg message)
        {
            if (!IsConnected) throw new ClientNotConnectedException();

            var data = MessagePackSerializer.Serialize(message, _serverSerializerOptions);
            _messageHandler.Send(new IClientMsg.SendGameMessage(data));
            return _messageHandler.WaitMsgOfType<IServerMsg.SendGameMessageResponse>();
        }

        private CurrentRoomInfo<TPlayerStatus, TRoomMessage> CreateCurrentRoomInfo(int id, ulong? ownerId, IReadOnlyDictionary<ulong, RoomPlayerStatus> statuses, byte[] roomMessage)
        {
            var currentRoomInfo = new CurrentRoomInfo<TPlayerStatus, TRoomMessage>(id, ownerId);

            if (statuses is { })
            {
                foreach (var (k, v) in statuses)
                {
                    if (v is null) continue;

                    if (v.Data is null)
                    {
                        currentRoomInfo.PlayerStatusesImpl[k] = null;
                    }
                    else
                    {
                        try
                        {
                            currentRoomInfo.PlayerStatusesImpl[k] = MessagePackSerializer.Deserialize<TPlayerStatus>(v.Data, _clientsSerializerOptions);
                        }
                        catch
                        {
                            //currentRoomInfo.PlayerStatusesImpl[k] = null;
                            NetDebug.Logger?.WriteNet(NetLogLevel.Error, $"Failed to deserialize player status from client({k}).");
                        }
                    }

                }
            }

            currentRoomInfo.SetRoomMessage(_clientsSerializerOptions, roomMessage);

            return currentRoomInfo;
        }

        private void UpdateCurrentRoomInfo(CurrentRoomInfo<TPlayerStatus, TRoomMessage> currentRoomInfo, IServerMsg.UpdateRoomPlayersAndMessage msg)
        {
            if (currentRoomInfo.OwnerId != msg.Owner)
            {
                currentRoomInfo.OwnerId = msg.Owner;
                _updateContext.Enqueue(() => { _listener.OnOwnerChanged(msg.Owner); });
            }

            foreach (var (k, v) in msg.Statuses)
            {
                if (v is null)
                {
                    currentRoomInfo.PlayerStatusesImpl.Remove(k);
                    _updateContext.Enqueue(() => { _listener.OnPlayerExited(k); });
                }
                else
                {
                    TPlayerStatus status = null;

                    if (v.Data != null)
                    {
                        try
                        {
                            status = MessagePackSerializer.Deserialize<TPlayerStatus>(v.Data, _clientsSerializerOptions);
                        }
                        catch
                        {
                            NetDebug.Logger?.WriteNet(NetLogLevel.Error, $"Failed to deserialize player status from client({k}).");
                            continue;
                        }
                    }

                    if (currentRoomInfo.PlayerStatusesImpl.ContainsKey(k))
                    {
                        _updateContext.Enqueue(() => { _listener.OnPlayerEntered(k, status); });
                    }
                    else
                    {
                        _updateContext.Enqueue(() => { _listener.OnPlayerStatusUpdated(k, status); });
                    }

                    currentRoomInfo.PlayerStatusesImpl[k] = status;
                }
            }

            currentRoomInfo.SetRoomMessage(_clientsSerializerOptions, msg.RoomStatus);
        }

        private sealed class Listener : INetEventListener
        {
            private readonly Client<TPlayerStatus, TRoomMessage, TMsg> _client;

            public Listener(Client<TPlayerStatus, TRoomMessage, TMsg> client)
            {
                _client = client;
            }

            void INetEventListener.OnConnectionRequest(ConnectionRequest request)
            {
                request.Reject();
            }

            void INetEventListener.OnPeerConnected(NetPeer peer)
            {
                var sender = new NetPeerSender<IClientMsg>(peer, _client._serverSerializerOptions);
                _client._messageHandler = new MessageHandler<IClientMsg, IServerMsg>(sender);

                NetDebug.Logger?.WriteNet(NetLogLevel.Info, $"Connected to the server.");
            }

            void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
            {
                _client.IsConnected = false;
                _client.Stop();

                NetDebug.Logger?.WriteNet(NetLogLevel.Info, $"Disconnected from server.");
            }

            void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
            {
                IServerMsg msg = null;
                try
                {
                    msg = MessagePackSerializer.Deserialize<IServerMsg>(reader.GetRemainingBytesSegment(), _client._serverSerializerOptions);
                }
                catch
                {
                    NetDebug.Logger?.WriteNet(NetLogLevel.Error, "Failed to deserialize message from server.");
                    return;
                }
                finally
                {
                    reader.Recycle();
                }

                NetDebug.Logger?.WriteNet(NetLogLevel.Error, $"Received message of {msg.GetType()}.");

                switch (msg)
                {
                    case IServerMsg.BroadcastGameMessages m:
                        {
                            _client._gameMessages.AddRange(m.Messages);
                            break;
                        }
                    case IServerMsg.NotifyRoomOperation m:
                        {
                            _client.CurrentRoom?.OnNotifiedRoomOperation(m.Operate);
                            switch (m.Operate)
                            {
                                case RoomOperateKind.StartPlaying:
                                    _client._updateContext.Enqueue(() => _client._listener.OnGameStarted());
                                    break;
                                case RoomOperateKind.FinishPlaying:
                                    _client._updateContext.Enqueue(() => _client._listener.OnGameFinished());
                                    break;
                                default: break;
                            }
                            break;
                        }
                    case IServerMsg.UpdateRoomPlayersAndMessage m:
                        {
                            if (_client.CurrentRoom is { } r)
                            {
                                _client.UpdateCurrentRoomInfo(r, m);
                            }
                            break;
                        }
                    default:
                        {
                            _client._messageHandler.Receive(msg);
                            break;
                        }
                }
            }

            void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
            {
                NetDebug.Logger?.WriteNet(NetLogLevel.Error, $"NetworkError at {endPoint} with {Enum.GetName(typeof(SocketError), socketError)}.");
            }

            void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
            {

            }

            void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency)
            {
                _client.Latency = latency;
            }
        }
    }
}
