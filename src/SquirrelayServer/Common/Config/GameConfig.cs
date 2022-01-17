using System.Runtime.Serialization;

using MessagePack;

namespace SquirrelayServer.Common
{
    [DataContract]
    [MessagePackObject]
    public sealed class GameConfig
    {


        [OnDeserialized]
        private void OnDeserialized(StreamingContext c)
        {

        }


    }
}
