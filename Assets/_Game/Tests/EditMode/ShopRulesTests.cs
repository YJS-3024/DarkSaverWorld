using NUnit.Framework;

namespace DarkSaver.Prototype.Tests
{
    public sealed class ShopRulesTests
    {
        [Test]
        public void TryBuyHealingPotion_DeductsGoldAndAddsOnePotion()
        {
            var gold = 40;
            var potions = 2;

            var purchased = ShopRules.TryBuyHealingPotion(ref gold, ref potions);

            Assert.That(purchased, Is.True);
            Assert.That(gold, Is.EqualTo(15));
            Assert.That(potions, Is.EqualTo(3));
        }

        [Test]
        public void TryBuyHealingPotion_DoesNotMutateWhenGoldIsInsufficient()
        {
            var gold = 24;
            var potions = 2;

            var purchased = ShopRules.TryBuyHealingPotion(ref gold, ref potions);

            Assert.That(purchased, Is.False);
            Assert.That(gold, Is.EqualTo(24));
            Assert.That(potions, Is.EqualTo(2));
        }
    }
}
