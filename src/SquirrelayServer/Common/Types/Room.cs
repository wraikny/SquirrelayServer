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
        public byte[]? Message { get; set; }

        [Key(5)]
        public bool IsPlaying { get; set; }

        [Key(6)]
        public string? ClientVersion { get; set; }
    }

    public class RoomInfo<T>
    {
        public int Id { get; private set; }
        public bool IsVisible { get; private set; }
        public int MaxNumberOfPlayers { get; private set; }

        public int NumberOfPlayers { get; private set; }

        public T? Message { get; private set; }

        public bool IsPlaying { get; private set; }

        public string ClientVersion { get; private set; }

        public RoomInfo(RoomInfo roomInfo, T? message)
        {
            if (!(roomInfo.ClientVersion is string clientVersion))
            {
                throw new System.ArgumentException("roomInfo.ClientVersion is null");
            }

            ClientVersion = clientVersion;

            Id = roomInfo.Id;
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
