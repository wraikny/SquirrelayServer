using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MessagePack;

namespace ClientExample
{
    [MessagePackObject]
    public sealed class Status
    {

    }

    [MessagePackObject]
    public sealed class GameMessage
    {
        [Key(0)]
        public string Message { get; set; }
    }
}
