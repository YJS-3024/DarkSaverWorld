using System;

namespace DarkSaver.Original.Maps
{
    // DManage BB_Map::GetTileData: MRC byte4<2 selects a stable random cell
    // variant; byte4>=2 selects the map's wrapping byte animation phase.
    public static class NativeMapAnimation
    {
        public static int Index(byte mode,byte random,byte phase,int count)
        {if(count<=0)throw new ArgumentOutOfRangeException(nameof(count));return (mode<2?random:phase)%count;}
    }
    public sealed class NativeMapAnimationClock
    {
        public const int Interval=300;
        public double Last {get;private set;}
        // TileDraw/FourStepTileDraw(stage0) advance once only when delta>300;
        // a stalled frame does not catch up. The host supplies presentation time.
        public bool Advance(double clock)
        {
            if(double.IsNaN(clock)||double.IsInfinity(clock)||clock<0)return false;
            clock=Math.Floor(clock); // Original clock_t is integral, including at300.x.
            if(clock<Last){Last=clock;return false;}
            if(clock-Last<=Interval)return false;
            Last=clock;return true;
        }
    }
    public sealed class NativeMapRandom
    {
        private uint state;
        // MSVCRT srand/rand used by BB_Map::LoadMap, low byte of each result.
        public NativeMapRandom(uint seed){state=seed;}
        public byte Next(){state=unchecked(state*214013+2531011);return (byte)((state>>16)&0x7fff);}
    }
    public sealed class NativeMapTileAnimation
    {
        public int x,y;
        public NativeMapStamp[] frames;
        public NativeMapStamp Frame(byte phase)=>frames[NativeMapAnimation.Index(2,0,phase,frames.Length)];
    }
}
