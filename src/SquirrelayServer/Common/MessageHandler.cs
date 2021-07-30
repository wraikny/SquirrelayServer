using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;

using System.Threading.Tasks;

using LiteNetLib;

namespace SquirrelayServer.Common
{
    /// <summary>
    /// Class to manage sending and receiving of messages
    /// </summary>
    internal sealed class MessageHandler<TSend, TRecv>
        where TSend : class
        where TRecv : class
    {
        private readonly ISubject<TRecv> _recvMsgs;

        private readonly ISender<TSend> _sender;

        public ulong? Id { get; set; }
        public int Latency { get; internal set; }

        public MessageHandler(ISubject<TRecv> subject, ISender<TSend> sender)
        {
            _recvMsgs = subject;
            _sender = sender;
        }

        /// <summary>
        /// Returns a task to be completed when a message of the specified type is received.
        /// </summary>
        public Task<URecv> WaitMsgOfType<URecv>()
            where URecv : TRecv
        {
            return _recvMsgs.Where(x => x is URecv).Select(x => (URecv)x).ToTask();
        }

        /// <summary>
        /// Add a message that was received
        /// </summary>
        public void Receive(TRecv msg)
        {
            _recvMsgs.OnNext(msg);
        }

        /// <summary>
        /// Send a message
        /// </summary>
        public void Send<USend>(USend msg, byte channelNumber, DeliveryMethod method)
            where USend : class, TSend
        {
            _sender.Send(msg, channelNumber, method);
        }

        /// <summary>
        /// Send a message of type USend and return a received response of type URecv
        /// </summary>
        public async ValueTask<URecv> SendWithResponseAsync<USend, URecv>(USend msg, byte channelNumber, DeliveryMethod method)
            where USend : class, TSend, IWithResponse<URecv>
            where URecv : class, TRecv, IResponse
        {
            _sender.Send(msg, channelNumber, method);
            return await WaitMsgOfType<URecv>();
        }
    }
}
