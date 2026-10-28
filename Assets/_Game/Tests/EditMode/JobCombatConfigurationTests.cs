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
        public void KnownMagicStatsDoNotAuthorizeAnUnresolvedDamageFormula()
        {
            var world=new CombatWorld();
            var actor=NativeCombatStateFixture.Received(new CombatUnit{Id=100,Hp=50,MaxHp=100,Mp=17,MaxMp=30,Attack=20,Defense=4},20);
            var target=NativeCombatStateFixture.Received(new CombatUnit{Id=800,Enemy=true,Hp=75,MaxHp=100,Mp=11,MaxMp=30,Attack=12,Defense=7},20);
            world.Add(actor);world.Add(target);var authority=new CombatServerBoundary();var flow=new CombatFlow(world,authority,true);
            actor.HasMagicStats=true;actor.MagicAttack=20;target.HasMagicStats=true;target.MagicDefense=8;
            Assert.IsFalse(authority.ConfigureTemporaryDamageMagic(201,2,4,10,2));
            Assert.Throws<System.NotSupportedException>(()=>authority.Resolve(world,new CombatRequest(100,800,CombatActionType.Magic,201)));
            Assert.AreEqual(20,actor.MagicAttack);Assert.AreEqual(8,target.MagicDefense);Assert.AreEqual(75,target.Hp);
        }

        [Test]
        public void StatAllocationRequiresAuthorityWhileReceivedAllocationsRemainIdempotent()
        {
            var profile=new DarkSaver.Core.NativeCharacterProfile{name="검증",jobId=100,unspentClassChangeStatPoints=20};
            string before=JsonUtility.ToJson(profile);
            Assert.IsFalse(profile.TryBuildStatAllocation(3,4,2,6,out var candidate));Assert.IsNull(candidate);
            Assert.AreEqual(before,JsonUtility.ToJson(profile));
            // A separately received profile may carry allocations; client copies them without allocating more points.
            var received=profile.Clone();received.allocatedAttack=3;received.allocatedDefense=4;received.allocatedMagicAttack=2;received.allocatedMagicDefense=6;
            Assert.IsTrue(received.IsValid);Assert.IsTrue(Catalog.TryGet(100,out var job));
            WithBattle(battle=>{Assert.IsTrue(battle.ConfigureTraversalCatalogs(new[]{Catalog}));Assert.IsTrue(battle.ConfigurePlayerJob(100));
                for(int i=0;i<2;i++){Assert.IsTrue(battle.ConfigurePlayerJobBaseStats(received));battle.TryGetUnit(100,out var unit);
                    Assert.AreEqual((int)job.GetNumericValue(6)+3,unit.Attack);Assert.AreEqual((int)job.GetNumericValue(8)+4,unit.Defense);}
            });
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
                    Assert.That(UnresolvedJobActionCosts.TryGetAttackApCost(job, out var cost), Is.False);Assert.AreEqual(0,cost);
                    battle.SetTemporaryLocalRules(true);
                    Assert.That(battle.PlayerAttackApCost, Is.EqualTo(cost));
                    battle.SetTemporaryLocalRules(false);
                    Assert.That(battle.PlayerAttackApCost, Is.EqualTo(GameConstants.GetBattleAttackApCost(id)));
                }
            });
        }

        [TestCase(100)] [TestCase(105)] [TestCase(115)] [TestCase(125)] [TestCase(135)]
        [TestCase(145)] [TestCase(155)] [TestCase(165)] [TestCase(169)]
        public void TierDataAloneCannotInventAttackCostOrServerVerdict(int jobId)
        {
            WithBattle(battle=>{
                Assert.IsTrue(battle.ConfigureTraversalCatalogs(new[]{Catalog}));Assert.IsTrue(battle.ConfigurePlayerJob(jobId));Assert.IsTrue(battle.ConfigurePlayerJobBaseStats());
                battle.SetTemporaryLocalRules(true);Assert.IsFalse(battle.TemporaryLocalRulesEnabled);Assert.AreEqual(0,battle.PlayerAttackApCost);
                NativeCombatStateFixture.ReceiveFullTicks(battle);int beforeHp=battle.EnemyHp,ap=battle.PlayerAp;
                Assert.IsFalse(battle.GetComponent<BattleRequestSender>().TrySendAttack(new AttackRequest(800,100,0,0),out _));
                Assert.AreEqual(beforeHp,battle.EnemyHp);Assert.AreEqual(ap,battle.PlayerAp);Assert.IsNull(battle.LastAttackPresentation);
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
            Assert.That(UnresolvedJobActionCosts.TryGetAttackApCost(job, out _), Is.False);
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
                NativeCombatStateFixture.Initialize(root.GetComponent<BattleSceneRoot>(),6);check(root.GetComponent<BattleSceneRoot>());
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
