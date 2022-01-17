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
    [Union(4, typeof(EnterRoomResponse<byte[]>))]
    [Union(5, typeof(ExitRoomResponse))]
    [Union(6, typeof(OperateRoomResponse))]
    [Union(7, typeof(SetPlayerStatusResponse))]
    [Union(8, typeof(SetRoomMessageResponse))]
    [Union(9, typeof(SendGameMessageResponse))]
    [Union(10, typeof(UpdateRoomPlayers))]
    [Union(11, typeof(UpdateRoomMessage))]
    [Union(12, typeof(Tick))]
    [Union(13, typeof(BroadcastGameMessages))]
    [Union(14, typeof(NotifyRoomOperation))]
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

            [IgnoreMember]
            public bool IsSuccess => true;

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

            [IgnoreMember]
            public bool IsSuccess => true;

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

        public static class EnterRoomResponse
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

            internal static EnterRoomResponse<byte[]> Success(ulong ownerId, IReadOnlyDictionary<ulong, RoomPlayerStatus> statuses, byte[]? roomMessage)
                => new EnterRoomResponse<byte[]>(ResultKind.Success, ownerId, statuses, roomMessage);
            internal static readonly EnterRoomResponse<byte[]> RoomNotFound = new EnterRoomResponse<byte[]>(ResultKind.RoomNotFound);
            internal static readonly EnterRoomResponse<byte[]> InvalidPassword = new EnterRoomResponse<byte[]>(ResultKind.InvalidPassword);
            internal static readonly EnterRoomResponse<byte[]> NumberOfPlayersLimitation = new EnterRoomResponse<byte[]>(ResultKind.NumberOfPlayersLimitation);
            internal static readonly EnterRoomResponse<byte[]> AlreadyEntered = new EnterRoomResponse<byte[]>(ResultKind.AlreadyEntered);
            internal static readonly EnterRoomResponse<byte[]> InvalidRoomStatus = new EnterRoomResponse<byte[]>(ResultKind.InvalidRoomStatus);

        }

        [MessagePackObject]
        public sealed class EnterRoomResponse<TRoomMessage> : IServerMsg, IResponse
            where TRoomMessage : class
        {
            [Key(0)]
            public EnterRoomResponse.ResultKind Result { get; private set; }

            [Key(1)]
            public ulong? OwnerId { get; private set; }

            [Key(2)]
            public IReadOnlyDictionary<ulong, RoomPlayerStatus>? Statuses { get; private set; }

            [Key(3)]
            public TRoomMessage? RoomMessage { get; private set; }

            [IgnoreMember]
            public bool IsSuccess => Result == EnterRoomResponse.ResultKind.Success;


            [SerializationConstructor]
            public EnterRoomResponse(EnterRoomResponse.ResultKind result, ulong? ownerId, IReadOnlyDictionary<ulong, RoomPlayerStatus>? statuses, TRoomMessage? roomMessage)
            {
                Result = result;
                OwnerId = ownerId;
                Statuses = statuses;
                RoomMessage = roomMessage;
            }

            public EnterRoomResponse(EnterRoomResponse.ResultKind result)
            {
                Result = result;
                OwnerId = null;
                Statuses = null;
                RoomMessage = null;
            }
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
                NotEnoughPeople = 4,
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
            internal static readonly OperateRoomResponse NotEnoughPeople = new OperateRoomResponse(ResultKind.NotEnoughPeople);
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
        public sealed class UpdateRoomPlayers : IServerMsg
        {
            [Key(0)]
            public ulong? Owner { get; private set; }

            [Key(1)]
            public IReadOnlyDictionary<ulong, RoomPlayerStatus?> Statuses { get; private set; }

            [SerializationConstructor]
            public UpdateRoomPlayers(ulong? owner, IReadOnlyDictionary<ulong, RoomPlayerStatus?> statuses)
            {
                Owner = owner;
                Statuses = statuses;
            }
        }

        [MessagePackObject]
        public sealed class UpdateRoomMessage : IServerMsg
        {
            [Key(0)]
            public byte[]? RoomMessage { get; private set; }

            [SerializationConstructor]
            public UpdateRoomMessage(byte[]? roomMessage)
            {
                RoomMessage = roomMessage;
            }
        }

        [MessagePackObject]
        public sealed class Tick : IServerMsg
        {
            [Key(0)]
            public float ElapsedSeconds { get; private set; }

            [SerializationConstructor]
            public Tick(float elapsedSeconds)
            {
                ElapsedSeconds = elapsedSeconds;
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
