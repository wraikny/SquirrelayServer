﻿using System;
using System.Collections.Generic;
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

        internal static void Check<T, U>(T value)
            where T : U
        {
            var memory = new ReadOnlyMemory<byte>(MessagePackSerializer.Serialize<U>(value, Options.DefaultOptions));
            var obj = (T)MessagePackSerializer.Deserialize<U>(memory, Options.DefaultOptions);
            Assert.NotNull(obj);

            Utils.CheckEquality(value, obj);
        }

        [Fact]
        public async Task ServerMsg_HelloResponse()
        {
            var config = await Config.LoadFromFileAsync(@"config/config.json");
            for (ulong i = 0; i < 3; i++)
            {
                Check<IServerMsg.HelloResponse, IServerMsg>(new IServerMsg.HelloResponse(i, config.RoomConfig));
            }
        }

        [Fact]
        public void ServerMsg_ClientsCountResponse()
        {
            Check<IServerMsg.ClientsCountResponse, IServerMsg>(new IServerMsg.ClientsCountResponse(0));
            Check<IServerMsg.ClientsCountResponse, IServerMsg>(new IServerMsg.ClientsCountResponse(1));
            Check<IServerMsg.ClientsCountResponse, IServerMsg>(new IServerMsg.ClientsCountResponse(2));
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
            Check<IServerMsg.EnterRoomResponse<byte[]>, IServerMsg>(IServerMsg.EnterRoomResponse.RoomNotFound);
            Check<IServerMsg.EnterRoomResponse<byte[]>, IServerMsg>(IServerMsg.EnterRoomResponse.InvalidPassword);
            Check<IServerMsg.EnterRoomResponse<byte[]>, IServerMsg>(IServerMsg.EnterRoomResponse.NumberOfPlayersLimitation);
            Check<IServerMsg.EnterRoomResponse<byte[]>, IServerMsg>(IServerMsg.EnterRoomResponse.AlreadyEntered);
            Check<IServerMsg.EnterRoomResponse<byte[]>, IServerMsg>(IServerMsg.EnterRoomResponse.Success(0, null, new byte[1]));
        }

        [Fact]
        public void ServerMsg_ExitRoomResponse()
        {
            Check<IServerMsg.ExitRoomResponse, IServerMsg>(IServerMsg.ExitRoomResponse.PlayerOutOfRoom);
            Check<IServerMsg.ExitRoomResponse, IServerMsg>(IServerMsg.ExitRoomResponse.Success);
        }

        [Fact]
        public void ServerMsg_OperateRoomResponse()
        {
            Check<IServerMsg.OperateRoomResponse, IServerMsg>(IServerMsg.OperateRoomResponse.Success);
            Check<IServerMsg.OperateRoomResponse, IServerMsg>(IServerMsg.OperateRoomResponse.PlayerOutOfRoom);
            Check<IServerMsg.OperateRoomResponse, IServerMsg>(IServerMsg.OperateRoomResponse.PlayerIsNotOwner);
            Check<IServerMsg.OperateRoomResponse, IServerMsg>(IServerMsg.OperateRoomResponse.InvalidRoomStatus);
        }

        [Fact]
        public void ServerMsg_SetPlayerStatusResponse()
        {
            Check<IServerMsg.SetPlayerStatusResponse, IServerMsg>(IServerMsg.SetPlayerStatusResponse.PlayerOutOfRoom);
            Check<IServerMsg.SetPlayerStatusResponse, IServerMsg>(IServerMsg.SetPlayerStatusResponse.Success);
        }

        [Fact]
        public void ServerMsg_SendGameMessageResponse()
        {
            Check<IServerMsg.SendGameMessageResponse, IServerMsg>(IServerMsg.SendGameMessageResponse.Success);
            Check<IServerMsg.SendGameMessageResponse, IServerMsg>(IServerMsg.SendGameMessageResponse.PlayerOutOfRoom);
            Check<IServerMsg.SendGameMessageResponse, IServerMsg>(IServerMsg.SendGameMessageResponse.InvalidRoomStatus);
        }

        [Fact]
        public void ServerMsg_UpdateRoomPlayers()
        {
            {
                ulong? owner = null;
                var statuses = new Dictionary<ulong, RoomPlayerStatus>();
                _ = new byte[1];
                Check<IServerMsg.UpdateRoomPlayers, IServerMsg>(new IServerMsg.UpdateRoomPlayers(owner, statuses));
            }

            {
                ulong? owner = 0uL;
                var statuses = new Dictionary<ulong, RoomPlayerStatus>
                {
                    {
                        0uL,
                        new RoomPlayerStatus(new byte[]{ 1, 4 })
                    },
                    {
                        1uL,
                        new RoomPlayerStatus(new byte[]{ 9, 3, 5, 2, 0})
                    }
                };
                Check<IServerMsg.UpdateRoomPlayers, IServerMsg>(new IServerMsg.UpdateRoomPlayers(owner, statuses));
            }
        }

        [Fact]
        public void ServerMsg_UpdateRoomMessages()
        {
            var roomMessage = new byte[] { 1, 2, 3, 4, 5, 6 };
            Check<IServerMsg.UpdateRoomMessage, IServerMsg>(new IServerMsg.UpdateRoomMessage(roomMessage));
        }

        [Fact]
        public void ServerMsg_Tick()
        {
            Check<IServerMsg.Tick, IServerMsg>(new IServerMsg.Tick(0.0f));
            Check<IServerMsg.Tick, IServerMsg>(new IServerMsg.Tick(1.0f));
            Check<IServerMsg.Tick, IServerMsg>(new IServerMsg.Tick(-1.0f));
            Check<IServerMsg.Tick, IServerMsg>(new IServerMsg.Tick(12.34f));
        }

        [Fact]
        public void ServerMsg_BroadcastGameMessage()
        {
            {
                var msgs = new List<RelayedGameMessage>();
                Check<IServerMsg.BroadcastGameMessages, IServerMsg>(new IServerMsg.BroadcastGameMessages(msgs));
            }

            {
                var msgs = new List<RelayedGameMessage>();
                msgs.Add(new RelayedGameMessage(0uL, 1.0f, new byte[1]));
                msgs.Add(new RelayedGameMessage(1uL, 1.1f, new byte[1]));
                msgs.Add(new RelayedGameMessage(2uL, 0.0f, null));
                Check<IServerMsg.BroadcastGameMessages, IServerMsg>(new IServerMsg.BroadcastGameMessages(msgs));
            }
        }

        [Fact]
        public void ClientMsg_Hello()
        {
            Check<IClientMsg.Hello, IClientMsg>(new IClientMsg.Hello("v1.0"));
        }

        [Fact]
        public void ClientMsg_GetClientsCount()
        {
            Check<IClientMsg.GetClientsCount, IClientMsg>(IClientMsg.GetClientsCount.Instance);
        }

        [Fact]
        public void ClientMsg_GetRoomList()
        {
            Check<IClientMsg.GetRoomList, IClientMsg>(new IClientMsg.GetRoomList());
        }

        [Fact]
        public void ClientMsg_CreateRoom()
        {
            Check<IClientMsg.CreateRoom, IClientMsg>(new IClientMsg.CreateRoom(false, null, 0, null, null));
            Check<IClientMsg.CreateRoom, IClientMsg>(new IClientMsg.CreateRoom(false, "", -1, new byte[1], new byte[2]));
            Check<IClientMsg.CreateRoom, IClientMsg>(new IClientMsg.CreateRoom(true, "password", 2, new byte[1], new byte[3]));
        }

        [Fact]
        public void ClientMsg_EnterRoom()
        {
            Check<IClientMsg.EnterRoom, IClientMsg>(new IClientMsg.EnterRoom(0, null, null));
            Check<IClientMsg.EnterRoom, IClientMsg>(new IClientMsg.EnterRoom(1, "", new byte[1]));
            Check<IClientMsg.EnterRoom, IClientMsg>(new IClientMsg.EnterRoom(-1, "password", new byte[2]));
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

        [Fact]
        public void ClientMsg_SetPlayerStatus()
        {
            Check<IClientMsg.SetPlayerStatus, IClientMsg>(new IClientMsg.SetPlayerStatus(null));
            Check<IClientMsg.SetPlayerStatus, IClientMsg>(new IClientMsg.SetPlayerStatus(new RoomPlayerStatus(null)));
            Check<IClientMsg.SetPlayerStatus, IClientMsg>(new IClientMsg.SetPlayerStatus(new RoomPlayerStatus(new byte[] { })));
            Check<IClientMsg.SetPlayerStatus, IClientMsg>(new IClientMsg.SetPlayerStatus(new RoomPlayerStatus(new byte[] { 42 })));
        }

        [Fact]
        public void ClientMsg_SendGameMessage()
        {
            Check<IClientMsg.SendGameMessage, IClientMsg>(new IClientMsg.SendGameMessage(null));
            Check<IClientMsg.SendGameMessage, IClientMsg>(new IClientMsg.SendGameMessage(new byte[0]));
            Check<IClientMsg.SendGameMessage, IClientMsg>(new IClientMsg.SendGameMessage(new byte[1]));
            Check<IClientMsg.SendGameMessage, IClientMsg>(new IClientMsg.SendGameMessage(new byte[2]));
        }
    }
}
