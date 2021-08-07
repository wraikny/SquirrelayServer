using System.Collections.Concurrent;
using System.Threading;

namespace Examples
{
    public class Context : SynchronizationContext
    {
        private readonly struct Entry
        {
            private readonly SendOrPostCallback _d;
            private readonly object _state;

            internal Entry(SendOrPostCallback d, object state)
            {
                _d = d;
                _state = state;
            }

            internal void Invoke()
            {
                _d.Invoke(_state);
            }
        }

        private readonly ConcurrentQueue<Entry> _entries = new ConcurrentQueue<Entry>();

        public override void Post(SendOrPostCallback d, object state)
        {
            if (d is null) return;
            _entries.Enqueue(new Entry(d, state));
        }

        public void Update()
        {
            var count = _entries.Count;
            for (var i = 0; i < count; i++)
            {
                if (_entries.TryDequeue(out var e)) e.Invoke();
            }
        }
    }
}
