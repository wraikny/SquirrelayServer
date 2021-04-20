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

            while (true)
            {
                _manager.PollEvents();

                // Todo: Update

                await _fps.Update();
            }
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

            var client = new ClientHandler(id, peer, _options);
            _clients[peer.Id] = client;

            client.NotifyClientId();
        }

        void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            _clients.Remove(peer.Id);

            // TODO: remove from room is client had been in a room.
        }

        void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            var client = _clients[peer.Id];
            var clientMsg = MessagePackSerializer.Deserialize<IClientMsg>(reader.GetRemainingBytesSegment(), _options);

            // TODO

            client.Receive(clientMsg);

            reader.Recycle();
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
