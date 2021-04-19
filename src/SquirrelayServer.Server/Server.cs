using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

using LiteNetLib;

using MessagePack;

using SquirrelayServer.Common;

namespace SquirrelayServer.Server
{
    internal sealed class Server : INetEventListener
    {
        private readonly Config _config;
        private ulong _clientIdNext;
        private readonly Dictionary<int, PeerHandler<IServerMsg, IClientMsg>> _clients;
        private readonly MessagePackSerializerOptions _options;

        public Server(Config config, MessagePackSerializerOptions options)
        {
            _config = config;
            _clients = new Dictionary<int, PeerHandler<IServerMsg, IClientMsg>>();
            _options = options;
        }

        public void Start()
        {

        }


        void INetEventListener.OnConnectionRequest(ConnectionRequest request)
        {
            // TODO
        }

        void INetEventListener.OnPeerConnected(NetPeer peer)
        {
            // TODO
            var id = _clientIdNext;
            _clientIdNext++;

            var client = new PeerHandler<IServerMsg, IClientMsg>(id, peer, _options);
            _clients[peer.Id] = client;
        }

        void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            _clients.Remove(peer.Id);
        }

        void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            var client = _clients[peer.Id];
            var clientMsg = MessagePackSerializer.Deserialize<IClientMsg>(reader.GetRemainingBytesSegment(), _options);

            // TODO

            reader.Recycle();
        }

        void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            // TODO
        }

        void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            // TODO
        }

        void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            _clients[peer.Id].Latency = latency;
        }
    }
}
