using MessagePack;

namespace SquirrelayServer.Common
{
    [MessagePackObject]
    public struct RoomPlayerStatus
    {
        [Key(0)]
        public byte[] Date { get; set; }
    }
}
