using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace SquirrelayServer.Server
{
    [DataContract]
    public sealed class GameConfig
    {
    }

    [DataContract]
    public sealed class Config
    {
        [DataMember(Name = "port")]
        public int Port { get; private set; }

        [DataMember(Name = "mexClientCount")]
        public int MaxClientCount { get; private set; }

        [DataMember(Name = "games")]
        private readonly Dictionary<string, GameConfig> _games;

        [IgnoreDataMember]
        public IReadOnlyDictionary<string, GameConfig> Games => _games;


        internal static ValueTask<Config> LoadAsync(string path)
        {
            var settings = new DataContractJsonSerializerSettings
            {
                UseSimpleDictionaryFormat = true
            };
            return Common.Utils.DeserializeJsonFromFileAsync<Config>(path, settings);
        }
    }
}
