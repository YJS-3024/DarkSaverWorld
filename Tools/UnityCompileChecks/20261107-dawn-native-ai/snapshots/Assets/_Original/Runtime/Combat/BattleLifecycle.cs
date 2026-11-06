using System;

namespace DarkSaver.Original.World
{
    public enum BattleLifecycleKind { Remove, Death }
    public sealed class BattleLifecycleEvent
    {
        public BattleLifecycleKind kind;
        public int unit;
        public long life;
    }
    public sealed class BattleLifecycleNotice
    {
        public string id,character,session;
        public long expectedRevision,unitLife;
        public bool approved;
        public byte[] packet;
    }
    public sealed class BattleLifecyclePacket
    {
        public BattleLifecycleKind kind;
        public int unit,unused;
    }
    public static class OriginalBattleLifecycle
    {
        public static int AppearanceKey(TrainingUnit unit)=>unit.nativeAppearanceJob??unit.job;
        public static byte[] Packet(BattleLifecycleKind kind,int unit,int unused=0)
        {
            if(unit<0||unit>=3000||unused<short.MinValue||unused>short.MaxValue)return null;
            if(kind==BattleLifecycleKind.Remove)return unused==0?new[]{(byte)0x5c,(byte)3,(byte)unit,(byte)(unit>>8)}:null;
            return kind==BattleLifecycleKind.Death?new[]{(byte)0x5c,(byte)7,(byte)unit,(byte)(unit>>8),(byte)unused,(byte)(unused>>8)}:null;
        }
        public static bool TryDecode(byte[] bytes,out BattleLifecyclePacket packet)
        {
            packet=null;if(bytes==null||bytes.Length<4||bytes[0]!=0x5c)return false;
            if(bytes[1]!=3&&bytes[1]!=7||bytes.Length!=(bytes[1]==3?4:6))return false;
            int unit=(short)(bytes[2]|bytes[3]<<8);if(unit<0||unit>=3000)return false;
            packet=new BattleLifecyclePacket{kind=bytes[1]==3?BattleLifecycleKind.Remove:BattleLifecycleKind.Death,unit=unit,unused=bytes.Length==6?(short)(bytes[4]|bytes[5]<<8):0};return true;
        }
        public static bool TryApply(TrainingUnit before,BattleLifecycleKind kind,out TrainingUnit after)
        {
            after=null;if(before==null||!before.nativeState.HasValue||!Enum.IsDefined(typeof(BattleLifecycleKind),kind))return false;
            after=before.Copy();
            if(kind==BattleLifecycleKind.Death)after.nativeState|=0x80u;
            else if((before.nativeState.Value&0x80)==0)after.nativeActive=false;
            // dmKillChar/2 do not set HP0, revive, turn a commander into a ghost, pay XP,
            // respawn or choose a town destination. Those are separate received outcomes.
            return true;
        }
    }
    public sealed class BattleDeathEffect
    {
        // job here is the resolved runtime+68 appearance key, not a class-change instruction.
        public int unit,job;
        public long life;
        public double age;
        public bool held;
        public bool LargeJob=>job/100==7;
        public int Counter=>LargeJob?0:(int)Math.Min(19,Math.Floor(age/OriginalBattleFeedback.CounterMilliseconds));
        public bool Expired=>LargeJob?age>=OriginalBattleFeedback.CounterMilliseconds:Counter>=19;
        public bool Visible=>!held&&!Expired;
        public BattleDeathEffect Copy()=>(BattleDeathEffect)MemberwiseClone();
        public void Advance(double milliseconds,double numericRemaining)
        {
            if(Expired)return;
            double pause=Math.Max(0,numericRemaining);age+=Math.Max(0,milliseconds-pause);held=pause>milliseconds;
        }
        public BattleFeedbackFrame[] Frames(double x,double y)
        {
            if(!Visible)return Array.Empty<BattleFeedbackFrame>();
            // Original corpse rendering is32px even in the initial large-job transition.
            if(unit/100==7&&unit%10==0){x+=16;y+=16;}
            return new[]{new BattleFeedbackFrame{surface=BattleFeedbackSurface.Body,x=x,y=y,atlasX=0,atlasY=(Counter<8?32:Counter%4)*32,width=32,height=32},
                new BattleFeedbackFrame{surface=BattleFeedbackSurface.Effects,x=x,y=y,atlasX=Counter/3*32,atlasY=0,width=31,height=31}};
        }
    }
}
