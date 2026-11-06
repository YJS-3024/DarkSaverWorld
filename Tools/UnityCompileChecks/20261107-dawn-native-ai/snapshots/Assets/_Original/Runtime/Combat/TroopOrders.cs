using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;

namespace DarkSaver.Original.World
{
    public enum TroopOrderChoice { Direct=1,AutomaticAttack=2,AutomaticDefense=3 }
    public static class OriginalTroopOrders
    {
        public static bool ValidChoice(int choice)=>choice>=1&&choice<=3;
        public static int NativeMode(int choice)=>choice==1?1:choice==2?2:choice==3?8:0;
        // Native setsolai also has an internal4 branch; its meaning is not invented or exposed as a fourth menu choice.
        public static bool ValidMode(int? mode)=>!mode.HasValue||mode==1||mode==2||mode==4||mode==8;
        public static string Name(int choice)=>choice==1?"직접 명령":choice==2?"자동 공격":choice==3?"자동 방어":"";
        public static int IconX(int choice)=>(choice-1)*32;
        public static int MenuX(int choice)=>choice==1?32:choice==2?8:56;
        public static int MenuY(int choice)=>choice==1?0:64;
        public static BattleLifeRecord[] KnownMembers(TrainingUnit[] units,int commander)
        {
            int first=commander/10*10;
            // setsolai scans first+1 through first+9. This differs from Unit8 magic's slots0..8.
            return units.Where(u=>u.id>first&&u.id<first+10).OrderBy(u=>u.id).Select(u=>new BattleLifeRecord{id=u.id,life=u.life}).ToArray();
        }
        public static bool SameExceptOrder(TrainingUnit before,TrainingUnit after)
        {
            var a=before.Copy();var b=after.Copy();a.troopOrder=b.troopOrder=null;
            var serializer=new DataContractJsonSerializer(typeof(TrainingUnit));
            using(var x=new MemoryStream())using(var y=new MemoryStream())
            {serializer.WriteObject(x,a);serializer.WriteObject(y,b);return x.ToArray().SequenceEqual(y.ToArray());}
        }
    }
}
