using MessagePack;


namespace SquirrelayServer.Common
{
    [Union(0, typeof(ClientId))]
    [Union(1, typeof(RoomList))]
    public interface IServerMsg
    {
        [MessagePackObject]
        public sealed class ClientId
        {
            [Key(0)]
            public ulong Id { get; private set; }

            [SerializationConstructor]
            public ClientId(ulong id)
            {
                Id = id;
            }
        }

        [MessagePackObject]
        public sealed class RoomList
        {
            [SerializationConstructor]
            public RoomList()
            {

            }
        }
    }
}
