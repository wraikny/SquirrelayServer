using MessagePack;


namespace SquirrelayServer.Common
{
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
    }
}
