using System;
using System.Linq;

namespace DarkSaver.Original.World
{
    public sealed class BattleTickNotice
    {
        public string id,character,session;public long expectedRevision,unitLife;public bool approved;
        public byte[] packet;
    }
    public sealed class BattleTickPacket
    {
        public bool pulse;public int unit,ap,menu;
    }
    public static class OriginalBattleTicks
    {
        public static bool ValidMask(int? mask)=>!mask.HasValue||mask>=0&&mask<=65535;
        public static byte[] PulsePacket()=>new byte[]{0x5c,4};
        public static byte[] MenuPacket(int unit,int ap,int mask)
        {
            if(unit<0||unit>=3000||ap<short.MinValue||ap>short.MaxValue||mask<0||mask>65535)return null;
            return new[]{(byte)0x5c,(byte)0x52,(byte)(unit&255),(byte)(unit>>8),(byte)(ap&255),(byte)((ap>>8)&255),(byte)(mask&255),(byte)(mask>>8)};
        }
        public static bool TryDecode(byte[] packet,out BattleTickPacket result)
        {
            result=null;if(packet==null||packet.Length<2||packet[0]!=0x5c)return false;
            if(packet[1]==4&&packet.Length==2){result=new BattleTickPacket{pulse=true};return true;}
            if(packet[1]!=0x52||packet.Length!=8)return false;
            int unit=packet[2]|packet[3]<<8;if(unit<0||unit>=3000)return false;
            result=new BattleTickPacket{unit=unit,ap=(packet[4]|packet[5]<<8)>=32768?(packet[4]|packet[5]<<8)-65536:packet[4]|packet[5]<<8,menu=packet[6]|packet[7]<<8};return true;
        }
        public static uint CommandFlags(int mask)
        {
            int[] destination={2,16,8,32,1,4,64,128};uint result=0;
            for(int bit=0;bit<8;bit++)if((mask&(1<<bit))!=0)result|=(uint)destination[bit];return result;
        }
        public static bool TryPulse(TrainingUnit[] units,out TrainingUnit[] next)
        {
            next=null;
            if(units==null||units.Any(u=>!u.nativeActive.HasValue||u.nativeActive==true&&!u.nativeEnabled.HasValue))return false;
            next=units.Select(u=>u.Copy()).ToArray();foreach(var u in next)if(u.nativeActive==true&&u.nativeEnabled==true)u.ap=(int)Math.Min((long)u.ap+1,u.maxAp);return true;
        }
        public static bool TryMenu(TrainingUnit unit,int ap,int menu,out TrainingUnit next)
        {
            next=null;if(unit==null||!unit.nativeActive.HasValue||!ValidMask(menu)||ap<short.MinValue||ap>short.MaxValue)return false;
            next=unit.Copy();if(unit.nativeActive==false)return true;
            next.ap=Math.Max(0,Math.Min(ap,unit.maxAp));next.nativeMenuMask=menu;
            if(unit.nativeState.HasValue)next.nativeState=(unit.nativeState.Value&~0x30u)|((menu&1)!=0?0x10u:0)|((menu&16)!=0?0x20u:0);
            return true;
        }
        public static bool Allows(TrainingUnit unit,TrainingAction action)
        {
            if(unit==null)return false;if(!unit.nativeMenuMask.HasValue)return true;
            int bit=action==TrainingAction.Move?16:action==TrainingAction.Attack?1:action==TrainingAction.Rest?8:action==TrainingAction.CastMagic?4:action==TrainingAction.UseSkill?2:action==TrainingAction.UseItem?32:action==TrainingAction.TroopOrder?64:0;
            return bit==0||(unit.nativeMenuMask.Value&bit)!=0;
        }
        public static bool AllowsIcon(TrainingUnit unit,int icon)
        {
            if(unit==null)return false;if(!unit.nativeMenuMask.HasValue)return true;
            int bit=icon==0?16:icon==1?1:icon==2?4:icon==3?32:icon==4?64:icon==5?128:icon==8?8:icon==12?2:0;
            return bit!=0&&(unit.nativeMenuMask.Value&bit)!=0;
        }
    }
}
