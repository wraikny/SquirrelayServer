using System.Runtime.Serialization;

namespace SquirrelayServer.Common
{
    [DataContract]
    public sealed class NetConfig
    {
        public const string DefaultPath = @"config/netconfig.json";

        [DataMember(Name = "connectionKey")]
        public string ConnectionKey { get; private set; }

        [DataMember(Name = "port")]
        public int Port { get; private set; }

        [DataMember(Name = "mexClientsCount")]
        public int MaxClientsCount { get; private set; }

#pragma warning disable 0649

        [DataMember(Name = "updateTime")]
        private readonly int? _updateTime;

        [IgnoreDataMember]
        public int UpdateTime { get; private set; }

        /* NatPunchEnabled */
        [DataMember(Name = "natPunchEnabled")]
        private readonly bool? _natPunchEnabled;
        [IgnoreDataMember]
        public bool NatPunchEnabled { get; private set; }

        /* PingInterval */
        [DataMember(Name = "pingInterval")]
        private readonly int? _pingInterval;
        [IgnoreDataMember]
        public int PingInterval { get; private set; }

        /* DisconnectedTimeout */
        [DataMember(Name = "disconnectTimeout")]
        private readonly int? _disconnectTimeout;
        [IgnoreDataMember]
        public int DisconnectedTimeout { get; private set; }

#if DEBUG
        [DataMember(Name = "debugOnly")]
        public DebugOnlyConfig DebugOnly { get; private set; }

        [DataContract]
        public sealed class DebugOnlyConfig
        {
            [DataMember(Name = "simulationPacketLossChance")]
            public int? SimulationPacketLossChance { get; private set; }

            [DataMember(Name = "simulateLatencyRange")]
            private readonly int[] _simulationLatencyRange;

            [IgnoreDataMember]
            public (int, int)? SimulationLatencyRange { get; private set; }

            [OnDeserialized]
            private void OnDeserialized(StreamingContext c)
            {
                if (_simulationLatencyRange != null && _simulationLatencyRange.Length == 2)
                {
                    SimulationLatencyRange = (_simulationLatencyRange[0], _simulationLatencyRange[1]);
                }
            }
        }
#endif

#pragma warning restore 0649


        [OnDeserialized]
        private void OnDeserialized(StreamingContext c)
        {
            NatPunchEnabled = _natPunchEnabled ?? false;
            PingInterval = _pingInterval ?? 1000;
            DisconnectedTimeout = _disconnectTimeout ?? 5000;
            UpdateTime = _updateTime ?? 15;
        }
    }
}

