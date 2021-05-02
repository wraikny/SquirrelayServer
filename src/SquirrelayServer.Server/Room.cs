using System;
using System.Collections.Generic;
using System.Text;

using SquirrelayServer.Common;

namespace SquirrelayServer.Server
{
    internal class Room
    {
        private readonly Dictionary<ulong, ClientHandler> _clients;

        public int Id { get; private set; }
        public RoomInfo Info { get; private set; }
        public string Password { get; private set; }

        public Room(int id, RoomInfo info, string password)
        {
            _clients = new Dictionary<ulong, ClientHandler>();

            Id = id;
            Info = info;
            Password = password;
        }

        public IServerMsg.EnterRoomResponse EnterRoom(ClientHandler client, string password)
        {
            if (Password is string p && p != password) return IServerMsg.EnterRoomResponse.InvalidPassword;
            if (Info.MaxNumberOfPlayers == _clients.Count) return IServerMsg.EnterRoomResponse.NumberOfPlayersLimitation;
            if (_clients.ContainsKey(client.Id)) return IServerMsg.EnterRoomResponse.AlreadEntered;

            _clients.Add(client.Id, client);
            client.EnterRoom(Id);

            return IServerMsg.EnterRoomResponse.Success;
        }
    }
}
