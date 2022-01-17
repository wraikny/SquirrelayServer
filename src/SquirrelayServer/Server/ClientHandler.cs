using System;

using LiteNetLib;

using SquirrelayServer.Common;

namespace SquirrelayServer.Server
{

    public interface IClientHandler
    {
        public ulong Id { get; }

        public int? RoomId { get; set; }

        void Send(IServerMsg msg, byte channel = 0, DeliveryMethod method = DeliveryMethod.ReliableOrdered);

        void SendByte(byte[] data, byte channel = 0, DeliveryMethod method = DeliveryMethod.ReliableOrdered);
    }

    /// <summary>
    /// Class that sends and receives messages for each client
    /// </summary>
    internal sealed class ClientHandler : IClientHandler
    {
        private readonly ServerLoggingConfig _loggingConfig;
        private readonly MessageHandler<IServerMsg, IClientMsg> _handler;

        public ulong Id { get; internal set; }

        private int? _roomId;

        public int? RoomId
        {
            get => _roomId;
            set
            {
                if ((value is { }) && (_roomId is { }))
                {
                    throw new InvalidOperationException($"Client '{Id}' has been already in room '{_roomId}'");
                }

                _roomId = value;
            }
        }

        public int Latency { get; internal set; }

        public ClientHandler(ulong id, NetPeerSender<IServerMsg> sender, ServerLoggingConfig loggingConfig)
        {
            _loggingConfig = loggingConfig;
            
            Id = id;

            //var subject = Subject.Synchronize(new Subject<IClientMsg>());
            _handler = new MessageHandler<IServerMsg, IClientMsg>();
            _handler.SetSender(sender);
        }

        //public void Receive(IClientMsg msg)
        //{
        //    _handler.Receive(msg);
        //}

        public void Send(IServerMsg msg, byte channel = 0, DeliveryMethod method = DeliveryMethod.ReliableOrdered)
        {
            _handler.Send(msg, channel, method);
            if (_loggingConfig.Logging && _loggingConfig.MessageLogging)
            {
                NetDebug.Logger?.WriteNet(NetLogLevel.Info, $"Send message of {msg.GetType()} to client({Id}).");
            }
        }

        public void SendByte(byte[] data, byte channel = 0, DeliveryMethod method = DeliveryMethod.ReliableOrdered)
        {
            _handler.SendByte(data, channel, method);
        }
    }
}
