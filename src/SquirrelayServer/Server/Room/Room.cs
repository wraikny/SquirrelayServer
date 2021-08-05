using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using LiteNetLib;

using MessagePack;

using SquirrelayServer.Common;

namespace SquirrelayServer.Server
{
    internal class Room
    {
        private readonly MessagePackSerializerOptions _serializerOptions;
        private ulong? _owner;
        private readonly List<IClientHandler> _clients;
        private readonly Dictionary<ulong, RoomPlayerStatus> _playersStatuses;
        private readonly Dictionary<ulong, RoomPlayerStatus> _updatedPlayersStatuses;

        // StopWatch to count the time when Room is disposed.
        private readonly Stopwatch _disposeStopwatch;

        // StopWatch for counting game time.
        private readonly Stopwatch _gameStopWatch;

        private readonly List<RelayedGameMessage> _temporalGameMessageBuffer;


        internal IReadOnlyDictionary<ulong, RoomPlayerStatus> PlayerStatuses => _playersStatuses;

        public int Id { get; private set; }
        public RoomInfo Info { get; private set; }
        public string Password { get; private set; }

        public int PlayersCount => _clients.Count;

        public RoomStatus RoomStatus { get; private set; }

        public Room(MessagePackSerializerOptions serializerOptions, int id, RoomInfo info, string password)
        {
            _serializerOptions = serializerOptions;

            _clients = new List<IClientHandler>();

            _playersStatuses = new Dictionary<ulong, RoomPlayerStatus>();
            _updatedPlayersStatuses = new Dictionary<ulong, RoomPlayerStatus>();

            _disposeStopwatch = new Stopwatch();
            _gameStopWatch = new Stopwatch();
            _temporalGameMessageBuffer = new List<RelayedGameMessage>();

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
        private void Broadcast(IServerMsg msg)
        {
            var data = MessagePackSerializer.Serialize(msg, _serializerOptions);

            foreach (var client in _clients)
            {
                client.SendByte(data);
            }
        }

        private void UpdatePlayerStatus(ulong clientId, RoomPlayerStatus status)
        {
            _updatedPlayersStatuses[clientId] = status;
        }

        public void Update()
        {

            // When playerstatuses is updated
            if (_updatedPlayersStatuses.Count > 0)
            {
                foreach (var x in _updatedPlayersStatuses)
                {
                    if (x.Value is null)
                    {
                        _playersStatuses.Remove(x.Key);
                    }
                    else
                    {
                        _playersStatuses[x.Key] = x.Value;
                    }
                }
                var msg = new IServerMsg.UpdateRoomPlayers(_owner, _updatedPlayersStatuses);
                Broadcast(msg);
                _updatedPlayersStatuses.Clear();
            }

            // When playing the game
            if (RoomStatus == RoomStatus.Playing)
            {
                var msg = new IServerMsg.DistributeGameMessage(_temporalGameMessageBuffer);
                Broadcast(msg);
                _temporalGameMessageBuffer.Clear();
            }
        }

        public void EnterRoomWithoutCheck(IClientHandler client)
        {
            _clients.Add(client);
            Info.NumberOfPlayers++;

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

        public IServerMsg.EnterRoomResponse EnterRoom(IClientHandler client, string password)
        {
            if (Password is string p && p != password) return IServerMsg.EnterRoomResponse.InvalidPassword;
            if (Info.MaxNumberOfPlayers == _clients.Count) return IServerMsg.EnterRoomResponse.NumberOfPlayersLimitation;
            if (RoomStatus == RoomStatus.Playing) return IServerMsg.EnterRoomResponse.InvalidRoomStatus;
            if (_clients.Contains(client)) return IServerMsg.EnterRoomResponse.AlreadyEntered;

            EnterRoomWithoutCheck(client);

            UpdatePlayerStatus(client.Id, new RoomPlayerStatus { Data = null });

            return IServerMsg.EnterRoomResponse.Success(_owner.Value, _playersStatuses);
        }

        public IServerMsg.ExitRoomResponse ExitRoom(IClientHandler client)
        {
            if (!_clients.Remove(client))
            {
                throw new InvalidOperationException($"Client '{client.Id}' doesn't exists in room '{Id}'");
            }

            if (_owner == client.Id)
            {
                if (_clients.Count != 0)
                {
                    _owner = _clients.First().Id;
                }
                else if (_clients.Count == 0)
                {
                    RoomStatus = RoomStatus.OwnerExited;
                    Info.IsPlaying = false;
                    _owner = null;
                    _disposeStopwatch.Start();
                }
            }

            Info.NumberOfPlayers--;

            UpdatePlayerStatus(client.Id, null);

            return IServerMsg.ExitRoomResponse.Success;
        }

        public IServerMsg.OperateRoomResponse OperateRoom(IClientHandler client, RoomOperateKind kind)
        {
            if (_owner != client.Id)
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
                        Info.IsPlaying = true;
                        _gameStopWatch.Start();

                        // todo

                        break;
                    }
                case RoomOperateKind.FinishPlaying:
                    {
                        if (RoomStatus != RoomStatus.Playing)
                        {
                            return IServerMsg.OperateRoomResponse.InvalidRoomStatus;
                        }

                        _gameStopWatch.Reset();
                        RoomStatus = RoomStatus.WaitingToPlay;
                        Info.IsPlaying = false;

                        // todo

                        break;
                    }
                default:
                    break;
            }

            return IServerMsg.OperateRoomResponse.Success;
        }

        public IServerMsg.SetPlayerStatusResponse SetPlayerStatus(IClientHandler client, RoomPlayerStatus status)
        {
            UpdatePlayerStatus(client.Id, status);

            return IServerMsg.SetPlayerStatusResponse.Success;
        }

        public IServerMsg.SendGameMessageResponse ReceiveGameMessage(IClientHandler client, byte[] data)
        {
            if (RoomStatus != RoomStatus.Playing)
            {
                return IServerMsg.SendGameMessageResponse.InvalidRoomStatus;
            }

            var msg = new RelayedGameMessage(client.Id, Utils.MsToSec((int)_gameStopWatch.ElapsedMilliseconds), data);

            _temporalGameMessageBuffer.Add(msg);

            return IServerMsg.SendGameMessageResponse.Success;
        }
    }
}
