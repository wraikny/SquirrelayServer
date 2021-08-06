﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

using LiteNetLib;

using MessagePack;

using SquirrelayServer.Common;

namespace SquirrelayServer.Client
{
    

    public sealed class Client<TStatus, TMsg>
        where TStatus : class
    {
        private readonly NetConfig _netConfig;
        private readonly MessagePackSerializerOptions _serverSerializerOptions;
        private readonly MessagePackSerializerOptions _clientsSerializerOptions;
        private readonly NetManager _manager;

        private readonly List<RelayedGameMessage> _gameMessages;

        private MessageHandler<IClientMsg, IServerMsg> _messageHandler;
        private IGameMessageListener<TMsg> _listener;

        public CurrentRoomInfo<TStatus> CurrentRoom { get; private set; }

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

        public async Task Start(string host, IGameMessageListener<TMsg> listener)
        {
            if (IsStarted)
            {
                NetDebug.Logger.WriteNet(NetLogLevel.Info, "Client has already been started.");
                return;
            }

            IsStarted = true;


            _manager.Start();
            _manager.Connect(host, _netConfig.Port, _netConfig.ConnectionKey);

            _listener = listener;

            while (_messageHandler is null)
            {
                await Task.Delay(_netConfig.UpdateTime);
            }

            var hello = await _messageHandler.WaitMsgOfType<IServerMsg.Hello>();
            Id = hello.Id;
            RoomConfig = hello.RoomConfig;

            IsConnected = true;
        }

        public Task Start(string host, Action<ulong, float, TMsg> onReceived)
        {
            var listener = new EventBasedGameMessageListener<TMsg>();
            listener.OnReceived += onReceived;

            return Start(host, listener);
        }

        public void Stop()
        {
            _manager.Stop(true);

            _listener = null;
            _messageHandler.Cancel();
            _messageHandler = null;

            _gameMessages.Clear();

            Id = null;
            RoomConfig = null;

            IsStarted = false;
        }

        public void Update()
        {
            _manager?.PollEvents();

            if (!IsStarted || !IsConnected) return;

            List<Exception> exceptions = null;

            foreach (var m in _gameMessages)
            {
                try
                {
                    var message = MessagePackSerializer.Deserialize<TMsg>(m.Data, _clientsSerializerOptions);

                    try
                    {
                        _listener.OnReceived(m.ClientId, m.ElapsedSecond, message);
                    }
                    catch (Exception e)
                    {
                        exceptions ??= new List<Exception>();
                        exceptions.Add(e);
                    }
                }
                catch
                {
                    NetDebug.Logger.WriteNet(NetLogLevel.Error, $"Failed to deserialize gameMessage from client({m.ClientId}).");
                }
            }

            _gameMessages.Clear();

            if (exceptions is { })
            {
                throw new AggregateException(exceptions);
            }
        }

        public async Task<int> GetClientsCountAsync()
        {
            if (!IsConnected) throw new ClientNotConnectedException();

            _messageHandler.Send(IClientMsg.GetClientsCount.Instance);
            var res = await _messageHandler.WaitMsgOfType<IServerMsg.ClientsCountResponse>();
            return res.Count;
        }

        public async Task<IReadOnlyCollection<RoomInfo>> GetRoomListAsync()
        {
            if (!IsConnected) throw new ClientNotConnectedException();

            _messageHandler.Send(IClientMsg.GetRoomList.Instance);
            var res = await _messageHandler.WaitMsgOfType<IServerMsg.RoomListResponse>();
            return res.Info;
        }

        public async Task<IServerMsg.CreateRoomResponse> CreateRoomAsync(bool isVisible = true, string password = null, int? maxNumberOfPlayers = null, string message = null)
        {
            if (!IsConnected) throw new ClientNotConnectedException();

            isVisible = RoomConfig.InvisibleEnabled ? isVisible : true;
            password = RoomConfig.PasswordEnabled ? password : null;
            var maxNum = maxNumberOfPlayers ?? RoomConfig.NumberOfPlayersRange.Item2;
            message = RoomConfig.RoomMessageEnabled ? message : null;

            _messageHandler.Send(new IClientMsg.CreateRoom(isVisible, password, maxNum, message));
            var res = await _messageHandler.WaitMsgOfType<IServerMsg.CreateRoomResponse>();

            if (res.IsSuccess)
            {
                CurrentRoom = new CurrentRoomInfo<TStatus>(_clientsSerializerOptions, res.Id, Id, null);
            }

            return res;
        }

        public async Task<IServerMsg.EnterRoomResponse> EnterRoomAsync(int roomId, string password = null)
        {
            if (!IsConnected) throw new ClientNotConnectedException();

            _messageHandler.Send(new IClientMsg.EnterRoom(roomId, RoomConfig.PasswordEnabled ? password : null));
            var res = await _messageHandler.WaitMsgOfType<IServerMsg.EnterRoomResponse>();

            if (res.IsSuccess)
            {
                CurrentRoom = new CurrentRoomInfo<TStatus>(_clientsSerializerOptions, roomId, res.OwnerId, res.Statuses);
            }

            return res;
        }

        public Task<IServerMsg.ExitRoomResponse> ExitRoomAsync()
        {
            if (!IsConnected) throw new ClientNotConnectedException();

            _messageHandler.Send(IClientMsg.ExitRoom.Instance);
            return _messageHandler.WaitMsgOfType<IServerMsg.ExitRoomResponse>();
        }

        public Task<IServerMsg.OperateRoomResponse> StartPlayingAsync()
        {
            if (!IsConnected) throw new ClientNotConnectedException();

            _messageHandler.Send(IClientMsg.OperateRoom.StartPlaying);
            return _messageHandler.WaitMsgOfType<IServerMsg.OperateRoomResponse>();
        }

        public Task<IServerMsg.OperateRoomResponse> FinishPlayingAsync()
        {
            if (!IsConnected) throw new ClientNotConnectedException();

            _messageHandler.Send(IClientMsg.OperateRoom.FinishPlaying);
            return _messageHandler.WaitMsgOfType<IServerMsg.OperateRoomResponse>();
        }

        public Task<IServerMsg.SetPlayerStatusResponse> SetPlayerStatusAsync(TStatus status)
        {
            if (!IsConnected) throw new ClientNotConnectedException();

            var data = MessagePackSerializer.Serialize(status, _serverSerializerOptions);
            _messageHandler.Send(new IClientMsg.SetPlayerStatus(new RoomPlayerStatus { Data = data }));
            return _messageHandler.WaitMsgOfType<IServerMsg.SetPlayerStatusResponse>();
        }

        public Task<IServerMsg.SendGameMessageResponse> SendGameMessage(TMsg message)
        {
            if (!IsConnected) throw new ClientNotConnectedException();

            var data = MessagePackSerializer.Serialize(message, _serverSerializerOptions);
            _messageHandler.Send(new IClientMsg.SendGameMessage(data));
            return _messageHandler.WaitMsgOfType<IServerMsg.SendGameMessageResponse>();
        }


        private sealed class Listener : INetEventListener
        {
            private readonly Client<TStatus, TMsg> _client;

            public Listener(Client<TStatus, TMsg> client)
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

                NetDebug.Logger.WriteNet(NetLogLevel.Info, $"Connected to server.");
            }

            void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
            {
                _client.IsConnected = false;
                _client.Stop();

                NetDebug.Logger.WriteNet(NetLogLevel.Info, $"Disconnected from server.");
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
                    NetDebug.Logger.WriteNet(NetLogLevel.Error, "Failed to deserialize message from server.");
                    return;
                }
                finally
                {
                    reader.Recycle();
                }

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
                NetDebug.Logger.WriteNet(NetLogLevel.Error, $"NetworkError at {endPoint} with {Enum.GetName(typeof(SocketError), socketError)}.");
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