#if DEBUG
using Altseed2;

namespace SquirrelayServer.Altseed2
{
    internal static class Program
    {
        private static void Main()
        {
            Engine.Initialize("SquireelayServer test for Altseed2", 1280, 720);

            while (Engine.DoEvents())
            {
                Engine.Update();
            }

            Engine.Terminate();
        }
    }
}
#endif
