using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;

using System.Threading.Tasks;

using LiteNetLib;

using MessagePack;

namespace SquirrelayServer.Common
{
    internal sealed class PeerHandler<TSend, TRecv>
        where TSend : class
        where TRecv : class
    {
        private readonly Subject<TRecv> _recvMsgs;

        private readonly NetPeer _peer;
        private readonly MessagePackSerializerOptions _options;

        public ulong Id { get; private set; }
        public int Latency { get; internal set; }

        public PeerHandler(ulong id, NetPeer peer, MessagePackSerializerOptions options)
        {
            Id = id;
            _recvMsgs = new Subject<TRecv>();
            _peer = peer;
            _options = options;
        }

        public Task<URecv> WaitMsgOfType<URecv>()
            where URecv : TRecv
        {
            return _recvMsgs.Where(x => x is URecv).Select(x => (URecv)x).ToTask();
        }

        public void Receive(TRecv msg)
        {
            _recvMsgs.OnNext(msg);
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
            return await WaitMsgOfType<URecv>();
        }
    }
}
