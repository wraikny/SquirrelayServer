using System.Threading.Tasks;

using SquirrelayServer.Common;

using Xunit;

namespace SquirrelayServer.Tests
{
    public class Serialization
    {
        [Fact]
        public async Task SerializeServerConfig()
        {
            _ = await Config.LoadFromFileAsync(@"config/config.json");
        }
    }
}
