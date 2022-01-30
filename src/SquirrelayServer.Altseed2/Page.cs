using Altseed2;

namespace SquirrelayServer.Altseed2
{
    internal interface IPage
    {
        void Resize(Vector2F size);
        void UpdateCameraGroup(ulong cameraGroup);
        int UpdateZOrder(int zOrder);
        void SetIsDrawn(bool isDrawn);
    }
}
