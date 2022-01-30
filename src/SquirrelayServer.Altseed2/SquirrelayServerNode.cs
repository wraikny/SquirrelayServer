using Altseed2;

using MessagePack;

using SquirrelayServer.Client;
using SquirrelayServer.Common;

namespace SquirrelayServer.Altseed2
{
    public sealed class SquirrelayServerNode<TMsg> : TransformNode
    {
        public Client<Message, Message, TMsg> Client { get; private init; }

        public SquirrelayServerNode(
            NetConfig netConfig,
            MessagePackSerializerOptions serverSerializerOptions,
            MessagePackSerializerOptions clientSerializerOptions,
            IClientListener<Message, Message, TMsg> listener
        )
        {
            Client = new(netConfig, serverSerializerOptions, clientSerializerOptions, listener);
        }
    }
}
