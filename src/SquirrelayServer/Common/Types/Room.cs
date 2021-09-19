using MessagePack;

namespace SquirrelayServer.Common
{
    public enum RoomStatus
    {
        WaitingToPlay,
        Playing,
        OwnerExited,
    }

    /// <summary>
    /// Information about the room
    /// </summary>
    [MessagePackObject]
    public class RoomInfo
    {
        [Key(0)]
        public int Id { get; set; }
        
        [Key(1)]
        public bool IsVisible { get; set; }

        [Key(2)]
        public int MaxNumberOfPlayers { get; set; }

        [Key(3)]
        public int NumberOfPlayers { get; set; }

        [Key(4)]
        public byte[] Message { get; set; }

        [Key(5)]
        public bool IsPlaying { get; set; }
    }

    public class RoomInfo<T>
    {
        public bool Id { get; set; }
        public bool IsVisible { get; set; }
        public int MaxNumberOfPlayers { get; set; }

        public int NumberOfPlayers { get; set; }

        public T Message { get; set; }

        public bool IsPlaying { get; set; }

        public RoomInfo(RoomInfo roomInfo, T message)
        {
            Id = Id;
            IsVisible = roomInfo.IsVisible;
            MaxNumberOfPlayers = roomInfo.MaxNumberOfPlayers;
            NumberOfPlayers = roomInfo.NumberOfPlayers;
            IsPlaying = roomInfo.IsPlaying;
            Message = message;
        }
    }

    public enum RoomOperateKind
    {
        StartPlaying,
        FinishPlaying,
    }
}
