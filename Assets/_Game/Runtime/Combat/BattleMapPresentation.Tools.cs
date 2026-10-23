using System;
using System.Collections.Generic;
using DarkSaver.Items;
using UnityEngine;

namespace DarkSaver.Combat
{
    // UI input only, not a recovered inventory/stacking rule or original packet payload.
    public readonly struct BattleToolDisplayEntry
    {
        public readonly int ItemId;
        public readonly int Count;
        public BattleToolDisplayEntry(int itemId, int count) { ItemId = itemId; Count = count; }
    }

    public sealed partial class BattleMapPresentation
    {
        private readonly Dictionary<short, (ItemCatalog Catalog, IReadOnlyList<BattleToolDisplayEntry> Rows)> toolDisplays = new();

        private BattleTransitionController toolSession;
        private DarkSaver.Core.NativeInventory toolInventoryBaseline;
        private int? pendingRecoveryItem;
        public bool IsToolTargeting => pendingRecoveryItem.HasValue;

        public bool TryBindSessionToolDisplay(BattleTransitionController session)
        {
            if(!IsBound || session==null || !session.isActiveAndEnabled || !battle.TryGetUnit(100,out _))return false;
            var inventory=session.CaptureInventory();
            if(inventory==null || !inventory.IsValid)return false;
            var catalog=session.GetComponent<DarkSaver.UI.ItemCatalogPanel>()?.Catalog;
            var rows=new List<BattleToolDisplayEntry>();
            foreach(var stack in inventory.items)
            {
                if(catalog==null || !catalog.TryGet(stack.itemId,out var item))return false;
                // GameCtl 0x10009399..0x100093A2: itemrecord+0x0C must be type 7.
                if(item.GetNumericValue(3)==7)rows.Add(new BattleToolDisplayEntry(stack.itemId,stack.quantity));
            }
            if(!ConfigureToolDisplay(catalog,100,rows))return false;
            toolSession=session;toolInventoryBaseline=inventory;return true;
        }

        public bool ConfigureToolDisplay(ItemCatalog catalog, short unitId, IReadOnlyList<BattleToolDisplayEntry> rows)
        {
            if (!isActiveAndEnabled || !IsBound || rows == null || !battle.TryGetUnit(unitId, out var unit) || unit.Enemy) return false;
            var copy = new BattleToolDisplayEntry[rows.Count];
            for (int i = 0; i < copy.Length; i++)
            {
                var row = rows[i];
                if (row.ItemId < 0 || row.Count <= 0 || catalog == null || !catalog.TryGet(row.ItemId, out _)) return false;
                copy[i] = row; // Preserve explicit rows; do not infer slots or combine duplicate item IDs.
            }
            toolDisplays[unitId] = (catalog, Array.AsReadOnly(copy));
            return true;
        }

        public bool TryGetToolDisplay(short unitId, out IReadOnlyList<BattleToolDisplayEntry> rows)
        {
            rows = Array.Empty<BattleToolDisplayEntry>();
            if (!isActiveAndEnabled || !IsBound || !battle.TryGetUnit(unitId, out var unit) || unit.Enemy
                || !toolDisplays.TryGetValue(unitId, out var display)) return false;
            rows = display.Rows; return true;
        }

        public bool TryGetToolDescription(short unitId, int rowIndex, out string description)
        {
            description = "";
            if (!TryGetToolDisplay(unitId, out var rows) || rowIndex < 0 || rowIndex >= rows.Count) return false;
            var catalog = toolDisplays[unitId].Catalog;
            if (catalog == null || !catalog.TryGet(rows[rowIndex].ItemId, out var item)) return false;
            // Source text only: no inferred use effects, numeric columns, or ownership rules.
            description = item.Description ?? "";
            return true;
        }

