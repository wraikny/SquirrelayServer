using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;

using System.Threading.Tasks;

using LiteNetLib;

namespace SquirrelayServer.Common
{
    internal sealed class MessageHandler<TSend, TRecv>
        where TSend : class
        where TRecv : class
    {
        private readonly Subject<TRecv> _recvMsgs;

        private readonly ISender<TSend> _sender;

        public ulong? Id { get; set; }
        public int Latency { get; internal set; }

        public MessageHandler(ISender<TSend> sender)
        {
            _recvMsgs = new Subject<TRecv>();
            _sender = sender;
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

        public void Send<USend>(USend msg, byte channelNumber, DeliveryMethod method)
            where USend : class, TSend
        {
            _sender.Send(msg, channelNumber, method);
        }

        public async ValueTask<URecv> SendWithResponseAsync<USend, URecv>(USend msg, byte channelNumber, DeliveryMethod method)
            where USend : class, TSend, IWithResponse<URecv>
            where URecv : class, TRecv
        {
            _sender.Send(msg, channelNumber, method);
            return await WaitMsgOfType<URecv>();
        }
    }
}
