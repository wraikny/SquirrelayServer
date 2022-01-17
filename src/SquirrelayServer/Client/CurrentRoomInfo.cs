using System.Collections.Generic;

using LiteNetLib;

using MessagePack;

using SquirrelayServer.Common;

namespace SquirrelayServer.Client
{
    internal interface CurrentRoomInfoListener<TPlayerStatus, TRoomMessage>
        where TPlayerStatus : class
        where TRoomMessage : class
    {

    }

    public sealed class CurrentRoomInfo<TPlayerStatus, TRoomMessage>
        where TPlayerStatus : class
        where TRoomMessage : class
    {
        public int Id { get; internal set; }

        public ulong? OwnerId { get; internal set; }

        internal Dictionary<ulong, TPlayerStatus?> PlayerStatusesImpl { get; set; }

        public IReadOnlyDictionary<ulong, TPlayerStatus?> PlayerStatuses => PlayerStatusesImpl;

        public TRoomMessage? RoomMessage { get; internal set; }

        public bool IsPlaying { get; internal set; }

        internal CurrentRoomInfo(int id, ulong? ownerId)
        {
            Id = id;
            OwnerId = ownerId;
            PlayerStatusesImpl = new Dictionary<ulong, TPlayerStatus?>();
        }
        public void OnNotifiedRoomOperation(RoomOperateKind kind)
        {
            IsPlaying = kind == RoomOperateKind.StartPlaying;
        }

        internal void SetRoomMessage(MessagePackSerializerOptions options, byte[]? roomMessage)
        {
            if (roomMessage is null)
            {
                RoomMessage = null;
                return;
            }

            try
            {
                RoomMessage = MessagePackSerializer.Deserialize<TRoomMessage>(roomMessage, options);
            }
            catch
            {
                NetDebug.Logger?.WriteNet(NetLogLevel.Error, $"Failed to deserialize room status.");
            }
        }
    }
}
