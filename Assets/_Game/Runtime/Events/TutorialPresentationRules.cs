using System;
namespace DarkSaver.Events
{
    // User-requested tutorial presentation; never changes the recovered script/state.
    public static class TutorialPresentationRules
    {
        public static int IconMask(int mask,int previousMask,bool interactive)
            => !interactive && mask==0 ? previousMask : mask;
        public static TutorialRangeMode RangeMode(string command,bool moveSelected,bool attackSelected,bool magicSelected,bool blocked)
        {
            if(blocked)return TutorialRangeMode.None;
            if(command=="Move" && moveSelected)return TutorialRangeMode.Move;
            if(command=="Attack" && attackSelected)return TutorialRangeMode.Attack;
            if(command=="MagicTarget" && magicSelected)return TutorialRangeMode.Magic;
            return TutorialRangeMode.None;
        }
        public static void PlaceMenu(float anchorX,float anchorY,out float x,out float y)
        {
            x=Math.Max(8,Math.Min(536,anchorX-48));y=Math.Max(8,Math.Min(376,anchorY-48));
            // Keep the menu on the actor; overlap is resolved by Canvas sorting only.
        }
    }
}
