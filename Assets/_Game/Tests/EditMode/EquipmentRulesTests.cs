using NUnit.Framework;

namespace DarkSaver.Prototype.Tests
{
    public sealed class EquipmentRulesTests
    {
        [Test]
        public void EquippedItems_AddBonusesWithoutChangingBaseStats()
        {
            var unit = new BattleUnit
            {
                Attack = 20,
                Defense = 7,
                SwordEquipped = true,
                ArmorEquipped = true
            };

            Assert.That(unit.EffectiveAttack, Is.EqualTo(24));
            Assert.That(unit.EffectiveDefense, Is.EqualTo(10));
            Assert.That(unit.Attack, Is.EqualTo(20));
            Assert.That(unit.Defense, Is.EqualTo(7));
        }
    }
}
