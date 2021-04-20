using MessagePack;

namespace SquirrelayServer.Common
{
    [Union(0, typeof(GetRoomList))]
    [Union(1, typeof(SetPlayerInfo))]
    public interface IClientMsg
    {
        [MessagePackObject]
        public sealed class SetPlayerInfo : IClientMsg
        {
            [SerializationConstructor]
            public SetPlayerInfo()
            {

            }
        }

        [MessagePackObject]
        public sealed class GetRoomList : IClientMsg, IWithResponse<IServerMsg.RoomList>
        {
            public GetRoomList() { }
        }
    }
}
