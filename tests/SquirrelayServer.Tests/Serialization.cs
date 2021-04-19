using System;

using SquirrelayServer;

using Xunit;

namespace SquirrelayServer.Tests
{
    public class Serialization
    {
        [Fact]
        public void SerializeServerConfig()
        {
            var _config = Server.Config.LoadAsync(@"netconfig/config.json");
        }
    }
}
