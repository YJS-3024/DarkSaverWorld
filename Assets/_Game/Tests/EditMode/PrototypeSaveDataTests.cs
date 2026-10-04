using NUnit.Framework;

namespace DarkSaver.Prototype.Tests
{
    public sealed class PrototypeSaveDataTests
    {
        [Test]
        public void Serialize_RoundTripsProgressAndPartyGrowth()
        {
            var source = new PrototypeSaveData
            {
                defeated = 2,
                gold = 135,
                healingPotionCount = 1,
                fieldX = 7,
                fieldY = 4,
                questStage = (int)QuestStage.Active,
                questMonsterKills = 2,
                questFieldItemCollected = true,
                questMonsterDropCount = 1
            };
            source.party.Add(new PrototypePartySaveData
            {
                name = "아켄 용병대장",
                hp = 91,
                maxHp = 134,
                mp = 12,
                maxMp = 34,
                attack = 30,
                defense = 9,
                level = 2,
                experience = 17,
                swordEquipped = true,
                armorEquipped = true
            });

            var loaded = PrototypeSaveStore.TryDeserialize(
                PrototypeSaveStore.Serialize(source), out var result);

            Assert.That(loaded, Is.True);
            Assert.That(result.defeated, Is.EqualTo(2));
            Assert.That(result.fieldX, Is.EqualTo(7));
            Assert.That(result.questMonsterKills, Is.EqualTo(2));
            Assert.That(result.questFieldItemCollected, Is.True);
            Assert.That(result.questMonsterDropCount, Is.EqualTo(1));
            Assert.That(result.party, Has.Count.EqualTo(1));
            Assert.That(result.party[0].maxHp, Is.EqualTo(134));
            Assert.That(result.party[0].level, Is.EqualTo(2));
            Assert.That(result.party[0].swordEquipped, Is.True);
        }

        [Test]
        public void TryDeserialize_RejectsUnknownVersion()
        {
            var json = "{\"version\":999}";

            Assert.That(PrototypeSaveStore.TryDeserialize(json, out _), Is.False);
        }

        [Test]
        public void TryDeserialize_MigratesOlderPrototypeSave()
        {
            var json = "{\"version\":2,\"gold\":70}";

            var loaded = PrototypeSaveStore.TryDeserialize(json, out var result);

            Assert.That(loaded, Is.True);
            Assert.That(result.version, Is.EqualTo(PrototypeSaveStore.CurrentVersion));
            Assert.That(result.gold, Is.EqualTo(70));
            Assert.That(result.questStage, Is.EqualTo((int)QuestStage.NotAccepted));
        }
    }
}
