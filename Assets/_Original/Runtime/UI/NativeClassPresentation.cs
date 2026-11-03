using System;
using System.Globalization;
using DarkSaver.Original.World;
using DarkSaver.Original.Tutorial;
namespace DarkSaver.Original.UI
{
    public readonly struct NativeClassStatBar
    {
        public readonly int Filled,Baseline;
        public NativeClassStatBar(int filled,int baseline){Filled=filled;Baseline=baseline;}
    }
    public static class NativeClassPresentation
    {
        public const int ChoiceSize=33;
        public static TutorialTile ChoiceOrigin(int slot)
        {switch(slot){case 0:return new TutorialTile(32,228);case 1:return new TutorialTile(167,228);case 2:return new TutorialTile(100,296);default:throw new ArgumentOutOfRangeException(nameof(slot));}}
        //463F00/464150: the colored current-before bar is overlaid white after the opening baseline.
        public static NativeClassStatBar StatBar(AttributeTriplet initial,AttributeTriplet current,int width=176)
        {
            if(initial==null||current==null)return new NativeClassStatBar(0,0);
            int filled=Math.Min(width,MulDiv(current.before,width,current.limit));
            int baseline=Math.Min(filled,Math.Min(width,MulDiv(initial.before,width,current.limit)));
            return new NativeClassStatBar(filled,baseline);
        }
        private static int MulDiv(int number,int numerator,int denominator)
        {
            if(denominator==0)return -1;long product=(long)number*numerator,divisor=Math.Abs((long)denominator);
            long value=(Math.Abs(product)+divisor/2)/divisor;if((product<0)!=(denominator<0))value=-value;
            return value<int.MinValue||value>int.MaxValue?-1:(int)value;
        }
        private static string Number(int value)=>value.ToString(CultureInfo.InvariantCulture);
        public static string StatText(AttributeTriplet value)=>value==null?"—":"장착전:"+Number(value.before)+"/장착후:"+Number(value.after)+"/한계치:"+Number(value.limit);
        //4635C0/463720 use the received record coordinates directly, without a300ms phase offset.
        public static TutorialTile MagicIconOrigin(MagicMenuEntry value)=>value==null?new TutorialTile(-1,-1):new TutorialTile(value.iconX,value.iconY);
        public static string MagicKind(MagicMenuEntry value)=>value==null?"":"마법 종류: "+(value.team==34?"공격 계열":"보조 계열");
        public static string MagicDetails(MagicMenuEntry value)=>value==null?"":(value.mp>0?"마법력 소모: "+Number(value.mp)+" ":"")+(value.distance>0?"거리: "+Number(value.distance)+" ":"")+(value.range>0?"범위: "+Number(value.range)+" ":"");
        //462C50 formats the second received level WORD and the low signed XP DWORD.
        public static string Level(CommanderProfile value)=>value?.growth?.nativeLevels?.Length==2?Number(value.growth.nativeLevels[1]):value?.level?.ToString(CultureInfo.InvariantCulture)??"—";
        public static string Experience(CommanderProfile value)=>value?.growth==null?"—":Number(unchecked((int)value.growth.experience));
    }
    [Serializable] public sealed class NativeClassLabel {public int job;public string name,description;}
    [Serializable] public sealed class NativeClassLabelCatalog
    {
        public NativeClassLabel[] entries;
        public NativeClassLabel Find(int job){if(entries!=null)foreach(var value in entries)if(value.job==job)return value;return null;}
    }
}
