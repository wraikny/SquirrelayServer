using System;

using MessagePack;

namespace SquirrelayServer.Common
{
    [MessagePackObject]
    public sealed class RoomPlayerStatus
    {
        [Key(0)]
        public byte[] Data { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is RoomPlayerStatus)) return false;

            var other = (RoomPlayerStatus)obj;

            return Utils.GetStructualEquatable(Data, other.Data);
        }

        public override int GetHashCode()
        {
            return Data.GetHashCode();
        }
    }

    [MessagePackObject]
    public sealed class RelayedGameMessage
    {
        [Key(0)]
        public ulong ClientId { get; private set; }

        [Key(1)]
        public float ElapsedSecond { get; private set; }

        [Key(2)]
        public byte[] Data { get; private set; }

        public override bool Equals(object obj)
        {
            if (!(obj is RelayedGameMessage)) return false;

            var other = (RelayedGameMessage)obj;

            return (ClientId == other.ClientId)
                && (ElapsedSecond == other.ElapsedSecond)
                && Utils.GetStructualEquatable(Data, other.Data);

        }
        public override int GetHashCode()
        {
            return HashCode.Combine(ClientId, ElapsedSecond, Data);
        }

        [SerializationConstructor]
        public RelayedGameMessage(ulong clientId, float elapsedSecond, byte[] data)
        {
            ClientId = clientId;
            ElapsedSecond = elapsedSecond;
            Data = data;
        }
    }
}
