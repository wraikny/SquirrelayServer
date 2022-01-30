using MessagePack;

namespace SquirrelayServer.Altseed2
{
    [MessagePackObject]
    public class Message
    {
        [Key(0)]
        public uint Index { get; private set; }

        [Key(1)]
        public string? CustomMessage { get; private set; }


        [SerializationConstructor]
        public Message(uint index, string customMessage)
        {
            Index = index;
            CustomMessage = customMessage;
        }
    }
}
