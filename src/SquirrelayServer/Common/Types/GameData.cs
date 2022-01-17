using System;

using MessagePack;

namespace SquirrelayServer.Common
{
    [MessagePackObject]
    public sealed class RoomPlayerStatus
    {
        [Key(0)]
        public byte[]? Data { get; private set; }

        [SerializationConstructor]
        public RoomPlayerStatus(byte[]? data)
        {
            Data = data;
        }

        public override bool Equals(object? obj)
        {
            if (!(obj is RoomPlayerStatus)) return false;

            var other = (RoomPlayerStatus)obj;

            return Utils.GetStructualEquatable(Data, other.Data);
        }

        public override int GetHashCode()
        {
            return Data?.GetHashCode() ?? 0;
        }
    }

    [MessagePackObject]
    public sealed class RelayedGameMessage
    {
        [Key(0)]
        public ulong ClientId { get; private set; }

        [Key(1)]
        public float ElapsedSeconds { get; private set; }

        [Key(2)]
        public byte[] Data { get; private set; }

        public override bool Equals(object? obj)
        {
            if (!(obj is RelayedGameMessage)) return false;

            var other = (RelayedGameMessage)obj;

            return (ClientId == other.ClientId)
                && (ElapsedSeconds == other.ElapsedSeconds)
                && Utils.GetStructualEquatable(Data, other.Data);

        }
        public override int GetHashCode()
        {
            return HashCode.Combine(ClientId, ElapsedSeconds, Data);
        }

        [SerializationConstructor]
        public RelayedGameMessage(ulong clientId, float elapsedSeconds, byte[] data)
        {
            ClientId = clientId;
            ElapsedSeconds = elapsedSeconds;
            Data = data;
        }
    }
}
