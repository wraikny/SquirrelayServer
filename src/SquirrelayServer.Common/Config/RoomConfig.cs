using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace SquirrelayServer.Common
{
    [DataContract]
    public sealed class RoomConfig
    {
        [DataMember(Name = "invisibleEnabled")]
        public bool InvisibleEnabled { get; private set; }

        [DataMember(Name = "messageEnabled")]
        public bool MessageEnabled { get; private set; }

        [DataMember(Name = "passwordEnabled")]
        public bool PasswordEnabled { get; private set; }

#pragma warning disable 0649

        [DataMember(Name = "maxNumberOfPlayersRange")]
        private readonly int[] _maxNumberOfPlayersRange;

        [IgnoreDataMember]
        public (int, int) MaxNumberOfPlayersRange { get; private set; }

#pragma warning restore 0649

        [OnDeserialized]
        private void OnDeserialized(StreamingContext c)
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
    }
}
