using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using LiteNetLib;

using MessagePack;

using SquirrelayServer.Common;

namespace SquirrelayServer.Server
{
    internal sealed class RoomList
    {
        private readonly RoomConfig _roomConfig;

        private readonly MessagePackSerializerOptions _serializerOptions;

        private readonly Random _rand;

        private readonly Dictionary<int, RoomInfo> _roomInfoList;
        private readonly Dictionary<int, Room> _rooms;

        internal IReadOnlyDictionary<int, Room> Rooms => _rooms;

        private readonly Stopwatch _disposeIntervalStopWatch;

        internal Dictionary<int, RoomInfo> RoomInfoList => _roomInfoList;

        public RoomList(RoomConfig roomConfig, MessagePackSerializerOptions serializerOptions)
        {
            _roomConfig = roomConfig;
            _serializerOptions = serializerOptions;

            _rooms = new Dictionary<int, Room>();
            _roomInfoList = new Dictionary<int, RoomInfo>();

            _disposeIntervalStopWatch = new Stopwatch();
            _rand = new Random();
        }

        public void Start()
        {
            _disposeIntervalStopWatch.Start();
        }

        public void Update()
        {
            foreach (var (_, room) in _rooms)
            {
                room.Update();
            }

            // Update dispose
            var disposeSecond = Utils.MsToSec((int)_disposeIntervalStopWatch.ElapsedMilliseconds);

            if (disposeSecond >= _roomConfig.UpdatingDisposeStatusIntervalSeconds)
            {
                UpdateDisposeStatus();
                _disposeIntervalStopWatch.Restart();
            }
        }

        public void Stop()
        {
            _rooms.Clear();
            _roomInfoList.Clear();
            _disposeIntervalStopWatch.Reset();
        }

        // `internal` for unit test
        internal void UpdateDisposeStatus()
        {
            if (_rooms.Count == 0) return;

            var removeIds = _rooms.Where(x =>
                x.Value.DeltaSecondToDispose is float t
                && (x.Value.RoomStatus == RoomStatus.OwnerExited && t > _roomConfig.DisposeSecondsWhenNoMember)
            ).Select(x => x.Key).ToArray();

            foreach (var id in removeIds)
            {
                _rooms.Remove(id);
                _roomInfoList.Remove(id);

                NetDebug.Logger.WriteNet(NetLogLevel.Info, $"Room({id}): disposed.");
            }
        }

        private int GenerateRoomId()
        {
            var (idMin, idMax) = _roomConfig.GeneratedRoomIdRange;
            var roomId = _rand.Next(idMin, idMax + 1);
            while (_rooms.ContainsKey(roomId))
            {
                roomId = _rand.Next(idMin, idMax + 1);
            }

            return roomId;
        }

        public IServerMsg.RoomListResponse GetRoomInfoList()
        {
            return new IServerMsg.RoomListResponse(_roomInfoList.Values.Where(x => x.IsVisible).ToArray());
        }

        public IServerMsg.CreateRoomResponse CreateRoom(IClientHandler client, IClientMsg.CreateRoom msg)
        {
            // is not null
            if (client.RoomId is { })
            {
                return IServerMsg.CreateRoomResponse.AlreadyEntered;
            }

            var roomId = GenerateRoomId();

            var maxRange = _roomConfig.NumberOfPlayersRange;

            var roomInfo = new RoomInfo
            {
                IsVisible = _roomConfig.InvisibleEnabled ? msg.IsVisible : true,
                MaxNumberOfPlayers = Utils.Clamp(msg.MaxNumberOfPlayers, maxRange.Item1, maxRange.Item2),
                NumberOfPlayers = 0,
                Message = _roomConfig.RoomMessageEnabled ? msg.Message : null,
            };

            var password = _roomConfig.PasswordEnabled ? msg.Password : null;

            var room = new Room(_serializerOptions, _roomConfig, roomId, roomInfo, password);

            _rooms[roomId] = room;
            _roomInfoList[roomId] = roomInfo;

            room.EnterRoomWithoutCheck(client);

            client.RoomId = roomId;

            NetDebug.Logger.WriteNet(NetLogLevel.Info, $"Room({roomId}): created");

            return IServerMsg.CreateRoomResponse.Success(roomId);
        }

        public IServerMsg.EnterRoomResponse EnterRoom(IClientHandler client, IClientMsg.EnterRoom msg)
        {
            if (client.RoomId is { }) return IServerMsg.EnterRoomResponse.AlreadyEntered;
            if (!_rooms.TryGetValue(msg.RoomId, out var room)) return IServerMsg.EnterRoomResponse.RoomNotFound;

            var res = room.EnterRoom(client, msg.Password);

            if (res.IsSuccess)
            {
                client.RoomId = room.Id;
            }

            return res;
        }

        public IServerMsg.ExitRoomResponse ExitRoom(IClientHandler client)
        {
            if (client.RoomId is int roomId)
            {
                var room = _rooms[roomId];
                var res = room.ExitRoom(client);
                if (res.IsSuccess)
                {
                    client.RoomId = null;
                }
                return res;
            }
            else
            {
                return IServerMsg.ExitRoomResponse.PlayerOutOfRoom;
            }
        }

        public IServerMsg.OperateRoomResponse OperateRoom(IClientHandler client, IClientMsg.OperateRoom msg)
        {
            if (client.RoomId is null) return IServerMsg.OperateRoomResponse.PlayerOutOfRoom;

            var room = _rooms[client.RoomId.Value];

            var res = room.OperateRoom(client, msg.Operate);

            return res;
        }

        public IServerMsg.SendGameMessageResponse ReceiveGameMessage(IClientHandler client, IClientMsg.SendGameMessage msg)
        {
            if (client.RoomId is null) return IServerMsg.SendGameMessageResponse.PlayerOutOfRoom;

            var room = _rooms[client.RoomId.Value];

            var res = room.ReceiveGameMessage(client, msg.Data);

            return res;
        }

        public IServerMsg.SetPlayerStatusResponse SetPlayerStatus(IClientHandler client, IClientMsg.SetPlayerStatus msg)
        {
            if (client.RoomId is null)
            {
                return IServerMsg.SetPlayerStatusResponse.PlayerOutOfRoom;
            }

            var room = _rooms[client.RoomId.Value];

            return room.SetPlayerStatus(client, msg.Status);
        }
    }
}
