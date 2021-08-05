using System;
using System.Collections.Generic;
using System.Threading;
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

            public TRecv Msg { get; set; }

            public bool IsCanceled { get; set; } = false;
        }

        private readonly List<Receiver> _receivers;

        private readonly ISender<TSend> _sender;

        public ulong? Id { get; set; }

        public MessageHandler(ISender<TSend> sender)
        {
            _receivers = new List<Receiver>();
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
            var receiver = new Receiver { Type = typeof(URecv) };
            _receivers.Add(receiver);

            while (receiver.Msg is null)
            {
                if (receiver.IsCanceled) throw new OperationCanceledException();

                await Task.Yield();
            }
            return receiver.Msg as URecv;
        }

        /// <summary>
        /// Add received message
        /// </summary>
        public void Receive(TRecv msg)
        {
            _receivers.RemoveAll(r =>
            {
                if (r.Type.IsAssignableFrom(msg.GetType()))
                {
                    r.Msg = msg;
                    return true;
                }

                return false;
            });
        }

        /// <summary>
        /// Send a message
        /// </summary>
        public void Send(TSend msg, byte channelNumber = 0, DeliveryMethod method = DeliveryMethod.ReliableOrdered)
        {
            _sender.Send(msg, channelNumber, method);
        }

        public void SendByte(byte[] data, byte channel, DeliveryMethod method)
        {
            _sender.SendByte(data, channel, method);
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
