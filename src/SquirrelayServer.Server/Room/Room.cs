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
        private readonly Dictionary<ulong, ClientHandler> _clients;
        private readonly Dictionary<ulong, RoomPlayerStatus> _playersStatuses;

        private readonly Stopwatch _disposeStopwatch;

        public int Id { get; private set; }
        public RoomInfo Info { get; private set; }
        public string Password { get; private set; }

        public int PlayersCount => _clients.Count;

        public RoomStatus RoomStatus { get; private set; }

        public Room(int id, RoomInfo info, string password)
        {
            _clients = new Dictionary<ulong, ClientHandler>();
            _playersStatuses = new Dictionary<ulong, RoomPlayerStatus>();

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

        public void EnterRoomWithoutCheck(ClientHandler client)
        {
            _clients.Add(client.Id, client);
            client.EnterRoom(Id);

            if (_owner is null)
            {
                _owner = client.Id;
                RoomStatus = RoomStatus.WaitingToPlay;
            }

            if (_disposeStopwatch.IsRunning)
            {
                _disposeStopwatch.Reset();
            }
        }

        public IServerMsg.EnterRoomResponse EnterRoom(ClientHandler client, string password)
        {
            if (Password is string p && p != password) return IServerMsg.EnterRoomResponse.InvalidPassword;
            if (Info.MaxNumberOfPlayers == _clients.Count) return IServerMsg.EnterRoomResponse.NumberOfPlayersLimitation;
            if (_clients.ContainsKey(client.Id)) return IServerMsg.EnterRoomResponse.AlreadyEntered;

            EnterRoomWithoutCheck(client);

            return IServerMsg.EnterRoomResponse.Success;
        }

        public IServerMsg.ExitRoomResponse ExitRoom(ClientHandler client)
        {
            if (!_clients.Remove(client.Id))
            {
                throw new InvalidOperationException($"Client '{client.Id}' doesn't exists in room '{Id}'");
            }

            if (_owner == client.Id && _clients.Count != 0)
            {
                _owner = _clients.Values.First().Id;
            }
            else if (_clients.Count == 0)
            {
                RoomStatus = RoomStatus.Playing;
                _owner = null;
                _disposeStopwatch.Start();
            }

            return IServerMsg.ExitRoomResponse.Success;
        }
    }
}
