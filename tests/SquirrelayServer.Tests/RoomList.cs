﻿using System.Linq;

using MessagePack;

using Moq;

using SquirrelayServer.Common;

using Xunit;

namespace SquirrelayServer.Tests
{
    public class RoomListTest
    {
        private static RoomConfig GetRoomConfig() => new RoomConfig()
        {
            InvisibleEnabled = true,
            RoomMessageEnabled = true,
            PasswordEnabled = true,
            EnterWhenPlayingAllowed = true,
            DisposeSecondsWhenNoMember = 120,
            UpdatingDisposeStatusIntervalSeconds = 10.0f,
            NumberOfPlayersRange = (1, 6),
            GeneratedRoomIdRange = (1000, 9999),
        };

        private static ServerLoggingConfig GetLoggingConfig() => new ServerLoggingConfig()
        {
            Logging = true,
            ServerLogging = true,
            RoomListLogging = true,
            RoomLogging = true,
            MessageLogging = false,
        };

        private static Mock<Server.IClientHandler> CreateClientHandlerMock(ulong id)
        {
            var c = new Mock<Server.IClientHandler>();
            c.SetupGet(p => p.ClientVersion).Returns("v1.0");
            c.SetupGet(p => p.Id).Returns(id);
            c.SetupProperty(p => p.RoomId, null);
            return c;
        }

        private static (Server.RoomList, Mock<Server.IClientHandler>, int) CreateRoomWithOnePlayer(RoomConfig config, ServerLoggingConfig loggingConfig, MessagePackSerializerOptions options)
        {
            var roomList = new Server.RoomList(config, loggingConfig, options);

            var playerMock0 = CreateClientHandlerMock(0);

            var createRoom = new IClientMsg.CreateRoom(true, null, 6, new byte[1], new byte[2]);

            var roomCreatedRes = roomList.CreateRoom(playerMock0.Object, createRoom);
            Assert.True(roomCreatedRes.IsSuccess);

            var info = roomList.GetRoomInfoList();
            Assert.Equal(playerMock0.Object.RoomId, roomCreatedRes.Id);
            Assert.Equal(1, info.Info.Count);
            Assert.Equal(1, info.Info.First().NumberOfPlayers);

            return (roomList, playerMock0, roomCreatedRes.Id);
        }

        [Fact]
        public void CreateRoom()
        {
            _ = CreateRoomWithOnePlayer(GetRoomConfig(), GetLoggingConfig(), Options.DefaultOptions);
        }

        [Fact]
        public void EnterRoom()
        {
            var (roomList, _, roomId) = CreateRoomWithOnePlayer(GetRoomConfig(), GetLoggingConfig(), Options.DefaultOptions);
            var info = roomList.GetRoomInfoList();

            var clientMock1 = CreateClientHandlerMock(1);

            var enterRoom = new IClientMsg.EnterRoom(roomId, null, null);

            roomList.EnterRoom(clientMock1.Object, enterRoom);
            Assert.Equal(clientMock1.Object.RoomId, roomId);
            Assert.Equal(2, info.Info.First().NumberOfPlayers);
        }

        [Fact]
        public void ExitRoom()
        {
            var config = GetRoomConfig();
            config.DisposeSecondsWhenNoMember = 0.0f;
            var (roomList, clientMock0, _) = CreateRoomWithOnePlayer(config, GetLoggingConfig(), Options.DefaultOptions);
            var info = roomList.RoomInfoList;

            roomList.ExitRoom(clientMock0.Object);
            Assert.True(clientMock0.Object.RoomId is null);
            Assert.Single(info);
            Assert.Equal(0, info.First().Value.NumberOfPlayers);

            roomList.UpdateDisposeStatus();
            Assert.Empty(info);
        }

        [Fact]
        public void SetPlayerStatus()
        {
            var (roomList, clientMock0, roomId) = CreateRoomWithOnePlayer(GetRoomConfig(), GetLoggingConfig(), Options.DefaultOptions);

            var playerStatus = new IClientMsg.SetPlayerStatus(new RoomPlayerStatus(new byte[0]));
            var setPlayerStatusRes = roomList.SetPlayerStatus(clientMock0.Object, playerStatus);

            Assert.True(setPlayerStatusRes.IsSuccess);
            roomList.Update();
            Assert.True(roomList.Rooms[roomId].PlayerStatuses.ContainsKey(clientMock0.Object.Id));
        }

