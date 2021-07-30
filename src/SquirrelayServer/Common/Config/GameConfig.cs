using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

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
