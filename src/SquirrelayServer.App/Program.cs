using System;
using System.Threading.Tasks;

using LiteNetLib;

using MessagePack;

using SquirrelayServer.Common;

namespace SquirrelayServer.App
{
    internal sealed class Logger : INetLogger
    {
        public Logger()
        {

        }

        void INetLogger.WriteNet(NetLogLevel level, string str, params object[] args)
        {
            var msg = args.Length == 0 ? str : string.Format(str, args);
            Console.WriteLine($"[Log] {msg}");
        }
    }

    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Set Logger
            NetDebug.Logger = new Logger();

            // Load config
            var configPath = args.Length == 0 ? @"config/config.json" : args[0];
            var config = await Config.LoadFromFileAsync(configPath);

            NetDebug.Logger.WriteNet(NetLogLevel.Info, "Config is loaded from '{0}'.", configPath);

            // Start server
            var server = new Server.Server(config, Options.DefaultOptions);
            await server.Start();
        }
    }
}
