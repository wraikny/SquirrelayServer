using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SquirrelayServer.Common
{
    internal sealed class FPS
    {
        private readonly int _updateTime;
        private readonly Stopwatch _stopwatch;

        public float DeltaSecond { get; private set; }

        public FPS(int updateTime)
        {
            _updateTime = updateTime;
            _stopwatch = new Stopwatch();
        }

        public void Start()
        {
            _stopwatch.Start();
        }

        public async ValueTask Update()
        {
            _stopwatch.Stop();
            var deltaMs = (int)_stopwatch.ElapsedMilliseconds;

            if (deltaMs < _updateTime)
            {
                await Task.Delay(_updateTime - deltaMs);
            }

            DeltaSecond = Utils.MsToSec(deltaMs);

            _stopwatch.Restart();
        }
    }
}
