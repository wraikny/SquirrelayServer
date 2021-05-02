using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

using MessagePack;

using SquirrelayServer.Common;

using Xunit;

namespace SquirrelayServer.Tests
{
    public class MessageHandler
    {
        [MessagePackObject]
        public sealed class Msg
        {
            [Key(0)]
            public string Message { get; set; }

            [SerializationConstructor]
            public Msg(string message)
            {
                Message = message;
            }
        }

        internal sealed class SenderMock : ISender<Msg>
        {
            public SenderMock() { }

            void ISender<Msg>.Send<U>(U message, byte channelNumber, LiteNetLib.DeliveryMethod deliveryMethod)
            {
                var msg = MessagePackSerializer.Serialize<Msg>(message, Options.DefaultOptions);
                var deserialized = MessagePackSerializer.Deserialize<Msg>(msg, Options.DefaultOptions);
                Assert.Equal(message.Message, deserialized.Message);
            }
        }

        private MessageHandler<Msg, Msg> CreateMessageHandler()
        {
            var subject = Subject.Synchronize(new Subject<Msg>());
            var sender = new SenderMock();
            var handler = new MessageHandler<Msg, Msg>(subject, sender);
            return handler;
        }

        [Fact]
        public void Send()
        {
            var handler = CreateMessageHandler();
            var msg = new Msg("Send Message");

            handler.Send(msg, default, default);
        }

        [Fact]
        public async ValueTask WaitOfType()
        {
            var handler = CreateMessageHandler();

            var msg = new Msg("Recv Message");

            var task = handler.WaitMsgOfType<Msg>();

            handler.Receive(msg);

            await Task.Delay(2);

            Assert.True(task.IsCompletedSuccessfully);
        }
    }
}
