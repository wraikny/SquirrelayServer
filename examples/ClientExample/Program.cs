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

            NetDebug.Logger.WriteNet(NetLogLevel.Info, $"Config is loaded from '{configPath}'.");

            var options = Options.DefaultOptions;

            var client = new Client<Status, GameMessage>(config.NetConfig, options, options);

            _ = Task.Run(async () => {
                while (true)
                {
                    client.Update();
                    await Task.Delay(config.NetConfig.UpdateTime);
                }
            });

            var messageReceivedCount = 0;

            var msg = new GameMessage { Message = "Hello, world!" };

            await client.Start("localhost", (clientId, elapsedSeconds, message) => {
                NetDebug.Logger.WriteNet(NetLogLevel.Info, $"Message received: '{message.Message}'");

                messageReceivedCount++;
                Assert.True(message.Message == msg.Message);
            });

            NetDebug.Logger.WriteNet(NetLogLevel.Info, "Client started");

            var clientsCount = await client.GetClientsCountAsync();
            Assert.True(clientsCount == 1);

            NetDebug.Logger.WriteNet(NetLogLevel.Info, $"Clients count:{clientsCount}");

            {
                var roomList = await client.GetRoomListAsync();
                Assert.True(roomList.Count == 0);
                NetDebug.Logger.WriteNet(NetLogLevel.Info, $"RoomList.Count:{roomList.Count}");
            }

            var createRoomRes = await client.CreateRoomAsync();
            Assert.True(createRoomRes.IsSuccess);
            NetDebug.Logger.WriteNet(NetLogLevel.Info, $"Success to create room:{createRoomRes.Id}");

            Assert.True(client.CurrentRoom is { });
            Assert.True(client.IsOwner);

            {
                var roomList = await client.GetRoomListAsync();
                Assert.True(roomList.Count == 1);
                NetDebug.Logger.WriteNet(NetLogLevel.Info, $"RoomList.Count:{roomList.Count}");
            }

            var gameStartRes = await client.StartPlayingAsync();

            var sendRes = await client.SendGameMessage(msg);

            Assert.True(sendRes.IsSuccess);
            NetDebug.Logger.WriteNet(NetLogLevel.Info, $"Send game message.");

            await Task.Delay(2000);

            Assert.True(messageReceivedCount == 1);
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
