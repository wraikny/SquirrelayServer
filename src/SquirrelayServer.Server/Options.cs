using MessagePack;

namespace SquirrelayServer.Server
{
    public class Options
    {
        public static MessagePackSerializerOptions DefaultOptions =
            MessagePackSerializerOptions.Standard
                .WithSecurity(MessagePackSecurity.UntrustedData)
                .WithCompression(MessagePackCompression.Lz4BlockArray);
    }
}
