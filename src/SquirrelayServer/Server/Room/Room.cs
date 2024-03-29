﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using LiteNetLib;

using MessagePack;

using SquirrelayServer.Common;

namespace SquirrelayServer.Server
{
    internal class Room
    {
        private readonly MessagePackSerializerOptions _serializerOptions;
        private readonly RoomConfig _roomConfig;
        private readonly ServerLoggingConfig _loggingConfig;
        private ulong? _owner;
        private readonly List<IClientHandler> _clients;
        private readonly Dictionary<ulong, RoomPlayerStatus> _playersStatuses;
        private readonly Dictionary<ulong, RoomPlayerStatus?> _updatedPlayersStatuses;

        // StopWatch to count the time when Room is disposed.
        private readonly Stopwatch _disposeStopwatch;

        // StopWatch for counting game time.
        private readonly Stopwatch _gameStopWatch;

        private readonly List<RelayedGameMessage> _temporalGameMessagesBuffer;

        private bool _updatedRoomStatus;

        internal IReadOnlyDictionary<ulong, RoomPlayerStatus> PlayerStatuses => _playersStatuses;

        public int Id { get; private set; }
        public RoomInfo Info { get; private set; }
        public string? Password { get; private set; }
        public string ClientVersion { get; private set; }

        public int PlayersCount => _clients.Count;

        public RoomStatus RoomStatus { get; private set; }

        public Room(MessagePackSerializerOptions serializerOptions, RoomConfig roomConfig, ServerLoggingConfig loggingConfig, string clientVersion, int id, RoomInfo info, string? password)
        {
            _serializerOptions = serializerOptions;
            _roomConfig = roomConfig;
            _loggingConfig = loggingConfig;

            _clients = new List<IClientHandler>();

            _playersStatuses = new Dictionary<ulong, RoomPlayerStatus>();
            _updatedPlayersStatuses = new Dictionary<ulong, RoomPlayerStatus?>();

            _disposeStopwatch = new Stopwatch();
            _gameStopWatch = new Stopwatch();
            _temporalGameMessagesBuffer = new List<RelayedGameMessage>();

            Id = id;
            Info = info;
            Password = password;
            ClientVersion = clientVersion;
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

        private void WriteInfo(string str, params object[] args)
        {
            if (!_loggingConfig.Logging || !_loggingConfig.RoomLogging) return;
            NetDebug.Logger?.WriteNet(NetLogLevel.Info, str, args);
        }

        private void Broadcast(IServerMsg msg)
        {
            if (_clients.Count == 0) return;

            var data = MessagePackSerializer.Serialize(msg, _serializerOptions);

            foreach (var client in _clients)
            {
                client.SendByte(data);
            }

            if (_loggingConfig.MessageLogging)
            {
                WriteInfo($"Send message of {msg.GetType()} to clients in room({Id}).");
            }
        }

        private void UpdatePlayerStatus(ulong clientId, RoomPlayerStatus? status)
        {
            _updatedPlayersStatuses[clientId] = status;
        }

        public void Update()
        {
            if (_clients.Count > 0)
            {
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

                    // clear
                    _updatedPlayersStatuses.Clear();
                }

                if (_updatedRoomStatus)
                {
                    var msg = new IServerMsg.UpdateRoomMessage(Info.Message);
                    Broadcast(msg);
                    _updatedRoomStatus = false;
                }
            }

            // When playing the game
            if (RoomStatus == RoomStatus.Playing)
            {
                if (_temporalGameMessagesBuffer.Count > 0)
                {
                    var msg = new IServerMsg.BroadcastGameMessages(_temporalGameMessagesBuffer);
                    Broadcast(msg);
                    _temporalGameMessagesBuffer.Clear();
                }

                if (_roomConfig.TickMessageEnabled)
                {
                    var gameTime = Utils.MsToSec((int)_gameStopWatch.ElapsedMilliseconds);
                    Broadcast(new IServerMsg.Tick(gameTime));
                }
            }
        }

        public ulong EnterRoomWithoutCheck(IClientHandler client, byte[]? status)
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

            UpdatePlayerStatus(client.Id, new RoomPlayerStatus(status));

            return _owner.Value;
        }

