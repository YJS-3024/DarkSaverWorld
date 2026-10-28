using System;
using System.IO;
using DarkSaver.Combat;
using DarkSaver.Core;
using DarkSaver.Player;
using DarkSaver.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class NativeOwnedRosterTests
    {
        private static NativeOwnedUnitData Unit(int id) => new()
        {
            persistentId = "owned-" + id, instanceId = id, jobId = 100, name = "검증 유닛 " + id,
            hp = 40, maxHp = 50, mp = 5, maxMp = 10, attack = 0, defense = 0,
            experience = uint.MaxValue, teamId = 1, recordEligible = true,
            finalStateConfirmed = true, learnedMagicIds = new short[] { 701 }
        };
        private static NativeOwnedRoster Roster() => new() { units = new[] { Unit(200), Unit(209), Unit(300) } };
        private static NativeSaveData Save(bool roster) => new()
        {
            format = NativeSaveStore.Format, version = roster ? NativeSaveStore.RosterVersion : NativeSaveStore.Version,
            mapScene = "Kaosia_Map", runtimeScene = "Town_Runtime", clearedScenarios = new[] { "01" },
            hasVitals = true, hp = 37, maxHp = 120, mp = 30, maxMp = 30, experience = uint.MaxValue,
            hasOwnedRoster = roster, ownedRoster = roster ? Roster() : null
        };

        [Test]
        public void V2DiskRoundTripAndV1UpgradeKeepBackupAndRejectDowngrade()
        {
            Files(store => {
                Assert.That(store.TrySave(Save(false), out _), Is.True);
                Assert.That(store.TrySave(Save(true), out _), Is.True);
                Assert.That(store.TryLoad(out var loaded, out _), Is.True);
                Assert.That(loaded.version, Is.EqualTo(2)); Assert.That(loaded.ownedRoster.units.Length, Is.EqualTo(3));
                Assert.That(loaded.ownedRoster.units[1].experience, Is.EqualTo(uint.MaxValue));
                Assert.That(loaded.ownedRoster.units[0].learnedMagicIds, Is.EqualTo(new short[] { 701 }));
                var backup = new NativeSaveStore(store.FilePath + ".bak");
                Assert.That(backup.TryLoad(out var old, out _), Is.True); Assert.That(old.version, Is.EqualTo(1));
                var before = File.ReadAllText(store.FilePath); var oldBackup = File.ReadAllText(backup.FilePath);
                Assert.That(store.TrySave(Save(false), out _), Is.False);
                Assert.That(File.ReadAllText(store.FilePath), Is.EqualTo(before)); Assert.That(File.ReadAllText(backup.FilePath), Is.EqualTo(oldBackup));
                Assert.That(Directory.GetFiles(Path.GetDirectoryName(store.FilePath), "*.tmp-*"), Is.Empty);
            });
        }

        [Test]
        public void InvalidRosterCannotReplaceValidSaveOrLeakPartialData()
        {
            Files(store => {
                Assert.That(store.TrySave(Save(true), out _), Is.True); var before = File.ReadAllText(store.FilePath);
                for (int kind = 0; kind < 14; kind++)
                {
                    var bad = Save(true); var unit = bad.ownedRoster.units[0];
                    switch (kind)
                    {
                        case 0: bad.ownedRoster = null; break;
                        case 1: bad.hasOwnedRoster = false; break;
                        case 2: bad.version = 99; break;
                        case 3: unit.finalStateConfirmed = false; break;
                        case 4: unit.instanceId = 100; break;
                        case 5: unit.instanceId = 40000; break;
                        case 6: unit.persistentId = "owned-209"; break;
                        case 7: unit.instanceId = 209; break;
                        case 8: unit.hp = -1; break;
                        case 9: unit.mp = 11; break;
                        case 10: unit.lifeState = 2; break;
                        case 11: unit.learnedMagicIds = null; break;
                        case 12: unit.learnedMagicIds = new short[] { 701, 701 }; break;
                        case 13: bad.version = 1; bad.hasOwnedRoster = false; break;
                    }
                    Assert.That(NativeSaveStore.IsValid(bad), Is.False, "kind " + kind);
                    Assert.That(store.TrySave(bad, out _), Is.False);
                    Assert.That(File.ReadAllText(store.FilePath), Is.EqualTo(before));
                }
            });
        }

        [Test]
        public void OversizedOutputIsRejectedBeforeReplacingOrBackingUp()
        {
            Files(store => {
                Assert.That(store.TrySave(Save(false), out _), Is.True); var before = File.ReadAllText(store.FilePath);
                var large = Save(true); large.ownedRoster.units[0].name = new string('가', 400000);
                Assert.That(NativeSaveStore.IsValid(large), Is.True); Assert.That(store.TrySave(large, out _), Is.False);
                Assert.That(File.ReadAllText(store.FilePath), Is.EqualTo(before)); Assert.That(File.Exists(store.FilePath + ".bak"), Is.False);
                Assert.That(Directory.GetFiles(Path.GetDirectoryName(store.FilePath), "*.tmp-*"), Is.Empty);
            });
        }

        [Test]
        public void SessionRestoresCopiesAndLegacyLoadDoesNotCreateMercenaries()
        {
            Session((session, panel, player) => {
                var source = Save(true); Assert.That(session.RestoreSaveState(source), Is.True);
                Assert.That(session.OwnedUnitCount, Is.EqualTo(3));
                source.ownedRoster.units[0].hp = 1; source.ownedRoster.units[0].learnedMagicIds[0] = 702;
                var copy = session.CaptureOwnedRoster(); Assert.That(copy.units[0].hp, Is.EqualTo(40));
                Assert.That(copy.units[0].learnedMagicIds[0], Is.EqualTo(701)); copy.units[0].hp = 2;
                var saved = session.CaptureSaveState("Kaosia_Map", "Town_Runtime", Vector2.zero);
                Assert.That(saved.version, Is.EqualTo(2)); Assert.That(saved.ownedRoster.units[0].hp, Is.EqualTo(40));
                var invalid = Save(true); invalid.ownedRoster.units[1].hp = -1;
                var before = JsonUtility.ToJson(saved); Assert.That(session.RestoreSaveState(invalid), Is.False);
                Assert.That(JsonUtility.ToJson(session.CaptureSaveState("Kaosia_Map", "Town_Runtime", Vector2.zero)), Is.EqualTo(before));
                Assert.That(session.RestoreSaveState(Save(false)), Is.True); Assert.That(session.OwnedUnitCount, Is.Zero);
                var legacy = session.CaptureSaveState("Kaosia_Map", "Town_Runtime", Vector2.zero);
                Assert.That(legacy.version, Is.EqualTo(1)); Assert.That(legacy.hasOwnedRoster, Is.False);
            });
        }

        [Test]
        public void ResumeQueueDeepCopiesOwnedUnitsAndMagicAndConsumesOnce()
        {
            Files(store => {
                try {
                    var source = Save(true); Assert.That(NativeSaveStore.QueueResume(source, store.FilePath), Is.True);
                    source.ownedRoster.units[0].hp = 1; source.ownedRoster.units[0].learnedMagicIds[0] = 702;
                    Assert.That(NativeSaveStore.TakeResume(out var resumed, out _), Is.True);
                    Assert.That(resumed.ownedRoster.units[0].hp, Is.EqualTo(40));
                    Assert.That(resumed.ownedRoster.units[0].learnedMagicIds[0], Is.EqualTo(701));
                    Assert.That(NativeSaveStore.TakeResume(out _, out _), Is.False);
                } finally { NativeSaveStore.ClearResume(); }
            });
        }

        [Test]
        public void PanelReceivesSessionOwnershipWithoutCreatingCoordinatesOrDeploying()
        {
            Session((session, panel, player) => {
                var roster = Roster(); Assert.That(session.TrySetOwnedRoster(roster, out _), Is.True);
                roster.units[0].hp = 1;
                panel.UseSessionRoster(); Assert.That(panel.Open(), Is.True); Assert.That(panel.OwnedCount, Is.EqualTo(3));
                Assert.That(session.TrySetOwnedRoster(Roster(), out _), Is.False);
                Assert.That(panel.Assign("owned-200", "owned-200"), Is.True);
                Assert.That(panel.Assign("owned-209", "owned-200"), Is.True);
                Assert.That(panel.Confirm(), Is.False); Assert.That(panel.ConfirmedSnapshot, Is.Null);
                panel.Cancel(); Assert.That(session.CaptureOwnedRoster().units[0].hp, Is.EqualTo(40));
                var dead = Roster(); dead.units[1].hp = 0; dead.units[1].lifeState = 2;
                Assert.That(session.TrySetOwnedRoster(dead, out _), Is.True);
                panel.Open(); panel.Assign("owned-200", "owned-200");
                Assert.That(panel.Assign("owned-209", "owned-200"), Is.False); panel.Cancel();
            });
        }

        [Test]
        public void ObservedReturnUpdatesOnlyKnownVitalsAndExperienceAtomically()
        {
            var roster = Roster(); var drafts = roster.CreatePreparationDrafts();
            var deployed = new[] { drafts[0], drafts[1] };
            for (var i = 0; i < 2; i++) {
                deployed[i].CommanderId = 200; deployed[i].CommanderPersistentId = "owned-200";
                deployed[i].Position = new CombatGridCell(i, 0); deployed[i].Unit.X = i;
            }
            Assert.That(BattleFormation.TryCreateSnapshot(deployed, 20, 20, Array.Empty<short>(), out var formation, out _), Is.True);
            var world = new CombatWorld(); world.Add(deployed[0].Unit); world.Add(deployed[1].Unit);
            deployed[0].Unit.Hp = 17; deployed[0].Unit.Mp = 2; deployed[0].Unit.Experience = 0; deployed[0].Unit.Attack = 999;
            deployed[1].Unit.Hp = 0; deployed[1].Unit.Mp = 0; deployed[1].Unit.LifeState = UnitLifeState.Removed;
            world.TryGetSnapshot(200, out var one); world.TryGetSnapshot(209, out var two);
            Assert.That(BattleFormationReturn.TryCapture(formation, new[] { two, one }, out var returned, out _), Is.True);
            Assert.That(roster.TryApplyObservedReturn(returned, out var updated, out _), Is.True);
            Assert.That(updated.units[0].hp, Is.EqualTo(17)); Assert.That(updated.units[0].mp, Is.EqualTo(2));
            Assert.That(updated.units[0].experience, Is.Zero); Assert.That(updated.units[0].attack, Is.Zero);
            Assert.That(updated.units[1].lifeState, Is.EqualTo(2)); Assert.That(updated.units[1].mp, Is.Zero);
            Assert.That(updated.units[2].hp, Is.EqualTo(40)); Assert.That(roster.units[0].hp, Is.EqualTo(40));
            Assert.That(roster.TryApplyBattleReturn(returned, out var recovered, out _), Is.True);
            Assert.That(recovered.units[0].mp, Is.EqualTo(2),"return does not invent MP recovery");
            Assert.That(recovered.units[1].mp, Is.Zero,"removed member is not healed");
            Assert.That(recovered.units[1].hp, Is.Zero); Assert.That(recovered.units[1].lifeState, Is.EqualTo(2));
            Assert.That(recovered.units[2].mp, Is.EqualTo(roster.units[2].mp)); // Non-deployed unit unchanged.
            Assert.That(returned.Members[0].Unit.Mp, Is.EqualTo(2)); Assert.That(returned.Members[1].Unit.Mp, Is.Zero);
            var save = Save(true); save.ownedRoster = recovered; var json = JsonUtility.ToJson(save);
            var loaded = JsonUtility.FromJson<NativeSaveData>(json);
            Assert.That(NativeSaveStore.IsValid(loaded), Is.True);
            Assert.That(loaded.ownedRoster.units[0].mp, Is.EqualTo(2),"serialization preserves received MP, not maximum MP");
            Assert.That(loaded.ownedRoster.units[0].maxMp, Is.EqualTo(10),"maximum remains independent");
            Assert.That(loaded.ownedRoster.units[1].mp, Is.Zero);
            Assert.That(loaded.ownedRoster.units[1].lifeState, Is.EqualTo(2));
            Files(store=>{
                Assert.IsTrue(store.TrySave(loaded,out var reason),reason);
                Assert.IsTrue(store.TryLoad(out var disk,out reason),reason);
                Assert.AreEqual(2,disk.ownedRoster.units[0].mp);
                Assert.AreEqual(17,disk.ownedRoster.units[0].hp);
                Assert.AreEqual(10,disk.ownedRoster.units[0].maxMp);
                Assert.AreEqual(0,disk.ownedRoster.units[1].mp);
                Assert.AreEqual(2,disk.ownedRoster.units[1].lifeState);
                Assert.AreEqual(roster.units[2].mp,disk.ownedRoster.units[2].mp,"undeployed member is unchanged");
            });
            var missing = roster.Clone(); missing.units[1].persistentId = "missing";
            Assert.That(missing.TryApplyObservedReturn(returned, out var partial, out _), Is.False); Assert.That(partial, Is.Null);
            Assert.That(missing.units[0].hp, Is.EqualTo(40));
            Assert.That(missing.TryApplyBattleReturn(returned, out partial, out _), Is.False); Assert.That(partial, Is.Null);
            var transformed = roster.Clone(); transformed.units[1].maxHp = 60;
            Assert.That(transformed.TryApplyObservedReturn(returned, out partial, out _), Is.False); Assert.That(partial, Is.Null);
        }

        [Test]
        public void DeadRosterAndExplicitEmptyRosterRoundTripWithoutRevival()
        {
            Files(store => {
                var source = Save(true); source.ownedRoster.units[0].hp = 0; source.ownedRoster.units[0].mp = 0;
                source.ownedRoster.units[0].lifeState = 2;
                Assert.That(store.TrySave(source, out _), Is.True); Assert.That(store.TryLoad(out var loaded, out _), Is.True);
                Assert.That(loaded.ownedRoster.units[0].hp, Is.Zero); Assert.That(loaded.ownedRoster.units[0].mp, Is.Zero);
                Assert.That(loaded.ownedRoster.units[0].lifeState, Is.EqualTo(2));
                source.ownedRoster.units = Array.Empty<NativeOwnedUnitData>();
                Assert.That(store.TrySave(source, out _), Is.True); Assert.That(store.TryLoad(out loaded, out _), Is.True);
                Assert.That(loaded.version, Is.EqualTo(2)); Assert.That(loaded.ownedRoster.units, Is.Empty);
            });
        }

        private static BattleFormationSnapshot DeployTwo(NativeOwnedRoster roster)
        {
            var drafts = roster.CreatePreparationDrafts(); var selected = new[] { drafts[0], drafts[1] };
            for (var i = 0; i < selected.Length; i++) {
                selected[i].CommanderId = 200; selected[i].CommanderPersistentId = "owned-200";
                selected[i].Position = new CombatGridCell(i, 0); selected[i].Unit.X = i;
            }
            Assert.That(BattleFormation.TryCreateSnapshot(selected, 20, 20, new short[] { 100, 800 }, out var formation, out _), Is.True);
            return formation;
        }

        [Test]
        public void InterruptedFormationCheckpointKeepsSpentMpWithoutFinalizingLiveRoster()
        {
            Session((session,panel,player)=>{
                Assert.That(session.RestoreSaveState(Save(true)),Is.True);
                var original=session.CaptureOwnedRoster();var formation=DeployTwo(original);
                int liveMp=session.PlayerMp;
                var go=new GameObject("Interrupted formation battle");SceneManager.MoveGameObjectToScene(go,session.gameObject.scene);
                var battle=go.AddComponent<BattleSceneRoot>();Assert.That(battle.TryRegisterAlliedFormation(formation,out _),Is.True);
                Assert.That(battle.ApplyMp(100,1),Is.True);Assert.That(battle.ApplyMp(200,2),Is.True);
                Assert.That(battle.ApplyMp(209,0),Is.True);Assert.That(battle.ApplyDeath(209,false),Is.True);
                var candidate=session.CaptureSaveState("Kaosia_Map","Town_Runtime",Vector2.zero);
                var method=typeof(BattleTransitionController).GetMethod("TryCaptureBattleState",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                var args=new object[]{battle,formation,candidate,null};
                Assert.That((bool)method.Invoke(session,args),Is.True,args[3]?.ToString());
                Assert.That(candidate.mp,Is.EqualTo(1));Assert.That(candidate.ownedRoster.units[0].mp,Is.EqualTo(2));
                Assert.That(candidate.ownedRoster.units[1].mp,Is.Zero);Assert.That(candidate.ownedRoster.units[1].lifeState,Is.EqualTo(2));
                Assert.That(candidate.ownedRoster.units[2].mp,Is.EqualTo(original.units[2].mp));
                Assert.That(session.PlayerMp,Is.EqualTo(liveMp));
                Assert.That(session.CaptureOwnedRoster().units[0].mp,Is.EqualTo(original.units[0].mp));
                Assert.That(session.MatchesOwnedFormation(formation),Is.True);
                Files(store=>{
                    Assert.That(store.TrySave(candidate,out var reason),Is.True,reason);
                    Assert.That(store.TryLoad(out var loaded,out reason),Is.True,reason);
                    Assert.That(loaded.mp,Is.EqualTo(1));Assert.That(loaded.ownedRoster.units[0].mp,Is.EqualTo(2));
                    Assert.That(loaded.ownedRoster.units[1].mp,Is.Zero);
                });
            });
        }

        [Test]
        public void SessionReturnCommitsPrimaryAndOwnedMaximumMpAndDiskRoundTrip()
        {
            Session((session, panel, player) => {
                Assert.That(session.RestoreSaveState(Save(true)), Is.True); var source = session.CaptureOwnedRoster(); var formation = DeployTwo(source);
                Assert.That(session.MatchesOwnedFormation(formation), Is.True);
                var go = new GameObject("Session return battle"); SceneManager.MoveGameObjectToScene(go, session.gameObject.scene);
                var battle = go.AddComponent<BattleSceneRoot>(); Assert.That(battle.TryRegisterAlliedFormation(formation, out _), Is.True);
                Assert.That(battle.ApplyHp(100, 19), Is.True); Assert.That(battle.ApplyMp(100, 1), Is.True);
                Assert.That(battle.ApplyHp(200, 17), Is.True); Assert.That(battle.ApplyMp(200, 2), Is.True);
                Assert.That(battle.ApplyMp(209, 0), Is.True); Assert.That(battle.ApplyDeath(209, false), Is.True);
                Assert.That(session.TryCaptureBattleReturn(battle, formation, out _), Is.True);
                Assert.That(session.PlayerHp, Is.EqualTo(19)); Assert.That(session.PlayerMp, Is.EqualTo(battle.PlayerMaxMp));
                var result = session.CaptureOwnedRoster(); Assert.That(result.units[0].hp, Is.EqualTo(17)); Assert.That(result.units[0].mp, Is.EqualTo(10));
                Assert.That(result.units[1].lifeState, Is.EqualTo(2)); Assert.That(result.units[1].hp, Is.Zero); Assert.That(result.units[1].mp, Is.EqualTo(10));
                Assert.That(result.units[2].mp, Is.EqualTo(5)); Assert.That(source.units[0].mp, Is.EqualTo(5));
                Assert.That(session.MatchesOwnedFormation(formation), Is.False); Assert.That(panel.ConfirmedSnapshot, Is.Null);
                Files(store => {
                    Assert.That(store.TrySave(session.CaptureSaveState("Kaosia_Map", "Town_Runtime", Vector2.zero), out _), Is.True);
                    Assert.That(store.TryLoad(out var saved, out _), Is.True);
                    Session((next, _, _) => { Assert.That(next.RestoreSaveState(saved), Is.True);
                        Assert.That(next.PlayerHp, Is.EqualTo(19)); Assert.That(next.CaptureOwnedRoster().units[1].mp, Is.EqualTo(10));
                        Assert.That(next.CaptureOwnedRoster().units[1].lifeState, Is.EqualTo(2)); });
                });
            });
        }

        [Test]
        public void SessionReturnMismatchDoesNotPublishPartialPrimaryOrOwnedState()
        {
            Session((session, panel, player) => {
                session.RestoreSaveState(Save(true)); var roster = session.CaptureOwnedRoster(); var formation = DeployTwo(roster);
                roster.units[1].maxHp = 60; Assert.That(session.TrySetOwnedRoster(roster, out _), Is.True);
                Assert.That(session.MatchesOwnedFormation(formation), Is.False);
                var go = new GameObject("Rejected return battle"); SceneManager.MoveGameObjectToScene(go, session.gameObject.scene);
                var battle = go.AddComponent<BattleSceneRoot>(); battle.TryRegisterAlliedFormation(formation, out _); battle.ApplyHp(100, 19);
                var before = JsonUtility.ToJson(session.CaptureSaveState("Kaosia_Map", "Town_Runtime", Vector2.zero));
                Assert.That(session.TryCaptureBattleReturn(battle, formation, out _), Is.False);
                Assert.That(JsonUtility.ToJson(session.CaptureSaveState("Kaosia_Map", "Town_Runtime", Vector2.zero)), Is.EqualTo(before));
                Assert.That(session.TryCaptureBattleReturn(null, formation, out _), Is.False);
            });
        }

        private static void Session(Action<BattleTransitionController, BattleFormationPanel, TopDownPlayerController> action)
        {
            var active = SceneManager.GetActiveScene(); var dirty = active.isDirty; var preview = EditorSceneManager.NewPreviewScene();
            try {
                var go = new GameObject("Owned roster test"); SceneManager.MoveGameObjectToScene(go, preview);
                go.AddComponent<WorldSessionHud>(); var session = go.GetComponent<BattleTransitionController>();
                var panel = go.GetComponent<BattleFormationPanel>();
                var playerGo = new GameObject("Owned roster input test"); SceneManager.MoveGameObjectToScene(playerGo, preview);
                var player = playerGo.AddComponent<TopDownPlayerController>(); panel.Configure(null, 0, 0, null, player);
                try { action(session, panel, player); } finally { panel.Cancel(); }
            } finally { EditorSceneManager.ClosePreviewScene(preview); }
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(active)); Assert.That(active.isDirty, Is.EqualTo(dirty));
        }
        private static void Files(Action<NativeSaveStore> action)
        {
            var directory = Path.Combine(Path.GetTempPath(), "DarkSaverRosterChecks-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try { action(new NativeSaveStore(Path.Combine(directory, "slot.json"))); }
            finally { Directory.Delete(directory, true); }
        }
    }
}
