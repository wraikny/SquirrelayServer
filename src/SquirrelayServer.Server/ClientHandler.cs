using System;
using System.Reactive.Subjects;
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
            var subject = Subject.Synchronize(new Subject<IClientMsg>());
            var sender = new NetPeerSender<IServerMsg>(peer, options);
            _handler = new MessageHandler<IServerMsg, IClientMsg>(subject, sender)
            {
                Id = id,
            };
        }

        public void Receive(IClientMsg msg)
        {
            _handler.Receive(msg);
        }

        public void NotifyClientId()
        {
            _handler.Send(new IServerMsg.ClientId(_handler.Id.Value), 0, DeliveryMethod.ReliableOrdered);
        }
    }
}
