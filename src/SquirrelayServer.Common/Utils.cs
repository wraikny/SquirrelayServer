using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace SquirrelayServer.Common
{
    internal class Utils
    {
        public static T DeserializeJson<T>(string json, DataContractJsonSerializerSettings settings = null)
        {
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var serializer = new DataContractJsonSerializer(typeof(T), settings);
            return (T)serializer.ReadObject(ms);
        }

        public static async ValueTask<T> DeserializeJsonFromFileAsync<T>(string path, DataContractJsonSerializerSettings settings = null)
        {
            var content = await File.ReadAllTextAsync(path);
            return DeserializeJson<T>(content, settings);
        }
    }
}
