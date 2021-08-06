using System;
using System.Collections.Generic;
using System.Text;

using LiteNetLib;

using MessagePack;

using SquirrelayServer.Common;

namespace SquirrelayServer.Client
{
    public sealed class CurrentRoomInfo<T>
        where T : class
    {
        private readonly MessagePackSerializerOptions _options;

        public ulong? OwnerId { get; private set; }

        public Dictionary<ulong, T> Statuses { get; private set; }

        public bool IsPlaying { get; private set; }

        public CurrentRoomInfo(MessagePackSerializerOptions options, ulong? ownerId, IReadOnlyDictionary<ulong, RoomPlayerStatus> statuses)
        {
            _options = options;
            OwnerId = ownerId;
            Statuses = new Dictionary<ulong, T>();

            if (statuses is { })
            {
                foreach (var (k, v) in statuses)
                {
                    try
                    {
                        Statuses[k] = MessagePackSerializer.Deserialize<T>(v.Data, _options);
                    }
                    catch
                    {

                    }

                }
            }
        }

        public void OnUpdatedRoomPlayers(IServerMsg.UpdateRoomPlayers msg)
        {
            OwnerId = msg.Owner;

            foreach (var x in msg.Statuses)
            {
                if (x.Value is null)
                {
                    Statuses.Remove(x.Key);
                }
                else if (x.Value is null)
                {
                    Statuses[x.Key] = null;
                }
                else
                {
                    try
                    {
                        Statuses[x.Key] = MessagePackSerializer.Deserialize<T>(x.Value.Data, _options);
                    }
                    catch
                    {
                        NetDebug.Logger.WriteNet(NetLogLevel.Error, $"Failed to deserialize player status from client({x.Key}).");
                    }
                }
            }
        }

        public void OnNotifiedRoomOperation(RoomOperateKind kind)
        {
            IsPlaying = kind == RoomOperateKind.StartPlaying;
        }
    }
}
