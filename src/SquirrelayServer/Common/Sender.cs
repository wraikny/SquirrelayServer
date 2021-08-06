using LiteNetLib;

using MessagePack;

namespace SquirrelayServer.Common
{
    public interface ISender<T>
    {
        void Send(T message, byte channelNumber = 0, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered);

        void SendByte(byte[] data, byte channelNumber, DeliveryMethod deliveryMethod);
    }

    /// <summary>
    /// Class for sending messages using NetPeer of LiteNetLib
    /// </summary>
    internal class NetPeerSender<T> : ISender<T>
    {
        private readonly NetPeer _peer;
        private readonly MessagePackSerializerOptions _options;

        public NetPeerSender(NetPeer peer, MessagePackSerializerOptions options)
        {
            _peer = peer;
            _options = options;
        }

        void ISender<T>.Send(T message, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            var data = MessagePackSerializer.Serialize(message, _options);
            _peer.Send(data, channelNumber, deliveryMethod);
        }

        void ISender<T>.SendByte(byte[] data, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            _peer.Send(data, channelNumber, deliveryMethod);
        }
    }
}
