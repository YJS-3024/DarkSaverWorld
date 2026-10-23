using System;
namespace DarkSaver.Core
{
    public sealed partial class NativeCharacterProfile
    {
        // Optional legacy state: -1 unknown, 0 explicitly empty, positive = owned item ID.
        public int[] equipmentItemIds;
        public int EquipmentItemAt(int slot) => slot<0 || slot>=7 || equipmentItemIds==null || equipmentItemIds.Length!=7 ? -1 : equipmentItemIds[slot];
        public bool HasEquippedItems {get {for(int i=0;i<7;i++)if(EquipmentItemAt(i)>0)return true;return false;}}
        public bool HasSameEquipment(NativeCharacterProfile other)
        {if(other==null)return false;for(int i=0;i<7;i++)if(EquipmentItemAt(i)!=other.EquipmentItemAt(i))return false;return true;}
        public bool IsEquipmentStateValid
        {
            get
            {
                if(equipmentItemIds==null || equipmentItemIds.Length==0)return true;
                if(equipmentItemIds.Length!=7)return false;
                foreach(int id in equipmentItemIds)if(id< -1)return false;
                return true;
            }
        }
        public int EquippedCount(int itemId)
        {
            int count=0;if(itemId<=0)return 0;
            for(int slot=0;slot<7;slot++)if(EquipmentItemAt(slot)==itemId)count++;
            return count;
        }
        public bool IsEquipmentOwned(NativeInventory inventory)
        {
            if(!IsEquipmentStateValid)return false;
            for(int slot=0;slot<7;slot++)
            {
                int id=EquipmentItemAt(slot);
                if(id>0 && (inventory==null || !inventory.IsValid || inventory.Count(id)<EquippedCount(id)))return false;
            }
            return true;
        }
        public bool TryBuildEquipmentSlot(int slot,int itemId,NativeInventory inventory,out NativeCharacterProfile changed)
        {
            changed=null;if(!IsValid || slot<0 || slot>=7 || itemId<0)return false;
            var copy=Clone();var slots=new int[7];for(int i=0;i<7;i++)slots[i]=EquipmentItemAt(i);
            slots[slot]=itemId;copy.equipmentItemIds=slots;
            if(!copy.IsValid || !copy.IsEquipmentOwned(inventory))return false;
            changed=copy;return true;
        }
    }
}
