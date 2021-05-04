using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using SquirrelayServer.Common;

namespace SquirrelayServer.Server
{
    internal enum RoomStatus
    {
        WaitingToPlay,
        Playing,
        OwnerExited,
    }

    internal class Room
    {
        private ulong? _owner;
        private readonly List<ulong> _clientIds;
        private readonly Dictionary<ulong, RoomPlayerStatus> _playersStatuses;
        private readonly HashSet<ulong> _statusUpdatedIds;

        private readonly Stopwatch _disposeStopwatch;

        internal IReadOnlyDictionary<ulong, RoomPlayerStatus> PlayerStatuses => _playersStatuses;

        public int Id { get; private set; }
        public RoomInfo Info { get; private set; }
        public string Password { get; private set; }

        public int PlayersCount => _clientIds.Count;

        public RoomStatus RoomStatus { get; private set; }

        public Room(int id, RoomInfo info, string password)
        {
            _clientIds = new List<ulong>();
            _playersStatuses = new Dictionary<ulong, RoomPlayerStatus>();
            _statusUpdatedIds = new HashSet<ulong>();

            _disposeStopwatch = new Stopwatch();

            Id = id;
            Info = info;
            Password = password;
        }

        public float? DeltaSecondToDispose
        {
            get
            {
                if (_disposeStopwatch.IsRunning)
                {
                    return Utils.MsToSec((int)_disposeStopwatch.ElapsedMilliseconds);
                }

                return null;
            }
        }

        public void Update()
        {
            // TODO: status が playing のときにリレー処理を行う。
            // playerのメッセージをためておくQueueも必要

        }

        public IServerMsg.SetPlayerStatusResponse SetPlayerStatus(ulong clientId, RoomPlayerStatus status)
        {
            _playersStatuses[clientId] = status;
            _statusUpdatedIds.Add(clientId);
            return IServerMsg.SetPlayerStatusResponse.Success;
        }

        public void EnterRoomWithoutCheck(ulong clientId)
        {
            _clientIds.Add(clientId);
            Info.NumberOfPlayers++;

            if (_owner is null)
            {
                _owner = clientId;
                RoomStatus = RoomStatus.WaitingToPlay;
            }

            if (_disposeStopwatch.IsRunning)
            {
                _disposeStopwatch.Reset();
            }
        }

        public IServerMsg.EnterRoomResponse EnterRoom(ulong clientId, string password)
        {
            if (Password is string p && p != password) return IServerMsg.EnterRoomResponse.InvalidPassword;
            if (Info.MaxNumberOfPlayers == _clientIds.Count) return IServerMsg.EnterRoomResponse.NumberOfPlayersLimitation;
            if (_clientIds.Contains(clientId)) return IServerMsg.EnterRoomResponse.AlreadyEntered;

            EnterRoomWithoutCheck(clientId);

            return IServerMsg.EnterRoomResponse.Success(_playersStatuses);
        }

        public IServerMsg.ExitRoomResponse ExitRoom(ulong clientId)
        {
            if (!_clientIds.Remove(clientId))
            {
                throw new InvalidOperationException($"Client '{clientId}' doesn't exists in room '{Id}'");
            }

            if (_owner == clientId && _clientIds.Count != 0)
            {
                _owner = _clientIds.First();
            }
            else if (_clientIds.Count == 0)
            {
                RoomStatus = RoomStatus.OwnerExited;
                _owner = null;
                _disposeStopwatch.Start();
            }

            Info.NumberOfPlayers--;

            return IServerMsg.ExitRoomResponse.Success;
        }

        public IServerMsg.OperateRoomResponse OperateRoom(ulong clientId, RoomOperateKind kind)
        {
            if (_owner != clientId)
            {
                return IServerMsg.OperateRoomResponse.PlayerIsNotOwner;
            }

            switch (kind)
            {
                case RoomOperateKind.StartPlaying:
                    {
                        if (RoomStatus == RoomStatus.Playing)
                        {
                            return IServerMsg.OperateRoomResponse.InvalidRoomStatus;
                        }

                        RoomStatus = RoomStatus.Playing;

                        break;
                    }
                case RoomOperateKind.FinishPlaying:
                    {
                        if (RoomStatus != RoomStatus.Playing)
                        {
                            return IServerMsg.OperateRoomResponse.InvalidRoomStatus;
                        }

                        RoomStatus = RoomStatus.WaitingToPlay;

                        break;
                    }
                default:
                    break;
            }

            return IServerMsg.OperateRoomResponse.Success;
        }
    }
}
