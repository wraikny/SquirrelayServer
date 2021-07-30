using MessagePack;

namespace SquirrelayServer.Common
{
    /// <summary>
    /// Message sent by the client
    ///</summary>
    [Union(0, typeof(SetPlayerStatus))]
    [Union(1, typeof(GetRoomList))]
    [Union(2, typeof(CreateRoom))]
    [Union(3, typeof(EnterRoom))]
    [Union(4, typeof(ExitRoom))]
    [Union(5, typeof(OperateRoom))]
    public interface IClientMsg
    {
        [MessagePackObject]
        public sealed class SetPlayerStatus : IClientMsg
        {
            [Key(0)]
            public RoomPlayerStatus Status { get; private set; }

            [SerializationConstructor]
            public SetPlayerStatus(RoomPlayerStatus status)
            {
                Status = status;
            }
        }

        [MessagePackObject]
        public sealed class GetRoomList : IClientMsg, IWithResponse<IServerMsg.RoomListResponse>
        {
            public GetRoomList() { }
        }

        [MessagePackObject]
        public sealed class CreateRoom : IClientMsg, IWithResponse<IServerMsg.CreateRoomResponse>
        {
            [Key(0)]
            public bool IsVisible { get; private set; }

            [Key(1)]
            public string Password { get; private set; }

            [Key(2)]
            public int MaxNumberOfPlayers { get; private set; }

            [Key(3)]
            public string Message { get; private set; }

            [SerializationConstructor]
            public CreateRoom(bool isVisible, string password, int maxNumberOfPlayers, string message)
            {
                IsVisible = isVisible;
                Password = password;
                MaxNumberOfPlayers = maxNumberOfPlayers;
                Message = message;
            }
        }

        [MessagePackObject]
        public sealed class EnterRoom : IClientMsg, IWithResponse<IServerMsg.EnterRoomResponse>
        {
            [Key(0)]
            public int RoomId { get; private set; }

            [Key(1)]
            public string Password { get; private set; }

            [SerializationConstructor]
            public EnterRoom(int roomId, string password)
            {
                RoomId = roomId;
                Password = password;
            }
        }

        [MessagePackObject]
        public sealed class ExitRoom : IClientMsg, IWithResponse<IServerMsg.ExitRoomResponse>
        {
            public static readonly ExitRoom Instance = new ExitRoom();
        }

        [MessagePackObject]
        public sealed class OperateRoom : IClientMsg
        {
            [Key(0)]
            public RoomOperateKind Operate { get; private set; }

            [SerializationConstructor]
            public OperateRoom(RoomOperateKind operate)
            {
                Operate = operate;
            }

            public static readonly OperateRoom StartPlaying = new OperateRoom(RoomOperateKind.StartPlaying);
            public static readonly OperateRoom FinishPlaying = new OperateRoom(RoomOperateKind.FinishPlaying);
        }
    }
}
