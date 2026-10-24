using System;
using System.IO;
using System.Text;
using DarkSaver.Combat;
using DarkSaver.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class CombatAiCatalogTests
    {
        [TestCase("ODNORMAL", 2)]
        [TestCase("ODATTACK", 4)]
        [TestCase("ODDEFENCE", 8)]
        public void ModeMatchesOriginalUserunitParser(string token, int mode)
        {
            var steps = new[] { new CombatAiAction("ATTACK", "ATTACK", null, 100) };
            var valid = new CombatAiRule(1, "01", false, 400, token, mode, "AREA", 0, 0, 1, null, steps);
            Assert.That(valid.HasSupportedMode, Is.True);
            var wrong = new CombatAiRule(1, "01", false, 400, token, mode == 4 ? 8 : 4, "AREA", 0, 0, 1, null, steps);
            Assert.That(wrong.HasSupportedMode, Is.False);
        }

        private static CombatAiCatalog Native() => AssetDatabase.LoadAssetAtPath<CombatAiCatalog>(CombatAiCatalogConverter.Output);

        [Test]
        public void EveryNativeRuleAndOrderedActionMatchesAnalyzedData()
        {
            var folder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "분석/Saver_1.2/catalogs");
            var source = CombatAiCatalogConverter.Parse(File.ReadAllText(Path.Combine(folder, "ai_rules.csv"), Encoding.UTF8),
                File.ReadAllText(Path.Combine(folder, "ai_actions.csv"), Encoding.UTF8));
            var catalog = Native(); CombatAiCatalogConverter.Verify(catalog, source);
            Assert.That(catalog.Count, Is.EqualTo(1134));
            var active = 0; var actions = 0;
            for (var i = 0; i < catalog.Count; i++) { var rule = catalog.GetAt(i); if (!rule.IsBackup) active++; actions += rule.ActionCount; }
            Assert.That(active, Is.EqualTo(923)); Assert.That(actions, Is.EqualTo(4656));
        }

        [Test]
        public void AmbiguousActiveRulesBackupsAndUnresolvedModesAreNotSilentlyReplaced()
        {
            var catalog = Native();
            Assert.That(catalog.TryGetUniqueActiveRule("09", 550, out var ambiguous), Is.False);
            Assert.That(ambiguous, Is.Null);
            Assert.That(catalog.TryGet(140, out var first) && catalog.TryGet(141, out var second), Is.True);
            Assert.That(first.TargetY, Is.EqualTo(20));
            catalog.TryGet(141, out var other); Assert.That(other.TargetY, Is.EqualTo(28));
            Assert.That(catalog.TryGet(760, out var unknown), Is.True);
            Assert.That(unknown.AiType, Is.EqualTo("ODATTACKL"));
            Assert.That(unknown.ModeCode, Is.Null); Assert.That(unknown.HasSupportedMode, Is.False);
            Assert.That(catalog.TryGet(762, out var backup), Is.True); Assert.That(backup.IsBackup, Is.True);
            Assert.That(catalog.TryGetUniqueActiveRule("no-map", 400, out _), Is.False);
        }

        [Test]
        public void NativeScenarioOneSelectsOriginalTokensWithoutNormalizing()
        {
            Assert.That(Native().TryGetUniqueActiveRule("01", 400, out var rule), Is.True);
            Assert.That(rule.ModeCode, Is.EqualTo(2)); Assert.That(rule.TargetType, Is.EqualTo("AREA"));
            Assert.That(rule.TargetX, Is.EqualTo(14)); Assert.That(rule.TargetY, Is.EqualTo(28)); Assert.That(rule.Radius, Is.EqualTo(5));
            Assert.That(rule.TargetUnitId, Is.Null);
            for (var roll = 0; roll < 100; roll++)
            {
                Assert.That(rule.TrySelectAction(roll, out var action), Is.True);
                Assert.That(action.Token, Is.EqualTo(roll < 50 ? "ATTACK" : roll < 70 ? "MAGIC_101" : "REST"));
                Assert.That(action.MagicId, Is.EqualTo(roll >= 50 && roll < 70 ? (int?)101 : null));
            }
        }

        [Test]
        public void RuleOwnsActionsAndRejectedInitializationPreservesCatalog()
        {
            var steps = new[] { new CombatAiAction("ATTACK", "ATTACK", null, 100) };
            var rule = new CombatAiRule(1, "01", false, 400, "ODNORMAL", 2, "AREA", 1, 2, 3, null, steps);
            steps[0] = new CombatAiAction("REST", "REST", null, 100);
            Assert.That(rule.GetAction(0).Kind, Is.EqualTo("ATTACK"));
            var catalog = ScriptableObject.CreateInstance<CombatAiCatalog>();
            try
            {
                catalog.Initialize(new[] { rule });
                Assert.Throws<ArgumentException>(() => catalog.Initialize(new[] { rule, rule }));
                Assert.That(catalog.Count, Is.EqualTo(1)); Assert.That(catalog.GetAt(0), Is.SameAs(rule));
                Assert.Throws<ArgumentOutOfRangeException>(() => new CombatAiAction("REST", "REST", null, -1));
            }
            finally { UnityEngine.Object.DestroyImmediate(catalog); }
        }

        [Test]
        public void ConverterRejectsBrokenJoinsWeightsHeadersAndOrphans()
        {
            const string rules = "ruleId,source,isBackup,unit,aiType,aiModeCode,targetType,targetX,targetY,radius,targetUnit,actionCount,weightTotal\n"
                + "1,01set/01.AI,False,400,ODNORMAL,2,AREA,14,28,5,,1,100\n";
            const string actions = "ruleId,source,isBackup,unit,actionOrder,actionType,token,magicId,weight,cumulativeStart,cumulativeEnd,effectiveProbability\n"
                + "1,01set/01.AI,False,400,1,ATTACK,ATTACK,,100,0,100,100\n";
            Assert.That(CombatAiCatalogConverter.Parse(rules, actions).Length, Is.EqualTo(1));
            Assert.Throws<InvalidDataException>(() => CombatAiCatalogConverter.Parse(rules, actions.Replace("False,400,1", "False,401,1")));
            Assert.Throws<InvalidDataException>(() => CombatAiCatalogConverter.Parse(rules, actions.Replace("100,0,100,100", "100,0,100,99")));
            Assert.Throws<InvalidDataException>(() => CombatAiCatalogConverter.Parse(rules, actions.Replace("False,400,1", "False,400,2")));
            Assert.Throws<InvalidDataException>(() => CombatAiCatalogConverter.Parse(rules.Replace("ruleId", "wrong"), actions));
            Assert.Throws<InvalidDataException>(() => CombatAiCatalogConverter.Parse(rules, actions + "2,01set/01.AI,False,410,1,REST,REST,,100,0,100,100\n"));
        }

        [Test]
        public void SchedulerChecksOneUnitPerCallWithoutSpendingApOrApplyingCombat()
        {
            WithBattle(battle => {
                var scheduler = new BattleAiDecisionScheduler(battle, Native(), "01");
                Assert.That(scheduler.TrySetUnits(new short[] { 999, 400, 401 }), Is.True);
                Assert.That(scheduler.TryStep(50, out var missing, out _, out _, out _), Is.False); Assert.That(missing, Is.EqualTo(999));
                Assert.That(scheduler.TryStep(50, out var notReady, out _, out _, out _), Is.False); Assert.That(notReady, Is.EqualTo(400));
                battle.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp);
                Assert.That(scheduler.TryStep(50, out var noRule, out _, out _, out _), Is.False); Assert.That(noRule, Is.EqualTo(401));
                Assert.That(scheduler.TryStep(50, out _, out _, out _, out _), Is.False); // 999 only; no recursive skip.
                Assert.That(scheduler.TryStep(50, out var id, out var rule, out var action, out _), Is.True);
                Assert.That(id, Is.EqualTo(400)); Assert.That(rule.Id, Is.EqualTo(1)); Assert.That(action.MagicId, Is.EqualTo(101));
                battle.TryGetUnit(400, out var unit); Assert.That(unit.Ap, Is.EqualTo(unit.MaxAp)); Assert.That(unit.Hp, Is.EqualTo(100));
                Assert.That(battle.PlayerHp, Is.EqualTo(120)); Assert.That(battle.PlayerMp, Is.EqualTo(30));
                Assert.That(battle.LastAttackPresentation, Is.Null);
            });
        }

        [Test]
        public void SchedulerDeadUnitAndInvalidRollNeverInventActionsOrAdvanceTwice()
        {
            WithBattle(battle => {
                battle.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp);
                var scheduler = new BattleAiDecisionScheduler(battle, Native(), "01");
                scheduler.TrySetUnits(new short[] { 400, 401 });
                Assert.That(scheduler.TryStep(100, out _, out _, out _, out _), Is.False);
                Assert.That(scheduler.TryStep(0, out var id, out _, out _, out _), Is.True); Assert.That(id, Is.EqualTo(400));
                battle.ApplyDeath(400, true);
                scheduler.TryStep(0, out _, out _, out _, out _); // 401 has no rule.
                Assert.That(scheduler.TryStep(0, out var dead, out _, out _, out _), Is.False); Assert.That(dead, Is.EqualTo(400));
                battle.TryGetUnit(400, out var unit); Assert.That(unit.Ap, Is.EqualTo(unit.MaxAp));
            });
        }

        private static void WithBattle(Action<BattleSceneRoot> check)
        {
            var active = SceneManager.GetActiveScene(); var dirty = active.isDirty;
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var go = new GameObject("Native AI decision test"); SceneManager.MoveGameObjectToScene(go, scene);
                var battle = go.AddComponent<BattleSceneRoot>();
                Assert.That(battle.TryRegisterGroup(new[] {
                    new CombatUnit { Id = 400, JobId = 701, Enemy = true, Name = "AI commander", Hp = 100, MaxHp = 100 },
                    new CombatUnit { Id = 401, JobId = 701, Enemy = true, Name = "AI soldier", Hp = 100, MaxHp = 100 } }), Is.True);
                check(battle);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(active)); Assert.That(active.isDirty, Is.EqualTo(dirty));
        }

        [Test]
        public void MonsterTargetPriorityRetargetsOutsideRangeOrBlockedAttackerWithoutSpending()
        {
            WithBattle(battle =>
            {
                Assert.That(battle.TryRegisterGroup(new[] {
                    new CombatUnit { Id = 200, JobId = 100, Name = "Second ally", Hp = 50, MaxHp = 50 }
                }), Is.True);
                var catalog = ScriptableObject.CreateInstance<CombatAiCatalog>();
                try
                {
                    var scheduler = new BattleAiDecisionScheduler(battle, catalog, "01");
                    var attackerInRange = true; var attackerReachable = true;
                    var ids = new short[] { 100, 200, 400, 401, 999 };
                    Func<CombatUnitSnapshot, bool> inRange = u => u.Id != 200 || attackerInRange;
                    Func<CombatUnitSnapshot, bool> reachable = u => u.Id != 200 || attackerReachable;
                    Func<CombatUnitSnapshot, long> distance = u => u.Id == 100 ? 1 : 5;
                    Assert.That(battle.ApplyAttackPresentation(400, 200, 0), Is.True);
                    Assert.That(scheduler.TrySelectMonsterTarget(400, ids, inRange, reachable, distance, out var recordedTarget, out _), Is.True);
                    Assert.That(recordedTarget, Is.EqualTo(200));
                    Assert.That(battle.ApplyAttackPresentation(400, 100, 0), Is.True);
                    Assert.That(scheduler.TrySelectMonsterTarget(400, ids, inRange, reachable, distance, out recordedTarget, out _), Is.True);
                    Assert.That(recordedTarget, Is.EqualTo(100));
                    Assert.That(battle.ApplyAttackPresentation(400, 200, 0), Is.True);
                    Assert.That(scheduler.TrySelectMonsterTarget(400, ids, 200, inRange, reachable, distance, out var target, out _), Is.True);
                    Assert.That(target, Is.EqualTo(200));
                    attackerInRange = false;
                    Assert.That(scheduler.TrySelectMonsterTarget(400, ids, 200, inRange, reachable, distance, out target, out _), Is.True);
                    Assert.That(target, Is.EqualTo(100));
                    attackerInRange = true; attackerReachable = false;
                    Assert.That(scheduler.TrySelectMonsterTarget(400, ids, 200, inRange, reachable, distance, out target, out _), Is.True);
                    Assert.That(target, Is.EqualTo(100));
                    Assert.That(scheduler.TrySelectMonsterTarget(400, ids, 100, inRange, reachable, distance, out target, out _), Is.True);
                    Assert.That(target, Is.EqualTo(100));
                    Assert.That(scheduler.TrySelectMonsterTarget(400, ids, null, _ => false, reachable, distance, out target, out _), Is.False);
                    Assert.That(target, Is.Null);
                    Assert.That(battle.TryGetUnit(400, out var monster), Is.True);
                    Assert.That(monster.Ap, Is.EqualTo(GameConstants.BattleStartingAp));
                    Assert.That(monster.Hp, Is.EqualTo(100)); Assert.That(scheduler.Count, Is.Zero);
                    Assert.That(battle.PlayerHp, Is.EqualTo(120));
                }
                finally { UnityEngine.Object.DestroyImmediate(catalog); }
            });
        }

        [Test]
        public void MonsterTargetQueryRejectsMissingMetadataDuplicatesAndUnconfirmedTies()
        {
            var active = SceneManager.GetActiveScene(); var dirty = active.isDirty;
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var go = new GameObject("Native AI decision test"); SceneManager.MoveGameObjectToScene(go, scene);
                var battle = go.AddComponent<BattleSceneRoot>();
                Assert.That(battle.TryRegisterGroup(new[] {
                    new CombatUnit { Id = 400, JobId = 701, Enemy = true, Name = "AI commander", Hp = 100, MaxHp = 100 },
                    new CombatUnit { Id = 401, JobId = 701, Enemy = true, Name = "AI soldier", Hp = 100, MaxHp = 100 } }), Is.True);
                var catalog = ScriptableObject.CreateInstance<CombatAiCatalog>();
                try
                {
                    var scheduler = new BattleAiDecisionScheduler(battle, catalog, "01");
                    Assert.That(battle.TryRegisterGroup(new[] {
                        new CombatUnit { Id = 200, JobId = 100, Name = "Second ally", Hp = 50, MaxHp = 50 }
                    }), Is.True);
                    var ids = new short[] { 100, 200 };
                    Assert.That(scheduler.TrySelectMonsterTarget(400, ids, null, null, _ => true, _ => 1, out var target, out _), Is.False);
                    Assert.That(target, Is.Null);
                    Assert.That(scheduler.TrySelectMonsterTarget(400, new short[] { 100, 100 }, 100, _ => true, _ => true, _ => 1, out target, out _), Is.False);
                    Assert.That(scheduler.TrySelectMonsterTarget(400, ids, null, _ => true, _ => true, _ => 1, out target, out var reason), Is.False);
                    Assert.That(reason, Does.Contain("동거리")); Assert.That(target, Is.Null);
                    Assert.That(scheduler.TrySelectMonsterTarget(400, ids, 200, _ => true, _ => true, _ => 1, out target, out _), Is.True);
                    Assert.That(target, Is.EqualTo(200));
                }
                finally { UnityEngine.Object.DestroyImmediate(catalog); }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(active)); Assert.That(active.isDirty, Is.EqualTo(dirty));
        }
    }
}
