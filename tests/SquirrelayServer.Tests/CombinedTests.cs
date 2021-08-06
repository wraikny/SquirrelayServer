using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using SquirrelayServer.Common;

using Xunit;

using MessagePack;

namespace SquirrelayServer.Tests
{
    //[Collection("Combined tests")]
    public class CombinedTests
    {
        [MessagePackObject]
        public sealed class Status
        {

        }

        [MessagePackObject]
        public sealed class Msg
        {

        }

        private readonly Config Config =
            new Config(
                new NetConfig(8080, "key"),
                new RoomConfig
                {
                    InvisibleEnabled = true,
                    RoomMessageEnabled = true,
                    PasswordEnabled = true,
                    EnterWhenPlaingAllowed = true,
                    DisposeSecondsWhenNoMember = 1.0f,
                    UpdatingDisposeStatusIntervalSeconds = 0.5f,
                    NumberOfPlayersRange = (2, 6),
                    GeneratedRoomIdRange = (1000, 9999)
                }
            );

        //[Fact(Timeout = 5000)]
        public async Task ConnectToServer()
        {
            var options = Options.DefaultOptions;

            var server = new Server.Server(Config, options);

            _ = server.Start();

            await Task.Delay(5);

            var client = new Client.Client<Status, Msg>(Config.NetConfig, options, options);

            await client.Start("localhost", (clientId, elapsedSeconds, message) => { });

            Assert.True(client.IsConnected);
            Assert.True(client.Id is { });
        }
    }
}
