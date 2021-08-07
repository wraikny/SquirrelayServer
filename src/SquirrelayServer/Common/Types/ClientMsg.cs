using MessagePack;

namespace SquirrelayServer.Common
{
    /// <summary>
    /// Message sent by the client
    ///</summary>
    [Union(0, typeof(GetClientsCount))]
    [Union(1, typeof(GetRoomList))]
    [Union(2, typeof(CreateRoom))]
    [Union(3, typeof(EnterRoom))]
    [Union(4, typeof(ExitRoom))]
    [Union(5, typeof(OperateRoom))]
    [Union(6, typeof(SetPlayerStatus))]
    [Union(7, typeof(SetRoomMessage))]
    [Union(8, typeof(SendGameMessage))]
    public interface IClientMsg
    {
        [MessagePackObject]
        public sealed class GetClientsCount : IClientMsg, IWithResponse<IServerMsg.ClientsCountResponse>
        {
            public GetClientsCount() { }

            public static readonly GetClientsCount Instance = new GetClientsCount();
        }

        [MessagePackObject]
        public sealed class GetRoomList : IClientMsg, IWithResponse<IServerMsg.RoomListResponse>
        {
            public GetRoomList() { }

            public static readonly GetRoomList Instance = new GetRoomList();
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
            public byte[] PlayerStatus { get; private set; }

            [Key(4)]
            public byte[] RoomMessage { get; private set; }


            [SerializationConstructor]
            public CreateRoom(bool isVisible, string password, int maxNumberOfPlayers, byte[] playerStatus, byte[] roomMessage)
            {
                IsVisible = isVisible;
                Password = password;
                MaxNumberOfPlayers = maxNumberOfPlayers;
                PlayerStatus = playerStatus;
                RoomMessage = roomMessage;
            }
        }

        [MessagePackObject]
        public sealed class EnterRoom : IClientMsg, IWithResponse<IServerMsg.EnterRoomResponse>
        {
            [Key(0)]
            public int RoomId { get; private set; }

            [Key(1)]
            public string Password { get; private set; }

            [Key(2)]
            public byte[] Status { get; private set; }

            [SerializationConstructor]
            public EnterRoom(int roomId, string password, byte[] status)
            {
                RoomId = roomId;
                Password = password;
                Status = status;
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

        [MessagePackObject]
        public sealed class SetPlayerStatus : IClientMsg, IWithResponse<IServerMsg.SetPlayerStatusResponse>
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
        public sealed class SetRoomMessage : IClientMsg, IWithResponse<IServerMsg.SetRoomMessageResponse>
        {
            [Key(0)]
            public byte[] RoomMessage { get; private set; }

            [SerializationConstructor]
            public SetRoomMessage(byte[] roomMessage)
            {
                RoomMessage = roomMessage;
            }
        }

        [MessagePackObject]
        public sealed class SendGameMessage : IClientMsg, IWithResponse<IServerMsg.SendGameMessageResponse>
        {
            [Key(0)]
            public byte[] Data { get; private set; }

            [SerializationConstructor]
            public SendGameMessage(byte[] data)
            {
                Data = data;
            }
        }
    }
}
