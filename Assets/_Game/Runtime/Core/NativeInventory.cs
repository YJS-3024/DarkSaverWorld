using System;
using System.Collections.Generic;

namespace DarkSaver.Core
{
    [Serializable] public sealed class NativeItemStack
    {
        public int itemId, quantity;
    }

    [Serializable] public sealed class NativeInventory
    {
        public long gold;
        public NativeItemStack[] items = Array.Empty<NativeItemStack>();
        public bool IsValid
        {
            get
            {
                if (gold < 0 || items == null || items.Length > 10000) return false;
                var seen = new HashSet<int>();
                foreach (var item in items)
                    if (item == null || item.itemId <= 0 || item.quantity <= 0 || !seen.Add(item.itemId)) return false;
                return true;
            }
        }
        public NativeInventory Clone()
        {
            var copy = new NativeInventory {gold = gold, items = items == null ? null : new NativeItemStack[items.Length]};
            if (items != null) for (int i = 0; i < items.Length; i++)
                copy.items[i] = items[i] == null ? null : new NativeItemStack {itemId = items[i].itemId, quantity = items[i].quantity};
            return copy;
        }
        public int Count(int id) { foreach (var item in items) if (item.itemId == id) return item.quantity; return 0; }

        public bool TryConsume(int itemId, int quantity, out NativeInventory changed)
        {
            changed=null;
            if(!IsValid || itemId<=0 || quantity<=0 || Count(itemId)<quantity)return false;
            var copy=Clone();var stacks=new List<NativeItemStack>(copy.items);
            var stack=stacks.Find(row=>row.itemId==itemId);
            stack.quantity-=quantity;if(stack.quantity==0)stacks.Remove(stack);
            copy.items=stacks.ToArray();if(!copy.IsValid)return false;
            changed=copy;return true;
        }

        // Explicit resolved shop quote, not an inferred original resale ratio.
        public bool CanTrade(int id, int quantity, long unitPrice, bool buying, out string reason)
            => PrepareTrade(id, quantity, unitPrice, buying, out _, out reason);

        private bool PrepareTrade(int id, int quantity, long unitPrice, bool buying, out long total, out string reason)
        {
            total = 0; reason = "소지품 상태가 올바르지 않습니다.";
            if (!IsValid) return false;
            reason = "거래 수량은 1 이상의 정수여야 합니다.";
            if (id <= 0 || quantity <= 0) return false;
            reason = "거래 가격이 미확인입니다.";
            if (unitPrice < 0) return false;
            reason = "거래 금액의 표현 한도를 초과합니다.";
            try { total = checked(unitPrice * quantity); } catch (OverflowException) { return false; }
            int owned = Count(id);
            if (buying) {
                reason = "소지금이 부족합니다.";
                if (gold < total) return false;
                reason = "아이템 보유 수량 한도를 초과합니다.";
                if ((long)owned + quantity > int.MaxValue) return false;
                reason = "소지품 종류 한도를 초과합니다.";
                if (owned == 0 && items.Length >= 10000) return false;
            } else {
                reason = "판매할 보유 수량이 부족합니다.";
                if (owned < quantity) return false;
                reason = "소지금 표현 한도를 초과합니다.";
                if (gold > long.MaxValue - total) return false;
            }
            reason = buying ? "구매 가능" : "판매 가능"; return true;
        }

        public bool TryTrade(int id, int quantity, long unitPrice, bool buying, out NativeInventory changed)
        {
            changed = null;
            if (!PrepareTrade(id, quantity, unitPrice, buying, out long total, out _)) return false;
            var copy = Clone(); var stacks = new List<NativeItemStack>(copy.items);
            var stack = stacks.Find(item => item.itemId == id);
            if (stack == null) { stack = new NativeItemStack {itemId = id}; stacks.Add(stack); }
            stack.quantity += buying ? quantity : -quantity;
            if (stack.quantity == 0) stacks.Remove(stack);
            copy.gold += buying ? -total : total; copy.items = stacks.ToArray();
            if (!copy.IsValid) return false;
            changed = copy; return true;
        }
    }
}
