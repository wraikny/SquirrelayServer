using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

using LiteNetLib;

using MessagePack;

using SquirrelayServer.Common;

namespace SquirrelayServer.Server
{
    /// <summary>
    /// Class that sends and receives messages for each client
    /// </summary>
    internal sealed class ClientHandler : IPlayer
    {
        private readonly MessageHandler<IServerMsg, IClientMsg> _handler;

        public ulong Id => _handler.Id.Value;

        private int? _roomId;

        public int? RoomId
        {
            get => _roomId;
            set
            {
                if ((value is { }) && (_roomId is { }))
                {
                    throw new InvalidOperationException($"Client '{_handler.Id}' has been already in room '{_roomId}'");
                }

                _roomId = value;
            }
        }

        public int Latency
        {
            get => _handler.Latency;
            set => _handler.Latency = value;
        }

        public ClientHandler(ulong id, NetPeerSender<IServerMsg> sender)
        {
            var subject = Subject.Synchronize(new Subject<IClientMsg>());
            _handler = new MessageHandler<IServerMsg, IClientMsg>(subject, sender)
            {
                Id = id,
            };
        }

        public void Receive(IClientMsg msg)
        {
            _handler.Receive(msg);
        }

        public void Send(IServerMsg msg, byte channel = 0, DeliveryMethod method = DeliveryMethod.ReliableSequenced)
        {
            _handler.Send(msg, channel, method);
        }
    }
}
