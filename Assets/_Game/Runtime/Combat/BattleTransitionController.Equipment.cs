using DarkSaver.Core;
using DarkSaver.Items;
using DarkSaver.Jobs;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DarkSaver.Combat
{
    public sealed partial class BattleTransitionController
    {
        public int EquipmentItemAt(int slot)=>character?.EquipmentItemAt(slot) ?? -1;
        private JobCatalogEntry FindEquipmentJob(int id)
        {
            JobCatalogEntry result=null;
            foreach(var catalog in formationCatalogs)
                if(catalog!=null && catalog.TryGet(id,out var row)){if(result!=null)return null;result=row;}
            return result;
        }
        public bool TryChangeEquipment(int slot,int itemId,NativeCharacterProfile baseline,out string reason)
{reason=DarkSaver.Core.OriginalRestorationPolicy.PendingServerRule;return false;}        public string KnownEquipmentWeaponName()
        {
            if(character==null)return null;int first=character.EquipmentItemAt(2),second=character.EquipmentItemAt(3);
            if(first<0 && second<0)return null;
            var items=GetComponent<DarkSaver.UI.ItemCatalogPanel>()?.Catalog;
            string Name(int id)=>id>0 && items!=null && items.TryGet(id,out var item)?(item.Name??"?").Replace('_',' '):null;
            string a=Name(first),b=Name(second);
            if(a!=null && b!=null)return a+" / "+b;
            if(a!=null || b!=null)return (a??b)+((first<0 || second<0)?" / 일부 미확인":"");
            return first==0 && second==0?"비무장":null;
        }
    }
}