        public IServerMsg.EnterRoomResponse<byte[]> EnterRoom(IClientHandler client, string? password, byte[]? status)
        {
            if (ClientVersion != client.ClientVersion) return IServerMsg.EnterRoomResponse.DifferentClientVersion;
            if (Password is string p && p != password) return IServerMsg.EnterRoomResponse.InvalidPassword;
            if (Info.MaxNumberOfPlayers == _clients.Count) return IServerMsg.EnterRoomResponse.NumberOfPlayersLimitation;

            if (!_roomConfig.EnterWhenPlayingAllowed && RoomStatus == RoomStatus.Playing)
            {
                return IServerMsg.EnterRoomResponse.InvalidRoomStatus;
            }

            if (_clients.Contains(client)) return IServerMsg.EnterRoomResponse.AlreadyEntered;

            var ownerId = EnterRoomWithoutCheck(client, status);

            WriteInfo($"Room({Id}): client({client.Id}) entered.");

            return IServerMsg.EnterRoomResponse.Success(ownerId, _playersStatuses, Info.Message);
        }

        public IServerMsg.ExitRoomResponse ExitRoom(IClientHandler client)
        {
            if (!_clients.Remove(client))
            {
                throw new InvalidOperationException($"Client '{client.Id}' doesn't exists in room '{Id}'");
            }

            WriteInfo($"Room({Id}): client({client.Id}) exited.");

            if (_owner == client.Id)
            {
                if (_clients.Count > 0)
                {
                    _owner = _clients.First().Id;

                    WriteInfo($"Room({Id}): set new owner({_owner}).");
                }
                else
                {
                    Info.IsPlaying = false;
                    _owner = null;
                    _disposeStopwatch.Start();

                    WriteInfo($"Room({Id}): owner exit.");

                    if (RoomStatus == RoomStatus.Playing)
                    {
                        FinishPlaying();
                    }

                    RoomStatus = RoomStatus.OwnerExited;
                }
            }

            Info.NumberOfPlayers--;

            UpdatePlayerStatus(client.Id, null);

            return IServerMsg.ExitRoomResponse.Success;
        }

        private void StartPlaying()
        {
            RoomStatus = RoomStatus.Playing;
            Info.IsPlaying = true;
            _gameStopWatch.Start();

            Broadcast(new IServerMsg.NotifyRoomOperation(RoomOperateKind.StartPlaying));

            WriteInfo($"Room({Id}): start playing.");
        }

        private void FinishPlaying()
        {
            _gameStopWatch.Reset();
            RoomStatus = RoomStatus.WaitingToPlay;
            Info.IsPlaying = false;

            Broadcast(new IServerMsg.NotifyRoomOperation(RoomOperateKind.FinishPlaying));

            _temporalGameMessagesBuffer.Clear();

            WriteInfo($"Room({Id}): finish playing.");
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

                        if (_clients.Count < _roomConfig.NumberOfPlayersRange.Item1)
                        {
                            return IServerMsg.OperateRoomResponse.NotEnoughPeople;
                        }

                        StartPlaying();

                        break;
                    }
                case RoomOperateKind.FinishPlaying:
                    {
                        if (RoomStatus != RoomStatus.Playing)
                        {
                            return IServerMsg.OperateRoomResponse.InvalidRoomStatus;
                        }

                        FinishPlaying();

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

        public IServerMsg.SetRoomMessageResponse SetRoomStatus(IClientHandler client, byte[] roomStatus)
        {
            if (client.Id != _owner) return IServerMsg.SetRoomMessageResponse.PlayerIsNotOwner;

            Info.Message = roomStatus;
            _updatedRoomStatus = true;

            return IServerMsg.SetRoomMessageResponse.Success;
        }

        public IServerMsg.SendGameMessageResponse ReceiveGameMessage(IClientHandler client, byte[] data)
        {
            if (RoomStatus != RoomStatus.Playing)
            {
                return IServerMsg.SendGameMessageResponse.InvalidRoomStatus;
            }

            var msg = new RelayedGameMessage(client.Id, Utils.MsToSec((int)_gameStopWatch.ElapsedMilliseconds), data);

            _temporalGameMessagesBuffer.Add(msg);

            return IServerMsg.SendGameMessageResponse.Success;
        }
    }
}
