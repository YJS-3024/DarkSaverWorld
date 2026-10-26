using System;
using System.Collections.Generic;
using DarkSaver.Combat;
using DarkSaver.Core;
using DarkSaver.Editor;
using DarkSaver.Jobs;
using DarkSaver.Magic;
using DarkSaver.Player;
using DarkSaver.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class ClassChangeExecutionTests
    {
        private static ClassArtworkCatalog Artwork => AssetDatabase.LoadAssetAtPath<ClassArtworkCatalog>(ClassArtworkConverter.Output);
        private static JobCatalog Jobs => AssetDatabase.LoadAssetAtPath<JobCatalog>(JobCatalogConverter.Output);
        private static ExperienceProgressionTable Growth => AssetDatabase.LoadAssetAtPath<ExperienceProgressionTable>(ExperienceProgressionConverter.Output);
        private static MagicCatalog Magic => AssetDatabase.LoadAssetAtPath<MagicCatalog>("Assets/_Game/Runtime/Magic/MagicCatalog.asset");

        [Test]
        public void ClassChangeRetainsEquipmentAndRecoversToItsNewCombinedMaximum()
        {
            WithTown((session,panel,player)=> {
                var items=ScriptableObject.CreateInstance<DarkSaver.Items.ItemCatalog>();
                try
                {
                    var values=new long[DarkSaver.Items.ItemCatalogEntry.NumericColumnCount];values[0]=500;values[3]=1;values[19]=25;values[23]=5;values[20]=7;
                    for(int i=6;i<=15;i++)values[i]=1;
                    items.Initialize(new[]{new DarkSaver.Items.ItemCatalogEntry(values,"0","","Fixture","","weapon")});
                    session.gameObject.AddComponent<ItemCatalogPanel>().Configure(items,player);
                    var save=session.CaptureSaveState("Kaosia_Map","Town_Runtime",Vector2.one);save.inventory.items=new[]{new DarkSaver.Core.NativeItemStack {itemId=500,quantity=1}};
                    Assert.IsTrue(session.RestoreSaveState(save));Assert.IsTrue(session.TryChangeEquipment(2,500,session.CaptureCharacterProfile(),out var gearReason),gearReason);
                    Assert.IsTrue(session.TryChangeClass(Growth,Magic,player,out var reason),reason);
                    Assert.AreEqual(500,session.CaptureCharacterProfile().EquipmentItemAt(2));
                    Assert.AreEqual(265,session.PlayerMaxHp);Assert.AreEqual(265,session.PlayerHp);Assert.AreEqual(53,session.PlayerMaxMp);Assert.AreEqual(53,session.PlayerMp);
                }
                finally {UnityEngine.Object.DestroyImmediate(items);}
            });
        }

        [Test]
        public void BattleMagicEditCommitsOrCancelsWithoutUnlearningOrSpendingStats()
        {
            WithTown((session,panel,player)=> {
                var save=session.CaptureSaveState("Kaosia_Map","Town_Runtime",Vector2.one);
                save.character.hasClassAbilities=true;save.character.learnedMagicIds=new[]{101,102};save.character.usableSkillIds=Array.Empty<int>();
                Assert.IsTrue(session.RestoreSaveState(save));Assert.IsTrue(panel.Open(TownService.CharacterInfo,player));
                Assert.IsTrue(panel.OpenCharacterAdjustment());Assert.IsTrue(panel.HasSessionBattleMagic);
                Assert.IsFalse(panel.ConfirmCharacterStatAllocation()); // Opening a legacy list alone never commits a selection.
                Assert.IsTrue(panel.SelectCharacterAdjustmentMagic(1,1));Assert.IsTrue(panel.RemoveSelectedBattleMagic());
                Assert.IsFalse(session.CaptureCharacterProfile().hasBattleMagicSelection);
                Assert.IsTrue(panel.ConfirmCharacterStatAllocation());
                var profile=session.CaptureCharacterProfile();CollectionAssert.AreEqual(new[]{101},profile.battleMagicIds);
                CollectionAssert.AreEqual(new[]{101,102},profile.learnedMagicIds);Assert.AreEqual(0,profile.unspentClassChangeStatPoints);
                Assert.IsTrue(profile.TryBuildClassChange(Growth,Magic,5,out var changed));CollectionAssert.AreEqual(new[]{101},changed.battleMagicIds);
                Assert.IsTrue(panel.OpenCharacterAdjustment());Assert.IsTrue(panel.SelectCharacterAdjustmentMagic(0,1));Assert.IsTrue(panel.AddSelectedBattleMagic());
                panel.CloseCharacterAdjustment();CollectionAssert.AreEqual(new[]{101},session.CaptureCharacterProfile().battleMagicIds);
                Assert.IsTrue(panel.OpenCharacterAdjustment());Assert.IsTrue(panel.SelectCharacterAdjustmentMagic(1,0));Assert.IsTrue(panel.RemoveSelectedBattleMagic());
                Assert.IsTrue(panel.ConfirmCharacterStatAllocation());Assert.IsEmpty(session.CaptureCharacterProfile().battleMagicIds);
                CollectionAssert.AreEqual(new[]{101,102},session.CaptureCharacterProfile().learnedMagicIds);
            });
        }

        [Test]
        public void SessionProgressAndVitalsUseSavedStateAndRefreshAfterClassChange()
        {
            WithTown((session,panel,player)=> {
                Assert.That(panel.Open(TownService.CharacterInfo,player),Is.True);
                Assert.That(panel.CharacterInfoLevelText,Is.EqualTo("5(12)"));
                Assert.That(panel.CharacterInfoExperienceText,Is.EqualTo("1234/-"));
                Assert.That(panel.CharacterInfoProgressStateText,Is.EqualTo("전직 대기"));
                Assert.That(panel.CharacterInfoValueText(0,0),Is.EqualTo("200"));
                Assert.That(panel.CharacterInfoValueText(0,1),Is.EqualTo("45"));
                Assert.That(panel.CharacterInfoVitalsText,Is.EqualTo("HP 17/200 · MP 3/45"));
                var save=session.CaptureSaveState("Kaosia_Map","Town_Runtime",Vector2.one);
                save.character.jobLevel=4; save.experience=uint.MaxValue; save.hp=7; save.mp=0;
                Assert.That(session.RestoreSaveState(save),Is.True);
                string before=JsonUtility.ToJson(session.CaptureSaveState("Kaosia_Map","Town_Runtime",Vector2.one));
                Assert.That(panel.BindSessionCharacterInfoValues(),Is.True);
                Assert.That(Growth.TryGet(100,4,out var row),Is.True);
                Assert.That(panel.CharacterInfoLevelText,Is.EqualTo("4(12)"));
                Assert.That(panel.CharacterInfoExperienceText,Is.EqualTo("4294967295/"+row.requiredXp));
                Assert.That(panel.CharacterInfoVitalsText,Is.EqualTo("HP 7/200 · MP 0/45"));
                Assert.That(JsonUtility.ToJson(session.CaptureSaveState("Kaosia_Map","Town_Runtime",Vector2.one)),Is.EqualTo(before));
                save.character.jobLevel=5; Assert.That(session.RestoreSaveState(save),Is.True);
                Assert.That(panel.Open(TownService.ClassChange,player),Is.True);
                Assert.That(panel.RequestClassChange(),Is.True); Assert.That(panel.ConfirmClassChange(),Is.True);
                Assert.That(panel.Open(TownService.CharacterInfo,player),Is.True);
                Assert.That(panel.CharacterInfoLevelText,Is.EqualTo("1(12)"));
                Assert.That(panel.CharacterInfoValueText(0,0),Is.EqualTo("240"));
                Assert.That(panel.CharacterInfoValueText(0,1),Is.EqualTo("48"));
                Assert.That(panel.CharacterInfoVitalsText,Is.EqualTo("HP 240/240 · MP 48/48"));
            });
        }

        [Test]
        public void UnknownVitalsLevelsAndXpTargetsAreNotSynthesized()
        {
            WithTown((session,panel,player)=> {
                var save=session.CaptureSaveState("Kaosia_Map","Town_Runtime",Vector2.one);
                save.character.hasLevels=false; save.character.baseLevel=save.character.jobLevel=0; save.hasVitals=false;
                Assert.That(session.RestoreSaveState(save),Is.True);
                Assert.That(panel.Open(TownService.CharacterInfo,player),Is.True);
                Assert.That(panel.CharacterInfoLevelText,Is.Null);
                Assert.That(panel.CharacterInfoExperienceText,Is.EqualTo("1234/?"));
                Assert.That(panel.CharacterInfoValueText(0,0),Is.Null); Assert.That(panel.CharacterInfoValueText(0,1),Is.Null);
                Assert.That(panel.CharacterInfoVitalsText,Is.Null);
                save.character.hasLevels=true; save.character.baseLevel=12; save.character.jobLevel=1;
                save.hasVitals=true; save.hp=1; save.mp=0; save.maxMp=0;
                Assert.That(session.RestoreSaveState(save),Is.True);
                panel.ConfigureProgression(null);
                Assert.That(panel.BindSessionCharacterInfoValues(),Is.True);
                Assert.That(panel.CharacterInfoLevelText,Is.EqualTo("1(12)"));
                Assert.That(panel.CharacterInfoExperienceText,Is.EqualTo("1234/?"));
                Assert.That(panel.CharacterInfoValueText(0,1),Is.EqualTo("0"));
                Assert.That(panel.CharacterInfoVitalsText,Is.EqualTo("HP 1/200 · MP 0/0"));
                save.character.jobLevel=100; save.character.baseLevel=int.MaxValue;
                Assert.That(session.RestoreSaveState(save),Is.True); Assert.That(panel.BindSessionCharacterInfoValues(),Is.True);
                Assert.That(panel.CharacterInfoLevelText,Is.EqualTo("100(2147483647)"));
                Assert.That(panel.CharacterInfoExperienceText,Is.EqualTo("1234/?"));
                var original=new byte[724]; Assert.That(panel.BindCharacterInfoValues(original),Is.True);
                Assert.That(panel.CharacterInfoVitalsText,Is.Null); Assert.That(panel.CharacterInfoLocalSummaryText,Is.Null);
                Assert.That(panel.Open(TownService.Inventory,player),Is.True);
                Assert.That(panel.CharacterInfoLevelText,Is.Null); Assert.That(panel.CharacterInfoExperienceText,Is.Null);
            });
        }

        [Test]
        public void SessionPhysicalInfoMatchesConfirmedAllocationAndClearsBetweenServices()
        {
            WithTown((session,panel,player)=> {
                var save=session.CaptureSaveState("Kaosia_Map","Town_Runtime",Vector2.one);
                save.character.unspentClassChangeStatPoints=20; Assert.That(session.RestoreSaveState(save),Is.True);
                Assert.That(Jobs.TryGet(100,out var job),Is.True);
                Assert.That(panel.Open(TownService.CharacterInfo,player),Is.True);
                Assert.That(panel.CharacterInfoValuesAreLocal,Is.True);
                int attack=(int)job.GetNumericValue(6),defense=(int)job.GetNumericValue(8);
                Assert.That(panel.CharacterInfoValueText(0,2),Is.EqualTo(attack.ToString()));
                Assert.That(panel.CharacterInfoValueText(0,3),Is.EqualTo(defense.ToString()));
                Assert.That(panel.CharacterInfoValueText(0,0),Is.EqualTo("200"));
                Assert.That(panel.CharacterInfoValueText(1,0),Is.Null);
                Assert.That(panel.CharacterInfoAuxiliaryCountText,Is.Null);
                var inc=DarkSaver.Network.CharacterStatAdjustmentDirection.Increase;
                Assert.That(panel.OpenCharacterAdjustment(),Is.True);
                Assert.That(panel.PreviewCharacterAdjustmentStat(2,inc),Is.True);
                Assert.That(panel.CharacterInfoValueText(0,2),Is.EqualTo(attack.ToString()));
                panel.CloseCharacterAdjustment();
                Assert.That(panel.CharacterInfoValueText(0,2),Is.EqualTo(attack.ToString()));
                Assert.That(panel.OpenCharacterAdjustment(),Is.True);
                Assert.That(panel.PreviewCharacterAdjustmentStat(2,inc),Is.True);
                Assert.That(panel.PreviewCharacterAdjustmentStat(3,inc),Is.True);
                Assert.That(panel.ConfirmCharacterStatAllocation(),Is.True);
                Assert.That(panel.CharacterInfoValueText(0,2),Is.EqualTo((attack+1).ToString()));
                Assert.That(panel.CharacterInfoValueText(0,3),Is.EqualTo((defense+1).ToString()));
                Assert.That(panel.CharacterInfoPhysicalBreakdown,Does.Contain("직업 기본값 + 확정 배분"));
                Assert.That(panel.BindCharacterInfoValues(new byte[1]),Is.False);
                Assert.That(panel.CharacterInfoValuesAreLocal,Is.True);
                var original=new byte[724]; original[0x22]=77; original[0x28]=88;
                Assert.That(panel.BindCharacterInfoValues(original),Is.True);
                Assert.That(panel.CharacterInfoValuesAreLocal,Is.False);
                Assert.That(panel.CharacterInfoPhysicalBreakdown,Is.Null);
                Assert.That(panel.CharacterInfoValueText(0,2),Is.EqualTo("77"));
                Assert.That(panel.BindSessionCharacterInfoValues(),Is.True);
                Assert.That(panel.CharacterInfoValueText(0,2),Is.EqualTo((attack+1).ToString()));
                Assert.That(panel.Open(TownService.Inventory,player),Is.True);
                Assert.That(panel.HasCharacterInfoValues,Is.False);
                Assert.That(panel.CharacterInfoValuesAreLocal,Is.False);
                Assert.That(panel.Open(TownService.CharacterInfo,player),Is.True);
                Assert.That(panel.CharacterInfoValueText(0,3),Is.EqualTo((defense+1).ToString()));
            });
        }

        [Test]
        public void MissingCatalogAndOverflowRejectNativeInfoWithoutReplacingExistingSnapshot()
        {
            WithTown((session,panel,player)=> {
                Assert.That(panel.Open(TownService.CharacterInfo,player),Is.True);
                string before=panel.CharacterInfoValueText(0,2);
                var missing=ScriptableObject.CreateInstance<JobCatalog>();
                try {
                    Assert.That(session.ConfigureFormationSupport(new[]{missing},new Dictionary<int,DirectionalSpriteAnimator>()),Is.True);
                    Assert.That(panel.BindSessionCharacterInfoValues(),Is.False);
                    Assert.That(panel.CharacterInfoValueText(0,2),Is.EqualTo(before));
                    Assert.That(session.TryGetPlayerPhysicalStats(out var ba0,out var bd0,out var a0,out var d0),Is.False);
                    Assert.That((ba0,bd0,a0,d0),Is.EqualTo((0,0,0,0)));
                } finally {UnityEngine.Object.DestroyImmediate(missing);}
                session.ConfigureFormationSupport(new[]{Jobs},new Dictionary<int,DirectionalSpriteAnimator>());
                var save=session.CaptureSaveState("Kaosia_Map","Town_Runtime",Vector2.one);
                save.character.allocatedAttack=int.MaxValue;
                Assert.That(session.RestoreSaveState(save),Is.True);
                Assert.That(panel.BindSessionCharacterInfoValues(),Is.False);
                Assert.That(session.TryGetPlayerPhysicalStats(out var ba,out var bd,out var a,out var d),Is.False);
                Assert.That((ba,bd,a,d),Is.EqualTo((0,0,0,0)));
                Assert.That(panel.CharacterInfoValueText(0,2),Is.EqualTo(before));
            });
        }

        [Test]
        public void TownStatAllocationCancelsConfirmsAndRoundTripsThroughSaveFile()
        {
            WithTown((session,panel,player)=> {
                var save=session.CaptureSaveState("Kaosia_Map","Town_Runtime",Vector2.one);
                save.character.unspentClassChangeStatPoints=20;
                Assert.That(session.RestoreSaveState(save),Is.True);
                Assert.That(panel.Open(TownService.CharacterInfo,player),Is.True);
                Assert.That(panel.OpenCharacterAdjustment(),Is.True);
                var inc=DarkSaver.Network.CharacterStatAdjustmentDirection.Increase;
                var dec=DarkSaver.Network.CharacterStatAdjustmentDirection.Decrease;
                Assert.That(panel.PreviewCharacterAdjustmentStat(0,inc),Is.False);
                Assert.That(panel.PreviewCharacterAdjustmentStat(1,inc),Is.False);
                Assert.That(panel.PreviewCharacterAdjustmentStat(2,inc),Is.True);
                Assert.That(panel.PreviewCharacterAdjustmentStat(4,inc),Is.True);
                Assert.That(panel.CharacterAdjustmentPointsText,Is.EqualTo("16"));
                Assert.That(session.CaptureCharacterProfile().unspentClassChangeStatPoints,Is.EqualTo(20));
                panel.CloseCharacterAdjustment();
                Assert.That(session.CaptureCharacterProfile().allocatedAttack,Is.Zero);
                Assert.That(panel.OpenCharacterAdjustment(),Is.True);
                Assert.That(panel.CharacterAdjustmentPointsText,Is.EqualTo("20"));
                Assert.That(panel.ConfirmCharacterStatAllocation(),Is.False);
                var baseline=session.CaptureCharacterProfile();
                Assert.That(panel.PreviewCharacterAdjustmentStat(2,inc),Is.True);
                Assert.That(panel.PreviewCharacterAdjustmentStat(5,inc),Is.True);
                Assert.That(panel.ConfirmCharacterStatAllocation(),Is.True);
                Assert.That(panel.IsCharacterAdjustmentOpen,Is.False);
                var committed=session.CaptureCharacterProfile();
                Assert.That((committed.allocatedAttack,committed.allocatedMagicDefense,committed.unspentClassChangeStatPoints),Is.EqualTo((1,1,17)));
                Assert.That(session.TryAllocateCharacterStats(baseline,1,0,0,0,out _),Is.False);
                Assert.That(panel.OpenCharacterAdjustment(),Is.True);
                Assert.That(panel.PreviewCharacterAdjustmentStat(2,dec),Is.False);
                panel.CloseCharacterAdjustment();
                string path=System.IO.Path.GetFullPath("Tools/CombatRulesChecks/stat-allocation-"+Guid.NewGuid().ToString("N")+".json");
                try {
                    var store=new NativeSaveStore(path);
                    Assert.That(store.TrySave(session.CaptureSaveState("Kaosia_Map","Town_Runtime",Vector2.one),out _),Is.True);
                    Assert.That(store.TryLoad(out var restored,out _),Is.True);
                    Assert.That(session.RestoreSaveState(restored),Is.True);
                    committed=session.CaptureCharacterProfile();
                    Assert.That((committed.allocatedAttack,committed.allocatedMagicDefense,committed.unspentClassChangeStatPoints),Is.EqualTo((1,1,17)));
                    Assert.That((session.PlayerHp,session.PlayerMp,session.PlayerExperience),Is.EqualTo((17,3,1234u)));
                    Assert.That(session.CaptureInventory().gold,Is.EqualTo(10000));
                } finally {if(System.IO.File.Exists(path))System.IO.File.Delete(path);}
            });
        }

        [Test]
        public void StaleOrDisabledTownAllocationCannotChangeSession()
        {
            WithTown((session,panel,player)=> {
                var save=session.CaptureSaveState("Kaosia_Map","Town_Runtime",Vector2.one);
                save.character.unspentClassChangeStatPoints=20; Assert.That(session.RestoreSaveState(save),Is.True);
                Assert.That(panel.Open(TownService.CharacterInfo,player),Is.True);
                Assert.That(panel.OpenCharacterAdjustment(),Is.True);
                Assert.That(panel.PreviewCharacterAdjustmentStat(2,DarkSaver.Network.CharacterStatAdjustmentDirection.Increase),Is.True);
                save.character.unspentClassChangeStatPoints=19; Assert.That(session.RestoreSaveState(save),Is.True);
                Assert.That(panel.CanPreviewCharacterAdjustmentStat(2,DarkSaver.Network.CharacterStatAdjustmentDirection.Increase),Is.False);
                Assert.That(panel.ConfirmCharacterStatAllocation(),Is.False);
                Assert.That(session.CaptureCharacterProfile().unspentClassChangeStatPoints,Is.EqualTo(19));
                var fresh=session.CaptureCharacterProfile(); session.enabled=false;
                Assert.That(session.TryAllocateCharacterStats(fresh,1,0,0,0,out _),Is.False);
                Assert.That(session.CaptureCharacterProfile().allocatedAttack,Is.Zero);
                session.enabled=true; panel.Close();
            });
        }

        [Test]
        public void NativeClassSheetsKeepBothFrameSizesAndScreenFacing()
        {
            Assert.That(Artwork.Count, Is.EqualTo(Jobs.Count));
            var scene = EditorSceneManager.NewPreviewScene();
            try {
                var root = new GameObject("Class animation check"); SceneManager.MoveGameObjectToScene(root, scene);
                var animator = root.AddComponent<DirectionalSpriteAnimator>();
                foreach (int id in new[] {100, 105, 115, 155, 164, 165, 168, 169, 170, 171, 172}) {
                    Assert.That(Artwork.TryGet(id, out var entry), Is.True);
                    Assert.That(entry.GetFrame(0).rect.height, Is.EqualTo(32));
                    Assert.That(entry.GetFrame(0).pixelsPerUnit, Is.EqualTo(32));
                    Assert.That(Artwork.TryApply(id, animator), Is.True);
                    animator.TrySetFacing(Vector2.left); animator.PresentFrame(0);
                    Assert.That(root.GetComponent<SpriteRenderer>().sprite.name, Is.EqualTo("Class" + id + "_Right_0"));
                }
                Assert.That(Artwork.TryGet(999, out _), Is.False); // Never substitute an unknown class.
            } finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void ShortNativePalettesPreserveEveryVisiblePixel()
        {
            foreach (int id in new[] {164, 168}) {
                string source = "분석/Saver_Late/decoded_art/gameres/" + (id == 164 ? "char5.arc/" : "char6.arc/") + id + "r.bmp";
                var bytes = System.IO.File.ReadAllBytes(source); int offset = BitConverter.ToInt32(bytes, 10);
                Assert.That(offset, Is.LessThanOrEqualTo(1078));
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(ClassArtworkConverter.SheetPath(id));
                var pixels = texture.GetPixels32();
                for (int i = 0; i < pixels.Length; i++) {
                    int palette = 54 + bytes[offset + i] * 4;
                    byte b = bytes[palette], g = bytes[palette + 1], r = bytes[palette + 2];
                    Assert.That(pixels[i].a, Is.EqualTo(r == 0 && g == 0 && b == 0 ? 0 : 255));
                    if (pixels[i].a != 0) Assert.That((pixels[i].r, pixels[i].g, pixels[i].b), Is.EqualTo((r, g, b)));
                }
            }
        }

        [Test]
        public void ConfirmedTownChangePreservesBaseXpAndMoneyAndFullyRecoversNewVitals()
        {
            WithTown((session, panel, player) => {
                Assert.That(panel.Open(TownService.ClassChange, player), Is.True);
                Assert.That(panel.ConfirmClassChange(), Is.False);
                Assert.That(panel.RequestClassChange(), Is.True);
                Assert.That(session.PlayerJobId, Is.EqualTo(100));
                Assert.That(panel.ConfirmClassChange(), Is.True);
                var profile = session.CaptureCharacterProfile();
                Assert.That((profile.jobId, profile.baseLevel, profile.jobLevel), Is.EqualTo((105, 12, 1)));
                Assert.That(profile.learnedMagicIds, Is.EqualTo(new[] {101, 2401}));
                Assert.That(profile.usableSkillIds, Is.EqualTo(new[] {10102, 10301, 21102}));
                Assert.That((session.PlayerHp, session.PlayerMaxHp, session.PlayerMp, session.PlayerMaxMp), Is.EqualTo((240, 240, 48, 48)));
                Assert.That(session.PlayerExperience, Is.EqualTo(1234)); Assert.That(session.CaptureInventory().gold, Is.EqualTo(10000));
                Assert.That(player.GetComponent<SpriteRenderer>().sprite.name, Does.StartWith("Class105_"));
                Assert.That(DarkSaver.World.TrainingEntrance2D.TryResolveTrainingMap(Jobs, profile.jobId, out var map), Is.True);
                Assert.That(map, Is.EqualTo(102));
                Assert.That(panel.ConfirmClassChange(), Is.False);
                var saved = JsonUtility.FromJson<NativeSaveData>(JsonUtility.ToJson(session.CaptureSaveState("Kaosia_Map", "Town_Runtime", Vector2.one)));
                Assert.That(session.RestoreSaveState(saved), Is.True);
                Artwork.TryApply(100, player.GetComponent<DirectionalSpriteAnimator>());
                Assert.That(session.TryRestoreCharacterArtwork(player), Is.True);
                Assert.That(player.GetComponent<SpriteRenderer>().sprite.name, Does.StartWith("Class105_"));
                Assert.That(session.PlayerHp, Is.EqualTo(240));
            });
        }

        [Test]
        public void MissingArtworkRejectsWithoutPartialClassStatsOrMoneyChanges()
        {
            WithTown((session, panel, player) => {
                var empty = ScriptableObject.CreateInstance<ClassArtworkCatalog>();
                try {
                    session.ConfigureClassArtwork(empty);
                    string before = JsonUtility.ToJson(session.CaptureSaveState("Kaosia_Map", "Town_Runtime", Vector2.one));
                    panel.Open(TownService.ClassChange, player);
                    Assert.That(panel.RequestClassChange(), Is.False); Assert.That(panel.ConfirmClassChange(), Is.False);
                    Assert.That(JsonUtility.ToJson(session.CaptureSaveState("Kaosia_Map", "Town_Runtime", Vector2.one)), Is.EqualTo(before));
                } finally { UnityEngine.Object.DestroyImmediate(empty); }
            });
        }

        [Test]
        public void StaleConfirmationAndUnknownLegacyLevelsCannotPromote()
        {
            WithTown((session, panel, player) => {
                panel.Open(TownService.ClassChange, player); Assert.That(panel.RequestClassChange(), Is.True);
                var save = session.CaptureSaveState("Kaosia_Map", "Town_Runtime", Vector2.one);
                save.character.jobLevel = 4; session.RestoreSaveState(save);
                Assert.That(panel.ConfirmClassChange(), Is.False); Assert.That(session.PlayerJobId, Is.EqualTo(100));
                Assert.That(session.PlayerHp, Is.EqualTo(17));
                panel.Close(); save.character.hasLevels = false; save.character.baseLevel = save.character.jobLevel = 0;
                Assert.That(session.RestoreSaveState(save), Is.True);
                panel.Open(TownService.ClassChange, player); Assert.That(panel.RequestClassChange(), Is.False);
                Assert.That(session.CaptureCharacterProfile().hasLevels, Is.False);
            });
        }

        [Test]
        public void SessionHireInfoGroupsActualJobsAndKeepsDeadAndLegacyOwnershipSeparate()
        {
            WithTown((session,panel,player)=> {
                var jobs=AssetDatabase.LoadAssetAtPath<JobCatalog>(JobCatalogConverter.MercenaryOutput);
                panel.ConfigureHiring(null,jobs);
                var save=session.CaptureSaveState("Kaosia_Map","Town_Runtime",Vector2.one);
                save.version=NativeSaveStore.RosterVersion; save.hasOwnedRoster=true;
                save.ownedRoster=new NativeOwnedRoster {units=new[] {
                    InfoHire("a",20000,200),InfoHire("b",20001,200),InfoHire("c",20002,201),InfoHire("legacy",20003,202)
                }};
                save.ownedRoster.units[1].hp=0; save.ownedRoster.units[1].lifeState=1;
                save.ownedRoster.units[3].hireType=null; save.ownedRoster.units[3].hireOwnerId=0;
                Assert.That(session.RestoreSaveState(save),Is.True);
                string before=JsonUtility.ToJson(session.CaptureSaveState("Kaosia_Map","Town_Runtime",Vector2.one));
                Assert.That(panel.Open(TownService.CharacterInfo,player),Is.True);
                Assert.That(panel.CharacterInfoAuxiliaryCountText,Is.Null); Assert.That(panel.CharacterInfoHiresText,Does.StartWith("보유 고용 용병 3명"));
                Assert.That(panel.CharacterInfoAuxiliaryClassId,Is.EqualTo(-1));
                Assert.That(jobs.TryGet(200,out var first),Is.True); Assert.That(jobs.TryGet(201,out var second),Is.True);
                Assert.That(panel.CharacterInfoHiresText,Does.Contain(first.Name.Replace('_',' ')+" × 2"));
                Assert.That(panel.CharacterInfoHiresText,Does.Contain(second.Name.Replace('_',' ')+" × 1"));
                Assert.That(panel.CharacterInfoHiresText,Does.Contain("고용 정보 미확인 1명"));
                Assert.That(JsonUtility.ToJson(session.CaptureSaveState("Kaosia_Map","Town_Runtime",Vector2.one)),Is.EqualTo(before));
                save.ownedRoster.units=new[]{InfoHire("only",20004,200)};
                Assert.That(session.RestoreSaveState(save),Is.True);
                Assert.That(panel.CharacterInfoAuxiliaryCountText,Is.Null); Assert.That(panel.CharacterInfoHiresText,Does.StartWith("보유 고용 용병 3명"));
                Assert.That(panel.BindSessionCharacterInfoValues(),Is.True);
                Assert.That(panel.CharacterInfoAuxiliaryCountText,Is.EqualTo("X 1")); Assert.That(panel.CharacterInfoHiresText,Does.StartWith("보유 고용 용병 1명"));
                Assert.That(panel.CharacterInfoAuxiliaryClassId,Is.EqualTo(200));
                panel.ConfigureHiring(null,null); Assert.That(panel.BindSessionCharacterInfoValues(),Is.True);
                Assert.That(panel.CharacterInfoHiresText,Does.Contain("병종 ID 200 × 1"));
                Assert.That(panel.BindCharacterInfoValues(new byte[724]),Is.True);
                Assert.That(panel.CharacterInfoHiresText,Is.Null);
                Assert.That(panel.Open(TownService.Inventory,player),Is.True);
                Assert.That(panel.CharacterInfoAuxiliaryCountText,Is.Null);
                Assert.That(panel.CharacterInfoAuxiliaryClassId,Is.EqualTo(-1));
                Assert.That(panel.Open(TownService.CharacterInfo,player),Is.True);
                panel.Close(); Assert.That(panel.CharacterInfoHiresText,Is.Null);
            });
        }

        [Test]
        public void UnknownRosterAndConfirmedEmptyRosterHaveDifferentHireDisplays()
        {
            WithTown((session,panel,player)=> {
                Assert.That(panel.Open(TownService.CharacterInfo,player),Is.True);
                Assert.That(panel.CharacterInfoHiresText,Is.EqualTo("보유 부대 정보 미확인"));
                Assert.That(panel.CharacterInfoAuxiliaryCountText,Is.Null);
                var save=session.CaptureSaveState("Kaosia_Map","Town_Runtime",Vector2.one);
                save.version=NativeSaveStore.RosterVersion; save.hasOwnedRoster=true;
                save.ownedRoster=new NativeOwnedRoster {units=Array.Empty<NativeOwnedUnitData>()};
                Assert.That(session.RestoreSaveState(save),Is.True);
                Assert.That(panel.BindSessionCharacterInfoValues(),Is.True);
                Assert.That(panel.CharacterInfoHiresText,Is.EqualTo("보유 고용 용병 0명"));
                Assert.That(panel.CharacterInfoAuxiliaryCountText,Is.EqualTo("X 0"));
                Assert.That(panel.CharacterInfoAuxiliaryClassId,Is.EqualTo(-1));
            });
        }

        [Test]
        public void OriginalSelectedRecordAndWeaponTextDoNotInheritLocalOwnershipOrUnknownLabel()
        {
            WithTown((session,panel,player)=> {
                Assert.That(panel.Open(TownService.CharacterInfo,player),Is.True);
                Assert.That(panel.CharacterInfoWeaponName,Is.Null);
                Assert.That(panel.CharacterInfoWeaponDisplayText,Is.EqualTo("미확인"));
                Assert.That(panel.CharacterInfoAuxiliaryCountText,Is.Null);
                var original=new byte[724]; original[0x96]=105; original[0x98]=254; original[0x99]=255;
                Assert.That(panel.BindCharacterInfoValues(new byte[1]),Is.False);
                Assert.That(panel.CharacterInfoWeaponDisplayText,Is.EqualTo("미확인"));
                Assert.That(panel.BindCharacterInfoValues(original),Is.True);
                Assert.That(panel.CharacterInfoAuxiliaryClassId,Is.EqualTo(105));
                Assert.That(panel.CharacterInfoAuxiliaryCountText,Is.EqualTo("X -2"));
                Assert.That(panel.CharacterInfoHiresText,Is.Null);
                Assert.That(panel.CharacterInfoWeaponDisplayText,Is.Null);
                Assert.That(panel.BindCharacterInfoWeaponName("없음"),Is.True);
                Assert.That(panel.CharacterInfoWeaponDisplayText,Is.EqualTo("없음"));
                Assert.That(panel.BindCharacterInfoValues(original),Is.True);
                Assert.That(panel.CharacterInfoWeaponDisplayText,Is.EqualTo("없음"));
                Assert.That(panel.BindSessionCharacterInfoValues(),Is.True);
                Assert.That(panel.CharacterInfoAuxiliaryCountText,Is.Null);
                Assert.That(panel.CharacterInfoAuxiliaryClassId,Is.EqualTo(-1));
                Assert.That(panel.CharacterInfoWeaponName,Is.Null);
                Assert.That(panel.CharacterInfoWeaponDisplayText,Is.EqualTo("미확인"));
                Assert.That(panel.BindCharacterInfoWeaponName(""),Is.True);
                Assert.That(panel.CharacterInfoWeaponDisplayText,Is.Empty);
                panel.Close();Assert.That(panel.CharacterInfoWeaponDisplayText,Is.Null);
            });
        }
        [Test]
        public void MixedLegacyHireTypesDoNotChooseOneMercenaryPictureEvenWhenJobMatches()
        {
            WithTown((session,panel,player)=> {
                var save=session.CaptureSaveState("Kaosia_Map","Town_Runtime",Vector2.one);
                save.version=NativeSaveStore.RosterVersion;save.hasOwnedRoster=true;
                save.ownedRoster=new NativeOwnedRoster {units=new[]{InfoHire("a",20000,200),InfoHire("b",20001,200)}};
                save.ownedRoster.units[1].hireType="승병용병고용";
                Assert.That(session.RestoreSaveState(save),Is.True);
                Assert.That(panel.Open(TownService.CharacterInfo,player),Is.True);
                Assert.That(panel.CharacterInfoAuxiliaryClassId,Is.EqualTo(-1));
                Assert.That(panel.CharacterInfoAuxiliaryCountText,Is.Null);
                Assert.That(panel.CharacterInfoHiresText,Does.Contain("고용 타입 혼재"));
            });
        }
        private static NativeOwnedUnitData InfoHire(string id,int instance,int job) => new NativeOwnedUnitData {
            persistentId=id,instanceId=instance,jobId=job,name="fixture",hp=1,maxHp=1,
            finalStateConfirmed=true,teamId=1,learnedMagicIds=Array.Empty<short>(),hireType="보병용병고용",hireOwnerId=100
        };
        [Test]
        public void PromotionPreservesKnownLevelsInPreviewConfirmationAndDiskSave()
        {
            WithTown((session,panel,player)=> {
                var state=session.CaptureSaveState("Kaosia_Map","Town_Runtime",Vector2.one);
                state.character.hasClassAbilities=true;state.character.learnedMagicIds=new[]{101,102};
                state.character.usableSkillIds=new[]{10101,10301,21101};
                Assert.That(session.RestoreSaveState(state),Is.True);
                Assert.That(panel.Open(TownService.ClassChange,player),Is.True);
                Assert.That(panel.TryPreviewClassChange(0,out _,out var preview),Is.True);
                Assert.That(preview.learnedMagicIds,Is.EqualTo(new[]{101,102,2401}));
                Assert.That(session.CaptureCharacterProfile().learnedMagicIds,Is.EqualTo(new[]{101,102}));
                Assert.That(panel.RequestClassChange(),Is.True);Assert.That(panel.ConfirmClassChange(),Is.True);
                Assert.That(session.CaptureCharacterProfile().learnedMagicIds,Is.EqualTo(new[]{101,102,2401}));
                var path=System.IO.Path.GetFullPath(System.IO.Path.Combine("Tools/CombatRulesChecks","magic-retention-"+Guid.NewGuid().ToString("N")+".json"));
                try {
                    var store=new NativeSaveStore(path);
                    Assert.That(store.TrySave(session.CaptureSaveState("Kaosia_Map","Town_Runtime",Vector2.one),out _),Is.True);
                    Assert.That(store.TryLoad(out var saved,out _),Is.True);
                    Assert.That(saved.character.learnedMagicIds,Is.EqualTo(new[]{101,102,2401}));
                    Assert.That(session.RestoreSaveState(saved),Is.True);
                    saved.character.learnedMagicIds[0]=103;
                    Assert.That(session.CaptureCharacterProfile().learnedMagicIds,Is.EqualTo(new[]{101,102,2401}));
                } finally {if(System.IO.File.Exists(path))System.IO.File.Delete(path);}
            });
        }
        private static void WithTown(Action<BattleTransitionController, TownServicesPanel, TopDownPlayerController> check)
        {
            var active = SceneManager.GetActiveScene(); var town = SceneManager.GetSceneByName("Town_Runtime"); bool created = !town.isLoaded;
            if (created) { town = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive); town.name = "Town_Runtime"; }
            var root = new GameObject("Class change fixture"); SceneManager.MoveGameObjectToScene(root, town);
            try {
                var player = root.AddComponent<TopDownPlayerController>(); root.AddComponent<DirectionalSpriteAnimator>();
                var session = root.AddComponent<BattleTransitionController>();
                session.InitializeCharacter(new NativeCharacterProfile {name = "검증", jobId = 100, hasLevels = true, baseLevel = 12, jobLevel = 5});
                session.ConfigureFormationSupport(new[] {Jobs}, new Dictionary<int, DirectionalSpriteAnimator>());
                session.ConfigureClassArtwork(Artwork); Artwork.TryApply(100, player.GetComponent<DirectionalSpriteAnimator>());
                var save = session.CaptureSaveState("Kaosia_Map", "Town_Runtime", Vector2.one);
                save.hasVitals = true; save.hp = 17; save.maxHp = 200; save.mp = 3; save.maxMp = 45; save.lifeState = 0;
                save.experience = 1234; save.inventory.gold = 10000; session.RestoreSaveState(save);
                root.AddComponent<MagicCatalogPanel>().Configure(Magic, player);
                var panel = root.AddComponent<TownServicesPanel>(); panel.ConfigureProgression(Growth);
                check(session, panel, player);
            } finally { UnityEngine.Object.DestroyImmediate(root); if (created) EditorSceneManager.CloseScene(town, true); if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active); }
        }
    }
}
