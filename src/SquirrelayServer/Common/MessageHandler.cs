using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using LiteNetLib;

namespace SquirrelayServer.Common
{
    /// <summary>
    /// Class to manage sending and receiving of messages
    /// </summary>
    internal sealed class MessageHandler<TSend, TRecv> : ISender<TSend>
        where TSend : class
        where TRecv : class
    {
        private sealed class Receiver
        {
            public Type Type { get; set; }

            public TRecv? Msg { get; set; }

            public bool IsCanceled { get; set; } = false;

            public Receiver(Type type)
            {
                Type = type;
            }
        }

        private readonly List<Receiver> _receivers;

        private ISender<TSend>? _sender;

        public MessageHandler()
        {
            _receivers = new List<Receiver>();
        }

        public bool SenderIsNull => _sender is null;
        public void SetSender(ISender<TSend>? sender)
        {
            _sender = sender;
        }

        public void Cancel()
        {
            foreach (var r in _receivers)
            {
                r.IsCanceled = true;
            }
            _receivers.Clear();
        }

        /// <summary>
        /// Returns a task to be completed when a message of the specified type is received.
        /// </summary>
        public async Task<URecv> WaitMsgOfType<URecv>()
            where URecv : class, TRecv
        {
            var receiver = new Receiver(typeof(URecv));
            _receivers.Add(receiver);

            while (true)
            {
                if (receiver.IsCanceled) throw new OperationCanceledException();

                await Task.Yield();

                if (receiver.Msg is URecv msg)
                {
                    return msg;
                }
            }
        }

        /// <summary>
        /// Add received message
        /// </summary>
        public void Receive(TRecv msg)
        {
            var index = _receivers.FindIndex(r =>
            {
                if (r.Type.IsAssignableFrom(msg.GetType()))
                {
                    r.Msg = msg;
                    return true;
                }

                return false;
            });

            if (index != -1)
            {
                _receivers.RemoveAt(index);
            }
        }

        /// <summary>
        /// Send a message
        /// </summary>
        public void Send(TSend msg, byte channelNumber = 0, DeliveryMethod method = DeliveryMethod.ReliableOrdered)
        {
            _sender?.Send(msg, channelNumber, method);
        }

        public void SendByte(byte[] data, byte channel, DeliveryMethod method)
        {
            _sender?.SendByte(data, channel, method);
        }

        ///// <summary>
        ///// Send a message of type USend and return a received response of type URecv
        ///// </summary>
        //public Task<URecv> SendWithResponseAsync<USend, URecv>(USend msg, byte channelNumber = 0, DeliveryMethod method = DeliveryMethod.ReliableOrdered)
        //    where USend : class, TSend, IWithResponse<URecv>
        //    where URecv : class, TRecv, IResponse
        //{
        //    _sender.Send(msg, channelNumber, method);
        //    return WaitMsgOfType<URecv>();
        //}
    }
}
