using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace SquirrelayServer.Common
{
    /// <summary>
    /// Configuration for room system
    ///</summary>
    [DataContract]
    public sealed class RoomConfig
    {
        [DataMember(Name = "invisibleEnabled")]
        public bool InvisibleEnabled { get; set; }

        [DataMember(Name = "roomMessageEnabled")]
        public bool RoomMessageEnabled { get; set; }

        [DataMember(Name = "passwordEnabled")]
        public bool PasswordEnabled { get; set; }

        [DataMember(Name = "enterWhilePlayingAllowed")]
        public bool EnterWhilePlayingAllowed { get; set; }

        [DataMember(Name = "disposeSecondAfterCreated")]
        public float DisposeSecondWhileNoMember { get; set; }

        [DataMember(Name = "updatingDisposeStatusIntervalSecond")]
        public float UpdatingDisposeStatusIntervalSecond { get; set; }


#pragma warning disable 0649

        [DataMember(Name = "maxNumberOfPlayersRange")]
        private readonly int[] _maxNumberOfPlayersRange;

        [IgnoreDataMember]
        public (int, int) MaxNumberOfPlayersRange { get; internal set; }

        [DataMember(Name = "generatedRoomIdRange")]
        private readonly int[] _generatedRoomIdRange;

        [IgnoreDataMember]
        public (int, int) GeneratedRoomIdRange { get; internal set; }

#pragma warning restore 0649

        [OnDeserialized]
        private void OnDeserialized(StreamingContext c)
        {
            {
                if (_maxNumberOfPlayersRange is int[] arr && arr.Length == 2)
                {
                    MaxNumberOfPlayersRange = (arr[0], arr[1]);
                }
                else
                {
                    throw new InvalidOperationException("Length of maxNumberOfPlayersRange is not equal to 2");
                }
            }

            {
                if (_generatedRoomIdRange is int[] arr && arr.Length == 2)
                {
                    GeneratedRoomIdRange = (arr[0], arr[1]);
                }
                else
                {
                    throw new InvalidOperationException("Length of generatedRoomIdRange is not equal to 2");
                }
            }
        }
    }
}
