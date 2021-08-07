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
            Console.WriteLine($"[Log] {msg}");
        }
    }

    public class Program
    {
        public static async Task Main(string[] args)
        {
            NetDebug.Logger = new Logger();

            var configPath = @"config/config.json";
            var config = await Config.LoadFromFileAsync(configPath);

            NetDebug.Logger?.WriteNet(NetLogLevel.Info, $"Config is loaded from '{configPath}'.");

            var options = Options.DefaultOptions;

            var client = new Client<PlayerStatus, RoomMessage, GameMessage>(config.NetConfig, options, options);

            _ = Task.Run(async () => {
                while (true)
                {
                    client.Update();
                    await Task.Delay(config.NetConfig.UpdateTime);
                }
            });

            var messageReceivedCount = 0;

            var msg = new GameMessage { Message = "Hello, world!" };

            var listener = new EventBasedClientLIstener<PlayerStatus, RoomMessage, GameMessage>();

            listener.OnGameMessageReceived += (clientId, elapsedSeconds, message) => {
                NetDebug.Logger?.WriteNet(NetLogLevel.Info, $"Message received: '{message.Message}'");

                messageReceivedCount++;
                Assert.True(message.Message == msg.Message);
            };

            await client.Start("localhost", listener);

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

            var sendRes = await client.RequestSendGameMessage(msg);

            Assert.True(sendRes.IsSuccess);
            NetDebug.Logger?.WriteNet(NetLogLevel.Info, $"Send game message.");

            await Task.Delay(2000);

            Assert.True(messageReceivedCount == 1);

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
