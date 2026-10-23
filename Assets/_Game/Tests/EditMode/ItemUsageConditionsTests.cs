using DarkSaver.Editor;
using DarkSaver.Items;
using DarkSaver.Jobs;
using NUnit.Framework;
using UnityEditor;

namespace DarkSaver.Tests
{
    public sealed class ItemUsageConditionsTests
    {
        private static ItemCatalogEntry EquipmentFixture(long position, long required = 1)
        {
            var values = new long[ItemCatalogEntry.NumericColumnCount]; values[3] = position; values[4] = required;
            return new ItemCatalogEntry(values,"0","","fixture","","");
        }
        [Test]
        public void HandPositionFiltersPreserveUnknownOppositeAndRejectOnlySameType()
        {
            foreach(int type in new[] {2,8}) foreach(int slot in new[] {2,3}) {
                var item = EquipmentFixture(type);
                Assert.That(ItemUsageConditions.TryGetEquipmentPositionPermission(item,slot,false,null,out bool allowed), Is.False);
                Assert.That(allowed, Is.False);
                Assert.That(ItemUsageConditions.TryGetEquipmentPositionPermission(item,slot,true,null,out allowed), Is.True);
                Assert.That(allowed, Is.True);
                Assert.That(ItemUsageConditions.TryGetEquipmentPositionPermission(item,slot,true,EquipmentFixture(type),out allowed), Is.True);
                Assert.That(allowed, Is.False);
                Assert.That(ItemUsageConditions.TryGetEquipmentPositionPermission(item,slot,true,EquipmentFixture(type==2?8:2),out allowed), Is.True);
                Assert.That(allowed, Is.True);
                Assert.That(ItemUsageConditions.TryGetEquipmentPositionPermission(item,0,false,null,out allowed), Is.True);
                Assert.That(allowed, Is.False);
            }
            Assert.That(ItemUsageConditions.TryGetEquipmentPositionPermission(EquipmentFixture(1),2,false,null,out bool twoHand), Is.True);
            Assert.That(twoHand, Is.True); // Position only; the original later handles paired occupancy separately.
            Assert.That(ItemUsageConditions.TryGetEquipmentPositionPermission(null,0,true,null,out _), Is.False);
            Assert.That(ItemUsageConditions.TryGetEquipmentPositionPermission(EquipmentFixture(2),7,true,null,out _), Is.False);
        }
        [TestCase(7,6,false)] [TestCase(7,7,true)] [TestCase(7,8,true)]
        [TestCase(-5,1,true)] [TestCase(-5,-6,false)] [TestCase(-5,-5,true)]
        [TestCase(32768,32767,false)] [TestCase(-32769,-32768,true)]
        public void EquipmentRequirementUsesOriginalSignedComparison(long required, int raw, bool expected)
        {
            Assert.That(ItemUsageConditions.TryCompareEquipmentRequirement(EquipmentFixture(1,required),(short)raw,out bool allowed), Is.True);
            Assert.That(allowed, Is.EqualTo(expected));
        }
        [Test]
        public void MissingOriginalRequirementInputDoesNotChooseANativeLevel()
        {
            Assert.That(ItemUsageConditions.TryCompareEquipmentRequirement(EquipmentFixture(1),null,out bool allowed), Is.False);
            Assert.That(allowed, Is.False);
            Assert.That(ItemUsageConditions.TryCompareEquipmentRequirement(null,1,out _), Is.False);
            Assert.That(ItemUsageConditions.TryCompareEquipmentRequirement(EquipmentFixture(1,long.MaxValue),1,out _), Is.False);
        }
        [TestCase(1,12,false)] [TestCase(2,12,true)] [TestCase(3,2,false)]
        [TestCase(4,32,false)] [TestCase(5,1,false)] [TestCase(6,80,false)]
        [TestCase(7,0,false)] [TestCase(8,12,true)] [TestCase(9,0,false)] [TestCase(10,1,false)]
        public void EquipmentPositionsMatchOriginalBranchTable(int type, int expected, bool opposite)
        {
            var values = new long[ItemCatalogEntry.NumericColumnCount]; values[3] = type;
            var item = new ItemCatalogEntry(values,"0","","fixture","","");
            Assert.That(ItemUsageConditions.TryGetEquipmentSlots(item,out int mask,out bool check), Is.True);
            Assert.That(mask, Is.EqualTo(expected)); Assert.That(check, Is.EqualTo(opposite));
            StringAssert.Contains(ItemUsageConditions.EquipmentSlotsSummary(item),DarkSaver.UI.TownServicesPanel.ItemDetails(item));
        }
        [Test]
        public void UnknownEquipmentPositionIsNotTreatedAsEligible()
        {
            foreach(long type in new long[] {-1,0,11,long.MaxValue}) {
                var values = new long[ItemCatalogEntry.NumericColumnCount]; values[3] = type;
                var item = new ItemCatalogEntry(values,"0","","fixture","","");
                Assert.That(ItemUsageConditions.TryGetEquipmentSlots(item,out int mask,out bool check), Is.False);
                Assert.That(mask, Is.Zero); Assert.That(check, Is.False);
            }
            Assert.That(ItemUsageConditions.TryGetEquipmentSlots(null,out _,out _), Is.False);
        }
        [Test]
        public void AllNativeFlagsAndPlayerTypesMatchTheirOriginalColumns()
        {
            var items = AssetDatabase.LoadAssetAtPath<ItemCatalog>(ItemCatalogConverter.Output);
            for (int i = 0; i < items.Count; i++) {
                var item = items.GetAt(i);
                for (int type = 0; type < 10; type++) {
                    Assert.That(ItemUsageConditions.TryGetPermission(item, type, out bool allowed), Is.True);
                    Assert.That(allowed, Is.EqualTo(item.GetNumericValue(6 + type) == 1));
                }
            }
            var jobs = AssetDatabase.LoadAssetAtPath<JobCatalog>(JobCatalogConverter.Output);
            for (int i = 0; i < jobs.Count; i++) {
                int type = (int)jobs.GetAt(i).GetNumericValue(22);
                Assert.That(type, Is.InRange(0, 9)); Assert.That(ItemUsageConditions.TypeName(type), Is.Not.EqualTo("미확인"));
            }
            Assert.That(items.TryGet(212, out var ring), Is.True);
            Assert.That(ring.GetNumericValue(4), Is.EqualTo(-5));
            StringAssert.Contains("음수 요구 레벨", DarkSaver.UI.TownServicesPanel.ItemDetails(ring));
        }

        [Test]
        public void UnknownTypesAndNonBooleanFlagsNeverBecomePermissions()
        {
            var values = new long[ItemCatalogEntry.NumericColumnCount]; values[6] = 2;
            var item = new ItemCatalogEntry(values, "0", "", "", "", "");
            Assert.That(ItemUsageConditions.TryGetPermission(item, 0, out bool allowed), Is.False); Assert.That(allowed, Is.False);
            Assert.That(ItemUsageConditions.TryGetPermission(item, -1, out _), Is.False);
            Assert.That(ItemUsageConditions.TryGetPermission(item, 10, out _), Is.False);
            Assert.That(ItemUsageConditions.TryGetPermission(null, 0, out _), Is.False);
            Assert.That(ItemUsageConditions.PermissionSummary(item), Is.EqualTo("미해석 값 포함"));
            values[6] = 1;
            item = new ItemCatalogEntry(values, "0", "", "", "", "");
            Assert.That(ItemUsageConditions.PermissionSummary(item), Is.EqualTo("보병"));
        }
    }
}
