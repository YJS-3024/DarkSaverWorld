using System;
namespace DarkSaver.Events
{
    // Client-side IncTick presentation: source commands remain intact, display clamps to the supplied maximum.
    // Seconds/rate are user settings, not a claim about the original server's timing.
    public sealed class OriginalTutorialTickCounter
    {
        private int literal,epoch;
        private bool initialized;
        private double value;
        public int Read(int source,int sourceEpoch,int maximum)
        {
            if(!initialized || source!=literal || sourceEpoch!=epoch)
            {literal=source;epoch=sourceEpoch;value=Math.Max(0,Math.Min(maximum,source));initialized=true;}
            return Math.Max(0,Math.Min(maximum,(int)Math.Floor(value+1e-8)));
        }
        public void Advance(float seconds,float perSecond,int maximum)
        {
            if(!initialized || seconds<0 || float.IsNaN(seconds) || float.IsInfinity(seconds)
                || perSecond<=0 || float.IsNaN(perSecond) || float.IsInfinity(perSecond) || maximum<=0)return;
            value=Math.Min(maximum,value+(double)seconds*perSecond);
        }
    }
}
