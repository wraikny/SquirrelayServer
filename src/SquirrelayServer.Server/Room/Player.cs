using System;
using System.Collections.Generic;
using System.Text;

namespace SquirrelayServer.Server
{
    internal interface IPlayer
    {
        public ulong Id { get; }
        public int? RoomId { get; set; }
    }
}
