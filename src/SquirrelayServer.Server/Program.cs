using System;
using System.Threading;
using System.Threading.Tasks;

using LiteNetLib;

using SquirrelayServer.Common;

namespace SquirrelayServer.Server
{
    public sealed class Logger : INetLogger
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
            NetDebug.Logger = new Logger();

            var configPath = args.Length == 0 ? Config.DefaultPath : args[1];

            var config = await Config.LoadFromFileAsync(configPath);

            var options = Options.DefaultOptions;

            var context = new Context();
            SynchronizationContext.SetSynchronizationContext(context);

            var server = new Server(config, options);
            _ = server.Start();

            while (true)
            {
                context.Update();
            }
        }
    }
}
