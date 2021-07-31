using System.Collections;

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

            if (Data is null && other.Data is null) return true;

            return ((IStructuralEquatable)Data).Equals(other.Data, StructuralComparisons.StructuralEqualityComparer);
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

        [SerializationConstructor]
        public RelayedGameMessage(ulong clientId, float elapsedSecond, byte[] data)
        {
            ClientId = clientId;
            ElapsedSecond = elapsedSecond;
            Data = data;
        }
    }
}
