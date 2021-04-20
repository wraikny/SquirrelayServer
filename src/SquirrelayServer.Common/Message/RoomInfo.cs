using System;
using System.Collections.Generic;
using System.Text;

using MessagePack;

namespace SquirrelayServer.Common
{
    [MessagePackObject]
    public sealed class RoomInfo
    {
        [Key(0)]
        public string RoomId { get; set; }

        [Key(1)]
        public bool IsVisible { get; set; }

        [Key(2)]
        public int MaxNumberOfPlayers { get; set; }
    }
}
