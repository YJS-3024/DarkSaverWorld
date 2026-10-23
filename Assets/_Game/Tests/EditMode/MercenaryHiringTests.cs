using System;
using System.IO;
using DarkSaver.Combat;
using DarkSaver.Core;
using DarkSaver.Editor;
using DarkSaver.Jobs;
using DarkSaver.Player;
using DarkSaver.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class MercenaryHiringTests
    {
        private static MercenaryHireCatalog Skills => AssetDatabase.LoadAssetAtPath<MercenaryHireCatalog>(MercenaryHireConverter.Output);
        private static JobCatalog Jobs => AssetDatabase.LoadAssetAtPath<JobCatalog>(JobCatalogConverter.MercenaryOutput);
        private static ExperienceProgressionTable Growth => AssetDatabase.LoadAssetAtPath<ExperienceProgressionTable>(ExperienceProgressionConverter.Output);

        [Test]
        public void NativeHireAndMercenaryTablesMatchAndPreserveMinusOneMp()
        {
            MercenaryHireConverter.Verify(Skills, MercenaryHireConverter.Parse(File.ReadAllText("분석/Saver_1.2/converted/solskill.csv")));
            JobCatalogConverter.Verify(Jobs, JobCatalogConverter.Parse(File.ReadAllText("분석/Saver_1.2/catalogs/mercenaries.csv")));
            Assert.That(Skills.TryGet(21101, out var infantry), Is.True); Assert.That(infantry.Maximum, Is.EqualTo(2));
            Assert.That(Skills.TryGet(21701, out var monk), Is.True); Assert.That(monk.Maximum, Is.EqualTo(4));
            Assert.That(Jobs.TryGet(200, out var soldier), Is.True); Assert.That(soldier.GetNumericValue(4), Is.EqualTo(-1));
            var copy = infantry.CopyJobIds(); copy[0] = 999;
            Assert.That(infantry.CopyJobIds()[0], Is.EqualTo(200));
            Assert.That(Skills.TryGet(21203, out var duplicate), Is.True);
            int repeated = 0; foreach (int id in duplicate.CopyJobIds()) if (id == 217) repeated++;
            Assert.That(repeated, Is.EqualTo(2));
        }

        [Test]
        public void DisplayQueriesAvoidAllocationsAndObserveHireAndRestoreImmediately()
        {
            WithTown((session, panel) => {
                var player = session.GetComponent<TopDownPlayerController>();
                Assert.That(panel.Open(TownService.Mercenary, player), Is.True);
                Assert.That(panel.HireMercenary(21101, 200), Is.True);
                Assert.That(Skills.TryGet(21101, out var skill), Is.True);
                var unit = session.CaptureOwnedRoster().units[0];
                Assert.That(session.CountPrimaryHires(skill.Name), Is.EqualTo(1));
                Assert.That(session.IsPrimaryHiredUnit(unit.persistentId), Is.True);
                long gold = session.CurrentGold.Value;
                int revision = session.OwnedRosterRevision;
                // Warm up before measuring only the value-query loop, not NUnit/UI allocations.
                session.CountPrimaryHires(skill.Name); session.IsPrimaryHiredUnit(unit.persistentId);
                long start = GC.GetAllocatedBytesForCurrentThread();
                int total = 0; bool valid = true;
                for (int i = 0; i < 128; i++) {
                    total += session.CountPrimaryHires(skill.Name);
                    valid &= session.IsPrimaryHiredUnit(unit.persistentId) && session.CurrentGold == gold;
                }
                long allocated = GC.GetAllocatedBytesForCurrentThread() - start;
                Assert.That(allocated, Is.Zero); Assert.That(total, Is.EqualTo(128)); Assert.That(valid, Is.True);
                Assert.That(session.OwnedRosterRevision, Is.EqualTo(revision));
                Assert.That(session.CountPrimaryHires(null), Is.Zero);
                Assert.That(session.IsPrimaryHiredUnit("missing"), Is.False);
                panel.Close();
                var state = session.CaptureSaveState("Kaosia_Map", "Town_Runtime", Vector2.one);
                state.inventory.gold = 0; state.ownedRoster.units = Array.Empty<NativeOwnedUnitData>();
                Assert.That(session.RestoreSaveState(state), Is.True);
                Assert.That(session.CurrentGold, Is.EqualTo(0));
                Assert.That(session.CountPrimaryHires(skill.Name), Is.Zero);
                Assert.That(session.IsPrimaryHiredUnit(unit.persistentId), Is.False);
            });
        }

        [Test]
        public void HireAvailabilityIsReadOnlyAndMatchesExecutionLimitsAndFunds()
        {
            WithTown((session, panel) => {
                Assert.That(panel.CanHireMercenary(21101, 200, out _), Is.False);
                var player = session.GetComponent<TopDownPlayerController>();
                Assert.That(panel.Open(TownService.Mercenary, player), Is.True);
                int revision = session.OwnedRosterRevision;
                long gold = session.CaptureInventory().gold;
                for (int i = 0; i < 20; i++) Assert.That(panel.CanHireMercenary(21101, 200, out _), Is.True);
                Assert.That(session.OwnedRosterRevision, Is.EqualTo(revision));
                Assert.That(session.CaptureInventory().gold, Is.EqualTo(gold));
                Assert.That(session.OwnedUnitCount, Is.Zero);
                Assert.That(panel.HireMessage, Is.Null);
                Assert.That(panel.CanHireMercenary(99999, 200, out _), Is.False);
                Assert.That(panel.CanHireMercenary(21101, 99999, out _), Is.False);
                Assert.That(panel.HireMercenary(21101, 200), Is.True);
                Assert.That(session.CaptureOwnedRoster().units[0].instanceId, Is.EqualTo(20000));
                Assert.That(panel.CanHireMercenary(21101, 201, out _), Is.True);
                Assert.That(panel.HireMercenary(21101, 201), Is.True);
                revision = session.OwnedRosterRevision;
                Assert.That(panel.CanHireMercenary(21101, 202, out var reason), Is.False);
                StringAssert.Contains("인원", reason);
                Assert.That(session.OwnedRosterRevision, Is.EqualTo(revision));
                panel.Close();
                var state = session.CaptureSaveState("Kaosia_Map", "Town_Runtime", Vector2.one);
                state.ownedRoster.units = Array.Empty<NativeOwnedUnitData>(); state.inventory.gold = 0;
                Assert.That(session.RestoreSaveState(state), Is.True);
                Assert.That(panel.Open(TownService.Mercenary, player), Is.True);
                Assert.That(panel.CanHireMercenary(21101, 200, out reason), Is.False);
                StringAssert.Contains("소지금", reason);
                Assert.That(panel.HireMercenary(21101, 200), Is.False);
                Assert.That(panel.HireMessage, Is.EqualTo(reason));
                Assert.That(session.OwnedUnitCount, Is.Zero);
            });
        }

        [Test]
        public void OneHireAtATimeCapsAtTwoAndPersistsGoldRosterAndType()
        {
            WithTown((session, panel) => {
                Assert.That(panel.HireMercenary(21101, 200), Is.False); // closed
                var player = session.GetComponent<TopDownPlayerController>();
                Assert.That(panel.Open(TownService.Mercenary, player), Is.True);
                Assert.That(panel.HireMercenary(21101, 200), Is.True); Assert.That(session.OwnedUnitCount, Is.EqualTo(1));
                Assert.That(panel.HireMercenary(21101, 201), Is.True); Assert.That(session.OwnedUnitCount, Is.EqualTo(2));
                var gold = session.CaptureInventory().gold;
                Assert.That(panel.HireMercenary(21101, 202), Is.False); Assert.That(session.CaptureInventory().gold, Is.EqualTo(gold));
                Assert.That(panel.HireMercenary(21701, 200), Is.False);
                var roster = session.CaptureOwnedRoster();
                Assert.That(roster.units[0].mp, Is.Zero); Assert.That(roster.units[0].maxMp, Is.Zero);
                Assert.That(roster.units[0].instanceId, Is.Not.EqualTo(roster.units[1].instanceId));
                Assert.That(roster.units[0].persistentId, Is.Not.EqualTo(roster.units[1].persistentId));
                Assert.That(roster.CreatePreparationDrafts().Count, Is.EqualTo(2));
                panel.Close();
                var formation = session.gameObject.AddComponent<BattleFormationPanel>();
                formation.Configure(null, 0, 0, null, player); formation.UseSessionRoster();
                Assert.That(formation.Open(), Is.True);
                Assert.That(formation.Assign(roster.units[0].persistentId, roster.units[0].persistentId), Is.False);
                Assert.That(formation.Message, Does.Contain("주인공")); formation.Cancel();
                panel.Open(TownService.Mercenary, player);
                var folder = Path.Combine(Application.temporaryCachePath, "HireCheck-" + Guid.NewGuid().ToString("N"));
                try
                {
                    var store = new NativeSaveStore(Path.Combine(folder, "save.json"));
                    Assert.That(store.TrySave(session.CaptureSaveState("Kaosia_Map", "Town_Runtime", Vector2.one), out _), Is.True);
                    Assert.That(store.TryLoad(out var loaded, out _), Is.True); panel.Close();
                    Assert.That(session.RestoreSaveState(loaded), Is.True);
                    Assert.That(session.CaptureInventory().gold, Is.EqualTo(gold));
                    Assert.That(session.CaptureOwnedRoster().units[0].hireType, Is.EqualTo("보병용병고용"));
                    Assert.That(panel.Open(TownService.Mercenary, player), Is.True);
                    Assert.That(panel.HireMercenary(21101, 200), Is.False);
                }
                finally
                {
                    if (File.Exists(Path.Combine(folder, "save.json"))) File.Delete(Path.Combine(folder, "save.json"));
                    if (Directory.Exists(folder)) Directory.Delete(folder);
                }
            });
        }

        [Test]
        public void FirstHireTypeLocksUntilEmptyAndInsufficientFundsDoNotPartiallyHire()
        {
            WithTown((session, panel) => {
                var save = session.CaptureSaveState("Kaosia_Map", "Town_Runtime", Vector2.one);
                save.character.hasClassAbilities = true; save.character.learnedMagicIds = Array.Empty<int>();
                save.character.usableSkillIds = new[] {21101, 21701}; // Multiple available skills, not permission to mix hired types.
                Assert.That(session.RestoreSaveState(save), Is.True);
                var player = session.GetComponent<TopDownPlayerController>(); panel.Open(TownService.Mercenary, player);
                for (int i = 0; i < 4; i++) Assert.That(panel.HireMercenary(21701, 200), Is.True);
                Assert.That(panel.HireMercenary(21701, 200), Is.False);
                string before=JsonUtility.ToJson(session.CaptureSaveState("Kaosia_Map","Town_Runtime",Vector2.one));
                Assert.That(panel.CanHireMercenary(21101,200,out var reason),Is.False);StringAssert.Contains("먼저 고용한",reason);
                Assert.That(panel.HireMercenary(21101,200),Is.False);
                Assert.That(JsonUtility.ToJson(session.CaptureSaveState("Kaosia_Map","Town_Runtime",Vector2.one)),Is.EqualTo(before));
                Assert.That(session.OwnedUnitCount,Is.EqualTo(4));
                var restored=JsonUtility.FromJson<NativeSaveData>(before);Assert.That(session.RestoreSaveState(restored),Is.True);
                Assert.That(panel.CanHireMercenary(21101,200,out _),Is.False);
                restored.ownedRoster.units=Array.Empty<NativeOwnedUnitData>();Assert.That(session.RestoreSaveState(restored),Is.True);
                Assert.That(panel.HireMercenary(21101,200),Is.True);Assert.That(panel.HireMercenary(21101,201),Is.True);
                Assert.That(panel.CanHireMercenary(21701,200,out _),Is.False);panel.Close();
                save = session.CaptureSaveState("Kaosia_Map", "Town_Runtime", Vector2.one);
                save.ownedRoster.units = Array.Empty<NativeOwnedUnitData>(); save.inventory.gold = 0;
                Assert.That(session.RestoreSaveState(save), Is.True); panel.Open(TownService.Mercenary, player);
                Assert.That(panel.HireMercenary(21101, 200), Is.False); Assert.That(session.OwnedUnitCount, Is.Zero);
                Assert.That(session.CaptureInventory().gold, Is.Zero);
            });
        }

        [Test]
        public void HiredSoldiersDeployUnderPrimaryAndReturnWithoutChangingPermanentIds()
        {
            WithTown((session, shop) => {
                var player = session.GetComponent<TopDownPlayerController>();
                shop.Open(TownService.Mercenary, player);
                Assert.That(shop.HireMercenary(21101, 200), Is.True);
                Assert.That(shop.HireMercenary(21101, 201), Is.True); shop.Close();
                var before = session.CaptureOwnedRoster();
                var panel = session.gameObject.AddComponent<BattleFormationPanel>();
                panel.Configure(null, 0, 0, null, player); panel.UseSessionRoster(); panel.Open();
                Assert.That(panel.ConfigurePlacementContext(8, 8, new short[] {100}, (_, _) => true), Is.True);
                for (int i = 0; i < 2; i++) {
                    Assert.That(panel.AssignToPrimary(before.units[i].persistentId), Is.True);
                    Assert.That(panel.TrySetPosition(before.units[i].persistentId, new CombatGridCell(i, 0)), Is.True);
                }
                Assert.That(panel.AssignToPrimary(before.units[0].persistentId), Is.False);
                Assert.That(panel.Confirm(), Is.True, panel.Message);
                var deployment = panel.ConfirmedSnapshot;
                Assert.That(deployment.Members[0].Unit.Id, Is.EqualTo(101));
                Assert.That(deployment.Members[1].Unit.Id, Is.EqualTo(102));
                Assert.That(session.MatchesOwnedFormation(deployment), Is.True);
                panel.Open(); Assert.That(panel.Confirm(), Is.True, panel.Message); // Reopen retains aliases and coordinates.
                var battleObject = new GameObject("Hired deployment integration");
                try {
                    var battle = battleObject.AddComponent<BattleSceneRoot>();
                    var mainJobs = AssetDatabase.LoadAssetAtPath<JobCatalog>("Assets/_Game/Runtime/Jobs/JobCatalog.asset");
                    Assert.That(battle.ConfigureTraversalCatalogs(new[] {mainJobs, Jobs}), Is.True);
                    Assert.That(battle.TryRegisterAlliedFormation(deployment, out var reason), Is.True, reason);
                    Assert.That(battle.TryGetUnit(101, out var soldier), Is.True);
                    Assert.That(soldier.JobId, Is.EqualTo(200));
                    Assert.That(battle.ApplyHp(101, soldier.MaxHp - 7), Is.True);
                    Assert.That(session.TryCaptureBattleReturn(battle, deployment, out reason), Is.True, reason);
                    var returned = session.CaptureOwnedRoster();
                    Assert.That(returned.units[0].hp, Is.EqualTo(soldier.MaxHp - 7));
                    for (int i = 0; i < 2; i++) {
                        Assert.That(returned.units[i].instanceId, Is.EqualTo(before.units[i].instanceId));
                        Assert.That(returned.units[i].persistentId, Is.EqualTo(before.units[i].persistentId));
                        Assert.That(returned.units[i].mp, Is.Zero);
                    }
                    var json = JsonUtility.ToJson(session.CaptureSaveState("Kaosia_Map", "Town_Runtime", Vector2.one));
                    Assert.That(session.RestoreSaveState(JsonUtility.FromJson<NativeSaveData>(json)), Is.True);
                    Assert.That(session.CaptureOwnedRoster().units[0].hp, Is.EqualTo(soldier.MaxHp - 7));
                } finally { UnityEngine.Object.DestroyImmediate(battleObject); }
            });
        }

        [Test]
        public void PrimarySlotsCapAtFourAndForgedOwnershipCannotReturn()
        {
            WithTown((session, shop) => {
                var save = session.CaptureSaveState("Kaosia_Map", "Town_Runtime", Vector2.one);
                save.character.hasClassAbilities = true; save.character.learnedMagicIds = Array.Empty<int>();
                save.character.usableSkillIds = new[] {21101, 21701}; session.RestoreSaveState(save);
                var player = session.GetComponent<TopDownPlayerController>(); shop.Open(TownService.Mercenary, player);
                for (int i = 0; i < 4; i++) Assert.That(shop.HireMercenary(21701, 200), Is.True);
                Assert.That(shop.HireMercenary(21101,200),Is.False);shop.Close();
                // Existing over-cap save fixture tests formation's independent four-slot contract.
                var legacy=session.CaptureSaveState("Kaosia_Map","Town_Runtime",Vector2.one);
                var extra=legacy.ownedRoster.units[0].Clone();extra.persistentId="legacy-extra";extra.instanceId=20004;
                var legacyUnits=new System.Collections.Generic.List<NativeOwnedUnitData>(legacy.ownedRoster.units){extra};
                legacy.ownedRoster.units=legacyUnits.ToArray();Assert.That(session.RestoreSaveState(legacy),Is.True);
                var units = session.CaptureOwnedRoster().units;
                var panel = session.gameObject.AddComponent<BattleFormationPanel>();
                panel.Configure(null, 0, 0, null, player); panel.UseSessionRoster(); panel.Open();
                panel.ConfigurePlacementContext(8, 8, new short[] {100}, (_, _) => true);
                for (int i = 0; i < 4; i++) {
                    Assert.That(panel.AssignToPrimary(units[i].persistentId), Is.True);
                    Assert.That(panel.TrySetPosition(units[i].persistentId, new CombatGridCell(i, 0)), Is.True);
                }
                Assert.That(panel.AssignToPrimary(units[4].persistentId), Is.False);
                Assert.That(panel.Confirm(), Is.True, panel.Message);
                var snapshot = panel.ConfirmedSnapshot;
                var observations = new System.Collections.Generic.List<CombatUnitSnapshot>();
                foreach (var member in snapshot.Members) observations.Add(member.Unit);
                Assert.That(BattleFormationReturn.TryCapture(snapshot, observations, out var result, out _), Is.True);
                var forged = session.CaptureOwnedRoster(); forged.units[0].hireOwnerId = 0; forged.units[0].hireType = null;
                Assert.That(forged.TryApplyBattleReturn(result, out var updated, out _), Is.False);
                Assert.That(updated, Is.Null);
                Assert.That(BattleFormation.MatchesOwnedIdentity(20000, 100, 105, 20000, 100, BattleFormation.PrimaryCommanderPersistentId), Is.False);
            });
        }

        [Test]
        public void DeadOrUnclassifiedOwnedMercenaryDoesNotUnlockAnotherHireType()
        {
            WithTown((session,panel)=> {
                var save=session.CaptureSaveState("Kaosia_Map","Town_Runtime",Vector2.one);
                save.character.hasClassAbilities=true;save.character.learnedMagicIds=Array.Empty<int>();save.character.usableSkillIds=new[]{21101,21701};
                Assert.That(session.RestoreSaveState(save),Is.True);
                Assert.That(panel.Open(TownService.Mercenary,session.GetComponent<TopDownPlayerController>()),Is.True);
                Assert.That(panel.HireMercenary(21101,200),Is.True);
                save=session.CaptureSaveState("Kaosia_Map","Town_Runtime",Vector2.one);
                save.ownedRoster.units[0].hp=0;save.ownedRoster.units[0].lifeState=1;
                Assert.That(session.RestoreSaveState(save),Is.True);
                Assert.That(panel.CanHireMercenary(21701,200,out var reason),Is.False);StringAssert.Contains("먼저 고용한",reason);
                save.ownedRoster.units[0].hireType=null;save.ownedRoster.units[0].hireOwnerId=0;
                Assert.That(session.RestoreSaveState(save),Is.True);
                Assert.That(panel.CanHireMercenary(21701,200,out reason),Is.False);StringAssert.Contains("미확인",reason);
                save.ownedRoster.units=Array.Empty<NativeOwnedUnitData>();
                Assert.That(session.RestoreSaveState(save),Is.True);
                Assert.That(panel.CanHireMercenary(21701,200,out _),Is.True);
            });
        }

        [Test]
        public void SameNamedHigherStageHireSkillKeepsTheExistingType()
        {
            WithTown((session,panel)=> {
                Assert.That(Skills.TryGet(21101,out var first),Is.True);Assert.That(Skills.TryGet(21102,out var next),Is.True);
                Assert.That(next.Name,Is.EqualTo(first.Name));
                var save=session.CaptureSaveState("Kaosia_Map","Town_Runtime",Vector2.one);
                save.character.hasClassAbilities=true;save.character.learnedMagicIds=Array.Empty<int>();save.character.usableSkillIds=new[]{21101,21102};
                save.inventory.gold=1000000;Assert.That(session.RestoreSaveState(save),Is.True);
                Assert.That(panel.Open(TownService.Mercenary,session.GetComponent<TopDownPlayerController>()),Is.True);
                Assert.That(panel.HireMercenary(21101,200),Is.True);
                int candidate=-1;foreach(int id in next.CopyJobIds())if(Jobs.TryGet(id,out var job) && job.GetNumericValue(4)==-1){candidate=id;break;}
                Assert.That(candidate,Is.GreaterThan(0));
                Assert.That(panel.HireMercenary(21102,candidate),Is.True,panel.HireMessage);
                Assert.That(session.CountPrimaryHires(first.Name),Is.EqualTo(2));
            });
        }
        private static void WithTown(Action<BattleTransitionController, TownServicesPanel> check)
        {
            var active = SceneManager.GetActiveScene(); var town = SceneManager.GetSceneByName("Town_Runtime"); bool created = !town.isLoaded;
            if (created) { town = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive); town.name = "Town_Runtime"; }
            var root = new GameObject("Mercenary hire check"); SceneManager.MoveGameObjectToScene(root, town);
            try
            {
                root.AddComponent<TopDownPlayerController>(); var session = root.AddComponent<BattleTransitionController>();
                session.InitializeCharacter(new NativeCharacterProfile {name = "검증", jobId = 100});
                var save = session.CaptureSaveState("Kaosia_Map", "Town_Runtime", Vector2.one); save.inventory.gold = 10000; // test funding only
                Assert.That(session.RestoreSaveState(save), Is.True);
                var panel = root.AddComponent<TownServicesPanel>(); panel.ConfigureProgression(Growth); panel.ConfigureHiring(Skills, Jobs);
                check(session, panel);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); if (created) EditorSceneManager.CloseScene(town, true); if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active); }
        }
    }
}
