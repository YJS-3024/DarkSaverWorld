using System;
using System.Globalization;
using System.Linq;

namespace DarkSaver.Original.World
{
    public enum BattleFeedbackKind { Health, Mana, Miss, Attack }
    // Explicit display events. A full unit snapshot is not an HP-display or attack packet.
    public sealed class BattleFeedbackEvent
    {
        public BattleFeedbackKind kind;
        public int unit,target=-1,value;
        public long life,targetLife;
        public BattleFeedbackEvent Copy()=>(BattleFeedbackEvent)MemberwiseClone();
    }
    public sealed class BattleFeedbackNotice
    {
        public string id,character,session;
        public long expectedRevision,unitLife,targetLife;
        public bool approved;
        public byte[] packet;
    }
    public sealed class BattleFeedbackPacket
    {
        public BattleFeedbackKind kind;
        public int unit,target=-1,value;
    }
    public static class OriginalBattleFeedback
    {
        // Presentation conversion only. Native attack/numeric counters advance on update calls;
        // this is not a measured original frame rate, a server/AP timer or a gameplay cooldown.
        public const double CounterMilliseconds=40;
        private static readonly int[] NumberBounce={0,-4,-5,-4,0,1,4,10,4,1,0,1,4,10};
        public static int BounceY(int counter)=>counter>=0&&counter<NumberBounce.Length?NumberBounce[counter]:0;
        private static int Word(byte[] p,int i)=>(short)(p[i]|p[i+1]<<8);
        private static bool Unit(int id)=>id>=0&&id<3000;
        public static byte[] Packet(BattleFeedbackKind kind,int unit,int value,int target=-1)
        {
            if(!Unit(unit)||value<short.MinValue||value>short.MaxValue)return null;
            if(kind==BattleFeedbackKind.Miss)
                return value==0&&target==-1?new[]{(byte)0x5c,(byte)0x0f,(byte)unit,(byte)(unit>>8)}:null;
            if(kind==BattleFeedbackKind.Health||kind==BattleFeedbackKind.Mana)
            {
                if(target!=-1)return null;
                return new[]{(byte)0x5c,(byte)(kind==BattleFeedbackKind.Health?5:6),(byte)unit,(byte)(unit>>8),(byte)value,(byte)(value>>8)};
            }
            if(kind!=BattleFeedbackKind.Attack||!Unit(target)||unit==target)return null;
            // GameCtl5C82 reads target, attacker, raw signed effect flag. It is not damage/MISS.
            return new[]{(byte)0x5c,(byte)0x82,(byte)target,(byte)(target>>8),(byte)unit,(byte)(unit>>8),(byte)value,(byte)(value>>8)};
        }
        public static bool TryDecode(byte[] bytes,out BattleFeedbackPacket packet)
        {
            packet=null;if(bytes==null||bytes.Length<2||bytes[0]!=0x5c)return false;
            if(bytes[1]==0x0f&&bytes.Length==4)
            {
                int unit=Word(bytes,2);if(!Unit(unit))return false;
                packet=new BattleFeedbackPacket{kind=BattleFeedbackKind.Miss,unit=unit};return true;
            }
            if((bytes[1]==5||bytes[1]==6)&&bytes.Length==6)
            {
                int unit=Word(bytes,2);if(!Unit(unit))return false;
                packet=new BattleFeedbackPacket{kind=bytes[1]==5?BattleFeedbackKind.Health:BattleFeedbackKind.Mana,unit=unit,value=Word(bytes,4)};return true;
            }
            if(bytes[1]!=0x82||bytes.Length!=8)return false;
            int source=Word(bytes,4),target=Word(bytes,2);if(!Unit(source)||!Unit(target)||source==target)return false;
            packet=new BattleFeedbackPacket{kind=BattleFeedbackKind.Attack,unit=source,target=target,value=Word(bytes,6)};return true;
        }
        public static bool Valid(BattleFeedbackEvent e,TrainingUnit[] before,TrainingUnit[] after)
        {
            if(e==null||!Enum.IsDefined(typeof(BattleFeedbackKind),e.kind))return false;
            var source=before?.FirstOrDefault(u=>u.id==e.unit&&u.life==e.life);
            var final=after?.FirstOrDefault(u=>u.id==e.unit&&u.life==e.life);
            if(source==null||final==null)return false;
            if(e.kind==BattleFeedbackKind.Attack)
                return e.unit!=e.target&&e.value>=short.MinValue&&e.value<=short.MaxValue&&e.targetLife>0
                    &&before.Any(u=>u.id==e.target&&u.life==e.targetLife)&&after.Any(u=>u.id==e.target&&u.life==e.targetLife);
            if(e.target!=-1||e.targetLife!=0)return false;
            if(e.kind==BattleFeedbackKind.Miss)return e.value==0;
            return e.value>=0&&e.value<=short.MaxValue&&(e.kind==BattleFeedbackKind.Health?e.value==final.hp:e.value==final.mp);
        }
        public static int DigitCell(char c)=>c=='0'?10:c=='+'?11:c=='-'?12:c>='1'&&c<='9'?c-'0':-1;
        public static int Facing(int dx,int dy,int previous)
        {
            // Native148 uses1:left,2:right,3:up,4:down; horizontal assignment wins.
            if(dx!=0)return dx<0?3:2;
            return dy<0?0:dy>0?1:previous;
        }
    }
    // One160/15C/2AC numeric slot per native unit; a newer HP/MP/MISS replaces it.
    public sealed class BattleNumberEffect
    {
        public int unit,delta,hp,maxHp;
        public long life;
        public BattleFeedbackKind kind;
        public double age;
        public int Counter=>(int)Math.Min(27,Math.Floor(age/OriginalBattleFeedback.CounterMilliseconds));
        public bool Expired=>Counter>=27;
        public bool DeltaVisible=>Counter>=1&&Counter<=13;
        public bool HealthVisible=>kind==BattleFeedbackKind.Health&&Counter>=1&&!Expired;
        public int DisplayHealth=>hp-(int)((long)delta*(20-Math.Max(10,Math.Min(20,Counter)))/10);
        public int HealthRow=>DisplayHealth<(long)maxHp*30/100?0:DisplayHealth>(long)maxHp*70/100?2:1;
        public int DeltaRow=>kind==BattleFeedbackKind.Mana?3:delta<0?0:1;
        public string DeltaText=>(delta>0?"+":"")+delta.ToString(CultureInfo.InvariantCulture);
        public int OffsetX=>Counter+10;
        public int OffsetY=>OriginalBattleFeedback.BounceY(Counter)+10;
        public BattleNumberEffect Copy()=>(BattleNumberEffect)MemberwiseClone();
    }
    // AttackChar has one158/280/284/288 slot per actor; preserve both sides separately.
    public sealed class BattleAttackEffect
    {
        public int unit,partner,offsetX,offsetY,direction,flag;
        public long life,partnerLife;
        public bool attacker;
        public double age;
        public int Counter=>(int)Math.Min(49,Math.Floor(age/OriginalBattleFeedback.CounterMilliseconds));
        public bool Expired=>Counter>=49;
        public int Frame=>Counter/2%4;
        public bool ContactVisible=>!Expired&&(!attacker||offsetX!=0||offsetY!=0);
        public bool ExtraVisible=>ContactVisible&&Counter<16&&flag!=0;
        public BattleAttackEffect Copy()=>(BattleAttackEffect)MemberwiseClone();
        public static BattleAttackEffect[] Create(TrainingUnit source,TrainingUnit target,int flag,int targetFlag=0)
        {
            int dx=target.x-source.x,dy=target.y-source.y;
            if(Math.Abs((long)dx)+Math.Abs((long)dy)>=3){dx=0;dy=0;}
            return new[]{new BattleAttackEffect{unit=source.id,life=source.life,partner=target.id,partnerLife=target.life,attacker=true,offsetX=dx*16,offsetY=dy*16,flag=flag,direction=OriginalBattleFeedback.Facing(dx,dy,source.direction)},
                new BattleAttackEffect{unit=target.id,life=target.life,partner=source.id,partnerLife=source.life,offsetX=-dx*16,offsetY=-dy*16,flag=targetFlag,direction=OriginalBattleFeedback.Facing(-dx,-dy,target.direction)}};
        }
    }
}
