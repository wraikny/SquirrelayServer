using System;
using System.ComponentModel;
using System.Runtime.Serialization;

using MessagePack;

namespace SquirrelayServer.Common
{
    /// <summary>
    /// Configuration for room system
    ///</summary>
    [DataContract]
    [MessagePackObject]
    public sealed class RoomConfig
    {
        [DataMember(Name = "invisibleEnabled")]
        [Key(0)]
        public bool InvisibleEnabled { get; set; }

        [DataMember(Name = "roomMessageEnabled")]
        [Key(1)]
        public bool RoomMessageEnabled { get; set; }

        [DataMember(Name = "passwordEnabled")]
        [Key(2)]
        public bool PasswordEnabled { get; set; }

        [DataMember(Name = "enterWhenPlaingAllowed")]
        [Key(3)]
        public bool EnterWhenPlayingAllowed { get; set; }

        [DataMember(Name = "tickMessageEnabled")]
        [Key(4)]
        public bool TickMessageEnabled { get; set; }

        [DataMember(Name = "disposeSecondsWhenNoMember")]
        [Key(5)]
        public float DisposeSecondsWhenNoMember { get; set; }

        [DataMember(Name = "updatingDisposeStatusIntervalSeconds")]
        [Key(6)]
        public float UpdatingDisposeStatusIntervalSeconds { get; set; }

#pragma warning disable 0649

        [DataMember(Name = "numberOfPlayersRange")]
        [Key(7)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int[] _numberOfPlayersRange;

        [IgnoreDataMember]
        [IgnoreMember]
        public (int, int) NumberOfPlayersRange
        {
            get => (_numberOfPlayersRange[0], _numberOfPlayersRange[1]);
            set
            {
                _numberOfPlayersRange[0] = value.Item1;
                _numberOfPlayersRange[1] = value.Item2;
            }
        }

        [DataMember(Name = "generatedRoomIdRange")]
        [Key(8)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int[] _generatedRoomIdRange;

        [IgnoreDataMember]
        [IgnoreMember]
        public (int, int) GeneratedRoomIdRange
        {
            get => (_generatedRoomIdRange[0], _generatedRoomIdRange[1]);
            set
            {
                _generatedRoomIdRange[0] = value.Item1;
                _generatedRoomIdRange[1] = value.Item2;
            }
        }

#pragma warning restore 0649

        [OnDeserialized]
        private void OnDeserialized(StreamingContext c)
        {
            if (_numberOfPlayersRange?.Length != 2)
            {
                throw new InvalidOperationException("Length of maxNumberOfPlayersRange is not equal to 2");
            }

            if (_generatedRoomIdRange?.Length != 2)
            {
                throw new InvalidOperationException("Length of generatedRoomIdRange is not equal to 2");
            }
        }

        public RoomConfig()
        {
            _numberOfPlayersRange = new int[2];
            _generatedRoomIdRange = new int[2];
        }

        public override bool Equals(object? obj)
        {
            if (!(obj is RoomConfig)) return false;

            var other = (RoomConfig)obj;

            return (InvisibleEnabled == other.InvisibleEnabled)
                && (RoomMessageEnabled == other.RoomMessageEnabled)
                && (PasswordEnabled == other.PasswordEnabled)
                && (EnterWhenPlayingAllowed == other.EnterWhenPlayingAllowed)
                && (TickMessageEnabled == other.TickMessageEnabled)
                && (DisposeSecondsWhenNoMember == other.DisposeSecondsWhenNoMember)
                && (UpdatingDisposeStatusIntervalSeconds == other.UpdatingDisposeStatusIntervalSeconds)
                && (NumberOfPlayersRange == other.NumberOfPlayersRange)
                && (GeneratedRoomIdRange == other.GeneratedRoomIdRange);

        }
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(InvisibleEnabled);
            hash.Add(RoomMessageEnabled);
            hash.Add(PasswordEnabled);
            hash.Add(EnterWhenPlayingAllowed);
            hash.Add(TickMessageEnabled);
            hash.Add(DisposeSecondsWhenNoMember);
            hash.Add(UpdatingDisposeStatusIntervalSeconds);
            hash.Add(NumberOfPlayersRange);
            hash.Add(GeneratedRoomIdRange);
            return hash.ToHashCode();
        }
    }
}
