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
        public void ServerMsg_ClientId()
        {
            for (ulong i = 0; i < 3; i++)
            {
                Check<IServerMsg.ClientId, IServerMsg>(new IServerMsg.ClientId(i));
            }
        }

        [Fact]
        public void ServerMsg_SetPlayerStatusResponse()
        {
            Check<IServerMsg.SetPlayerStatusResponse, IServerMsg>(IServerMsg.SetPlayerStatusResponse.PlayerOutOfRoom);
            Check<IServerMsg.SetPlayerStatusResponse, IServerMsg>(IServerMsg.SetPlayerStatusResponse.Success);
        }

        [Fact]
        public void ServerMsg_CreateRoomResponse()
        {
            for (var i = 0; i < 3; i++)
            {
                Check<IServerMsg.CreateRoomResponse, IServerMsg>(IServerMsg.CreateRoomResponse.Success(i));
            }

            Check<IServerMsg.CreateRoomResponse, IServerMsg>(IServerMsg.CreateRoomResponse.AlreadyEntered);
        }

        [Fact]
        public void ServerMsg_EnterRoomResponse()
        {
            Check<IServerMsg.EnterRoomResponse, IServerMsg>(IServerMsg.EnterRoomResponse.RoomNotFound);
            Check<IServerMsg.EnterRoomResponse, IServerMsg>(IServerMsg.EnterRoomResponse.InvalidPassword);
            Check<IServerMsg.EnterRoomResponse, IServerMsg>(IServerMsg.EnterRoomResponse.NumberOfPlayersLimitation);
            Check<IServerMsg.EnterRoomResponse, IServerMsg>(IServerMsg.EnterRoomResponse.AlreadyEntered);
            Check<IServerMsg.EnterRoomResponse, IServerMsg>(IServerMsg.EnterRoomResponse.Success(null));
        }

        [Fact]
        public void ServerMsg_ExitRoomResponse()
        {
            Check<IServerMsg.ExitRoomResponse, IServerMsg>(IServerMsg.ExitRoomResponse.PlayerOutOfRoom);
            Check<IServerMsg.ExitRoomResponse, IServerMsg>(IServerMsg.ExitRoomResponse.Success);
        }

        [Fact]
        public void ClientMsg_SetPlayerStatus()
        {
            Check<IClientMsg.SetPlayerStatus, IClientMsg>(new IClientMsg.SetPlayerStatus(null));
            Check<IClientMsg.SetPlayerStatus, IClientMsg>(new IClientMsg.SetPlayerStatus(new RoomPlayerStatus { Data = null }));
            Check<IClientMsg.SetPlayerStatus, IClientMsg>(new IClientMsg.SetPlayerStatus(new RoomPlayerStatus { Data = new byte[0] }));
            Check<IClientMsg.SetPlayerStatus, IClientMsg>(new IClientMsg.SetPlayerStatus(new RoomPlayerStatus { Data = new byte[1] }));
        }

        [Fact]
        public void ClientMsg_GetRoomList()
        {
            Check<IClientMsg.GetRoomList, IClientMsg>(new IClientMsg.GetRoomList());
        }

        [Fact]
        public void ClientMsg_CreateRoom()
        {
            Check<IClientMsg.CreateRoom, IClientMsg>(new IClientMsg.CreateRoom(false, null, 0, null));
            Check<IClientMsg.CreateRoom, IClientMsg>(new IClientMsg.CreateRoom(false, "", -1, ""));
            Check<IClientMsg.CreateRoom, IClientMsg>(new IClientMsg.CreateRoom(true, "password", 2, "mesage"));
        }

        [Fact]
        public void ClientMsg_EnterRoom()
        {
            Check<IClientMsg.EnterRoom, IClientMsg>(new IClientMsg.EnterRoom(0, null));
            Check<IClientMsg.EnterRoom, IClientMsg>(new IClientMsg.EnterRoom(1, ""));
            Check<IClientMsg.EnterRoom, IClientMsg>(new IClientMsg.EnterRoom(-1, "password"));
        }

        [Fact]
        public void ClientMsg_ExitRoom()
        {
            Check<IClientMsg.ExitRoom, IClientMsg>(new IClientMsg.ExitRoom());
        }

        [Fact]
        public void ClientMsg_OperateRoom()
        {
            Check<IClientMsg.OperateRoom, IClientMsg>(IClientMsg.OperateRoom.StartPlaying);
            Check<IClientMsg.OperateRoom, IClientMsg>(IClientMsg.OperateRoom.FinishPlaying);
        }
    }
}
