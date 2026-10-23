using DarkSaver.Combat;
using DarkSaver.Jobs;
using DarkSaver.Network;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class JobCombatConfigurationTests
    {
        private static JobCatalog Catalog => AssetDatabase.LoadAssetAtPath<JobCatalog>("Assets/_Game/Runtime/Jobs/JobCatalog.asset");

        [Test]
        public void TemporaryMagicUsesKnownMagicStatsSeparatelyFromPhysicalStats()
        {
            var world=new CombatWorld();
            var actor=new CombatUnit {Id=100,Hp=100,MaxHp=100,Mp=10,MaxMp=10,Attack=999,Defense=999,HasMagicStats=true,MagicAttack=20,MagicDefense=1};
            var target=new CombatUnit {Id=800,Enemy=true,Hp=100,MaxHp=100,Attack=999,Defense=999,HasMagicStats=true,MagicAttack=1,MagicDefense=8};
            world.Add(actor);world.Add(target);var authority=new LocalCombatAuthority {TemporaryRulesEnabled=true};
            Assert.IsTrue(authority.ConfigureTemporaryDamageMagic(201,2,4,10,2));
            var request=new CombatRequest(100,800,CombatActionType.Magic,201);
            int first=authority.Resolve(world,request).Damage;
            actor.Attack=1;target.Defense=1;Assert.AreEqual(first,authority.Resolve(world,request).Damage);
            actor.MagicAttack+=3;Assert.AreEqual(first+3,authority.Resolve(world,request).Damage);
            target.MagicDefense+=4;Assert.AreEqual(first+1,authority.Resolve(world,request).Damage);
            target.HasMagicAttack=false;Assert.IsFalse(target.HasMagicStats);Assert.IsTrue(target.HasMagicDefense);
            Assert.AreEqual(first+1,authority.Resolve(world,request).Damage);
            Assert.IsTrue(world.TryRegisterGroup(new[]{new CombatUnit {Id=810,Enemy=true,Hp=10,MaxHp=10,HasMagicStats=true,MagicAttack=7,MagicDefense=9}}));
            Assert.IsTrue(world.TryGetSnapshot(810,out var copy));Assert.IsTrue(copy.HasMagicStats);Assert.AreEqual(9,copy.MagicDefense);
            Assert.IsFalse(world.TryRegisterGroup(new[]{new CombatUnit {Id=820,Enemy=true,Hp=10,MaxHp=10,HasMagicStats=true,MagicAttack=-1}}));
        }

        [Test]
        public void ConfirmedPhysicalAllocationIsAppliedOnceAndPreservedWithSoldiers()
        {
            var profile=new DarkSaver.Core.NativeCharacterProfile {name="검증",jobId=100,unspentClassChangeStatPoints=20};
            Assert.That(profile.TryBuildStatAllocation(3,4,2,6,out var allocated),Is.True);
            Assert.That(Catalog.TryGet(100,out var job),Is.True);
            WithBattle(battle=> {
                Assert.That(battle.ConfigureTraversalCatalogs(new[]{Catalog}),Is.True);
                Assert.That(battle.ConfigurePlayerJob(100),Is.True);
                for(int i=0;i<2;i++) {
                    Assert.That(battle.ConfigurePlayerJobBaseStats(allocated),Is.True);
                    Assert.That(battle.TryGetUnit(100,out var unit),Is.True);
                    Assert.That((unit.Attack,unit.Defense),Is.EqualTo(((int)job.GetNumericValue(6)+3,(int)job.GetNumericValue(8)+4)));
                    Assert.IsTrue(unit.HasMagicStats);Assert.AreEqual((int)job.GetNumericValue(10)+2,unit.MagicAttack);Assert.AreEqual((int)job.GetNumericValue(12)+6,unit.MagicDefense);
                    Assert.That((unit.MaxHp,unit.MaxMp),Is.EqualTo(((int)job.GetNumericValue(2),(int)job.GetNumericValue(4))));
                }
                var soldier=new CombatUnit {Id=101,JobId=100,Name="검증 용병",Hp=10,MaxHp=10,Attack=1,Defense=1};
                Assert.That(battle.TryRegisterAlliedFormationGroup(new[]{soldier},out _),Is.True);
                Assert.That(battle.TryGetUnit(100,out var commander),Is.True);
                Assert.That((commander.Attack,commander.Defense),Is.EqualTo(((int)job.GetNumericValue(6)+3,(int)job.GetNumericValue(8)+4)));
                battle.SetTemporaryLocalRules(true);
                battle.ConfigurePhysicalAccuracy(100,100,0); battle.ConfigurePhysicalAccuracy(800,100,0);
                battle.AdvanceActionPoints(14);
                Assert.That(battle.TryGetUnit(800,out var enemyBefore),Is.True);
                int expected=TemporaryCombatDamageRules.Calculate(commander.Attack,enemyBefore.Defense,false);
                Assert.That(battle.GetComponent<BattleRequestSender>().TrySendAttack(new AttackRequest(800,100,0,0),out _),Is.True);
                Assert.That(battle.TryGetUnit(800,out var enemyAfter),Is.True);
                Assert.That(enemyAfter.Hp,Is.EqualTo(System.Math.Max(0,enemyBefore.Hp-expected)));
            });
            Assert.That((allocated.allocatedAttack,allocated.allocatedDefense,allocated.allocatedMagicAttack,allocated.allocatedMagicDefense),Is.EqualTo((3,4,2,6)));
        }

        [Test]
        public void InvalidOrOverflowingAllocationDoesNotPartiallyChangeBattleStats()
        {
            WithBattle(battle=> {
                battle.ConfigureTraversalCatalogs(new[]{Catalog}); battle.ConfigurePlayerJob(100);
                Assert.That(battle.ConfigurePlayerJobBaseStats(),Is.True);
                Assert.That(battle.TryGetUnit(100,out var before),Is.True);
                var profile=new DarkSaver.Core.NativeCharacterProfile {name="검증",jobId=100,allocatedAttack=int.MaxValue};
                Assert.That(battle.ConfigurePlayerJobBaseStats(profile),Is.False);
                profile.allocatedAttack=0; profile.allocatedDefense=int.MaxValue;
                Assert.That(battle.ConfigurePlayerJobBaseStats(profile),Is.False);
                profile.allocatedDefense=-1; Assert.That(battle.ConfigurePlayerJobBaseStats(profile),Is.False);
                profile.allocatedDefense=0; profile.jobId=105;
                Assert.That(battle.ConfigurePlayerJobBaseStats(profile),Is.False);
                Assert.That(battle.TryGetUnit(100,out var after),Is.True);
                Assert.That((after.MaxHp,after.MaxMp,after.Attack,after.Defense),Is.EqualTo((before.MaxHp,before.MaxMp,before.Attack,before.Defense)));
            });
        }

        [Test]
        public void AllSeventyJobsPreserveNativeBaseStatsAndSeparateTemporaryCost()
        {
            WithBattle(battle => {
                Assert.That(battle.ConfigureTraversalCatalogs(new[] { Catalog }), Is.True);
                for (int id = 100; id <= 169; id++)
                {
                    Assert.That(Catalog.TryGet(id, out var job), Is.True);
                    Assert.That(battle.ConfigurePlayerJob(id), Is.True);
                    Assert.That(battle.ConfigurePlayerJobBaseStats(), Is.True, "job=" + id);
                    Assert.That(battle.TryGetUnit(100, out var unit), Is.True);
                    Assert.IsTrue(unit.HasMagicAttack && unit.HasMagicDefense);
                    Assert.AreEqual((int)job.GetNumericValue(10),unit.MagicAttack);Assert.AreEqual((int)job.GetNumericValue(12),unit.MagicDefense);
                    Assert.That((unit.MaxHp, unit.MaxMp, unit.Attack, unit.Defense),
                        Is.EqualTo(((int)job.GetNumericValue(2), (int)job.GetNumericValue(4),
                            (int)job.GetNumericValue(6), (int)job.GetNumericValue(8))));
                    Assert.That(TemporaryJobCombatRules.TryGetAttackApCost(job, out var cost), Is.True);
                    battle.SetTemporaryLocalRules(true);
                    Assert.That(battle.PlayerAttackApCost, Is.EqualTo(cost));
                    battle.SetTemporaryLocalRules(false);
                    Assert.That(battle.PlayerAttackApCost, Is.EqualTo(GameConstants.GetBattleAttackApCost(id)));
                }
            });
        }

        [TestCase(100)] [TestCase(105)] [TestCase(115)] [TestCase(125)] [TestCase(135)]
        [TestCase(145)] [TestCase(155)] [TestCase(165)] [TestCase(169)]
        public void TierRepresentativeAttackSpendsItsTemporaryCostOnce(int jobId)
        {
            WithBattle(battle => {
                Assert.That(battle.ConfigureTraversalCatalogs(new[] { Catalog }), Is.True);
                Assert.That(battle.ConfigurePlayerJob(jobId), Is.True);
                Assert.That(battle.ConfigurePlayerJobBaseStats(), Is.True);
                battle.SetTemporaryLocalRules(true);
                battle.ConfigurePhysicalAccuracy(100, 100, 0);
                battle.ConfigurePhysicalAccuracy(800, 100, 0);
                battle.AdvanceActionPoints(14);
                var cost = battle.PlayerAttackApCost;
                var sender = battle.GetComponent<BattleRequestSender>();
                Assert.That(sender.TrySendAttack(new AttackRequest(800, 100, 0, 0), out _), Is.True);
                Assert.That(battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp - cost));
                Assert.That(sender.TrySendAttack(new AttackRequest(800, 100, 0, 0), out _), Is.False);
            });
        }

        [Test]
        public void ExplicitResolvedCostsOverrideTemporaryJobCostInBothModes()
        {
            WithBattle(battle => {
                battle.ConfigureTraversalCatalogs(new[] { Catalog });
                battle.ConfigurePlayerJob(107); battle.ConfigurePlayerJobBaseStats();
                battle.SetTemporaryLocalRules(true);
                Assert.That(battle.ConfigureActionPointCosts(2, 6), Is.True);
                Assert.That(battle.PlayerAttackApCost, Is.EqualTo(2));
                battle.SetTemporaryLocalRules(false);
                Assert.That(battle.PlayerAttackApCost, Is.EqualTo(2));
            });
        }

        [TestCase(0L)] [TestCase(-1L)] [TestCase(11L)] [TestCase(long.MaxValue)]
        public void InvalidRawTicksAreRejectedWithoutClamping(long tick)
        {
            var values = new long[JobCatalogEntry.NumericColumnCount]; values[0] = 107; values[25] = tick;
            var job = new JobCatalogEntry(values, "", "fixture", "", "player_job");
            Assert.That(TemporaryJobCombatRules.TryGetAttackApCost(job, out _), Is.False);
        }

        [Test]
        public void MissingTemporaryTickDoesNotBlockNativeBaseStats()
        {
            var catalog = ScriptableObject.CreateInstance<JobCatalog>();
            try
            {
                var values = new long[JobCatalogEntry.NumericColumnCount];
                values[0] = 107; values[2] = 120; values[4] = 30; values[6] = 40; values[8] = 20;
                catalog.Initialize(new[] { new JobCatalogEntry(values, "", "fixture", "", "player_job") });
                WithBattle(battle => {
                    battle.ConfigureTraversalCatalogs(new[] { catalog }); battle.ConfigurePlayerJob(107);
                    battle.SetTemporaryLocalRules(false);
                    Assert.That(battle.ConfigurePlayerJobBaseStats(), Is.True);
                    Assert.That(battle.TryGetUnit(100, out var unit), Is.True);
                    Assert.IsTrue(unit.HasMagicAttack && unit.HasMagicDefense);
                    Assert.AreEqual((int)values[10],unit.MagicAttack);Assert.AreEqual((int)values[12],unit.MagicDefense);
                    Assert.That((unit.MaxHp, unit.MaxMp, unit.Attack, unit.Defense), Is.EqualTo((120, 30, 40, 20)));
                });
            }
            finally { Object.DestroyImmediate(catalog); }
        }

        private static void WithBattle(System.Action<BattleSceneRoot> check)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("Job combat check"); SceneManager.MoveGameObjectToScene(root, scene);
                root.AddComponent<BattleRequestSender>();
                Assert.That(root.AddComponent<BattlePacketReceiver>().Bind(), Is.True);
                check(root.GetComponent<BattleSceneRoot>());
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
