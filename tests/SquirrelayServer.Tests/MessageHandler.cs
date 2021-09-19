using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using MessagePack;

using Moq;

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

        private MessageHandler<Msg, Msg> CreateMessageHandler()
        {
            var sender = new Mock<ISender<Msg>>();
            sender.Setup(s => s.Send(It.IsAny<Msg>(), It.IsAny<byte>(), It.IsAny<LiteNetLib.DeliveryMethod>()))
                .Callback((Msg message, byte channelNumber, LiteNetLib.DeliveryMethod deliveryMethod) =>
                {
                    var msg = MessagePackSerializer.Serialize<Msg>(message, Options.DefaultOptions);
                    var deserialized = MessagePackSerializer.Deserialize<Msg>(msg, Options.DefaultOptions);
                    Assert.Equal(message.Message, deserialized.Message);
                });

            var handler = new MessageHandler<Msg, Msg>();
            handler.SetSender(sender.Object);

            return handler;
        }

        [Fact]
        public void Send()
        {
            var handler = CreateMessageHandler();
            var msg = new Msg("Send Message");

            handler.Send(msg, default, default);
        }

        [Fact(Timeout = 100)]
        public async Task WaitOfType()
        {
            var handler = CreateMessageHandler();

            {
                var msg1 = new Msg("Recv Message");
                var task = handler.WaitMsgOfType<Msg>();
                handler.Receive(msg1);
                var result = await task;

                Assert.Equal(msg1.Message, result.Message);
            }

            {
                var msg2 = new Msg("Recv Message 2");
                var task = handler.WaitMsgOfType<Msg>();
                handler.Receive(msg2);
                var result = await task;

                Assert.Equal(msg2.Message, result.Message);
            }
        }

        [Fact]
        public async Task Cancel()
        {
            var handler = CreateMessageHandler();

            var task = handler.WaitMsgOfType<Msg>();

            handler.Cancel();

            await Task.Delay(1);

            Assert.True(task.IsCanceled);
        }

        [Fact(Timeout = 100)]
        public async Task AwaitCanceledTask()
        {
            var handler = CreateMessageHandler();

            var task = handler.WaitMsgOfType<Msg>();

            _ = Task.Run(async () =>
            {
                await Task.Delay(2);
                handler.Cancel();
            });

            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                var msg = await task;
            });

            await Task.Delay(4);

            Assert.True(task.IsCanceled);
        }
    }
}
