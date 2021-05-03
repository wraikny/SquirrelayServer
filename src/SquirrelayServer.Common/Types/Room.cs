using MessagePack;

namespace SquirrelayServer.Common
{
    [MessagePackObject]
    public class RoomInfo
    {
        [Key(0)]
        public bool IsVisible { get; set; }

        [Key(1)]
        public int MaxNumberOfPlayers { get; set; }

        [Key(2)]
        public int NumberOfPlayers { get; set; }

        [Key(3)]
        public string Message { get; set; }

        [Key(4)]
        public bool IsPlaying { get; set; }
    }

    public enum RoomOperateKind
    {
        StartPlaying,
        FinishPlaying,
    }
}
