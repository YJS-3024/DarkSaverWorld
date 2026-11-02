using System.Globalization;
using DarkSaver.Original.World;
namespace DarkSaver.Original.UI
{
    public static class NativeSelfInformation
    {
        // Native general-memory-record offsets, including its4-byte vptr. This is not a wire packet layout.
        private static readonly int[] offsets={0x1a,0x20,0x26,0x2c,0x2bc,0x2be,0x32,0x38,0x2c0,0x2c2,0x2c4,0x2c6,0x2c8,0x2ca,0x2cc,0x2ce};
        public const int Count=16;
        public static string Gender(int? value)=>value==0?"중성":value==1?"여":value==2?"남":"—";
        public static int X(int index)=>index<8?157:260;
        public static int Y(int index)=>146+index%8*14;
        public static int Width(int index)=>index<8?57:58;
        public static short[] FromMemoryRecord(byte[] record)
        {
            if(record?.Length!=732)return null;var values=new short[Count];
            for(int i=0;i<values.Length;i++){int at=offsets[i];values[i]=unchecked((short)(record[at]|record[at+1]<<8));}return values;
        }
        private static string Number(int? value)=>value?.ToString(CultureInfo.InvariantCulture)??"—";
        public static string[] Values(CommanderProfile profile)
        {
            var values=new string[Count];for(int i=0;i<Count;i++)values[i]="—";
            var stats=profile?.growth?.stats;
            if(stats?.nativeValues?.Length==Count){for(int i=0;i<Count;i++)values[i]=Number(stats.nativeValues[i]);return values;}
            // Known effective totals are distinct from the current wounded HP/MP. Do not create missing ratings.
            if(profile?.healthKnown==true){values[0]=Number(profile.maxHp);values[1]=Number(profile.maxMp);}
            values[2]=Number(stats?.attack);values[3]=Number(stats?.defense);values[6]=Number(stats?.magicAttack);values[7]=Number(stats?.magicDefense);return values;
        }
        public static short[] LevelsFromMemoryRecord(byte[] record)=>record?.Length==732?new[]{unchecked((short)(record[14]|record[15]<<8)),unchecked((short)(record[12]|record[13]<<8))}:null;
        public static uint[] ExperienceFromMemoryRecord(byte[] record)
        {if(record?.Length!=732)return null;uint Read(int at)=>unchecked((uint)(record[at]|record[at+1]<<8|record[at+2]<<16|record[at+3]<<24));return new[]{Read(0x10),Read(0x14)};}
        public static string Level(CommanderProfile profile)=>profile?.growth?.nativeLevels?.Length==2?Number(profile.growth.nativeLevels[0])+"("+Number(profile.growth.nativeLevels[1])+")":Number(profile?.level);
        public static string Experience(CommanderProfile profile)=>profile?.growth==null?"—":profile.growth.experience.ToString(CultureInfo.InvariantCulture)+"/"+(profile.growth.nextExperience?.ToString(CultureInfo.InvariantCulture)??"—");
    }
}
