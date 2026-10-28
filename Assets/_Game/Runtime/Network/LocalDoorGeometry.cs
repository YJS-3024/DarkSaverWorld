namespace DarkSaver.Network
{
    // Local two-cell trigger and server contact envelope share the same geometry.
    public static class LocalDoorGeometry
    {
        public const float Width=2,Height=1,OffsetX=.5f,VisitorMargin=.35f;
        public static bool Contains(float x,float y,float doorX,float doorY)
            => System.Math.Abs(x-doorX-OffsetX)<=Width*.5f+VisitorMargin
                && System.Math.Abs(y-doorY)<=Height*.5f+VisitorMargin;
    }
}
