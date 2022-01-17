using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

using LiteNetLib;

using MessagePack;

using SquirrelayServer.Common;

namespace SquirrelayServer.Server
{
    public sealed class Server
    {
        private readonly Config _config;
        private readonly MessagePackSerializerOptions _options;

        // key is peer id
        private readonly Dictionary<int, ClientHandler> _clients;

        // key is client id
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

            _roomList = new RoomList(config.RoomConfig, config.ServerLoggingConfig, options);

            _fps = new FPS(config.NetConfig.UpdateTime);

            _manager = new NetManager(new Listener(this))
            {
                NatPunchEnabled = config.NetConfig.NatPunchEnabled,
                UpdateTime = config.NetConfig.UpdateTime,
                PingInterval = config.NetConfig.PingInterval,
                DisconnectTimeout = config.NetConfig.DisconnectTimeout
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

        private void WriteNet(NetLogLevel info, string str, params object[] args)
        {
            if (!_config.ServerLoggingConfig.Logging || !_config.ServerLoggingConfig.ServerLogging) return;
            NetDebug.Logger?.WriteNet(info, str, args);
        }

        // Start the server.
        public async Task Start()
        {
            if (IsRunning)
            {
                WriteNet(NetLogLevel.Info, "Server has already been running.");
                return;
            }

            if (!_manager.Start(_config.NetConfig.Port))
            {
                throw new Exception("Failed to start the Server.");
            }

            WriteNet(NetLogLevel.Info, $"Server started at port {_config.NetConfig.Port}.");

            IsRunning = true;

            _roomList.Start();

            _fps.Start();

            while (true)
            {
                _manager.PollEvents();

                _roomList.Update();

                await _fps.Update();
            }
        }

        // Stop the server.
        public void Stop()
        {
            WriteNet(NetLogLevel.Info, "Server stop.");

            _manager.Stop(true);

            _clientIdNext = 0;
            _clients.Clear();
            _clientsByClientId.Clear();
            _roomList.Stop();
            _fps.Reset();

            IsRunning = false;
        }

        private sealed class Listener : INetEventListener
        {
            private readonly Server _server;

            public Listener(Server server)
            {
                _server = server;
            }

            void INetEventListener.OnConnectionRequest(ConnectionRequest request)
            {
                if (_server._manager.ConnectedPeersCount < _server._config.NetConfig.MaxClientsCount)
                {
                    if (request.AcceptIfKey(_server._config.NetConfig.ConnectionKey) is { })
                    {
                        _server.WriteNet(NetLogLevel.Info, "Accepted connection request.");
                    }
                    else
                    {
                        _server.WriteNet(NetLogLevel.Info, "Rejected connection request.");
                    }
                }
                else
                {
                    request.Reject();

                    _server.WriteNet(NetLogLevel.Info, "Rejected connection request because of the MaxClientsCount.");

                }
            }

            void INetEventListener.OnPeerConnected(NetPeer peer)
            {
                var id = _server._clientIdNext;
                _server._clientIdNext++;

                _server.WriteNet(NetLogLevel.Info, $"Client({id}) connected (Address = {peer.EndPoint.Address}).");

                var sender = new NetPeerSender<IServerMsg>(peer, _server._options);
                var client = new ClientHandler(id, sender, _server._config.ServerLoggingConfig);
                _server._clients[peer.Id] = client;
                _server._clientsByClientId[id] = client;

                client.Send(new IServerMsg.Hello(id, _server._config.RoomConfig));
            }

            void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
            {
                _server._clients.Remove(peer.Id, out var client);
                if (client is null)
                {
                    _server.WriteNet(NetLogLevel.Info, $"Client of Peer({peer.Id}) is not found.");
                    return;
                }
                _server._clientsByClientId.Remove(client.Id);

                if (client.RoomId is { })
                {
                    _server._roomList.ExitRoom(client);
                }
                _server.WriteNet(NetLogLevel.Info, $"Client({client.Id}) disconnected (Address = {peer.EndPoint.Address}) because '{disconnectInfo.Reason}'.");
            }

            void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
            {
                var client = _server._clients[peer.Id];

                try
                {
                    IClientMsg? clientMsg = null;

                    try
                    {
                        clientMsg = MessagePackSerializer.Deserialize<IClientMsg>(reader.GetRemainingBytesSegment(), _server._options);
                    }
                    catch
                    {
                        _server.WriteNet(NetLogLevel.Error, $"Failed to deserialize message from client({client.Id}).");
                        return;
                    }
                    finally
                    {
                        reader.Recycle();
                    }

                    switch (clientMsg)
                    {
                        case IClientMsg.GetClientsCount _:
                            {
                                var res = new IServerMsg.ClientsCountResponse(_server._clients.Count);
                                client.Send(res);
                                break;
                            }
                        case IClientMsg.GetRoomList _:
                            {
                                var res = _server._roomList.GetRoomInfoList();
                                client.Send(res);
                                break;
                            }
                        case IClientMsg.CreateRoom m:
                            {
                                var res = _server._roomList.CreateRoom(client, m);
                                client.Send(res);
                                break;
                            }
                        case IClientMsg.EnterRoom m:
                            {
                                var res = _server._roomList.EnterRoom(client, m);
                                client.Send(res);
                                break;
                            }
                        case IClientMsg.ExitRoom _:
                            {
                                var res = _server._roomList.ExitRoom(client);
                                client.Send(res);
                                break;
                            }
                        case IClientMsg.OperateRoom m:
                            {
                                var res = _server._roomList.OperateRoom(client, m);
                                client.Send(res);
                                break;
                            }
                        case IClientMsg.SetPlayerStatus m:
                            {
                                var res = _server._roomList.SetPlayerStatus(client, m);
                                client.Send(res);
                                break;
                            }
                        case IClientMsg.SetRoomMessage m:
                            {
                                var res = _server._roomList.SetRoomStatus(client, m);
                                client.Send(res);
                                break;
                            }
                        case IClientMsg.SendGameMessage m:
                            {
                                var res = _server._roomList.ReceiveGameMessage(client, m);
                                client.Send(res);
                                break;
                            }
                        //case IResponse _:
                        //    {
                        //        client.Receive(clientMsg);
                        //        break;
                        //    }
                        default:
                            break;
                    }
                }
                catch (Exception e)
                {
                    NetDebug.Logger?.WriteNet(NetLogLevel.Error, $"Error occured while handling client's message\n{e.Message}\n{e.StackTrace}");
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
                _server._clients[peer.Id].Latency = latency;
            }

        }
    }
}
