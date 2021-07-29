using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using LiteNetLib;

using MessagePack;

using SquirrelayServer.Common;

namespace SquirrelayServer.Server
{
    internal sealed class Server : INetEventListener
    {
        private readonly Config _config;
        private readonly MessagePackSerializerOptions _options;
        private readonly Dictionary<int, ClientHandler> _clients;
        private readonly Dictionary<ulong, ClientHandler> _clientsByClientId;
        private readonly RoomList _roomList;
        private readonly FPS _fps;

        private readonly NetManager _manager;

        private ulong _clientIdNext;

        public bool IsRunning { get; private set; }

        public Server(Config config, MessagePackSerializerOptions options)
        {
            _config = config;
            _options = options;

            _clientIdNext = 0;
            _clients = new Dictionary<int, ClientHandler>();
            _clientsByClientId = new Dictionary<ulong, ClientHandler>();

            _roomList = new RoomList(config.RoomConfig);

            _fps = new FPS(config.NetConfig.UpdateTime);

            _manager = new NetManager(this)
            {
                NatPunchEnabled = config.NetConfig.NatPunchEnabled,
                UpdateTime = config.NetConfig.UpdateTime,
                PingInterval = config.NetConfig.PingInterval,
                DisconnectTimeout = config.NetConfig.DisconnectedTimeout
            };

#if DEBUG
            var debugOnly = config.NetConfig.DebugOnly;
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

        public async ValueTask Start()
        {
            if (IsRunning) return;

            if (!_manager.Start(_config.NetConfig.Port))
            {
                throw new Exception("Failed to start the Server");
            }

            IsRunning = true;

            _fps.Start();

            _ = _roomList.StartUpdatingDisposeStatus();

            while (true)
            {
                _manager.PollEvents();

                Update();

                await _fps.Update();
            }
        }

        void Update()
        {
            // TODO: Update rooms that is started the game, and send messages to it's clients.
        }


        void INetEventListener.OnConnectionRequest(ConnectionRequest request)
        {
            if (_manager.ConnectedPeersCount < _config.NetConfig.MaxClientsCount)
            {
                request.AcceptIfKey(_config.NetConfig.ConnectionKey);
            }
            else
            {
                request.Reject();
            }
        }

        void INetEventListener.OnPeerConnected(NetPeer peer)
        {
            var id = _clientIdNext;
            _clientIdNext++;

            var sender = new NetPeerSender<IServerMsg>(peer, _options);
            var client = new ClientHandler(id, sender);
            _clients[peer.Id] = client;
            _clientsByClientId[id] = client;

            client.Send(new IServerMsg.ClientId(id));
        }

        void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            _clients.Remove(peer.Id, out var client);
            _clientsByClientId.Remove(client.Id);

            if (client.RoomId is { })
            {
                _roomList.ExitRoom(client);
            }
        }

        void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            var client = _clients[peer.Id];
            var clientMsg = MessagePackSerializer.Deserialize<IClientMsg>(reader.GetRemainingBytesSegment(), _options);
            reader.Recycle();

            switch (clientMsg)
            {
                case IClientMsg.SetPlayerStatus m:
                    {
                        var res = _roomList.SetPlayerStatus(client, m);
                        client.Send(res);
                        break;
                    }
                case IClientMsg.GetRoomList _:
                    {
                        var res = _roomList.GetRoomListInfo();
                        client.Send(res);
                        break;
                    }
                case IClientMsg.CreateRoom m:
                    {
                        var res = _roomList.CreateRoom(client, m);
                        client.Send(res);
                        break;
                    }
                case IClientMsg.EnterRoom m:
                    {
                        var res = _roomList.EnterRoom(client, m);
                        client.Send(res);
                        break;
                    }
                case IClientMsg.ExitRoom _:
                    {
                        var res = _roomList.ExitRoom(client);
                        client.Send(res);
                        break;
                    }
                case IClientMsg.OperateRoom m:
                    {
                        var res = _roomList.OperateRoom(client, m);
                        client.Send(res);
                        break;
                    }
                case IResponse _:
                    {
                        client.Receive(clientMsg);
                        break;
                    }
                default:
                    break;
            }
        }

        void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            NetDebug.Logger.WriteNet(NetLogLevel.Error, $"NetworkError at {endPoint} with {Enum.GetName(typeof(SocketError), socketError)}");
        }

        void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {

        }

        void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            _clients[peer.Id].Latency = latency;
        }
    }
}
