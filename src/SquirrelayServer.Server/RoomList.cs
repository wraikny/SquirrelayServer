using System;
using System.Collections.Generic;
using System.Text;

using SquirrelayServer.Common;

namespace SquirrelayServer.Server
{
    internal sealed class RoomList
    {
        private readonly Random _rand;
        private readonly Dictionary<int, Room> _rooms;

        private readonly RoomConfig _roomConfig;

        public RoomList(RoomConfig roomConfig)
        {
            _rand = new Random();
            _rooms = new Dictionary<int, Room>();

            _roomConfig = roomConfig;
        }

        public int CreateRoom(IClientMsg.CreateRoom msg)
        {
            const int IdMin = 10000;
            const int IdMax = 100000;

            var roomId = _rand.Next(IdMin, IdMax);
            while (_rooms.ContainsKey(roomId))
            {
                roomId = _rand.Next(IdMin, IdMax);
            }

            var maxRange = _roomConfig.MaxNumberOfPlayersRange;

            var roomInfo = new RoomInfo
            {
                IsVisible = _roomConfig.InvisibleEnabled && msg.IsVisible,
                MaxNumberOfPlayers = Utils.Clamp(msg.MaxNumberOfPlayers, maxRange.Item1, maxRange.Item2),
                NumberOfPlayers = 0,
                Message = _roomConfig.MessageEnabled ? msg.Message : null,
            };

            var password = _roomConfig.PasswordEnabled ? msg.Password : null;

            var room = new Room(roomId, roomInfo, password);

            _rooms[roomId] = room;

            return roomId;
        }

        public IServerMsg.EnterRoomResponse EnterRoom(ClientHandler client, IClientMsg.EnterRoom msg)
        {
            if (!_rooms.TryGetValue(msg.RoomId, out var room)) return IServerMsg.EnterRoomResponse.RoomNotFound;

            return room.EnterRoom(client, msg.Password);
        }
    }
}
