using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace SquirrelayServer.Common
{
    [DataContract]
    public sealed class Config
    {
        public const string DefaultPath = @"config/config.json";

        [DataMember(Name = "netConfig")]
        public NetConfig NetConfig { get; private set; }

        [DataMember(Name = "gameConfig")]
        public GameConfig GameConfig { get; private set; }

        public static Config LoadFromJson(string json)
        {
            var settings = new DataContractJsonSerializerSettings();

            return Utils.DeserializeJson<Config>(json, settings);
        }

        public static ValueTask<Config> LoadFromFileAsync(string path)
        {
            var settings = new DataContractJsonSerializerSettings();

            return Utils.DeserializeJsonFromFileAsync<Config>(path, settings);
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext c)
        {

        }
    }
}
