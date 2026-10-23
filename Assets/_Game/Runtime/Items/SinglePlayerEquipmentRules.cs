using System;
using DarkSaver.Core;
using DarkSaver.Jobs;
namespace DarkSaver.Items
{
    // Native slot/troop filters. Base-level requirement and additive bonuses are explicit local policy.
    public static class SinglePlayerEquipmentRules
    {
        public static bool IsEligible(NativeCharacterProfile profile,JobCatalogEntry job,ItemCatalog items,int slot,ItemCatalogEntry item)
        {
            if(profile==null || job==null || job.Id!=profile.jobId || item==null || slot<0 || slot>=7)return false;
            long troop=job.GetNumericValue(22),required=item.GetNumericValue(4);
            if(job.GetNumericValue(23)!=0 || troop<0 || troop>=10 || required<0 || required>int.MaxValue
                || (required>0 && (!profile.hasLevels || profile.baseLevel<required))
                || !ItemUsageConditions.TryGetPermission(item,(int)troop,out bool permitted) || !permitted)return false;
            bool oppositeKnown=true;ItemCatalogEntry opposite=null;
            int other=slot==2 ? 3 : slot==3 ? 2 : -1;
            if(other>=0)
            {
                int id=profile.EquipmentItemAt(other);oppositeKnown=id>=0;
                if(id>0 && (items==null || !items.TryGet(id,out opposite)))oppositeKnown=false;
            }
            return ItemUsageConditions.TryGetEquipmentPositionPermission(item,slot,oppositeKnown,opposite,out bool allowed) && allowed;
        }
        public static bool TryResolve(NativeCharacterProfile profile,JobCatalogEntry job,ItemCatalog items,
            out (int Hp,int Mp,int Attack,int Defense,int MagicAttack,int MagicDefense) stats)
        {
            stats=default;
            if(profile==null || !profile.IsValid || job==null || job.Id!=profile.jobId || job.CategoryLabel!="player_job")return false;
            int[] source={2,4,6,8,10,12},columns={19,23,20,21,24,25};var values=new long[6];
            for(int i=0;i<6;i++){values[i]=job.GetNumericValue(source[i]);if(values[i]<(i==0?1:0) || values[i]>int.MaxValue)return false;}
            try
            {
                values[2]+=profile.allocatedAttack;values[3]+=profile.allocatedDefense;
                values[4]+=profile.allocatedMagicAttack;values[5]+=profile.allocatedMagicDefense;
                for(int slot=0;slot<7;slot++)
                {
                    int id=profile.EquipmentItemAt(slot);if(id<=0)continue;
                    if(items==null || !items.TryGet(id,out var item) || !IsEligible(profile,job,items,slot,item))return false;
                    for(int i=0;i<6;i++)values[i]=checked(values[i]+item.GetNumericValue(columns[i]));
                }
            }
            catch(OverflowException){return false;}
            for(int i=0;i<6;i++){values[i]=Math.Max(i==0?1:0,values[i]);if(values[i]>int.MaxValue)return false;}
            stats=((int)values[0],(int)values[1],(int)values[2],(int)values[3],(int)values[4],(int)values[5]);return true;
        }
    }
}
