using System;
using System.Threading;
using System.Threading.Tasks;

using LiteNetLib;

using SquirrelayServer.Client;
using SquirrelayServer.Common;

namespace ClientExample
{
    internal sealed class Logger : INetLogger
    {
        public Logger()
        {

        }

        void INetLogger.WriteNet(NetLogLevel level, string str, params object[] args)
        {
            var msg = args.Length == 0 ? str : string.Format(str, args);
            var current = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            var output = $"[{current}] {msg}";

            Console.WriteLine(output);
        }
    }

    public class Program
    {
        private static int s_messageReceivedCount = 0;

        private static GameMessage s_msg = new GameMessage { Message = "Hello, world!" };

        public static void Main(string[] args)
        {
            NetDebug.Logger = new Logger();

            var context = new Examples.Context();
            SynchronizationContext.SetSynchronizationContext(context);

            var configPath = @"config/config.json";
            var config = Config.LoadFromFile(configPath);

            NetDebug.Logger?.WriteNet(NetLogLevel.Info, $"Config is loaded from '{configPath}'.");

            var options = Options.DefaultOptions;

            var listener = new EventBasedClientListener<PlayerStatus, RoomMessage, GameMessage>();

            listener.OnGameMessageReceived += (clientId, elapsedSeconds, message) => {
                NetDebug.Logger?.WriteNet(NetLogLevel.Info, $"Message received: '{message.Message}'");

                s_messageReceivedCount++;
                Assert.True(message.Message == s_msg.Message);
            };

            var client = new Client<PlayerStatus, RoomMessage, GameMessage>(config.NetConfig, options, options, listener);

            var task = Run(client);

            while (!task.IsCompleted)
            {
                context.Update();

                client.Update();

                Thread.Sleep(config.NetConfig.UpdateTime);
            }
        }

        private static async Task Run(Client<PlayerStatus, RoomMessage, GameMessage> client)
        {
            await client.Start("localhost");

            NetDebug.Logger?.WriteNet(NetLogLevel.Info, "Client started");

            var clientsCount = await client.RequestGetClientsCountAsync();
            Assert.True(clientsCount == 1);

            NetDebug.Logger?.WriteNet(NetLogLevel.Info, $"Clients count:{clientsCount}");

            {
                var roomList = await client.RequestGetRoomListAsync();
                Assert.True(roomList.Count == 0);
                NetDebug.Logger?.WriteNet(NetLogLevel.Info, $"RoomList.Count:{roomList.Count}");
            }

            var createRoomRes = await client.RequestCreateRoomAsync();
            Assert.True(createRoomRes.IsSuccess);
            NetDebug.Logger?.WriteNet(NetLogLevel.Info, $"Success to create room:{createRoomRes.Id}");

            Assert.True(client.CurrentRoom is { });
            Assert.True(client.IsOwner);

            {
                var roomList = await client.RequestGetRoomListAsync();
                Assert.True(roomList.Count == 1);
                NetDebug.Logger?.WriteNet(NetLogLevel.Info, $"RoomList.Count:{roomList.Count}");
            }

            var gameStartRes = await client.RequestStartPlayingAsync();

            var sendRes = await client.SendGameMessageAsync(s_msg);

            Assert.True(sendRes.IsSuccess);
            NetDebug.Logger?.WriteNet(NetLogLevel.Info, $"Send game message.");

            await Task.Delay(1000);

            Assert.True(s_messageReceivedCount == 1);

            client.Stop();
        }

        private static class Assert
        {
            public static void True(bool t)
            {
                if (!t) throw new Exception("not true.");
            }
        }
    }
}
