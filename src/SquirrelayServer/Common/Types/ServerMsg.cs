using System.Collections.Generic;

using MessagePack;


namespace SquirrelayServer.Common
{
    /// <summary>
    /// Message sent by the server
    ///</summary>
    [Union(0, typeof(ClientId))]
    [Union(1, typeof(SetPlayerStatusResponse))]
    [Union(2, typeof(RoomListResponse))]
    [Union(3, typeof(CreateRoomResponse))]
    [Union(4, typeof(EnterRoomResponse))]
    [Union(5, typeof(ExitRoomResponse))]
    [Union(6, typeof(OperateRoomResponse))]
    [Union(7, typeof(UpdateRoomPlayers))]
    public interface IServerMsg
    {
        [MessagePackObject]
        public sealed class ClientId : IServerMsg
        {
            [Key(0)]
            public ulong Id { get; private set; }

            [SerializationConstructor]
            public ClientId(ulong id)
            {
                Id = id;
            }
        }


        [MessagePackObject]
        public sealed class SetPlayerStatusResponse : IServerMsg, IResponse
        {
            public enum ErrorKind
            {
                PlayerOutOfRoom = 0,
            }

            [Key(0)]
            public ErrorKind? Error { get; private set; }

            [IgnoreMember]
            public bool IsSuccess => Error is null;

            [SerializationConstructor]
            public SetPlayerStatusResponse(ErrorKind? error)
            {
                Error = error;
            }

            public static readonly SetPlayerStatusResponse PlayerOutOfRoom = new SetPlayerStatusResponse(ErrorKind.PlayerOutOfRoom);
            public static readonly SetPlayerStatusResponse Success = new SetPlayerStatusResponse(null);
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
            public enum ErrorKind
            {
                AlreadyEntered = 0,
            }

            [Key(0)]
            public ErrorKind? Error { get; private set; }

            [Key(1)]
            public int Id { get; private set; }

            [IgnoreMember]
            public bool IsSuccess => Error is null;

            [SerializationConstructor]
            public CreateRoomResponse(ErrorKind? error, int id)
            {
                Error = error;
                Id = id;
            }

            public static readonly CreateRoomResponse AlreadyEntered = new CreateRoomResponse(ErrorKind.AlreadyEntered, 0);
            public static CreateRoomResponse Success(int id) => new CreateRoomResponse(null, id);
        }

        [MessagePackObject]
        public sealed class EnterRoomResponse : IServerMsg, IResponse
        {
            public enum ErrorKind
            {
                RoomNotFound = 0,
                InvalidPassword = 1,
                NumberOfPlayersLimitation = 2,
                AlreadyEntered = 3,
            }

            [Key(0)]
            public ErrorKind? Error { get; private set; }

            [Key(1)]
            public IReadOnlyDictionary<ulong, RoomPlayerStatus> Statuses { get; private set; }

            [IgnoreMember]
            public bool IsSuccess => Error is null;

            [SerializationConstructor]
            public EnterRoomResponse(ErrorKind? error, IReadOnlyDictionary<ulong, RoomPlayerStatus> statuses)
            {
                Error = error;
                Statuses = statuses;
            }

            public static readonly EnterRoomResponse RoomNotFound = new EnterRoomResponse(ErrorKind.RoomNotFound, null);
            public static readonly EnterRoomResponse InvalidPassword = new EnterRoomResponse(ErrorKind.InvalidPassword, null);
            public static readonly EnterRoomResponse NumberOfPlayersLimitation = new EnterRoomResponse(ErrorKind.NumberOfPlayersLimitation, null);
            public static readonly EnterRoomResponse AlreadyEntered = new EnterRoomResponse(ErrorKind.AlreadyEntered, null);
            public static EnterRoomResponse Success(IReadOnlyDictionary<ulong, RoomPlayerStatus> statuses)
                => new EnterRoomResponse(null, statuses);
        }

        [MessagePackObject]
        public sealed class ExitRoomResponse : IServerMsg, IResponse
        {

            public enum ErrorKind
            {
                PlayerOutOfRoom = 0,
            }

            [Key(0)]
            public ErrorKind? Error { get; private set; }

            [IgnoreMember]
            public bool IsSuccess => Error is null;

            [SerializationConstructor]
            public ExitRoomResponse(ErrorKind? error)
            {
                Error = error;
            }

            public static readonly ExitRoomResponse PlayerOutOfRoom = new ExitRoomResponse(ErrorKind.PlayerOutOfRoom);
            public static readonly ExitRoomResponse Success = new ExitRoomResponse(null);
        }

        [MessagePackObject]
        public sealed class OperateRoomResponse : IServerMsg, IResponse
        {
            public enum ErrorKind
            {
                PlayerOutOfRoom = 0,
                PlayerIsNotOwner = 1,
                InvalidRoomStatus = 2,
            }

            [Key(0)]
            public ErrorKind? Error { get; private set; }

            [IgnoreMember]
            public bool IsSuccess => Error is null;

            [SerializationConstructor]
            public OperateRoomResponse(ErrorKind? error)
            {
                Error = error;
            }

            public static readonly OperateRoomResponse PlayerIsNotOwner = new OperateRoomResponse(ErrorKind.PlayerIsNotOwner);
            public static readonly OperateRoomResponse PlayerOutOfRoom = new OperateRoomResponse(ErrorKind.PlayerOutOfRoom);
            public static readonly OperateRoomResponse InvalidRoomStatus = new OperateRoomResponse(ErrorKind.InvalidRoomStatus);

            public static OperateRoomResponse Success = new OperateRoomResponse(null);
        }

        [MessagePackObject]
        public sealed class UpdateRoomPlayers
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
    }
}
