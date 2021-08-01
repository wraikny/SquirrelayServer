using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using SquirrelayServer.Common;

namespace SquirrelayServer.Server
{
    internal sealed class RoomList
    {
        private readonly Random _rand;

        private readonly RoomConfig _roomConfig;

        private readonly Dictionary<int, RoomInfo> _roomInfoList;
        private readonly Dictionary<int, Room> _rooms;

        internal IReadOnlyDictionary<int, Room> Rooms => _rooms;

        private readonly Stopwatch _disposeIntervalStopWatch;


        public RoomList(RoomConfig roomConfig)
        {
            _rand = new Random();
            _roomConfig = roomConfig;

            _rooms = new Dictionary<int, Room>();
            _roomInfoList = new Dictionary<int, RoomInfo>();

            _disposeIntervalStopWatch = new Stopwatch();
        }

        public void Start()
        {
            _disposeIntervalStopWatch.Start();
        }

        public void Update<T>(IReadOnlyDictionary<ulong, T> clients)
            where T: IClientHandler
        {
            foreach (var (_, room) in _rooms)
            {
                room.Update(clients);
            }

            // Update dispose
            var disposeSecond = Utils.MsToSec((int)_disposeIntervalStopWatch.ElapsedMilliseconds);

            if (disposeSecond >= _roomConfig.UpdatingDisposeStatusIntervalSecond)
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
                && (x.Value.RoomStatus == RoomStatus.OwnerExited && t > _roomConfig.DisposeSecondWhileNoMember)
            ).Select(x => x.Key).ToArray();

            foreach (var id in removeIds)
            {
                _rooms.Remove(id);
                _roomInfoList.Remove(id);
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

        public IServerMsg.SetPlayerStatusResponse SetPlayerStatus(IPlayer player, IClientMsg.SetPlayerStatus msg)
        {
            if (player.RoomId is null)
            {
                return IServerMsg.SetPlayerStatusResponse.PlayerOutOfRoom;
            }

            var room = _rooms[player.RoomId.Value];

            return room.SetPlayerStatus(player.Id, msg.Status);
        }

        private IServerMsg.RoomListResponse _roomListResponseCache;

        public IServerMsg.RoomListResponse GetRoomListInfo()
        {
            _roomListResponseCache ??= new IServerMsg.RoomListResponse(_roomInfoList.Values);
            return _roomListResponseCache;
        }

        public IServerMsg.CreateRoomResponse CreateRoom(IPlayer player, IClientMsg.CreateRoom msg)
        {
            // is not null
            if (player.RoomId is { })
            {
                return IServerMsg.CreateRoomResponse.AlreadyEntered;
            }

            var roomId = GenerateRoomId();

            var maxRange = _roomConfig.MaxNumberOfPlayersRange;

            var roomInfo = new RoomInfo
            {
                IsVisible = _roomConfig.InvisibleEnabled && msg.IsVisible,
                MaxNumberOfPlayers = Utils.Clamp(msg.MaxNumberOfPlayers, maxRange.Item1, maxRange.Item2),
                NumberOfPlayers = 0,
                Message = _roomConfig.RoomMessageEnabled ? msg.Message : null,
            };

            var password = _roomConfig.PasswordEnabled ? msg.Password : null;

            var room = new Room(roomId, roomInfo, password);

            _rooms[roomId] = room;
            _roomInfoList[roomId] = roomInfo;

            room.EnterRoomWithoutCheck(player.Id);

            player.RoomId = roomId;

            return IServerMsg.CreateRoomResponse.Success(roomId);
        }

        public IServerMsg.EnterRoomResponse EnterRoom(IPlayer player, IClientMsg.EnterRoom msg)
        {
            if (player.RoomId is { }) return IServerMsg.EnterRoomResponse.AlreadyEntered;
            if (!_rooms.TryGetValue(msg.RoomId, out var room)) return IServerMsg.EnterRoomResponse.RoomNotFound;

            var res = room.EnterRoom(player.Id, msg.Password);

            if (res.IsSuccess)
            {
                player.RoomId = room.Id;
            }

            return res;
        }

        public IServerMsg.ExitRoomResponse ExitRoom(IPlayer player)
        {
            if (player.RoomId is int roomId)
            {
                var room = _rooms[roomId];
                var res = room.ExitRoom(player.Id);
                if (res.IsSuccess)
                {
                    player.RoomId = null;
                }
                return res;
            }
            else
            {
                return IServerMsg.ExitRoomResponse.PlayerOutOfRoom;
            }
        }

        public IServerMsg.OperateRoomResponse OperateRoom(IPlayer player, IClientMsg.OperateRoom msg)
        {
            if (player.RoomId is null) return IServerMsg.OperateRoomResponse.PlayerOutOfRoom;

            var room = _rooms[player.RoomId.Value];

            var res = room.OperateRoom(player.Id, msg.Operate);

            return res;
        }

        public IServerMsg.SendGameMessageResponse ReceiveGameMessage(IPlayer player, IClientMsg.SendGameMessage msg)
        {
            if (player.RoomId is null) return IServerMsg.SendGameMessageResponse.PlayerOutOfRoom;

            var room = _rooms[player.RoomId.Value];

            var res = room.ReceiveGameMessage(player.Id, msg.Data);

            return res;
        }
    }
}
