using MessagePack;

namespace SquirrelayServer.Common
{
    [Union(0, typeof(SetPlayerInfo))]
    [Union(1, typeof(GetRoomList))]
    [Union(2, typeof(CreateRoom))]
    [Union(3, typeof(EnterRoom))]
    [Union(4, typeof(ExitRoom))]
    public interface IClientMsg
    {
        [MessagePackObject]
        public sealed class SetPlayerInfo : IClientMsg
        {
            [SerializationConstructor]
            public SetPlayerInfo()
            {

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
            [SerializationConstructor]
            public ExitRoom()
            {

            }
        }
    }
}
