using NUnit.Framework;

namespace DarkSaver.Prototype.Tests
{
    public sealed class BattleRosterTests
    {
        [Test]
        public void CreateParty_ProvidesSkillAndMagicRoles()
        {
            var party = BattleRoster.CreateParty();

            Assert.That(party.Exists(unit => unit.CanUseSkill), Is.True);
            Assert.That(party.Exists(unit => unit.CanMagic), Is.True);
        }

        [Test]
        public void CreateParty_ReturnsFreshUnitState()
        {
            var first = BattleRoster.CreateParty();
            first[0].Hp = 1;

            var second = BattleRoster.CreateParty();

            Assert.That(second[0].Hp, Is.EqualTo(second[0].MaxHp));
            Assert.That(second[0], Is.Not.SameAs(first[0]));
        }
    }
}
