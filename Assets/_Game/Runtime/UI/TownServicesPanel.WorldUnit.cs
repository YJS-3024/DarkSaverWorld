using System;
using DarkSaver.Network;
using DarkSaver.Core;
using DarkSaver.Combat;
using UnityEngine;
namespace DarkSaver.UI
{
    public sealed partial class TownServicesPanel
    {
        private WorldRosterEntry inspectedWorldUnit;
        private CharacterAdjustmentValuesNotification worldStatCosts;
        private byte[] worldAdjustmentBaseline;
        private Notify_WorldInventory.Item[] originalWorldInventory;
        public ushort? InspectedWorldRecordId=>inspectedWorldUnit?.RecordId;
        public bool BindWorldUnit(WorldRosterEntry unit,Notify_WorldInventory.Item[] inventory=null)
        {
            if(!IsOpen || Service!=TownService.CharacterInfo && Service!=TownService.Inventory || unit==null)return false;
            bool preserveAdjustment=inspectedWorldUnit?.RecordId==unit.RecordId && IsCharacterAdjustmentOpen;
            inspectedWorldUnit=unit.Clone();originalWorldInventory=inventory==null?null:Array.ConvertAll(inventory,i=>i.Clone());
            ui?.CancelItemDrags();if(!preserveAdjustment){CloseCharacterAdjustment();worldStatCosts=null;worldAdjustmentBaseline=null;}selectedInventoryId=selectedEquipmentSlot=-1;
            if(Service==TownService.CharacterInfo)
            {
                ClearCharacterInfoValues();
                if(unit.CharacterInfoPayload!=null)BindCharacterInfoValues(unit.CharacterInfoPayload);
                else
                {
                    characterInfoValues=new string[16];characterInfoValues[0]=unit.MaxHp?.ToString();characterInfoValues[1]=unit.MaxMp?.ToString();
                    characterInfoValues[2]=unit.Attack?.ToString();characterInfoValues[3]=unit.Defense?.ToString();
                    CharacterInfoLevelText=unit.Profile?.hasLevels==true?unit.Profile.jobLevel+"("+unit.Profile.baseLevel+")":null;
                    CharacterInfoExperienceText=unit.Experience?.ToString();
                }
            }
            if(preserveAdjustment && unit.CharacterInfoPayload!=null){BindCharacterAdjustmentMagicSnapshot(unit.CharacterInfoPayload);BindCharacterAdjustmentStatBars(unit.CharacterInfoPayload,worldAdjustmentBaseline ?? unit.CharacterInfoPayload);}
            return true;
        }
        public bool BindWorldStatCosts(CharacterAdjustmentValuesNotification costs)
        {if(!CanInspectAdjustmentMagic || inspectedWorldUnit==null)return false;worldStatCosts=costs;return true;}
        private bool CanRequestWorldStat(int stat,CharacterStatAdjustmentDirection direction)
            =>CanInspectAdjustmentMagic && worldStatCosts!=null && worldStatCosts.TryGetValue(stat,out short cost)
                && OriginalCharacterInfoValues.TryPreviewStatAdjustment(inspectedWorldUnit.CharacterInfoPayload,worldAdjustmentBaseline,cost,stat,direction,out _,out _);
        public bool RequestWorldMagicArrow(ushort action)
        {
            if(inspectedWorldUnit==null || action<1 || action>2 || !TryGetCharacterAdjustmentMagic(CharacterAdjustmentSelectedMagicList,CharacterAdjustmentSelectedMagicIndex,out short id))return false;
            return SendWorldUnitRequest(new CharacterMagicSelectionRequest(action,inspectedWorldUnit.RecordId,unchecked((ushort)id)));
        }
        public bool RequestWorldAdjustmentCommand(CharacterAdjustmentCommand command)
            =>inspectedWorldUnit!=null && SendWorldUnitRequest(new CharacterAdjustmentCommandRequest(command,inspectedWorldUnit.RecordId));
        private bool SendWorldUnitRequest(ClientPacket request)
        {
            if(!IsOpen || !CanOpen || inspectedWorldUnit==null)return false;
            var server=GetComponent<ServerManager>();if(server==null)return false;
            if(server.LocalServer.HasHandler(request))
            {bool accepted=server.TryRequest(request,out var response);SaveMessage=response?.Reason;if(!accepted)GetComponent<NativeWorldWindows>()?.ReportOriginalUiError(SaveMessage);return accepted;}
            if(server.SendToServer(request))return true;
            SaveMessage="요청을 처리할 서버가 연결되지 않았습니다.";GetComponent<NativeWorldWindows>()?.ReportOriginalUiError(SaveMessage);return false;
        }
        public bool RequestOriginalEquipment(int slot,int itemId,ushort additionalValue)
        {
            if(!IsOpen || !CanOpen || Service!=TownService.Inventory || IsInventoryMinimized || inspectedWorldUnit==null
                || slot<0 || slot>6 || itemId>short.MaxValue || itemId< -1)return false;
            int previous=WorldEquipmentItemAt(slot);if(previous<0 || previous>short.MaxValue || itemId<=0 && previous==0)return false;
            var item=itemId>0 && InventoryCatalog!=null && InventoryCatalog.TryGet(itemId,out var found)?found:null;
            if(itemId>0 && (item==null || !DarkSaver.Items.ItemUsageConditions.TryGetEquipmentSlots(item,out int mask,out _) || (mask&(1<<slot))==0))return false;
            ushort rawItem=itemId<=0?ushort.MaxValue:(ushort)itemId;
            ushort rawPrevious=previous==0?ushort.MaxValue:(ushort)previous;
            return SendWorldUnitRequest(new EquipmentRequest(inspectedWorldUnit.RecordId,(ushort)slot,rawItem,additionalValue,rawPrevious));
        }
        private string WorldWeaponName
        {
            get
            {
                int first=inspectedWorldUnit?.Profile?.EquipmentItemAt(2) ?? -1,second=inspectedWorldUnit?.Profile?.EquipmentItemAt(3) ?? -1;
                if(first==0 && second==0)return "비무장";
                string Name(int id)=>id>0 && InventoryCatalog!=null && InventoryCatalog.TryGet(id,out var item)?item.Name.Replace('_',' '):null;
                return Name(first) ?? Name(second) ?? "?";
            }
        }
        private int WorldEquipmentItemAt(int slot)=>inspectedWorldUnit!=null ? inspectedWorldUnit.Profile?.EquipmentItemAt(slot) ?? -1 : GetComponent<BattleTransitionController>()?.EquipmentItemAt(slot) ?? -1;
        private bool EquipDroppedItem(int itemId,int slot)
        {
            var record=originalWorldInventory==null?null:Array.Find(originalWorldInventory,i=>i.ItemId==itemId);
            if(record?.OriginalAdditionalValue==null){SaveMessage="장비 상세 정보를 기다리고 있습니다.";GetComponent<NativeWorldWindows>()?.ReportOriginalUiError(SaveMessage);return false;}
            return RequestOriginalEquipment(slot,itemId,record.OriginalAdditionalValue.Value);
        }
    }
}
