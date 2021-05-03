using MessagePack;


namespace SquirrelayServer.Common
{
    [Union(0, typeof(ClientId))]
    [Union(1, typeof(RoomListResponse))]
    [Union(2, typeof(CreateRoomResponse))]
    [Union(3, typeof(EnterRoomResponse))]
    [Union(4, typeof(ExitRoomResponse))]
    public interface IServerMsg
    {
        [MessagePackObject]
        public class SimpleMsg<T> : IServerMsg
        {
            [Key(0)]
            public T Value { get; private set; }

            [SerializationConstructor]
            public SimpleMsg(T value)
            {
                Value = value;
            }
        }

        [MessagePackObject]
        public sealed class ClientId : SimpleMsg<ulong>
        {

            [SerializationConstructor]
            public ClientId(ulong id) : base(id) { }
        }

        [MessagePackObject]
        public sealed class RoomListResponse : IServerMsg
        {
            [SerializationConstructor]
            public RoomListResponse()
            {

            }
        }

        [MessagePackObject]
        public sealed class CreateRoomResponse : IServerMsg
        {
            public enum ErrorKind
            {
                AlreadyEntered = 0,
            }

            [Key(0)]
            public ErrorKind? Error { get; set; }

            [Key(1)]
            public int Id { get; set; }

            [IgnoreMember]
            public bool IsSuccess => Error is null;

            [SerializationConstructor]
            public CreateRoomResponse(ErrorKind? error, int id)
            {
                Error = error;
                Id = id;
            }

            public static readonly CreateRoomResponse AlreadyEntered = new CreateRoomResponse(ErrorKind.AlreadyEntered, 0);
            public static CreateRoomResponse Success(int id) => new CreateRoomResponse(null, id);
        }

        [MessagePackObject]
        public sealed class EnterRoomResponse : IServerMsg
        {
            public enum ErrorKind
            {
                RoomNotFound = 0,
                InvalidPassword = 1,
                NumberOfPlayersLimitation = 2,
                AlreadyEntered = 3,
            }

            [Key(0)]
            public ErrorKind? Error { get; set; }

            [IgnoreMember]
            public bool IsSuccess => Error is null;

            [SerializationConstructor]
            public EnterRoomResponse(ErrorKind? error)
            {
                Error = error;
            }

            public static readonly EnterRoomResponse RoomNotFound = new EnterRoomResponse(ErrorKind.RoomNotFound);
            public static readonly EnterRoomResponse InvalidPassword = new EnterRoomResponse(ErrorKind.InvalidPassword);
            public static readonly EnterRoomResponse NumberOfPlayersLimitation = new EnterRoomResponse(ErrorKind.NumberOfPlayersLimitation);
            public static readonly EnterRoomResponse AlreadyEntered = new EnterRoomResponse(ErrorKind.AlreadyEntered);
            public static readonly EnterRoomResponse Success = new EnterRoomResponse(null);
        }

        [MessagePackObject]
        public sealed class ExitRoomResponse : IServerMsg
        {

            public enum ErrorKind
            {
                PlayerOutOfRoom,
            }

            [Key(0)]
            public ErrorKind? Error { get; set; }

            [SerializationConstructor]
            public ExitRoomResponse(ErrorKind? error)
            {
                Error = error;
            }

            public static readonly ExitRoomResponse PlayerOutOfRoom = new ExitRoomResponse(ErrorKind.PlayerOutOfRoom);
            public static readonly ExitRoomResponse Success = new ExitRoomResponse(null);
        }
    }
}
