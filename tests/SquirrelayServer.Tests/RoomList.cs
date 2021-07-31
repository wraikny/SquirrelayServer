using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        [Fact]
        public void CreateRoom()
        {
            var roomList = new Server.RoomList(GetRoomConfig());

            var playerMock0 = new Mock<Server.IPlayer>();
            playerMock0.SetupProperty(p => p.RoomId, null);
            playerMock0.SetupGet(p => p.Id).Returns(0);

            var createRoom = new IClientMsg.CreateRoom(true, null, 6, "");

            var roomCreatedRes = roomList.CreateRoom(playerMock0.Object, createRoom);
            Assert.True(roomCreatedRes.IsSuccess);

            var info = roomList.GetRoomListInfo();
            Assert.True(playerMock0.Object.RoomId == roomCreatedRes.Id);
            Assert.True(info.Info.Count == 1);
            Assert.True(info.Info.First().NumberOfPlayers == 1);
        }

        [Fact]
        public void EnterRoom()
        {
            var roomList = new Server.RoomList(GetRoomConfig());

            var playerMock0 = new Mock<Server.IPlayer>();
            playerMock0.SetupProperty(p => p.RoomId, null);
            playerMock0.SetupGet(p => p.Id).Returns(0);

            var createRoom = new IClientMsg.CreateRoom(true, null, 6, "");

            var roomCreatedRes = roomList.CreateRoom(playerMock0.Object, createRoom);

            var info = roomList.GetRoomListInfo();
            Assert.True(playerMock0.Object.RoomId == roomCreatedRes.Id);
            Assert.True(info.Info.Count == 1);
            Assert.True(info.Info.First().NumberOfPlayers == 1);

            var playerMock1 = new Mock<Server.IPlayer>();
            playerMock1.SetupProperty(p => p.RoomId, null);
            playerMock1.SetupGet(p => p.Id).Returns(1);

            var enterRoom = new IClientMsg.EnterRoom(roomCreatedRes.Id, null);

            roomList.EnterRoom(playerMock1.Object, enterRoom);
            Assert.True(playerMock1.Object.RoomId == roomCreatedRes.Id);
            Assert.True(info.Info.First().NumberOfPlayers == 2);
        }

        [Fact]
        public void ExitRoom()
        {
            var config = GetRoomConfig();
            config.DisposeSecondWhileNoMember = 0.0f;
            var roomList = new Server.RoomList(config);

            var playerMock0 = new Mock<Server.IPlayer>();
            playerMock0.SetupProperty(p => p.RoomId, null);
            playerMock0.SetupGet(p => p.Id).Returns(0);

            var createRoom = new IClientMsg.CreateRoom(true, null, 6, "");

            var roomCreatedRes = roomList.CreateRoom(playerMock0.Object, createRoom);
            Assert.True(roomCreatedRes.IsSuccess);

            var info = roomList.GetRoomListInfo();
            Assert.True(playerMock0.Object.RoomId == roomCreatedRes.Id);
            Assert.True(info.Info.Count == 1);
            Assert.True(info.Info.First().NumberOfPlayers == 1);


            roomList.ExitRoom(playerMock0.Object);
            Assert.True(playerMock0.Object.RoomId is null);
            Assert.True(info.Info.Count == 1);
            Assert.True(info.Info.First().NumberOfPlayers == 0);

            roomList.UpdateDisposeStatus();
            Assert.True(info.Info.Count == 0);
        }

        [Fact]
        public void SetPlayerStatus()
        {
            var roomList = new Server.RoomList(GetRoomConfig());

            var playerMock0 = new Mock<Server.IPlayer>();
            playerMock0.SetupProperty(p => p.RoomId, null);
            playerMock0.SetupGet(p => p.Id).Returns(0);

            var createRoom = new IClientMsg.CreateRoom(true, null, 6, "");

            var roomCreatedRes = roomList.CreateRoom(playerMock0.Object, createRoom);
            Assert.True(roomCreatedRes.IsSuccess);

            var info = roomList.GetRoomListInfo();
            Assert.True(playerMock0.Object.RoomId == roomCreatedRes.Id);
            Assert.True(info.Info.Count == 1);
            Assert.True(info.Info.First().NumberOfPlayers == 1);

            var playerStatus = new IClientMsg.SetPlayerStatus(new RoomPlayerStatus { Data = new byte[0] });
            var setPlayerStatusRes = roomList.SetPlayerStatus(playerMock0.Object, playerStatus);

            Assert.True(setPlayerStatusRes.IsSuccess);
            Assert.True(roomList.Rooms[roomCreatedRes.Id].PlayerStatuses.ContainsKey(playerMock0.Object.Id));
        }

        // TODO: OperateRoom

        // TODO: ReceiveGameMessage

        // TODO: Update
    }
}
