using System;
using System.Linq;
using System.Runtime.Serialization;

namespace DarkSaver.Original.World
{
    [Serializable] public sealed class BattleItemLink { public int item,magic,rawDurability; }
    [Serializable] public sealed class BattleItemCatalog
    {public BattleItemLink[] entries;public BattleItemLink Find(int item)=>entries?.FirstOrDefault(e=>e.item==item);}
    [Serializable,DataContract] public sealed class BattleItemRule
    {
        [DataMember] public int item;[DataMember] public bool enabled;
        [DataMember(EmitDefaultValue=false)] public int? mpCost,apCost,menuSlot;
        [DataMember(EmitDefaultValue=false)] public int[] targets;
        [DataMember(EmitDefaultValue=false)] public bool? allowDeadTargets;
        public BattleItemRule Copy(){var r=(BattleItemRule)MemberwiseClone();r.targets=targets==null?null:(int[])targets.Clone();return r;}
        public BattleMagicRule ForMagic(int id)=>new BattleMagicRule{magic=id,enabled=enabled,mpCost=mpCost,apCost=apCost,targets=targets==null?null:(int[])targets.Clone(),allowDeadTargets=allowDeadTargets};
        public bool Valid=>item>=0&&(!menuSlot.HasValue||menuSlot>=0&&menuSlot<9)&&ForMagic(1).Valid;
        public static bool ValidRules(BattleItemRule[] rules)=>rules==null||rules.All(r=>r?.Valid==true)&&rules.Select(r=>r.item).Distinct().Count()==rules.Length&&rules.Where(r=>r.menuSlot.HasValue).Select(r=>r.menuSlot.Value).Distinct().Count()==rules.Count(r=>r.menuSlot.HasValue);
    }
}
