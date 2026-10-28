using DarkSaver.Combat;
using DarkSaver.Network;
using NUnit.Framework;
using UnityEngine;

namespace DarkSaver.Tests
{
    public sealed class OriginalTrainingTests
    {
        [TestCase("Map_101_;",101)][TestCase("Map_102_;",102)][TestCase("Map_103_;",103)]
        [TestCase("Map_104_;",104)][TestCase("Map_105_;",105)][TestCase("Map_106_;",106)][TestCase("Map_107_;",107)]
        [TestCase("Map_100_;",0)][TestCase("Map_108_;",0)][TestCase("Map_0101_;",0)]
        [TestCase("map_101_;",0)][TestCase("Training101_Runtime",0)][TestCase("Kaosia_Map",0)][TestCase(null,0)]
        public void NativeMapIdentityDoesNotInferClassOrRuntimeScene(string name,int expected)
        {
            Assert.AreEqual(expected!=0,OriginalTrainingMaps.TryGetMapId(name,out var id));
            Assert.AreEqual(expected,id);
        }

        [TestCase(101)][TestCase(102)][TestCase(103)][TestCase(104)][TestCase(105)][TestCase(106)][TestCase(107)]
        public void TrainingMapNeedsNoThemedEnemyOrLocalRespawnClock(int map)
        {
            var go=new GameObject("native training receipt fixture");
            try
            {
                var battle=go.AddComponent<BattleSceneRoot>();NativeCombatStateFixture.Initialize(battle,6);
                var receiver=go.AddComponent<BattlePacketReceiver>();
                Assert.IsTrue(battle.ConfigureTrainingMap(map));Assert.AreEqual(map,battle.TrainingMapId);
                Assert.IsTrue(battle.IsPersistentTraining);
                Assert.IsFalse(battle.ConfigureTrainingMap(108));Assert.AreEqual(map,battle.TrainingMapId);
                Assert.IsTrue(receiver.TryApplyPacket(new UnitCorpseNotification(800)));
                Assert.AreEqual(75,battle.EnemyHp);Assert.AreEqual(0u,battle.PlayerExperience);
                Assert.IsTrue(receiver.TryApplyPacket(new HpSetNotification(800,0)));
                Assert.IsFalse(battle.AdvanceActionPoints(100));Assert.AreEqual(0,battle.EnemyHp);
                Assert.IsFalse(battle.IsBattleFinished);Assert.IsFalse(battle.ShowsEncounterOutcome);
                Assert.IsFalse(battle.TryAcknowledgeOutcome());
                Assert.IsTrue(receiver.TryApplyPacket(new ExperienceSetNotification(100,123)));
                Assert.AreEqual(123u,battle.PlayerExperience);
                Assert.IsTrue(receiver.TryApplyPacket(new ExperienceSetNotification(100,123)));
                Assert.AreEqual(123u,battle.PlayerExperience);Assert.AreEqual(0,battle.EnemyHp);
                Assert.IsFalse(battle.TryAwardDefeatExperience());Assert.AreEqual(30,battle.PlayerMp);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
