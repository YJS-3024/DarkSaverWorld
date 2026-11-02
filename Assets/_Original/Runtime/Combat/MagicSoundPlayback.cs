using System;
using System.Collections.Generic;
using System.Linq;

namespace DarkSaver.Original.World
{
    public struct MagicSoundCue { public int sound;public bool stop; }
    // IDs >0 only, native sound1/sound2 fields. Presentation clock, not an audio-derived game timer.
    public sealed class MagicSoundPlayback
    {
        private sealed class State { public int stage;public int travel;public int coordinateStage; }
        private readonly Dictionary<string,State> states=new Dictionary<string,State>();
        public MagicSoundCue[] Update(IEnumerable<BattleMagicEffect> source)
        {
            var effects=(source??Array.Empty<BattleMagicEffect>()).Where(e=>e.pattern?.Valid==true&&e.pattern.type!=0&&!e.Expired).ToArray();
            var result=new List<MagicSoundCue>();
            void Cue(int id,bool stop=false){if(id>0)result.Add(new MagicSoundCue{sound=id,stop=stop});}
            foreach(var e in effects)
            {
                if(!states.TryGetValue(e.id,out var state)){state=new State();states.Add(e.id,state);}
                int type=e.pattern.type,c=e.Counter;
                bool charge=e.role==BattleMagicEffectRole.ChargeOnly;
                bool flying=type==1||type==10||type==11||type==24;
                bool travel=charge&&type!=12||flying;
                if(travel)
                {
                    if(state.stage==0&&c>=1){Cue(e.pattern.sound2);state.travel=e.pattern.sound2;state.stage=1;}
                    if(state.stage==1&&c>=(charge?type==18?23:BattleSkillFrames.HasCaster(type,e.radius)?int.MaxValue:11:type==24?6:10)){Cue(state.travel,true);state.travel=0;state.stage=2;}
                    if(!charge&&state.stage==2){Cue(e.pattern.sound1);state.stage=3;}
                }
                else if(state.stage==0&&c>=(type==6||type==14||type==15||type==18||type==19||type==20?11:1))
                {if(!charge||type==12)Cue(e.pattern.sound1);state.stage=3;}
            }
            // The shared coordinate pass reads the updated target counter. At10 it
            // stops sound1; the native final reset-to0 pass starts it once more.
            foreach(var e in BattleMagicPresentation.CoordinateOwners(effects).Where(e=>e.IsTornado||e.pattern.type==13))
            {
                var state=states[e.id];
                if(state.coordinateStage==0&&e.Counter>=10){Cue(e.pattern.sound1,true);state.coordinateStage=1;}
                if(state.coordinateStage==1&&e.Counter>=11&&e.PostCounter==0){Cue(e.pattern.sound1);state.coordinateStage=2;}
            }
            var active=new HashSet<string>(effects.Select(e=>e.id));
            foreach(string id in states.Keys.Where(id=>!active.Contains(id)).ToArray())
            {Cue(states[id].travel,true);states.Remove(id);}
            return result.ToArray();
        }
        public void Clear()=>states.Clear();
    }
}
