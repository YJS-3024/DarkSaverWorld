using System;
using System.Linq;
using System.Runtime.Serialization;
using DarkSaver.Original.Tutorial;

namespace DarkSaver.Original.World
{
    [Serializable,DataContract] public sealed class BattleMagicRule
    {
        [DataMember] public int magic;
        [DataMember] public bool enabled;
        [NonSerialized,DataMember(EmitDefaultValue=false)] public int? mpCost,apCost;
        [DataMember(EmitDefaultValue=false)] public int[] targets;
        [NonSerialized,DataMember(EmitDefaultValue=false)] public bool? allowDeadTargets;
        public BattleMagicRule Copy(){var r=(BattleMagicRule)MemberwiseClone();r.targets=targets==null?null:(int[])targets.Clone();return r;}
        public bool Valid=>magic>0&&(!mpCost.HasValue||mpCost>=0)&&(!apCost.HasValue||apCost>=0)
            &&(targets==null||targets.All(id=>id>=0&&id<=999)&&targets.Distinct().Count()==targets.Length);
        public static bool ValidRules(BattleMagicRule[] rules)=>rules==null||rules.All(r=>r?.Valid==true)&&rules.Select(r=>r.magic).Distinct().Count()==rules.Length;
    }
    public enum BattleMagicEffectRole { TargetWithCharge,TargetOnly,ChargeOnly }
    // Transient approved presentation; immutable endpoints survive target disappearance/movement.
    public sealed class BattleMagicEffect
    {
        public string id;public int magic,sourceX,sourceY,targetX,targetY;public double age;
        public int targetUnit=-1,sourceUnit,sourceJob;public BattleMagicEffectRole role;
        public int areaX,areaY,radius,mapWidth,mapHeight;
        public MagicProjectilePattern pattern;
        private BattleThunderPlayback thunder;
        public BattleMagicEffect Copy(){var r=(BattleMagicEffect)MemberwiseClone();r.pattern=pattern?.Copy();r.thunder=thunder?.Copy();return r;}
        public bool IsThunder=>pattern?.type==2;
        public bool IsTornado=>pattern?.type==3;
        public bool IsGeneral=>pattern!=null&&pattern.type!=1&&pattern.type!=2&&pattern.type!=3;
        public int PostCounter=>pattern?.type==4?(thunder?.Finished==true?0:thunder?.StoredCounter??0):IsTornado||pattern?.type==13?Counter==11?0:Counter:Counter;
        // Thunder has a random counter lifetime, not a fixed duration.
        public double DurationMs=>IsGeneral?GeneralMagicFrames.Duration(this):role==BattleMagicEffectRole.ChargeOnly?11*TutorialPresentationTiming.MagicPhaseMs:IsThunder?double.PositiveInfinity:IsTornado?12*TutorialPresentationTiming.MagicPhaseMs:pattern?.Valid==true?(10+pattern.impact.count)*TutorialPresentationTiming.MagicPhaseMs:0;
        public bool Expired=>role==BattleMagicEffectRole.ChargeOnly&&pattern?.type==12?thunder?.Finished==true:role==BattleMagicEffectRole.ChargeOnly?age>=DurationMs:IsThunder||pattern?.type==4||pattern?.type==12?thunder?.Finished==true&& (pattern.type!=4||Counter>thunder.Steps):age>=DurationMs;
        public void Advance(double ms)
        {
            age+=ms;
            if((IsThunder||pattern?.type==4||pattern?.type==12)&&(role!=BattleMagicEffectRole.ChargeOnly||pattern?.type==12)){if(thunder==null)thunder=new BattleThunderPlayback(id,IsThunder?20:(pattern.impact.count+1<3?8:pattern.impact.count+1)*4,role!=BattleMagicEffectRole.ChargeOnly);thunder.AdvanceTo(Counter);}
        }
        public int NativeRandomCounter=>thunder?.DrawCounter??0;
        public int Counter=>(int)(age/TutorialPresentationTiming.MagicPhaseMs);
        public bool Visible=>role==BattleMagicEffectRole.ChargeOnly||IsTornado?Counter>=1&&Counter<=10:IsThunder?thunder?.Visible==true:age>=TutorialPresentationTiming.MagicPhaseMs&&age<DurationMs;
        public bool Impact=>IsTornado||!IsThunder&&Counter>=10;
        private MagicEffectStrip Strip=>Impact?pattern.impact:pattern.travel;
        public int Width=>Strip.width;public int Height=>Strip.height;
        public int Frame
        {
            get
            {
                if(IsThunder)return (thunder?.DrawCounter??0)/2%pattern.travel.count;
                if(IsTornado)return Math.Max(0,Math.Min(9,Counter-1))%pattern.impact.count;
                if(Impact)return Math.Min(pattern.impact.count-1,Counter-10);
                // Native rand()%travelCount; a local visual seed does not emulate the original global RNG state.
                uint sample=2166136261;foreach(char c in id??"")sample=unchecked((sample^c)*16777619);
                for(int i=0;i<=Counter;i++)sample=unchecked(sample*214013+2531011);
                return (int)((sample>>16&32767)%(uint)pattern.travel.count);
            }
        }
        public int AtlasX=>Strip.x+Frame*Strip.stepX;public int AtlasY=>Strip.y+Frame*Strip.stepY;
        public double DrawX=>IsThunder||IsTornado?targetX*32-(Width-32)/2:PixelX-(Impact?(Width-32)/2:0);
        public double DrawY=>IsThunder?(targetY+1)*32-Height:IsTornado?targetY*32-(Height-32)/2:PixelY-(Impact?(Height-32)/2:0);
        private double T=>Math.Min(1,Counter/10.0);
        public double PixelX=>Position(sourceX*32,targetX*32,0);
        public double PixelY=>Position(sourceY*32,targetY*32,-50);
        private double Position(double source,double target,double offset)
        {double t=T,a=1-t,control=(source+target)*.5+offset;return source*a*a*a+control*(3*a*a*t+3*a*t*t)+target*t*t*t;}
        public BattleMagicFrame[] Frames
        {
            get
            {
                if(IsGeneral)return GeneralMagicFrames.Frames(this,thunder?.DrawCounter??0);
                if(role==BattleMagicEffectRole.ChargeOnly)return Visible?new[]{BattleThunderPlayback.Charge(pattern.charge,Counter-1,sourceX,sourceY)}:Array.Empty<BattleMagicFrame>();
                if(IsThunder)return thunder?.Frames(pattern,sourceX,sourceY,targetX,targetY,role==BattleMagicEffectRole.TargetWithCharge)??Array.Empty<BattleMagicFrame>();
                return Visible?new[]{new BattleMagicFrame{part=0,x=DrawX,y=DrawY,width=Width,height=Height,atlasX=AtlasX,atlasY=AtlasY}}:Array.Empty<BattleMagicFrame>();
            }
        }
    }
}
