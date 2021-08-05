using System.Collections.Generic;

using MessagePack;


namespace SquirrelayServer.Common
{
    /// <summary>
    /// Message sent by the server
    ///</summary>
    [Union(0, typeof(Hello))]
    [Union(1, typeof(ClientsCountResponse))]
    [Union(2, typeof(RoomListResponse))]
    [Union(3, typeof(CreateRoomResponse))]
    [Union(4, typeof(EnterRoomResponse))]
    [Union(5, typeof(ExitRoomResponse))]
    [Union(6, typeof(OperateRoomResponse))]
    [Union(7, typeof(SetPlayerStatusResponse))]
    [Union(8, typeof(SendGameMessageResponse))]
    [Union(9, typeof(UpdateRoomPlayers))]
    [Union(10, typeof(DistributeGameMessage))]
    public interface IServerMsg
    {
        [MessagePackObject]
        public sealed class Hello : IServerMsg
        {
            [Key(0)]
            public ulong Id { get; private set; }

            [Key(1)]
            public RoomConfig RoomConfig { get; private set; }

            [SerializationConstructor]
            public Hello(ulong id, RoomConfig roomConfig)
            {
                Id = id;
                RoomConfig = roomConfig;
            }
        }

        [MessagePackObject]
        public sealed class ClientsCountResponse : IServerMsg, IResponse
        {
            [Key(0)]
            public int Count { get; private set; }

            [SerializationConstructor]
            public ClientsCountResponse(int count)
            {
                Count = count;
            }
        }

        [MessagePackObject]
        public sealed class RoomListResponse : IServerMsg, IResponse
        {
            [Key(0)]
            public IReadOnlyCollection<RoomInfo> Info { get; private set; }

            [SerializationConstructor]
            public RoomListResponse(IReadOnlyCollection<RoomInfo> info)
            {
                Info = info;
            }
        }

        [MessagePackObject]
        public sealed class CreateRoomResponse : IServerMsg, IResponse
        {
            public enum ResultKind
            {
                Success = 0,
                AlreadyEntered = 1,
            }

            [Key(0)]
            public ResultKind Result { get; private set; }

            [Key(1)]
            public int Id { get; private set; }

            [IgnoreMember]
            public bool IsSuccess => Result == ResultKind.Success;

            [SerializationConstructor]
            public CreateRoomResponse(ResultKind kind, int id)
            {
                Result = kind;
                Id = id;
            }

            public static CreateRoomResponse Success(int id) => new CreateRoomResponse(ResultKind.Success, id);
            public static readonly CreateRoomResponse AlreadyEntered = new CreateRoomResponse(ResultKind.AlreadyEntered, 0);
        }

        [MessagePackObject]
        public sealed class EnterRoomResponse : IServerMsg, IResponse
        {
            public enum ResultKind
            {
                Success = 0,
                RoomNotFound = 1,
                InvalidPassword = 2,
                NumberOfPlayersLimitation = 3,
                AlreadyEntered = 4,
                InvalidRoomStatus = 5,
            }

            [Key(0)]
            public ResultKind Result { get; private set; }

            [Key(1)]
            public ulong? OwnerId { get; private set; }

            [Key(2)]
            public IReadOnlyDictionary<ulong, RoomPlayerStatus> Statuses { get; private set; }

            [IgnoreMember]
            public bool IsSuccess => Result == ResultKind.Success;


            [SerializationConstructor]
            public EnterRoomResponse(ResultKind result, ulong? ownerId, IReadOnlyDictionary<ulong, RoomPlayerStatus> statuses)
            {
                Result = result;
                OwnerId = ownerId;
                Statuses = statuses;
            }

            public EnterRoomResponse(ResultKind result)
            {
                Result = result;
                OwnerId = null;
                Statuses = null;
            }

            public static EnterRoomResponse Success(ulong ownerId, IReadOnlyDictionary<ulong, RoomPlayerStatus> statuses)
                => new EnterRoomResponse(ResultKind.Success, ownerId, statuses);
            public static readonly EnterRoomResponse RoomNotFound = new EnterRoomResponse(ResultKind.RoomNotFound);
            public static readonly EnterRoomResponse InvalidPassword = new EnterRoomResponse(ResultKind.InvalidPassword);
            public static readonly EnterRoomResponse NumberOfPlayersLimitation = new EnterRoomResponse(ResultKind.NumberOfPlayersLimitation);
            public static readonly EnterRoomResponse AlreadyEntered = new EnterRoomResponse(ResultKind.AlreadyEntered);
            public static readonly EnterRoomResponse InvalidRoomStatus = new EnterRoomResponse(ResultKind.InvalidRoomStatus);
        }