        [Fact]
        public void OperateRoom()
        {
            var roomConfig = GetRoomConfig();
            roomConfig.NumberOfPlayersRange = (2, 6);
            var (roomList, clientMock0, roomId) = CreateRoomWithOnePlayer(roomConfig, GetLoggingConfig(), Options.DefaultOptions);

            {
                var startPlaying = IClientMsg.OperateRoom.StartPlaying;
                var startPlayingRes = roomList.OperateRoom(clientMock0.Object, startPlaying);
                Assert.Equal(IServerMsg.OperateRoomResponse.ResultKind.NotEnoughPeople, startPlayingRes.Result);
            }

            var clientMock1 = CreateClientHandlerMock(1);
            var enterRoom = new IClientMsg.EnterRoom(roomId, null, null);
            roomList.EnterRoom(clientMock1.Object, enterRoom);

            {
                var startPlaying = IClientMsg.OperateRoom.StartPlaying;
                var startPlayingRes = roomList.OperateRoom(clientMock0.Object, startPlaying);
                Assert.True(startPlayingRes.IsSuccess);
            }

            var room = roomList.Rooms[roomId];
            Assert.Equal(RoomStatus.Playing, room.RoomStatus);

            var finishPlaying = IClientMsg.OperateRoom.FinishPlaying;
            var finishPlayingRes = roomList.OperateRoom(clientMock0.Object, finishPlaying);
            Assert.True(finishPlayingRes.IsSuccess);
        }

        [Fact]
        public void ReceiveGameMessage()
        {
            var options = Options.DefaultOptions;

            var roomConfig = GetRoomConfig();
            roomConfig.TickMessageEnabled = true;

            var (roomList, clientMock0, roomId) = CreateRoomWithOnePlayer(roomConfig, GetLoggingConfig(), options);

            var startRes = roomList.OperateRoom(clientMock0.Object, IClientMsg.OperateRoom.StartPlaying);

            Assert.True(startRes.IsSuccess);

            var msg0 = new IClientMsg.SendGameMessage(new byte[] { 0, 1, 2, 3, 4, 5 });
            roomList.ReceiveGameMessage(clientMock0.Object, msg0);

            var msg1 = new IClientMsg.SendGameMessage(new byte[] { 8, 7, 6, 5, 4 });
            roomList.ReceiveGameMessage(clientMock0.Object, msg1);

            var handlerCheckPassedCount = 0;
            var ticked = false;

            void Send(IServerMsg message, byte channelNumber, LiteNetLib.DeliveryMethod deliveryMethod)
            {
                if (message is IServerMsg.BroadcastGameMessages broadcast)
                {
                    Assert.Equal(2, broadcast.Messages.Count);
                    Assert.Equal(broadcast.Messages[0].ClientId, clientMock0.Object.Id);
                    Assert.True(Enumerable.SequenceEqual(broadcast.Messages[0].Data, msg0.Data));
                    Assert.True(Enumerable.SequenceEqual(broadcast.Messages[1].Data, msg1.Data));
                    handlerCheckPassedCount++;
                }
                else if (message is IServerMsg.Tick)
                {
                    ticked = true;
                }
            }

            clientMock0.Setup(s => s.Send(It.IsAny<IServerMsg>(), It.IsAny<byte>(), It.IsAny<LiteNetLib.DeliveryMethod>()))
                .Callback((IServerMsg message, byte channelNumber, LiteNetLib.DeliveryMethod deliveryMethod) =>
                {
                    Send(message, channelNumber, deliveryMethod);
                });

            clientMock0.Setup(s => s.SendByte(It.IsAny<byte[]>(), It.IsAny<byte>(), It.IsAny<LiteNetLib.DeliveryMethod>()))
                .Callback((byte[] data, byte channelNumber, LiteNetLib.DeliveryMethod deliveryMethod) =>
                {
                    var msg = MessagePackSerializer.Deserialize<IServerMsg>(data, options);
                    Send(msg, channelNumber, deliveryMethod);
                });

            roomList.Update();

            Assert.Equal(1, handlerCheckPassedCount);
            Assert.True(ticked);
        }
    }
}
