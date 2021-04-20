using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using SquirrelayServer.Common;

using Xunit;

using MessagePack;

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

        [Fact]
        public void Send()
        {
            var sender = new SenderMock();
            var handler = new MessageHandler<Msg, Msg>(sender);

            var msg = new Msg("Send Message");

            handler.Send(msg, default, default);
        }

        [Fact]
        public async ValueTask WaitOfType()
        {
            var sender = new SenderMock();
            var handler = new MessageHandler<Msg, Msg>(sender);

            var msg = new Msg("Recv Message");

            var task = handler.WaitMsgOfType<Msg>();

            handler.Receive(msg);

            await Task.Delay(10);
            
            Assert.True(task.IsCompletedSuccessfully);
        }
    }
}
