using System;
using System.Linq;
namespace DarkSaver.Original.World
{
    // Native commander+44/+46:16 learnable spells; +6E/+70:8 currently owned spells.
    // +42 is the displayed magic-point value, not a generic UI-ready flag.
    public sealed class NativeClassMagicState
    {
        public int[] available,owned;public int? points;
        public NativeClassMagicState Copy()=>new NativeClassMagicState{available=available==null?null:(int[])available.Clone(),owned=owned==null?null:(int[])owned.Clone(),points=points};
        public bool Valid=>available!=null&&available.Length<=16&&owned!=null&&owned.Length<=8&&available.Concat(owned).All(id=>id>0&&id<65535)&&(!points.HasValue||points>=short.MinValue&&points<=short.MaxValue);
        public bool CanButton(int list,int index)=>points.HasValue&&points!=0&&Index(list,index);
        public bool CanDrag(int list,int index)=>Index(list,index)&&(list==0?owned.Length<8:available.Length<16);
        private bool Index(int list,int index)=>Valid&&(list==0||list==1)&&index>=0&&index<(list==0?available.Length:owned.Length);
        public int Spell(int list,int index)=>Index(list,index)?(list==0?available:owned)[index]:-1;
        public void PredictButton()=>points=0;
        public void PredictDrag(int list,int index)
        {
            // The original83 only decrements its visible source count;85 compacts the owned list.
            // These predictions never grant/forget a spell in the saved player record.
            if(list==0)available=available.Take(available.Length-1).ToArray();else owned=owned.Where((_,i)=>i!=index).ToArray();
        }
    }
    public sealed class NativeClassMagicRequest
    {
        public string id,token,character,session;public int map,record,magic,list,index,mode;public long revision;public bool drag;public byte[] packet;
        public NativeClassMagicRequest Copy(){var r=(NativeClassMagicRequest)MemberwiseClone();r.packet=packet==null?null:(byte[])packet.Clone();return r;}
    }
    public static class NativeClassMagicDrop
    {
        public static bool Allowed(NativeClassMagicState state,int sourceList,int index,int x,int y)=>state?.CanDrag(sourceList,index)==true&&Contains(sourceList,x,y);
        public static bool Contains(int sourceList,int x,int y)
        {
            int count=sourceList==0?8:sourceList==1?16:0;
            for(int i=0;i<count;i++){int left=208+i%8*33,top=(sourceList==0?121:47)+i/8*33;if(x>=left&&x<left+32&&y>=top&&y<top+32)return true;}return false;
        }
    }
}
