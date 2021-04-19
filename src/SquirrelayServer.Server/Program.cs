using System;
using System.Linq;
using System.Threading.Tasks;

using MessagePack;
using MessagePack.Resolvers;

namespace SquirrelayServer.Server
{
    public class Program
    {
        private const string DefaultConfigPath = @"netconfig/config.json";

        private static readonly MessagePackSerializerOptions options =
            MessagePackSerializerOptions.Standard
                .WithSecurity(MessagePackSecurity.UntrustedData)
                .WithCompression(MessagePackCompression.Lz4BlockArray);

        public static async ValueTask Main(string[] args)
        {
            var configPath = args.Length == 0 ? DefaultConfigPath : args[1];

            var config = await Config.LoadAsync(configPath);


            var server = new Server(config, options);
            server.Start();
        }
    }
}
