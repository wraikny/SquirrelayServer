using System.Collections.Concurrent;
using System.Threading;

namespace SquirrelayServer.Common
{
    public sealed class Context : SynchronizationContext
    {
        private readonly struct Entry
        {
            private readonly SendOrPostCallback _d;
            private readonly object _state;

            public Entry(SendOrPostCallback d, object state)
            {
                _d = d;
                _state = state;
            }

            public void Invoke()
            {
                _d.Invoke(_state);
            }
        }

        private readonly ConcurrentQueue<Entry> _actions;

        public Context()
        {
            _actions = new ConcurrentQueue<Entry>();
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            if (d is null) return;
            _actions.Enqueue(new Entry(d, state));
        }

        public void Update()
        {
            while (_actions.TryDequeue(out var e))
            {
                e.Invoke();
            }
        }
    }
}
