using System;
using System.Linq;
using System.Threading.Tasks;

using MessagePack;
using MessagePack.Resolvers;

namespace SquirrelayServer.Server
{
    public class Program
    {
        private const string DefaultConfigPath = @"serverconfig.json";

        public static async Task Main(string[] args)
        {
            var configPath = args.Length == 0 ? Common.Config.DefaultPath : args[1];

            var config = await Common.Config.LoadFromFileAsync(configPath);

            var server = new Server(config, Options.DefaultOptions);
            server.Start();
        }
    }
}
