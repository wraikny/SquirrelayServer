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
}
