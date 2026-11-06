using System;
using System.Collections.Generic;
using System.Linq;

namespace DarkSaver.Original.World
{
    public sealed class DeathSoundPlayback
    {
        private readonly HashSet<(int unit,long life)> started=new HashSet<(int,long)>();
        public MagicSoundCue[] Update(BattleDeathEffect[] effects)
        {
            effects=effects??Array.Empty<BattleDeathEffect>();var cues=new List<MagicSoundCue>();
            // Retire an old lifetime before a replacement starts the shared sound1 buffer.
            // Otherwise cleanup can immediately stop the new character's death sound.
            foreach(var id in started.ToArray())
                if(!effects.Any(e=>e.unit==id.unit&&e.life==id.life&&!e.Expired))
                {started.Remove(id);cues.Add(new MagicSoundCue{sound=1,stop=true});}
            foreach(var e in effects.OrderBy(e=>e.unit))
                if(!e.LargeJob&&!e.held&&!e.Expired&&e.Counter>=1&&started.Add((e.unit,e.life)))cues.Add(new MagicSoundCue{sound=1});
            return cues.ToArray();
        }
        public void Clear()=>started.Clear();
    }
}
