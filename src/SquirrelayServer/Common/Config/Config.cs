using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace SquirrelayServer.Common
{
    [DataContract]
    public sealed class Config
    {
        [DataMember(Name = "netConfig")]
        public NetConfig NetConfig { get; private set; }

        [DataMember(Name = "roomConfig")]
        public RoomConfig RoomConfig { get; private set; }

        [DataMember(Name = "serverLoggingConfig")]
        public ServerLoggingConfig ServerLoggingConfig { get; private set; }

        //[DataMember(Name = "gameConfig")]
        //public GameConfig GameConfig { get; private set; }

        public static Config? LoadFromJson(string json)
        {
            var settings = new DataContractJsonSerializerSettings();

            return Utils.DeserializeJson<Config>(json, settings);
        }

        public static ValueTask<Config?> LoadFromFileAsync(string path)
        {
            var settings = new DataContractJsonSerializerSettings();

            return Utils.DeserializeJsonFromFileAsync<Config>(path, settings);
        }

        public static Config? LoadFromFile(string path)
        {
            var settings = new DataContractJsonSerializerSettings();

            return Utils.DeserializeJsonFromFile<Config>(path, settings);
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext c)
        {

        }

        public Config(NetConfig netConfig, RoomConfig roomConfig, ServerLoggingConfig serverLoggingConfig)
        {
            NetConfig = netConfig;
            RoomConfig = roomConfig;
            ServerLoggingConfig = serverLoggingConfig;
        }
    }
}
