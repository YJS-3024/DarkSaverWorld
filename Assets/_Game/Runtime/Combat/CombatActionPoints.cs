using System;
namespace DarkSaver.Combat
{
    // Received tick state. There is no locally invented starting AP, faction cap
    // or recovery clock. A confirmed maximum is supplied separately when known.
    public sealed class CombatActionPoints
    {
        private int? confirmedMaximum;
        public CombatActionPoints(bool enemy=false){}
        internal CombatActionPoints(Func<int> maximum)
        {if(maximum==null)throw new ArgumentNullException(nameof(maximum));}
        public int Current {get;private set;}
        public bool HasConfirmedMaximum=>confirmedMaximum.HasValue;
        public int Maximum=>confirmedMaximum ?? 0; // Unknown, not the original maximum of zero.
        public bool ConfigureMaximum(int value)
        {
            if(value<=0 || value>short.MaxValue)return false;
            confirmedMaximum=value;Current=Math.Min(Current,value);return true;
        }
        public bool ApplyAbsolute(int value)
        {Current=Math.Max(0,HasConfirmedMaximum?Math.Min(Maximum,value):value);return true;}
        public bool CanStart(int cost)=>HasConfirmedMaximum && cost>0 && cost<=Maximum && Current==Maximum;
        public bool TrySpend(int cost)
        {if(!CanStart(cost))return false;Current-=cost;return true;}
        public bool Advance(float seconds)=>false;
    }
}
