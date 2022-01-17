using System;
using System.Runtime.Serialization;

namespace SquirrelayServer.Common
{
    /// <summary>
    /// Configuration for room system
    ///</summary>
    [DataContract]
    public sealed class ServerLoggingConfig
    {
        [DataMember(Name = "logging")]
        public bool Logging { get; set; }

        [DataMember(Name = "serverLogging")]
        public bool ServerLogging { get; set; }

        [DataMember(Name = "roomListLogging")]
        public bool RoomListLogging { get; set; }

        [DataMember(Name = "roomLogging")]
        public bool RoomLogging { get; set; }

        [DataMember(Name = "messageLogging")]
        public bool MessageLogging { get; set; }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext c)
        {

        }

        public ServerLoggingConfig()
        {

        }

        public override bool Equals(object? obj)
        {
            if (!(obj is ServerLoggingConfig)) return false;

            var other = (ServerLoggingConfig)obj;

            return (Logging == other.Logging) && (ServerLogging == other.ServerLogging) && (RoomListLogging == other.RoomListLogging)
                && (RoomLogging == other.RoomLogging)
                && (MessageLogging == other.MessageLogging);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Logging);
            hash.Add(ServerLogging);
            hash.Add(RoomListLogging);
            hash.Add(RoomLogging);
            hash.Add(MessageLogging);
            return hash.ToHashCode();
        }
    }
}
