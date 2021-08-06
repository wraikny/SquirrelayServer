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
    [Union(8, typeof(SetRoomMessageResponse))]
    [Union(9, typeof(SendGameMessageResponse))]
    [Union(10, typeof(UpdateRoomPlayersAndMessage))]
    [Union(11, typeof(BroadcastGameMessages))]
    [Union(12, typeof(NotifyRoomOperation))]
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

            internal static CreateRoomResponse Success(int id) => new CreateRoomResponse(ResultKind.Success, id);
            internal static readonly CreateRoomResponse AlreadyEntered = new CreateRoomResponse(ResultKind.AlreadyEntered, 0);
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

            [Key(3)]
            public byte[] RoomStatus { get; private set; }

            [IgnoreMember]
            public bool IsSuccess => Result == ResultKind.Success;


            [SerializationConstructor]
            public EnterRoomResponse(ResultKind result, ulong? ownerId, IReadOnlyDictionary<ulong, RoomPlayerStatus> statuses, byte[] roomStatus)
            {
                Result = result;
                OwnerId = ownerId;
                Statuses = statuses;
                RoomStatus = roomStatus;
            }

            public EnterRoomResponse(ResultKind result)
            {
                Result = result;
                OwnerId = null;
                Statuses = null;
                RoomStatus = null;
            }

            internal static EnterRoomResponse Success(ulong ownerId, IReadOnlyDictionary<ulong, RoomPlayerStatus> statuses, byte[] roomStatus)
                => new EnterRoomResponse(ResultKind.Success, ownerId, statuses, roomStatus);
            internal static readonly EnterRoomResponse RoomNotFound = new EnterRoomResponse(ResultKind.RoomNotFound);
            internal static readonly EnterRoomResponse InvalidPassword = new EnterRoomResponse(ResultKind.InvalidPassword);
            internal static readonly EnterRoomResponse NumberOfPlayersLimitation = new EnterRoomResponse(ResultKind.NumberOfPlayersLimitation);
            internal static readonly EnterRoomResponse AlreadyEntered = new EnterRoomResponse(ResultKind.AlreadyEntered);
            internal static readonly EnterRoomResponse InvalidRoomStatus = new EnterRoomResponse(ResultKind.InvalidRoomStatus);
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

            internal static readonly ExitRoomResponse Success = new ExitRoomResponse(ResultKind.Success);
            internal static readonly ExitRoomResponse PlayerOutOfRoom = new ExitRoomResponse(ResultKind.PlayerOutOfRoom);
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

            internal static readonly OperateRoomResponse Success = new OperateRoomResponse(ResultKind.Success);
            internal static readonly OperateRoomResponse PlayerIsNotOwner = new OperateRoomResponse(ResultKind.PlayerIsNotOwner);
            internal static readonly OperateRoomResponse PlayerOutOfRoom = new OperateRoomResponse(ResultKind.PlayerOutOfRoom);
            internal static readonly OperateRoomResponse InvalidRoomStatus = new OperateRoomResponse(ResultKind.InvalidRoomStatus);

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

            internal static readonly SetPlayerStatusResponse Success = new SetPlayerStatusResponse(ResultKind.Success);
            internal static readonly SetPlayerStatusResponse PlayerOutOfRoom = new SetPlayerStatusResponse(ResultKind.PlayerOutOfRoom);
        }

        [MessagePackObject]
        public sealed class SetRoomMessageResponse : IServerMsg, IResponse
        {
            public enum ResultKind
            {
                Success = 0,
                PlayerOutOfRoom = 1,
                PlayerIsNotOwner = 2,
            }

            [Key(0)]
            public ResultKind Result { get; private set; }

            [IgnoreMember]
            public bool IsSuccess => Result == ResultKind.Success;

            [SerializationConstructor]
            public SetRoomMessageResponse(ResultKind result)
            {
                Result = result;
            }

            internal static readonly SetRoomMessageResponse Success = new SetRoomMessageResponse(ResultKind.Success);
            internal static readonly SetRoomMessageResponse PlayerOutOfRoom = new SetRoomMessageResponse(ResultKind.PlayerOutOfRoom);
            internal static readonly SetRoomMessageResponse PlayerIsNotOwner = new SetRoomMessageResponse(ResultKind.PlayerIsNotOwner);
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

            internal static readonly SendGameMessageResponse Success = new SendGameMessageResponse(ResultKind.Success);
            internal static readonly SendGameMessageResponse PlayerOutOfRoom = new SendGameMessageResponse(ResultKind.PlayerOutOfRoom);
            internal static readonly SendGameMessageResponse InvalidRoomStatus = new SendGameMessageResponse(ResultKind.InvalidRoomStatus);
        }

        [MessagePackObject]
        public sealed class UpdateRoomPlayersAndMessage : IServerMsg
        {
            [Key(0)]
            public ulong? Owner { get; private set; }

            [Key(1)]
            public IReadOnlyDictionary<ulong, RoomPlayerStatus> Statuses { get; private set; }

            [Key(2)]
            public byte[] RoomStatus { get; private set; }

            [SerializationConstructor]
            public UpdateRoomPlayersAndMessage(ulong? owner, IReadOnlyDictionary<ulong, RoomPlayerStatus> statuses, byte[] roomStatus)
            {
                Owner = owner;
                Statuses = statuses;
                RoomStatus = roomStatus;
            }
        }

        [MessagePackObject]
        public sealed class BroadcastGameMessages : IServerMsg
        {
            [Key(0)]
            public IReadOnlyList<RelayedGameMessage> Messages { get; private set; }

            [SerializationConstructor]
            public BroadcastGameMessages(IReadOnlyList<RelayedGameMessage> messages)
            {
                Messages = messages;
            }
        }

        [MessagePackObject]
        public sealed class NotifyRoomOperation : IServerMsg
        {
            [Key(0)]
            public RoomOperateKind Operate { get; private set; }

            public NotifyRoomOperation(RoomOperateKind operate)
            {
                Operate = operate;
            }
        }
    }
}
