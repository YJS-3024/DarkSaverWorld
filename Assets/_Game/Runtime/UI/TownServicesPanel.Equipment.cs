using DarkSaver.Combat;
using DarkSaver.Core;
using DarkSaver.Items;
using UnityEngine;
namespace DarkSaver.UI
{
    public sealed partial class TownServicesPanel
    {
        private int selectedEquipmentSlot=-1;
        private NativeCharacterProfile equipmentBaseline;
        private static readonly string[] EquipmentNames={"투구","갑옷","손(왼쪽)","손(오른쪽)","장신구(왼쪽)","신발","장신구(오른쪽)"};
        // Native scene4 paper-doll cells; explicit local selection/confirm actions below.
        private static readonly Vector2[] EquipmentCells={new Vector2(63,42),new Vector2(63,87),new Vector2(27,104),
            new Vector2(100,104),new Vector2(27,52),new Vector2(63,147),new Vector2(100,52)};
        public bool SelectEquipmentSlot(int slot)
        {
            if(!IsOpen || Service!=TownService.Inventory || !CanOpen || IsInventoryMinimized || slot<0 || slot>=7)return false;
            var session=GetComponent<BattleTransitionController>();if(session==null)return false;
            selectedEquipmentSlot=slot;equipmentBaseline=session.CaptureCharacterProfile();return equipmentBaseline!=null;
        }
        public bool ConfirmSelectedEquipment(bool remove=false,bool all=false)
        {
            if(!IsOpen || Service!=TownService.Inventory || !CanOpen || IsInventoryMinimized)return false;
            var session=GetComponent<BattleTransitionController>();if(session==null)return false;
            int slot=all ? -1 : selectedEquipmentSlot;
            if(!all && slot<0)return false;
            int id=remove ? 0 : SelectedInventoryItem?.Id ?? -1;if(id<0)return false;
            var baseline=equipmentBaseline ?? session.CaptureCharacterProfile();
            bool success=session.TryChangeEquipment(slot,id,baseline,out var reason);SaveMessage=reason;
            if(success)equipmentBaseline=session.CaptureCharacterProfile();return success;
        }
        private void DrawEquipmentSlots(Rect panel)
        {
            var session=GetComponent<BattleTransitionController>();
            for(int slot=0;slot<7;slot++)
            {
                int index=slot,id=session?.EquipmentItemAt(slot)??-1;string key="equipment-slot-"+slot;
                var rect=new Rect(panel.position+EquipmentCells[slot],new Vector2(32,32));
                var item=id>0 && InventoryCatalog!=null && InventoryCatalog.TryGet(id,out var found)?found:null;
                string name=id<0?"미확인":id==0?"비어 있음":item?.Name?.Replace('_',' ')??"미확인 아이템";
                if(item!=null)ui.ItemSlotButton(key,rect,()=>SelectEquipmentSlot(index),ItemIcons.Get(item),1,selectedEquipmentSlot==slot,EquipmentNames[slot]+" / "+name);
                else ui.Button(key,rect,id<0?"?":"",()=>SelectEquipmentSlot(index),session!=null,selectedEquipmentSlot==slot,EquipmentNames[slot]+" / "+name);
                ui.Raise(key);ui.Raise(key+"-icon");
            }
            ui.Panel("equipment-actions",new Rect(panel.x,426,panel.width,30));
            ui.Button("equipment-confirm",new Rect(panel.x+8,427,84,28),"장착 확인",()=>ConfirmSelectedEquipment(),selectedEquipmentSlot>=0 && SelectedInventoryItem!=null);
            ui.Button("equipment-remove",new Rect(panel.x+98,427,84,28),"선택 해제",()=>ConfirmSelectedEquipment(true),selectedEquipmentSlot>=0 && session!=null && session.EquipmentItemAt(selectedEquipmentSlot)!=0);
            ui.Button("equipment-clear",new Rect(panel.x+188,427,84,28),"모두 해제",()=>ConfirmSelectedEquipment(true,true),session!=null);
            ui.Label("equipment-selection",new Rect(panel.x+278,432,123,18),selectedEquipmentSlot<0?"장비 칸을 선택하세요.":EquipmentNames[selectedEquipmentSlot],10);
            foreach(var key in new[]{"equipment-actions","equipment-confirm","equipment-remove","equipment-clear","equipment-selection"})ui.Raise(key);
        }
    }
}
