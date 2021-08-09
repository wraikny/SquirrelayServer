using System;
using System.IO;
using System.Threading.Tasks;

using LiteNetLib;

using MessagePack;

using SquirrelayServer.Common;

namespace SquirrelayServer.App
{
    internal sealed class Logger : INetLogger
    {
        private readonly StreamWriter _stream;

        public Logger(StreamWriter stream)
        {
            _stream = stream;
        }

        void INetLogger.WriteNet(NetLogLevel level, string str, params object[] args)
        {
            var msg = args.Length == 0 ? str : string.Format(str, args);
            var current = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            var output = $"[{current}] {msg}";

            //_stream.WriteLine(output);
            //_stream.Flush();

            Console.WriteLine(output);
        }
    }

    public class Program
    {
        public static async Task Main(string[] _)
        {
            using var fileStream = File.OpenWrite("log.txt");
            using var streamWriter = new StreamWriter(fileStream);

            // Set Logger
            NetDebug.Logger = new Logger(streamWriter);

            // Load config
            var config = await Config.LoadFromFileAsync(@"config/config.json");
            NetDebug.Logger?.WriteNet(NetLogLevel.Info, "Config is loaded.");

            // Start server
            var server = new Server.Server(config, Options.DefaultOptions);
            await server.Start();
        }
    }
}
