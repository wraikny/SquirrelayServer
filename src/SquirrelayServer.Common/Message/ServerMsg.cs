using MessagePack;


namespace SquirrelayServer.Common
{
    [Union(0, typeof(ClientId))]
    [Union(1, typeof(RoomList))]
    [Union(2, typeof(RoomCreated))]
    [Union(3, typeof(EnterRoomResponse))]
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
        public sealed class RoomList : IServerMsg
        {
            [SerializationConstructor]
            public RoomList()
            {

            }
        }

        [MessagePackObject]
        public sealed class RoomCreated : SimpleMsg<int>
        {
            [SerializationConstructor]
            public RoomCreated(int id) : base(id) { }
        }

        [MessagePackObject]
        public sealed class EnterRoomResponse : IServerMsg
        {
            public enum ErrorKind
            {
                RoomNotFound,
                InvalidPassword,
                NumberOfPlayersLimitation,
                AlreadyEntered,
            }

            [Key(0)]
            public ErrorKind? Error { get; set; }

            [SerializationConstructor]
            public EnterRoomResponse(ErrorKind? error)
            {
                Error = error;
            }

            public static EnterRoomResponse RoomNotFound => new EnterRoomResponse(ErrorKind.RoomNotFound);
            public static EnterRoomResponse InvalidPassword => new EnterRoomResponse(ErrorKind.InvalidPassword);
            public static EnterRoomResponse NumberOfPlayersLimitation => new EnterRoomResponse(ErrorKind.NumberOfPlayersLimitation);
            public static EnterRoomResponse AlreadEntered => new EnterRoomResponse(ErrorKind.AlreadyEntered);
            public static EnterRoomResponse Success => new EnterRoomResponse(null);
        }
    }
}