        [MessagePackObject]
        public sealed class ExitRoomResponse : IServerMsg, IResponse
        {

            public enum ResultKind
            {
                Success = 0,
                PlayerOutOfRoom = 1,
            }

            [Key(0)]
            public ResultKind Result { get; private set; }

            [IgnoreMember]
            public bool IsSuccess => Result == ResultKind.Success;

            [SerializationConstructor]
            public ExitRoomResponse(ResultKind result)
            {
                Result = result;
            }

            public static readonly ExitRoomResponse Success = new ExitRoomResponse(ResultKind.Success);
            public static readonly ExitRoomResponse PlayerOutOfRoom = new ExitRoomResponse(ResultKind.PlayerOutOfRoom);
        }

        [MessagePackObject]
        public sealed class OperateRoomResponse : IServerMsg, IResponse
        {
            public enum ResultKind
            {
                Success = 0,
                PlayerOutOfRoom = 1,
                PlayerIsNotOwner = 2,
                InvalidRoomStatus = 3,
            }

            [Key(0)]
            public ResultKind Result { get; private set; }

            [IgnoreMember]
            public bool IsSuccess => Result == ResultKind.Success;

            [SerializationConstructor]
            public OperateRoomResponse(ResultKind result)
            {
                Result = result;
            }

            public static readonly OperateRoomResponse Success = new OperateRoomResponse(ResultKind.Success);
            public static readonly OperateRoomResponse PlayerIsNotOwner = new OperateRoomResponse(ResultKind.PlayerIsNotOwner);
            public static readonly OperateRoomResponse PlayerOutOfRoom = new OperateRoomResponse(ResultKind.PlayerOutOfRoom);
            public static readonly OperateRoomResponse InvalidRoomStatus = new OperateRoomResponse(ResultKind.InvalidRoomStatus);

        }

        [MessagePackObject]
        public sealed class SetPlayerStatusResponse : IServerMsg, IResponse
        {
            public enum ResultKind
            {
                Success = 0,
                PlayerOutOfRoom = 1,
            }

            [Key(0)]
            public ResultKind Result { get; private set; }

            [IgnoreMember]
            public bool IsSuccess => Result == ResultKind.Success;

            [SerializationConstructor]
            public SetPlayerStatusResponse(ResultKind result)
            {
                Result = result;
            }

            public static readonly SetPlayerStatusResponse Success = new SetPlayerStatusResponse(ResultKind.Success);
            public static readonly SetPlayerStatusResponse PlayerOutOfRoom = new SetPlayerStatusResponse(ResultKind.PlayerOutOfRoom);
        }

        [MessagePackObject]
        public sealed class SendGameMessageResponse : IServerMsg, IResponse
        {
            public enum ResultKind
            {
                Success = 0,
                PlayerOutOfRoom = 1,
                InvalidRoomStatus = 2,
            }

            [Key(0)]
            public ResultKind Result { get; private set; }

            [IgnoreMember]
            public bool IsSuccess => Result == ResultKind.Success;

            [SerializationConstructor]
            public SendGameMessageResponse(ResultKind result)
            {
                Result = result;
            }

            public static readonly SendGameMessageResponse Success = new SendGameMessageResponse(ResultKind.Success);
            public static readonly SendGameMessageResponse PlayerOutOfRoom = new SendGameMessageResponse(ResultKind.PlayerOutOfRoom);
            public static readonly SendGameMessageResponse InvalidRoomStatus = new SendGameMessageResponse(ResultKind.InvalidRoomStatus);
        }

        [MessagePackObject]
        public sealed class UpdateRoomPlayers : IServerMsg
        {
            [Key(0)]
            public ulong? Owner { get; private set; }

            [Key(1)]
            public IReadOnlyDictionary<ulong, RoomPlayerStatus> Statuses { get; private set; }

            [SerializationConstructor]
            public UpdateRoomPlayers(ulong? owner, IReadOnlyDictionary<ulong, RoomPlayerStatus> statuses)
            {
                Owner = owner;
                Statuses = statuses;
            }
        }

        [MessagePackObject]
        public sealed class DistributeGameMessage : IServerMsg
        {
            [Key(0)]
            public IReadOnlyList<RelayedGameMessage> Messages { get; private set; }

            [SerializationConstructor]
            public DistributeGameMessage(IReadOnlyList<RelayedGameMessage> messages)
            {
                Messages = messages;
            }
        }
    }
}
