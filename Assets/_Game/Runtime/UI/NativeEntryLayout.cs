using UnityEngine;
namespace DarkSaver.UI
{
    public static class NativeEntryLayout
    {
        public static readonly Rect LoginIdHit=new Rect(298,296,110,20);
        public static readonly Rect LoginPasswordHit=new Rect(298,320,110,18);
        // Saver_Late40C3B1..3D8: GDI text is centered, not left-aligned at the hitbox edge.
        public static readonly Vector2 LoginIdText=new Vector2(352,294);
        public static readonly Vector2 LoginPasswordText=new Vector2(352,318);
        // Keep the existing window anchor until its original global placement is established.
        public static Vector2 CreationOrigin=>new Vector2(Mathf.Floor((Rendering.GameScreen.Width-397)*.5f),64);
        // Native Wlib scene24: card x37/153/268, portrait inner70x70 at x47/163/278,y64.
        private static readonly int[] CardLeft={37,153,268};
        public static Rect CreationCard(int slot)=>slot>=0 && slot<3?new Rect(CardLeft[slot],54,90,176):default;
        public static Rect CreationFace(int slot)=>slot>=0 && slot<3?new Rect(CardLeft[slot]+13,67,64,64):default;
        public static readonly Rect CreationName=new Rect(254,241,88,15);
        public static Rect Offset(Rect rect,Vector2 origin)=>new Rect(rect.position+origin,rect.size);
    }
}
