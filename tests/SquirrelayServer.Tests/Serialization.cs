using System;
using System.Linq;
using System.Threading.Tasks;

using MessagePack;

using SquirrelayServer.Common;

using Xunit;

namespace SquirrelayServer.Tests
{
    public class Serialization
    {
        [Fact]
        public async Task SerializeServerConfig()
        {
            _ = await Config.LoadFromFileAsync(@"config/config.json");
        }

        private static void Check<T, U>(T value)
            where T : U
        {
            var memory = new ReadOnlyMemory<byte>(MessagePackSerializer.Serialize<U>(value, Options.DefaultOptions));
            var obj = (T)MessagePackSerializer.Deserialize<U>(memory, Options.DefaultOptions);

            var props = typeof(T).GetProperties().Where(p => Attribute.IsDefined(p, typeof(KeyAttribute)));
            foreach (var p in props)
            {
                var aValue = p.GetValue(value);
                var bValue = p.GetValue(obj);
                Assert.True((aValue is null && bValue is null) || (aValue.Equals(bValue)));
            }
        }

        [Fact]
        public void ServerMsg()
        {
            for (ulong i = 0; i < 3; i++)
            {
                Check<IServerMsg.ClientId, IServerMsg>(new IServerMsg.ClientId(i));
            }

            for (var i = 0; i < 3; i++)
            {
                Check<IServerMsg.CreateRoomResponse, IServerMsg>(IServerMsg.CreateRoomResponse.Success(i));
            }

            Check<IServerMsg.CreateRoomResponse, IServerMsg>(IServerMsg.CreateRoomResponse.AlreadyEntered);

            Check<IServerMsg.EnterRoomResponse, IServerMsg>(IServerMsg.EnterRoomResponse.RoomNotFound);
            Check<IServerMsg.EnterRoomResponse, IServerMsg>(IServerMsg.EnterRoomResponse.InvalidPassword);
            Check<IServerMsg.EnterRoomResponse, IServerMsg>(IServerMsg.EnterRoomResponse.NumberOfPlayersLimitation);
            Check<IServerMsg.EnterRoomResponse, IServerMsg>(IServerMsg.EnterRoomResponse.AlreadyEntered);
            Check<IServerMsg.EnterRoomResponse, IServerMsg>(IServerMsg.EnterRoomResponse.Success);

            Check<IServerMsg.ExitRoomResponse, IServerMsg>(IServerMsg.ExitRoomResponse.PlayerOutOfRoom);
            Check<IServerMsg.ExitRoomResponse, IServerMsg>(IServerMsg.ExitRoomResponse.Success);
        }

        [Fact]
        public void ClientMsg()
        {
            Check<IClientMsg.SetPlayerInfo, IClientMsg>(new IClientMsg.SetPlayerInfo());

            Check<IClientMsg.GetRoomList, IClientMsg>(new IClientMsg.GetRoomList());

            Check<IClientMsg.CreateRoom, IClientMsg>(new IClientMsg.CreateRoom(false, null, 0, null));
            Check<IClientMsg.CreateRoom, IClientMsg>(new IClientMsg.CreateRoom(false, "", -1, ""));
            Check<IClientMsg.CreateRoom, IClientMsg>(new IClientMsg.CreateRoom(true, "password", 2, "mesage"));

            Check<IClientMsg.EnterRoom, IClientMsg>(new IClientMsg.EnterRoom(0, null));
            Check<IClientMsg.EnterRoom, IClientMsg>(new IClientMsg.EnterRoom(1, ""));
            Check<IClientMsg.EnterRoom, IClientMsg>(new IClientMsg.EnterRoom(-1, "password"));

            Check<IClientMsg.ExitRoom, IClientMsg>(new IClientMsg.ExitRoom());

        }
    }
}
