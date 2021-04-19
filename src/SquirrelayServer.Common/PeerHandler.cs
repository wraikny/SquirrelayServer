using System.Collections.Generic;
using System.Threading.Tasks;

using LiteNetLib;

using MessagePack;

namespace SquirrelayServer.Common
{
    internal sealed class PeerHandler<TSend, TRecv>
        where TSend : class
        where TRecv : class
    {
        private readonly List<TRecv> _messages;
        private readonly NetPeer _peer;
        private readonly MessagePackSerializerOptions _options;

        public ulong Id { get; private set; }
        public int Latency { get; internal set; }

        public PeerHandler(ulong id, NetPeer peer, MessagePackSerializerOptions options)
        {
            Id = id;
            _messages = new List<TRecv>();
            _peer = peer;
            _options = options;
        }

        private bool TryFindByType<U>(out U res)
            where U : class, TRecv
        {
            foreach (var m in _messages)
            {
                if (m is U t)
                {
                    res = t;
                    return true;
                }
            }
            res = null;
            return false;
        }

        public void Send<USend>(USend msg, DeliveryMethod method)
            where USend : class, TSend
        {
            var data = MessagePackSerializer.Serialize<TSend>(msg, _options);
            _peer.Send(data, method);
        }

        public async ValueTask<URecv> SendWithResponse<USend, URecv>(USend msg, DeliveryMethod method)
            where USend : class, TSend
            where URecv : class, TRecv
        {
            Send(msg, method);

            URecv res;
            while (!TryFindByType(out res))
            {
                await Task.Yield();
            }

            return res;
        }
    }
}
