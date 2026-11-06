using System;
using System.Linq;
using DarkSaver.Original.Tutorial;

namespace DarkSaver.Original.World
{
    // Shared CheckMagicTarget geometry:5C68 uses E4C0 mode2;
    // mode1 event MAGIC uses E4C0 mode1. Neither path is a local damage result.
    // Eligibility is supplied separately from geometry; no costs or damage here.
    public static class OriginalMagicRedirect
    {
        public static bool TryDecode(byte[] bytes,out int magic,out int source,out int x,out int y)
        {
            magic=source=x=y=0;
            if(bytes==null||bytes.Length!=10||bytes[0]!=0x5c||bytes[1]!=0x68)return false;
            int Word(int at)=>(short)(bytes[at]|bytes[at+1]<<8);
            magic=Word(2);source=Word(4);x=Word(6);y=Word(8);return true;
        }
        public static byte[] Packet(int magic,int source,int x,int y)
        {
            var values=new[]{magic,source,x,y};if(values.Any(v=>v<short.MinValue||v>short.MaxValue))return null;
            var bytes=new byte[10];bytes[0]=0x5c;bytes[1]=0x68;
            for(int i=0;i<values.Length;i++){bytes[2+i*2]=(byte)values[i];bytes[3+i*2]=(byte)(values[i]>>8);}return bytes;
        }
        public static NativeMagicPacket Select(MagicMenuEntry metadata,int source,int x,int y,TrainingUnit caster,TrainingUnit[] units,int[] eligible,int? gridUnit=null,int mode=2)
        {
            if(metadata==null||units==null||mode<1||mode>2)return null;int[] targets;
            if(metadata.unit==8)
            {
                // The caller can supply the actual native mainmap occupant. The
                // fallback is for an explicitly supplied standalone grid fixture.
                var selected=gridUnit.HasValue?units.FirstOrDefault(u=>u.id==gridUnit.Value):units.FirstOrDefault(u=>u.x==x&&u.y==y);if(selected==null)return null;
                int first=selected.id/10*10;
                targets=units.Where(u=>u.id>=first&&u.id<first+9&&u.nativeActive==true&&u.nativeEnabled==true).OrderBy(u=>u.id).Select(u=>u.id).ToArray();
                x=y=-1; // A present group with no enabled members still succeeds.
            }
            else
            {
                if(eligible==null||metadata.distance<=0&&caster==null)return null;
                var center=caster?.Tile??new TutorialTile(x,y);
                targets=units.Where(u=>u.nativeActive==true&&u.nativeEnabled==true&&eligible.Contains(u.id)
                    &&BattleSkillArea.Weight(metadata,center,x,y,u.x,u.y)>0).OrderBy(u=>u.x).ThenBy(u=>u.y).Select(u=>u.id).ToArray();
                if(targets.Length==0)return null;
            }
            return new NativeMagicPacket{magic=metadata.id,source=source,x=x,y=y,mode=mode,signature=OriginalNativeMagic.Signature(metadata),targets=targets};
        }
    }
}
