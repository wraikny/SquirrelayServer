using System;
using System.Collections.Generic;
using System.Text;

namespace SquirrelayServer.Client
{
    public interface IGameMessageListener<T>
    {
        void OnReceived(ulong clientId, float elapsedSeconds, T message);
    }

    public sealed class EventBasedGameMessageListener<T> : IGameMessageListener<T>
    {
        public event Action<ulong, float, T> OnReceived = delegate { };

        void IGameMessageListener<T>.OnReceived(ulong clientId, float elapsedSeconds, T message)
        {
            OnReceived.Invoke(clientId, elapsedSeconds, message);
        }
    }
}
