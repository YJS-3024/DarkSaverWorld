using System;
using DarkSaver.Combat;
using DarkSaver.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DarkSaver.Tests
{
    public sealed class BattleVitalsSessionTests
    {
        [Test]
        public void FreshSessionLeavesInitialBattleStatsUntouched()
        {
            Check((session, first, second, hud) =>
            {
                Assert.That(session.HasPlayerVitals, Is.False);
                Assert.That(session.CanEnterBattle, Is.True);
                Assert.That(first.TryRestorePlayerVitals(45, 7), Is.True);
                Assert.That(session.RestorePlayerVitals(first), Is.True);
                Assert.That((first.PlayerHp, first.PlayerMp), Is.EqualTo((45, 7)));
                Assert.That(hud.SummaryText, Does.Not.Contain("HP "));
            });
        }

        [TestCase(1)]
        [TestCase(87)]
        [TestCase(120)]
        public void RemainingHpAndMpSurviveReplacementWithoutAutomaticRecovery(int hp)
        {
            Check((session, first, second, hud) =>
            {
                Assert.That(first.TryRestorePlayerVitals(hp, 0), Is.True);
                Assert.That(session.CapturePlayerVitals(first), Is.True);
                Assert.That(first.PlayerMp, Is.Zero);
                Assert.That(session.CanEnterBattle, Is.True);
                Assert.That((session.PlayerHp, session.PlayerMaxHp, session.PlayerMp, session.PlayerMaxMp),
                    Is.EqualTo((hp, 120, 0, 30)));
                Object.DestroyImmediate(first.gameObject);
                Assert.That(session.RestorePlayerVitals(second), Is.True);
                Assert.That(session.RestorePlayerVitals(second), Is.True);
                Assert.That((second.PlayerHp, second.PlayerMp), Is.EqualTo((hp, 0)));
            });
        }

        [Test]
        public void MissingOrDestroyedBattleDoesNotReplaceStoredVitals()
        {
            Check((session, first, second, hud) =>
            {
                first.TryRestorePlayerVitals(45, 7); session.CapturePlayerVitals(first);
                Object.DestroyImmediate(first.gameObject);
                Assert.That(session.CapturePlayerVitals(first), Is.False);
                Assert.That(session.CapturePlayerVitals(null), Is.False);
                Assert.That(session.RestorePlayerVitals(first), Is.False);
                Assert.That(session.RestorePlayerVitals(null), Is.False);
                Assert.That((session.PlayerHp, session.PlayerMp), Is.EqualTo((45, 7)));
                var fresh = second.gameObject.AddComponent<BattleTransitionController>();
                Assert.That(fresh.HasPlayerVitals, Is.False);
                Assert.That(fresh.CanEnterBattle, Is.True);
            });
        }

        [Test]
        public void VitalsDoNotModifyExperienceCompletionOrEnemy()
        {
            Check((session, first, second, hud) =>
            {
                first.RestorePlayerExperience(3477000000u); first.TryRestorePlayerVitals(87, 0);
                session.CapturePlayerVitals(first);
                Assert.That(session.PlayerExperience, Is.Zero);
                Assert.That(session.ClearedScenarioCount, Is.Zero);
                session.CapturePlayerExperience(first);
                Assert.That(session.RestorePlayerVitals(second), Is.True);
                Assert.That(second.PlayerExperience, Is.Zero);
                Assert.That(second.EnemyHp, Is.EqualTo(75));
                Assert.That(session.RestorePlayerExperience(second), Is.True);
                Assert.That(second.PlayerExperience, Is.EqualTo(3477000000u));
            });
        }

        [TestCase(-1, 0)]
        [TestCase(121, 0)]
        [TestCase(50, -1)]
        [TestCase(50, 31)]
        public void InvalidRestoreDoesNotPartiallyApply(int hp, int mp)
        {
            Check((session, first, second, hud) =>
            {
                Assert.That(first.TryRestorePlayerVitals(hp, mp), Is.False);
                Assert.That((first.PlayerHp, first.PlayerMp), Is.EqualTo((120, 30)));
                Assert.That(first.PlayerLifeState, Is.EqualTo(UnitLifeState.Active));
            });
        }

        [Test]
        public void DefeatBlocksEntryAndDoesNotReviveNextBattle()
        {
            Check((session, first, second, hud) =>
            {
                Defeat(first);
                Assert.That(session.CapturePlayerVitals(first), Is.True);
                Assert.That(session.PlayerHp, Is.Zero);
                Assert.That(session.PlayerMp, Is.EqualTo(30));
                Assert.That(session.CanEnterBattle, Is.False);
                Assert.That(session.RestorePlayerVitals(second), Is.False);
                Assert.That(second.PlayerHp, Is.EqualTo(120));
                Assert.DoesNotThrow(() => session.EnterBattle());
                Assert.That(SceneManager.GetSceneByName("Battle").isLoaded, Is.False);
                Assert.That(session.PlayerExperience, Is.Zero);
                Assert.That(session.ClearedScenarioCount, Is.Zero);
            });
        }

        [TestCase(37)]
        [TestCase(0)]
        public void HudReadsVitalsAndEntryRestrictionWithoutChangingThem(int hp)
        {
            Check((session, first, second, hud) =>
            {
                first.TryRestorePlayerVitals(hp, 5); session.CapturePlayerVitals(first);
                Assert.AreEqual(hp,session.PlayerHp);Assert.AreEqual(5,session.PlayerMp);
                Assert.That(hud.SummaryText, Does.Not.Contain("HP "),"retired temporary HUD is not native status UI");
                if (hp == 0) Assert.IsFalse(session.CanEnterBattle);
                else Assert.That(hud.SummaryText, Does.Not.Contain("전투 불능"));
                Assert.That((session.PlayerHp, session.PlayerMp, session.CanEnterBattle), Is.EqualTo((hp, 5, hp > 0)));
            });
        }

        [Test]
        public void SessionRestorationCannotReviveCorpse()
        {
            Check((session, first, second, hud) =>
            {
                Defeat(first);
                Assert.That(first.TryRestorePlayerVitals(120, 30), Is.False);
                Assert.That(first.PlayerHp, Is.Zero);
                Assert.That(first.PlayerLifeState, Is.EqualTo(UnitLifeState.Corpse));
            });
        }

        private static void Defeat(BattleSceneRoot battle)
        {
            Assert.That(battle.ApplyHp(100,0), Is.True);
            Assert.That(battle.ApplyDeath(100,true), Is.True);
        }

        private static void Check(Action<BattleTransitionController, BattleSceneRoot, BattleSceneRoot, WorldSessionHud> check)
        {
            var active = SceneManager.GetActiveScene(); var dirty = active.isDirty; var count = SceneManager.sceneCount;
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject Create(string name)
                {
                    var go = new GameObject(name); SceneManager.MoveGameObjectToScene(go, preview); return go;
                }
                var hud = Create("Vitals HUD test").AddComponent<WorldSessionHud>();
                var session = hud.GetComponent<BattleTransitionController>();
                var first = Create("First vitals battle").AddComponent<BattleSceneRoot>(); NativeCombatStateFixture.Initialize(first,6);
                var second = Create("Second vitals battle").AddComponent<BattleSceneRoot>(); NativeCombatStateFixture.Initialize(second,6);
                check(session, first, second, hud);
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(active));
            Assert.That(active.isDirty, Is.EqualTo(dirty));
            Assert.That(SceneManager.sceneCount, Is.EqualTo(count));
        }
    }
}
