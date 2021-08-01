using LiteNetLib;

using MessagePack;

namespace SquirrelayServer.Common
{
    public interface ISender<T>
    {
        void Send<U>(U message, byte channelNumber = 0, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered) where U : T;
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

        void ISender<T>.Send<U>(U message, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            var data = MessagePackSerializer.Serialize<T>(message, _options);
            _peer.Send(data, channelNumber, deliveryMethod);
        }
    }
}
