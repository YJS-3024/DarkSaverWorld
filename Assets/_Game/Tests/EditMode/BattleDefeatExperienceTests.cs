using System;
using System.IO;
using DarkSaver.Combat;
using DarkSaver.Core;
using DarkSaver.Jobs;
using DarkSaver.Network;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class BattleDefeatExperienceTests
    {
        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void CurrentPacketActionsApplyOnlyEnabledBaseRewardOnce(bool magic, bool enabled)
        {
            WithBattle(enabled, (battle, definition, preview) => {
                PreparePacketBattle(battle);
                battle.RestorePlayerExperience(1000);
                var sender = battle.gameObject.AddComponent<BattleRequestSender>();
                bool Send() => magic
                    ? sender.TrySendMagic(new MagicRequest(201, 100, 1, 0, 1, new[] { battle.PrimaryEnemyId }), out _)
                    : sender.TrySendAttack(new AttackRequest(battle.PrimaryEnemyId, 100, 0, 0), out _);
                Assert.That(Send(), Is.False); Assert.That(battle.PlayerExperience, Is.EqualTo(1000u));
                battle.AdvanceActionPoints(14);
                Assert.That(Send(), Is.True); Assert.That(battle.EnemyHp, Is.Zero);
                uint expected = enabled ? 1240u : 1000u;
                Assert.That(battle.PlayerExperience, Is.EqualTo(expected));
                Assert.That(battle.OutcomeGrantedExperience, Is.EqualTo(enabled ? "240" : string.Empty));
                Assert.That(battle.TryAwardDefeatExperience(), Is.False);
                Assert.That(Send(), Is.False); Assert.That(battle.PlayerExperience, Is.EqualTo(expected));
                var sessionGo = new GameObject("Packet reward return"); SceneManager.MoveGameObjectToScene(sessionGo, preview);
                var session = sessionGo.AddComponent<BattleTransitionController>(); session.CapturePlayerExperience(battle);
                Assert.That(session.PlayerExperience, Is.EqualTo(expected));
            });
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CurrentPacketRewardOverflowNeverWraps(bool magic)
        {
            WithBattle(true, (battle, definition, preview) => {
                PreparePacketBattle(battle); battle.RestorePlayerExperience(uint.MaxValue - 100);
                battle.AdvanceActionPoints(14);
                var sender = battle.gameObject.AddComponent<BattleRequestSender>();
                Assert.That(magic
                    ? sender.TrySendMagic(new MagicRequest(201, 100, 1, 0, 1, new[] { battle.PrimaryEnemyId }), out _)
                    : sender.TrySendAttack(new AttackRequest(battle.PrimaryEnemyId, 100, 0, 0), out _), Is.True);
                Assert.That(battle.PlayerExperience, Is.EqualTo(uint.MaxValue - 100));
                Assert.That(battle.OutcomeGrantedExperience, Is.Empty);
                Assert.That(battle.TryAwardDefeatExperience(), Is.False);
            });
        }

        private static void PreparePacketBattle(BattleSceneRoot battle)
        {
            battle.SetTemporaryLocalRules(true);
            battle.ConfigurePhysicalAccuracy(100, 100, 0);
            // Reward routing is the subject here, not the default target's random evasion.
            Assert.That(battle.ConfigurePhysicalAccuracy(battle.PrimaryEnemyId, 100, 0), Is.True);
            battle.ConfigureTemporaryDamageMagic(201, 1, 4, 1000, 1);
            battle.ConfigureMagicApCost(201, 3);
            Assert.That(battle.TryConfigureGrid(3, 3, new[] {
                new CombatGridPlacement(100, new CombatGridCell(0, 0)),
                new CombatGridPlacement(battle.PrimaryEnemyId, new CombatGridCell(1, 0)) },
                (_, _) => true, (a, b) => Math.Abs(a.X-b.X)+Math.Abs(a.Y-b.Y)==1), Is.True);
        }

        [Test]
        public void DefeatAwardsNativeBaseOnceAndSessionRetainsTotal()
        {
            WithBattle(true, (battle, definition, preview) =>
            {
                Assert.That(definition.TryGetDefeatExperience(out var reward), Is.True);
                Assert.That(reward, Is.EqualTo(240u));
                battle.RestorePlayerExperience(1000);
                Assert.That(battle.TryAwardDefeatExperience(), Is.False);
                battle.ConfigureActionPointCosts(2, 6); battle.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp); battle.PlayerAttack();
                Assert.That(battle.EnemyHp, Is.Zero); Assert.That(battle.PlayerExperience, Is.EqualTo(1240u));
                Assert.That(battle.TryAwardDefeatExperience(), Is.False);
                battle.PlayerAttack(); Assert.That(battle.PlayerExperience, Is.EqualTo(1240u));
                var go = new GameObject("Reward session"); SceneManager.MoveGameObjectToScene(go, preview);
                var session = go.AddComponent<BattleTransitionController>(); session.CapturePlayerExperience(battle);
                Assert.That(session.PlayerExperience, Is.EqualTo(1240u));
            });
        }

        [Test]
        public void DisabledRewardAndRejectedActionDoNotGrantExperience()
        {
            WithBattle(false, (battle, definition, preview) =>
            {
                battle.RestorePlayerExperience(9); battle.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp); battle.PlayerAttack();
                Assert.That(battle.EnemyHp, Is.EqualTo(1)); Assert.That(battle.PlayerExperience, Is.EqualTo(9u));
                battle.ConfigureActionPointCosts(2, 6); battle.PlayerAttack();
                Assert.That(battle.EnemyHp, Is.Zero); Assert.That(battle.PlayerExperience, Is.EqualTo(9u));
                Assert.That(battle.TryAwardDefeatExperience(), Is.False);
            });
        }

        [Test]
        public void AwardedTotalSurvivesNativeSaveAndSessionRestore()
        {
            WithBattle(true, (battle, definition, preview) =>
            {
                battle.RestorePlayerExperience(1000);
                battle.ConfigureActionPointCosts(2, 6); battle.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp); battle.PlayerAttack();
                var go = new GameObject("Reward save session"); SceneManager.MoveGameObjectToScene(go, preview);
                var session = go.AddComponent<BattleTransitionController>(); session.CapturePlayerExperience(battle);
                var directory = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Tools/SaveChecks");
                Directory.CreateDirectory(directory);
                var store = new NativeSaveStore(Path.Combine(directory, "reward-" + Guid.NewGuid().ToString("N") + ".json"));
                Assert.That(store.TrySave(session.CaptureSaveState("Kaosia_Map", "Town_Runtime", new Vector3(1.5f, 2.5f, 0)), out _), Is.True);
                Assert.That(store.TryLoad(out var data, out _), Is.True);
                Assert.That(data.experience, Is.EqualTo(1240u));
                var restoredGo = new GameObject("Reward restored session"); SceneManager.MoveGameObjectToScene(restoredGo, preview);
                var restored = restoredGo.AddComponent<BattleTransitionController>();
                Assert.That(restored.RestoreSaveState(data), Is.True);
                Assert.That(restored.PlayerExperience, Is.EqualTo(1240u));
            });
        }

        [Test]
        public void Uint32OverflowDoesNotWrapOrGrantTwice()
        {
            WithBattle(true, (battle, definition, preview) =>
            {
                battle.RestorePlayerExperience(uint.MaxValue - 100);
                battle.ConfigureActionPointCosts(2, 6); battle.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp); battle.PlayerAttack();
                Assert.That(battle.EnemyHp, Is.Zero);
                Assert.That(battle.PlayerExperience, Is.EqualTo(uint.MaxValue - 100));
                Assert.That(battle.TryAwardDefeatExperience(), Is.False);
                Assert.That(battle.Status, Does.Contain("미지급"));
            });
        }

        [Test]
        public void ExactUint32LimitIsAllowed()
        {
            WithBattle(true, (battle, definition, preview) =>
            {
                battle.RestorePlayerExperience(uint.MaxValue - 240);
                battle.ConfigureActionPointCosts(2, 6); battle.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp); battle.PlayerAttack();
                Assert.That(battle.PlayerExperience, Is.EqualTo(uint.MaxValue));
                Assert.That(battle.TryAwardDefeatExperience(), Is.False);
            });
        }

        [Test]
        public void MissingNegativeOversizedAndZeroRewardsAreDistinguished()
        {
            WithBattle(true, (battle, definition, preview) =>
            {
                var serialized = new SerializedObject(definition);
                var catalog = ScriptableObject.CreateInstance<JobCatalog>();
                try
                {
                    serialized.FindProperty("jobCatalog").objectReferenceValue = catalog;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    Assert.That(definition.TryGetDefeatExperience(out _), Is.False);
                    foreach (var value in new[] { -1L, (long)uint.MaxValue + 1 })
                    {
                        var numbers = new long[34]; numbers[0] = 701; numbers[32] = value;
                        catalog.Initialize(new[] { new JobCatalogEntry(numbers, "", "test", "", "monster") });
                        Assert.That(definition.TryGetDefeatExperience(out _), Is.False);
                    }
                    var zero = new long[34]; zero[0] = 701;
                    catalog.Initialize(new[] { new JobCatalogEntry(zero, "", "test", "", "monster") });
                    Assert.That(definition.TryGetDefeatExperience(out var reward), Is.True); Assert.That(reward, Is.Zero);
                }
                finally { UnityEngine.Object.DestroyImmediate(catalog); }
            });
        }

        [Test]
        public void HpZeroAloneDoesNotPermitRewardAndDeathDoesNotAutomaticallyAwardIt()
        {
            WithBattle(true, (battle, definition, preview) =>
            {
                battle.RestorePlayerExperience(1000);
                Assert.That(battle.ApplyHp(700, 0), Is.True);
                Assert.That(battle.TryAwardDefeatExperience(), Is.False);
                Assert.That(battle.ApplyDeath(700, true), Is.True);
                Assert.That(battle.PlayerExperience, Is.EqualTo(1000u));
                Assert.That(battle.TryAwardDefeatExperience(), Is.True); // Explicit opt-in fixture only.
                Assert.That(battle.PlayerExperience, Is.EqualTo(1240u));
                Assert.That(battle.ApplyDeath(700, false), Is.True);
                Assert.That(battle.TryAwardDefeatExperience(), Is.False);
                Assert.That(battle.PlayerExperience, Is.EqualTo(1240u));
                Assert.That(battle.ApplyHp(100, 0), Is.True);
                Assert.That(battle.IsBattleFinished, Is.True);
                Assert.That(battle.OutcomeGrantedExperience, Is.EqualTo("240"));
                Assert.That(battle.OutcomeGrantedExperience, Is.EqualTo("240"));
                Assert.That(battle.PlayerExperience, Is.EqualTo(1240u));
            });
        }

        private static void WithBattle(bool rewardEnabled, Action<BattleSceneRoot, BattleEnemyDefinition, Scene> check)
        {
            var active = SceneManager.GetActiveScene(); var dirty = active.isDirty;
            var preview = EditorSceneManager.NewPreviewScene();
            var source = AssetDatabase.LoadAssetAtPath<BattleEnemyDefinition>("Assets/_Game/Runtime/Combat/Brugos01Kisra.asset");
            var definition = UnityEngine.Object.Instantiate(source);
            try
            {
                var serialized = new SerializedObject(definition);
                serialized.FindProperty("useJobCatalogBaseStats").boolValue = false;
                serialized.FindProperty("baseHp").intValue = 1;
                serialized.FindProperty("useNativeBaseDefeatExperience").boolValue = rewardEnabled;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var go = new GameObject("Reward battle"); SceneManager.MoveGameObjectToScene(go, preview);
                var battle = go.AddComponent<BattleSceneRoot>();
                Assert.That(battle.ConfigureEnemy(definition), Is.True);
                check(battle, definition, preview);
                Assert.That(source.CreateUnit().Hp, Is.EqualTo(450));
            }
            finally { UnityEngine.Object.DestroyImmediate(definition); EditorSceneManager.ClosePreviewScene(preview); }
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(active)); Assert.That(active.isDirty, Is.EqualTo(dirty));
        }
    }
}