        private bool CanChooseRecoveryTool(ItemCatalogEntry item)
        {
            var magic=toolSession!=null ? toolSession.GetComponent<DarkSaver.UI.MagicCatalogPanel>()?.Catalog : null;
            return toolSession!=null && toolSession.OwnsActiveBattle(battle)
                && battle.TemporaryLocalRulesEnabled && battle.TryGetSelectedUnit(out var actor) && actor.Id==100 && actor.Alive
                && actor.Ap==actor.MaxAp && !battle.IsMovementInProgress(100)
                && (SinglePlayerItemRecoveryRules.TryGet(item,magic,out _,out _,out _,out _)
                    || SinglePlayerItemMagicRules.TryGet(item,magic,out _));
        }
        public bool TrySelectRecoveryTool(int rowIndex,int expectedItemId)
        {
            if(!IsBattleWindowShellOpen || shellWindowId!=ToolsShell || !battle.TryGetSelectedUnit(out var actor) || actor.Id!=100
                || !TryGetToolDisplay(100,out var rows) || rowIndex<0 || rowIndex>=rows.Count || rows[rowIndex].ItemId!=expectedItemId
                || !toolDisplays[100].Catalog.TryGet(expectedItemId,out var item) || !CanChooseRecoveryTool(item))return false;
            CancelMagicTargeting();CloseSystemMenu();pendingRecoveryItem=expectedItemId;
            LastInputMessage="사용품의 효과에 맞는 대상을 선택하세요. Esc/우클릭: 취소.";return true;
        }
        public bool TryUseSelectedRecoveryTool(short targetId)
        {
            if(!isActiveAndEnabled || !IsBound || !pendingRecoveryItem.HasValue || toolSession==null
                || !battle.TryGetSelectedUnit(out var actor) || actor.Id!=100)return false;
            if(!toolSession.TryUseBattleRecoveryItem(battle,pendingRecoveryItem.Value,targetId,toolInventoryBaseline,out var reason))
            {LastInputMessage=reason;return false;}
            pendingRecoveryItem=null;TryBindSessionToolDisplay(toolSession);LastInputMessage=reason;return true;
        }

        private int toolDisplayOffset;
        public int ToolDisplayOffset => toolDisplayOffset;
        public bool TryPageToolDisplay(bool forward)
        {
            if (!IsBattleWindowShellOpen || shellWindowId != ToolsShell || !battle.TryGetSelectedUnit(out var unit)
                || !TryGetToolDisplay(unit.Id, out var rows)) return false;
            int next = forward ? Math.Min(toolDisplayOffset + 6, Math.Max(0,rows.Count-6)) : Math.Max(0,toolDisplayOffset-6);
            if (next == toolDisplayOffset) return false;
            toolDisplayOffset = next; return true;
        }
        private void DrawToolDisplay(Rect rect)
        {
            if (!battle.TryGetSelectedUnit(out var unit) || !TryGetToolDisplay(unit.Id,out var rows) || rows.Count==0)
            {
                ui.BattleLabel("tool-status",new Rect(rect.x-72,rect.y+32,240,48),
                    rowsKnown() ? "사용할 수 있는 물건이 없습니다." : "소지품 정보를 받지 못했습니다.",13);
                ui.Raise("tool-status");return;
            }
            bool rowsKnown() => battle.TryGetSelectedUnit(out var selected) && TryGetToolDisplay(selected.Id,out _);
            toolDisplayOffset=Math.Min(toolDisplayOffset,Math.Max(0,rows.Count-6));
            var display=toolDisplays[unit.Id];
            // GameCtl 0x10009300: three top and three bottom cells; middle sides page by six.
            for(int slot=0;slot<6;slot++)
            {
                int index=toolDisplayOffset+slot;if(index>=rows.Count)break;
                var row=rows[index];display.Catalog.TryGet(row.ItemId,out var item);
                string caption=(item.Name??"?").Replace('_',' ')+" ×"+row.Count;
                TryGetToolDescription(unit.Id,index,out var description);
                var cell=new Rect(rect.x+(slot%3)*32,rect.y+(slot<3?0:64),32,32);
                bool usable=CanChooseRecoveryTool(item);int expected=row.ItemId;
                ui.ActionButton("tool-row-"+slot,cell,caption,()=>TrySelectRecoveryTool(index,expected),usable,caption+"\n"+description,ItemIcons.Get(item),dimDisabledIcon:false);
                if(row.Count>1)ui.Label("tool-count-"+slot,new Rect(cell.x,cell.yMax-12,32,12),row.Count.ToString(),10,alignment:TextAnchor.MiddleRight);
                ui.Raise("tool-row-"+slot);ui.Raise("tool-row-"+slot+"-icon");
                if(row.Count>1)ui.Raise("tool-count-"+slot);
            }
            if(toolDisplayOffset>0)ui.ActionButton("tool-prev",new Rect(rect.x,rect.y+32,32,32),"뒤로",()=>TryPageToolDisplay(false),toolDisplayOffset>0,
                "이전 사용품",DarkSaver.UI.BattleIconFrames.Magic(3,1,Time.unscaledTimeAsDouble));
            if(toolDisplayOffset+6<rows.Count)ui.ActionButton("tool-next",new Rect(rect.x+64,rect.y+32,32,32),"앞으로",()=>TryPageToolDisplay(true),toolDisplayOffset+6<rows.Count,
                "다음 사용품",DarkSaver.UI.BattleIconFrames.Magic(3,2,Time.unscaledTimeAsDouble));
            if(toolDisplayOffset>0){ui.Raise("tool-prev");ui.Raise("tool-prev-icon");}
            if(toolDisplayOffset+6<rows.Count){ui.Raise("tool-next");ui.Raise("tool-next-icon");}
        }
    }
}
