using System;

namespace SquirrelayServer.Client
{
    public sealed class ClientNotConnectedException : Exception
    {

    }

    public enum InvalidStatusReason
    {
        AlreadyEntered,
        ClientOutOfRoom,
        ClientIsNotOwner,
        InvalidRoomStatus,
    }

    public sealed class InvalidStatusException : Exception
    {
        public InvalidStatusReason Reason { get; private set; }

        public InvalidStatusException(InvalidStatusReason reason)
        {
            Reason = reason;
        }
    }
}
