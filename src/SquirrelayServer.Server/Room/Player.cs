using System;
using System.Collections.Generic;
using System.Text;

namespace SquirrelayServer.Server
{
    public interface IPlayer
    {
        public ulong Id { get; }
        public int? RoomId { get; set; }
    }
}
