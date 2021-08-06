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
                        if (v is null) continue;

                        if (v.Data is null)
                        {
                            Statuses[k] = null;
                        }
                        else
                        {
                            Statuses[k] = MessagePackSerializer.Deserialize<T>(v.Data, _options);
                        }
                    }
                    catch
                    {

                        NetDebug.Logger.WriteNet(NetLogLevel.Error, $"Failed to deserialize player status from client({k}).");
                    }

                }
            }
        }

        public void OnUpdatedRoomPlayers(IServerMsg.UpdateRoomPlayers msg)
        {
            OwnerId = msg.Owner;

            foreach (var (k, v) in msg.Statuses)
            {
                if (v is null)
                {
                    Statuses.Remove(k);
                }
                else if (v.Data is null)
                {
                    Statuses[k] = null;
                }
                else
                {
                    try
                    {
                        Statuses[k] = MessagePackSerializer.Deserialize<T>(v.Data, _options);
                    }
                    catch
                    {
                        NetDebug.Logger.WriteNet(NetLogLevel.Error, $"Failed to deserialize player status from client({k}).");
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
