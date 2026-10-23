using System.Text;

namespace DarkSaver.Items
{
    // Table permission only. Not a complete equip/use or level eligibility rule.
    public static class ItemUsageConditions
    {
        private static readonly string[] Types = {"보병", "비병", "기병", "수병", "창병", "궁병", "승려", "신관", "마법사", "사교"};
        public static string TypeName(int type) => type >= 0 && type < Types.Length ? Types[type] : "미확인";
        // Client-side position filter from Saver.exe 0x420460. Not a complete equip rule.
        // Bits are original slot indices 0..6; no inferred body-part or level names.
        public static bool TryGetEquipmentSlots(ItemCatalogEntry item, out int mask, out bool checkOppositeType)
        {
            mask = 0; checkOppositeType = false;
            if (item == null) return false;
            switch (item.GetNumericValue(3)) {
                case 1: mask = (1 << 2) | (1 << 3); return true;
                case 2: case 8: mask = (1 << 2) | (1 << 3); checkOppositeType = true; return true;
                case 3: mask = 1 << 1; return true;
                case 4: mask = 1 << 5; return true;
                case 5: case 10: mask = 1; return true;
                case 6: mask = (1 << 4) | (1 << 6); return true;
                case 7: case 9: return true; // Original branch rejects these types.
                default: return false;
            }
        }
        public static string EquipmentSlotsSummary(ItemCatalogEntry item)
        {
            if (!TryGetEquipmentSlots(item, out int mask, out bool opposite)) return "미확인";
            if (mask == 0) return "원본 장착 분기에서 제외";
            var result = new StringBuilder();
            for (int slot = 0; slot < 7; slot++) if ((mask & (1 << slot)) != 0) {
                if (result.Length > 0) result.Append(", ");
                result.Append("#").Append(slot);
            }
            if (opposite) result.Append(" (반대 칸의 동일 타입 제한)");
            return result.ToString();
        }
        // Independent client-side filters; neither is a server acceptance decision.
        // oppositeKnown=false includes occupied slots whose item record is unavailable.
        public static bool TryGetEquipmentPositionPermission(ItemCatalogEntry item, int slot,
            bool oppositeKnown, ItemCatalogEntry opposite, out bool allowed)
        {
            allowed = false;
            if (slot < 0 || slot >= 7 || !TryGetEquipmentSlots(item, out int mask, out bool checkOpposite)) return false;
            if ((mask & (1 << slot)) == 0) return true;
            if (checkOpposite && !oppositeKnown) return false;
            allowed = !checkOpposite || opposite == null || opposite.GetNumericValue(3) != item.GetNumericValue(3);
            return true;
        }
        // rawUnitValue is the signed16 field at original character-record +8.
        // Its base/job-level identity is unresolved. Never substitute either here.
        public static bool TryCompareEquipmentRequirement(ItemCatalogEntry item, short? rawUnitValue, out bool allowed)
        {
            allowed = false;
            if (item == null || !rawUnitValue.HasValue) return false;
            long required = item.GetNumericValue(4);
            if (required < int.MinValue || required > int.MaxValue) return false;
            allowed = required <= rawUnitValue.Value;
            return true;
        }
        public static bool TryGetPermission(ItemCatalogEntry item, int troopType, out bool allowed)
        {
            allowed = false;
            if (item == null || troopType < 0 || troopType >= Types.Length) return false;
            long flag = item.GetNumericValue(6 + troopType);
            if (flag != 0 && flag != 1) return false;
            allowed = flag == 1; return true;
        }
        public static string PermissionSummary(ItemCatalogEntry item)
        {
            var text = new StringBuilder(); int count = 0;
            for (int type = 0; type < Types.Length; type++) {
                if (!TryGetPermission(item, type, out bool allowed)) return "미해석 값 포함";
                if (!allowed) continue;
                if (count++ > 0) text.Append(", "); text.Append(Types[type]);
            }
            return count == Types.Length ? "전체 병종" : count == 0 ? "없음" : text.ToString();
        }
    }
}
