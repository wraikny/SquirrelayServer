using MessagePack;

namespace SquirrelayServer.Common
{
    public class Options
    {
        public static readonly MessagePackSerializerOptions DefaultOptions =
            MessagePackSerializerOptions.Standard
                .WithSecurity(MessagePackSecurity.UntrustedData)
                .WithCompression(MessagePackCompression.Lz4BlockArray);
    }
}
