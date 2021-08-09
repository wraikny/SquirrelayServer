using System;
using System.Runtime.Serialization;

namespace SquirrelayServer.Common
{
    /// <summary>
    /// Configuration for LiteNetLib
    ///</summary>
    [DataContract]
    public sealed class NetConfig
    {
        [DataMember(Name = "connectionKey")]
        public string ConnectionKey { get; set; }

        [DataMember(Name = "port")]
        public int Port { get; set; }

        [DataMember(Name = "maxClientsCount")]
        public int MaxClientsCount { get; set; }

#pragma warning disable 0649

        [DataMember(Name = "updateTime")]
        private readonly int? _updateTime;

        [IgnoreDataMember]
        public int UpdateTime { get; set; }

        /* NatPunchEnabled */
        [DataMember(Name = "natPunchEnabled")]
        private readonly bool? _natPunchEnabled;
        [IgnoreDataMember]
        public bool NatPunchEnabled { get; set; }

        /* PingInterval */
        [DataMember(Name = "pingInterval")]
        private readonly int? _pingInterval;
        [IgnoreDataMember]
        public int PingInterval { get; set; }

        /* DisconnectedTimeout */
        [DataMember(Name = "disconnectTimeout")]
        private readonly int? _disconnectTimeout;
        [IgnoreDataMember]
        public int DisconnectTimeout { get; set; }

#if DEBUG
        [DataMember(Name = "debugOnly")]
        public DebugOnlyConfig DebugOnly { get; set; }

        [DataContract]
        public sealed class DebugOnlyConfig
        {
            [DataMember(Name = "simulationPacketLossChance")]
            public int? SimulationPacketLossChance { get; set; }

            [DataMember(Name = "simulateLatencyRange")]
            private int[] _simulationLatencyRange;

            [IgnoreDataMember]
            public (int, int)? SimulationLatencyRange
            {
                get
                {
                    if (_simulationLatencyRange is null)
                    {
                        return null;
                    }

                    return (_simulationLatencyRange[0], _simulationLatencyRange[1]);
                }
                set
                {
                    if (value is (int a, int b))
                    {
                        if (_simulationLatencyRange is null)
                        {
                            _simulationLatencyRange = new int[2] { a, b };
                        }
                        else
                        {
                            _simulationLatencyRange[0] = a;
                            _simulationLatencyRange[1] = b;
                        }
                    }
                    else
                    {
                        _simulationLatencyRange = null;
                    }
                }
            }

            [OnDeserialized]
            private void OnDeserialized(StreamingContext c)
            {
                if (_simulationLatencyRange != null && _simulationLatencyRange.Length != 2)
                {
                    throw new InvalidOperationException("Length of simulationLatencyRange is not equal to 2");
                }
            }

            public DebugOnlyConfig() { }
        }
#endif

#pragma warning restore 0649


        [OnDeserialized]
        private void OnDeserialized(StreamingContext c)
        {
            NatPunchEnabled = _natPunchEnabled ?? false;
            PingInterval = _pingInterval ?? 1000;
            DisconnectTimeout = _disconnectTimeout ?? 5000;
            UpdateTime = _updateTime ?? 15;
        }

        public NetConfig(int port, string key)
        {
            ConnectionKey = key;
            Port = port;
            MaxClientsCount = 32;
            UpdateTime = 15;
            NatPunchEnabled = false;
            PingInterval = 1000;
            DisconnectTimeout = 5000;
#if DEBUG
            DebugOnly = new DebugOnlyConfig
            {
                SimulationPacketLossChance = null,
                SimulationLatencyRange = (17, 21),
            };
#endif
        }
    }
}

