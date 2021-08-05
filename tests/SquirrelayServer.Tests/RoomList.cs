using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MessagePack;

using Moq;

using SquirrelayServer;
using SquirrelayServer.Common;

using Xunit;

namespace SquirrelayServer.Tests
{
    public class RoomListTest
    {
        private static RoomConfig GetRoomConfig() => new RoomConfig
        {
            InvisibleEnabled = true,
            RoomMessageEnabled = true,
            PasswordEnabled = true,
            EnterWhilePlayingAllowed = true,
            DisposeSecondWhileNoMember = 120,
            UpdatingDisposeStatusIntervalSecond = 10.0f,
            MaxNumberOfPlayersRange = (2, 6),
            GeneratedRoomIdRange = (1000, 9999),
        };

        private static Mock<Server.IPlayer> CreatePlayerMock(ulong id)
        {
            var p = new Mock<Server.IPlayer>();
            p.SetupGet(p => p.Id).Returns(id);
            p.SetupProperty(p => p.RoomId, null);
            return p;
        }

        private static (Server.RoomList, Mock<Server.IPlayer>, int) CreateRoomWithOnePlayer(RoomConfig config, MessagePackSerializerOptions options)
        {
            var roomList = new Server.RoomList(config, options);

            var playerMock0 = CreatePlayerMock(0);

            var createRoom = new IClientMsg.CreateRoom(true, null, 6, "");

            var roomCreatedRes = roomList.CreateRoom(playerMock0.Object, createRoom);
            Assert.True(roomCreatedRes.IsSuccess);

            var info = roomList.GetRoomListInfo();
            Assert.Equal(playerMock0.Object.RoomId, roomCreatedRes.Id);
            Assert.Equal(1, info.Info.Count);
            Assert.Equal(1, info.Info.First().NumberOfPlayers);

            return (roomList, playerMock0, roomCreatedRes.Id);
        }

        [Fact]
        public void CreateRoom()
        {
            _ = CreateRoomWithOnePlayer(GetRoomConfig(), Options.DefaultOptions);
        }

        [Fact]
        public void EnterRoom()
        {
            var (roomList, _, roomId) = CreateRoomWithOnePlayer(GetRoomConfig(), Options.DefaultOptions);
            var info = roomList.GetRoomListInfo();

            var playerMock1 = CreatePlayerMock(1);

            var enterRoom = new IClientMsg.EnterRoom(roomId, null);

            roomList.EnterRoom(playerMock1.Object, enterRoom);
            Assert.Equal(playerMock1.Object.RoomId, roomId);
            Assert.Equal(2, info.Info.First().NumberOfPlayers);
        }

        [Fact]
        public void ExitRoom()
        {
            var config = GetRoomConfig();
            config.DisposeSecondWhileNoMember = 0.0f;
            var (roomList, playerMock0, _) = CreateRoomWithOnePlayer(config, Options.DefaultOptions);
            var info = roomList.GetRoomListInfo();

            roomList.ExitRoom(playerMock0.Object);
            Assert.True(playerMock0.Object.RoomId is null);
            Assert.Equal(1, info.Info.Count);
            Assert.Equal(0, info.Info.First().NumberOfPlayers);

            roomList.UpdateDisposeStatus();
            Assert.Equal(0, info.Info.Count);
        }

        [Fact]
        public void SetPlayerStatus()
        {
            var (roomList, playerMock0, roomId) = CreateRoomWithOnePlayer(GetRoomConfig(), Options.DefaultOptions);

            var playerStatus = new IClientMsg.SetPlayerStatus(new RoomPlayerStatus { Data = new byte[0] });
            var setPlayerStatusRes = roomList.SetPlayerStatus(playerMock0.Object, playerStatus);

            Assert.True(setPlayerStatusRes.IsSuccess);
            Assert.True(roomList.Rooms[roomId].PlayerStatuses.ContainsKey(playerMock0.Object.Id));
        }

        [Fact]
        public void OperateRoom()
        {
            var (roomList, playerMock0, roomId) = CreateRoomWithOnePlayer(GetRoomConfig(), Options.DefaultOptions);

            var startPlaying = IClientMsg.OperateRoom.StartPlaying;
            var startPlayingRes = roomList.OperateRoom(playerMock0.Object, startPlaying);
            Assert.True(startPlayingRes.IsSuccess);

            var room = roomList.Rooms[roomId];
            Assert.Equal(Server.RoomStatus.Playing, room.RoomStatus);

            var finishPlaying = IClientMsg.OperateRoom.FinishPlaying;
            var finishPlayingRes = roomList.OperateRoom(playerMock0.Object, finishPlaying);
            Assert.True(finishPlayingRes.IsSuccess);
        }

        [Fact]
        public void ReceiveGameMessage()
        {
            var options = Options.DefaultOptions;

            var (roomList, playerMock0, roomId) = CreateRoomWithOnePlayer(GetRoomConfig(), options);

            roomList.OperateRoom(playerMock0.Object, IClientMsg.OperateRoom.StartPlaying);

            var msg0 = new IClientMsg.SendGameMessage(new byte[] { 0, 1, 2, 3, 4, 5 });
            roomList.ReceiveGameMessage(playerMock0.Object, msg0);

            var msg1 = new IClientMsg.SendGameMessage(new byte[] { 8, 7, 6, 5, 4 });
            roomList.ReceiveGameMessage(playerMock0.Object, msg1);

            var handlerCheckPassed = false;

            var playerMock0Handler = new Mock<Server.IClientHandler>();
            playerMock0Handler.Setup(s => s.Send(It.IsAny<IServerMsg>(), It.IsAny<byte>(), It.IsAny<LiteNetLib.DeliveryMethod>()))
                .Callback((IServerMsg message, byte channelNumber, LiteNetLib.DeliveryMethod deliveryMethod) =>
                {
                    if (message is IServerMsg.DistributeGameMessage msg)
                    {
                        Assert.Equal(msg.Messages[0].ClientId, playerMock0.Object.Id);
                        Assert.True(Enumerable.SequenceEqual(msg.Messages[0].Data, msg0.Data));
                        Assert.True(Enumerable.SequenceEqual(msg.Messages[1].Data, msg1.Data));
                        handlerCheckPassed = true;
                    }
                    else
                    {
                        Assert.True(false);
                    }
                });

            playerMock0Handler.Setup(s => s.SendByte(It.IsAny<byte[]>(), It.IsAny<byte>(), It.IsAny<LiteNetLib.DeliveryMethod>()))
                .Callback((byte[] data, byte channelNumber, LiteNetLib.DeliveryMethod deliveryMethod) =>
                {
                    var msg = MessagePackSerializer.Deserialize<IServerMsg>(data, options);
                    playerMock0Handler.Object.Send(msg, channelNumber, deliveryMethod);
                });

            var handlers = new Dictionary<ulong, Server.IClientHandler>
            {
                { playerMock0.Object.Id, playerMock0Handler.Object }
            };

            roomList.Update(handlers);

            Assert.True(handlerCheckPassed);
        }
    }
}
