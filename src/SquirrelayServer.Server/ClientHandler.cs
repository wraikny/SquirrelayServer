using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using SquirrelayServer.Common;

using LiteNetLib;

using MessagePack;

namespace SquirrelayServer.Server
{
    internal sealed class ClientHandler
    {
        private readonly MessageHandler<IServerMsg, IClientMsg> _handler;

        public int Latency
        {
            get => _handler.Latency;
            set => _handler.Latency = value;
        }

        public ClientHandler(ulong id, NetPeer peer, MessagePackSerializerOptions options)
        {
            var sender = new NetPeerSender<IServerMsg>(peer, options);
            _handler = new MessageHandler<IServerMsg, IClientMsg>(sender)
            {
                Id = id,
            };
        }

        public void Receive(IClientMsg msg)
        {
            _handler.Receive(msg);
        }
    }
}
