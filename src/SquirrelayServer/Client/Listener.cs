using System;

namespace SquirrelayServer.Client
{
    public interface IClientListener<TPlayerStatus, TRoomMessage, TMsg>
        where TPlayerStatus : class
        where TRoomMessage : class
    {
        void OnGameStarted() { }
        void OnGameFinished() { }
        void OnOwnerChanged(ulong? id) { }
        void OnPlayerEntered(ulong id, TPlayerStatus? status) { }
        void OnPlayerExited(ulong id) { }
        void OnPlayerStatusUpdated(ulong id, TPlayerStatus? status) { }
        void OnRoomMessageUpdated(TRoomMessage roomMessage) { }
        void OnGameMessageReceived(ulong clientId, float elapsedSeconds, TMsg message) { }
        void OnTicked(float elapsedSeconds) { }
    }

    public sealed class EventBasedClientListener<TPlayerStatus, TRoomMessage, TMsg> : IClientListener<TPlayerStatus, TRoomMessage, TMsg>
        where TPlayerStatus : class
        where TRoomMessage : class
    {
        public event Action OnGameStarted = delegate { };
        public event Action OnGameFinished = delegate { };
        public event Action<ulong?> OnOwnerChanged = delegate { };
        public event Action<ulong, TPlayerStatus?> OnPlayerEntered = delegate { };
        public event Action<ulong> OnPlayerExited = delegate { };
        public event Action<ulong, TPlayerStatus?> OnPlayerStatusUpdated = delegate { };
        public event Action<TRoomMessage> OnRoomMessageUpdated = delegate { };
        public event Action<ulong, float, TMsg> OnGameMessageReceived = delegate { };
        public event Action<float> OnTicked = delegate { };


        void IClientListener<TPlayerStatus, TRoomMessage, TMsg>.OnGameStarted() => OnGameStarted.Invoke();
        void IClientListener<TPlayerStatus, TRoomMessage, TMsg>.OnGameFinished() => OnGameFinished.Invoke();
        void IClientListener<TPlayerStatus, TRoomMessage, TMsg>.OnOwnerChanged(ulong? id) => OnOwnerChanged.Invoke(id);
        void IClientListener<TPlayerStatus, TRoomMessage, TMsg>.OnPlayerEntered(ulong id, TPlayerStatus? status) => OnPlayerEntered.Invoke(id, status);
        void IClientListener<TPlayerStatus, TRoomMessage, TMsg>.OnPlayerExited(ulong id) => OnPlayerExited.Invoke(id);
        void IClientListener<TPlayerStatus, TRoomMessage, TMsg>.OnPlayerStatusUpdated(ulong id, TPlayerStatus? status) => OnPlayerStatusUpdated.Invoke(id, status);
        void IClientListener<TPlayerStatus, TRoomMessage, TMsg>.OnRoomMessageUpdated(TRoomMessage roomMessage) => OnRoomMessageUpdated.Invoke(roomMessage);
        void IClientListener<TPlayerStatus, TRoomMessage, TMsg>.OnGameMessageReceived(ulong clientId, float elapsedSeconds, TMsg message) => OnGameMessageReceived.Invoke(clientId, elapsedSeconds, message);
        void IClientListener<TPlayerStatus, TRoomMessage, TMsg>.OnTicked(float elapsedSeconds) => OnTicked.Invoke(elapsedSeconds);
    }
}
