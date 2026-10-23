using System;
using DarkSaver.Combat;
using DarkSaver.Core;
using DarkSaver.Magic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class ClassAbilityProgressionTests
    {
        [Test]
        public void StatAllocationCandidatesAreAtomicIrreversibleAndSerializable()
        {
            var source=new NativeCharacterProfile {name="검증",jobId=100,unspentClassChangeStatPoints=20};
            Assert.That(source.TryBuildStatAllocation(3,4,2,6,out var candidate),Is.True);
            Assert.That(source.unspentClassChangeStatPoints,Is.EqualTo(20));
            Assert.That(source.allocatedAttack,Is.Zero); // Cancel simply discards candidate.
            Assert.That(candidate.unspentClassChangeStatPoints,Is.Zero);
            Assert.That((candidate.allocatedAttack,candidate.allocatedDefense,candidate.allocatedMagicAttack,candidate.allocatedMagicDefense),Is.EqualTo((3,4,2,6)));
            Assert.That(source.TryBuildStatAllocation(3,4,2,6,out var secondPreview),Is.True);
            Assert.That(secondPreview.allocatedAttack,Is.EqualTo(3)); // Repeated preview never doubles the grant.
            var restored=JsonUtility.FromJson<NativeCharacterProfile>(JsonUtility.ToJson(candidate));
            Assert.That(restored.IsValid,Is.True);
            Assert.That((restored.allocatedAttack,restored.allocatedDefense,restored.allocatedMagicAttack,restored.allocatedMagicDefense),Is.EqualTo((3,4,2,6)));
            Assert.That(restored.Clone().allocatedMagicAttack,Is.EqualTo(2));
            Assert.That(restored.TryBuildStatAllocation(-1,0,0,0,out _),Is.False);
            Assert.That(restored.TryBuildStatAllocation(1,0,0,0,out _),Is.False);
            var legacy=JsonUtility.FromJson<NativeCharacterProfile>("{\"name\":\"검증\",\"jobId\":100}");
            Assert.That(legacy.IsValid,Is.True);
            Assert.That((legacy.allocatedAttack,legacy.allocatedDefense,legacy.allocatedMagicAttack,legacy.allocatedMagicDefense),Is.EqualTo((0,0,0,0)));
            Assert.That(candidate.TryBuildClassChange(Table,Catalog,5,out var promoted),Is.True);
            Assert.That(promoted.unspentClassChangeStatPoints,Is.EqualTo(20));
            Assert.That((promoted.allocatedAttack,promoted.allocatedDefense,promoted.allocatedMagicAttack,promoted.allocatedMagicDefense),Is.EqualTo((3,4,2,6)));
            Assert.That(promoted.TryBuildStatAllocation(1,0,0,0,out var spent),Is.True);
            Assert.That(spent.allocatedAttack,Is.EqualTo(4)); Assert.That(spent.unspentClassChangeStatPoints,Is.EqualTo(18));
        }

        [Test]
        public void InvalidOrOverflowingStatAllocationLeavesSourceUntouched()
        {
            var source=new NativeCharacterProfile {name="검증",jobId=100,unspentClassChangeStatPoints=20};
            foreach(var request in new[]{new[]{0,0,0,0},new[]{11,0,0,0},new[]{0,-1,0,0},new[]{0,0,-1,0},new[]{0,0,0,-1},new[]{int.MaxValue,int.MaxValue,int.MaxValue,int.MaxValue}}) {
                Assert.That(source.TryBuildStatAllocation(request[0],request[1],request[2],request[3],out var changed),Is.False);
                Assert.That(changed,Is.Null); Assert.That(source.unspentClassChangeStatPoints,Is.EqualTo(20));
            }
            source.allocatedAttack=int.MaxValue;
            Assert.That(source.TryBuildStatAllocation(1,0,0,0,out _),Is.False);
            source.allocatedAttack=-1; Assert.That(source.IsValid,Is.False);
            source.allocatedAttack=0; source.allocatedMagicDefense=-1; Assert.That(source.IsValid,Is.False);
        }

        [Test]
        public void ClassChangeAddsTwentyPointsWithoutMutatingPreviewSourceOrRetroactiveLegacyGrant()
        {
            var source=new NativeCharacterProfile {name="검증",jobId=100,unspentClassChangeStatPoints=7};
            Assert.That(source.TryBuildClassChange(Table,Catalog,5,out var projected),Is.True);
            Assert.That(source.unspentClassChangeStatPoints,Is.EqualTo(7)); Assert.That(projected.unspentClassChangeStatPoints,Is.EqualTo(27));
            Assert.That(source.TryBuildClassChange(Table,Catalog,5,out var repeatedPreview),Is.True);
            Assert.That(repeatedPreview.unspentClassChangeStatPoints,Is.EqualTo(27));
            Assert.That(projected.Clone().unspentClassChangeStatPoints,Is.EqualTo(27));
            var rows=Table.GetClassChangeRows(projected.jobId);
            Assert.That(rows.Length,Is.GreaterThan(0));
            Assert.That(projected.TryBuildClassChange(Table,Catalog,rows[0].level,out var next),Is.True);
            Assert.That(next.unspentClassChangeStatPoints,Is.EqualTo(47));
            var legacy=JsonUtility.FromJson<NativeCharacterProfile>("{\"name\":\"검증\",\"jobId\":100}");
            Assert.That(legacy.IsValid,Is.True); Assert.That(legacy.unspentClassChangeStatPoints,Is.Zero);
            source.unspentClassChangeStatPoints=int.MaxValue;
            Assert.That(source.TryBuildClassChange(Table,Catalog,5,out _),Is.False);
            Assert.That(source.unspentClassChangeStatPoints,Is.EqualTo(int.MaxValue));
            source.unspentClassChangeStatPoints=-1; Assert.That(source.IsValid,Is.False);
        }
        private static MagicCatalog Catalog => AssetDatabase.LoadAssetAtPath<MagicCatalog>("Assets/_Game/Runtime/Magic/MagicCatalog.asset");
        private static ExperienceProgressionTable Table => AssetDatabase.LoadAssetAtPath<ExperienceProgressionTable>("Assets/_Game/Runtime/Combat/ExperienceProgression.asset");

        [Test]
        public void SeparateBaseAndJobLevelsConsumeExperienceWithoutGrantingAbilities()
        {
            var source = new NativeCharacterProfile {name = "검증", jobId = 105, hasClassAbilities = true,
                learnedMagicIds = new[] {101, 2401}, usableSkillIds = new[] {10102, 10301, 21102},
                hasLevels = true, baseLevel = 12, jobLevel = 7};
            Assert.That(source.TryAdvanceLevels(Table, 11000, out var changed, out var remaining), Is.True);
            Assert.That((changed.baseLevel, changed.jobLevel, remaining), Is.EqualTo((13, 8, 0u)));
            Assert.That((source.baseLevel, source.jobLevel), Is.EqualTo((12, 7)));
            Assert.That(changed.learnedMagicIds, Is.EqualTo(source.learnedMagicIds));
            Assert.That(changed.usableSkillIds, Is.EqualTo(source.usableSkillIds));
            Assert.That(source.TryAdvanceLevels(Table, 10999, out changed, out remaining), Is.True);
            Assert.That((changed.baseLevel, changed.jobLevel, remaining), Is.EqualTo((12, 7, 10999u)));
        }

        [Test]
        public void MultipleLevelUpsStopAtClassChangeAndKeepUnspentExperience()
        {
            var source = new NativeCharacterProfile {name = "검증", jobId = 100, hasLevels = true, baseLevel = 1, jobLevel = 1};
            Assert.That(source.TryAdvanceLevels(Table, 9000, out var grown, out var remaining), Is.True);
            Assert.That((grown.baseLevel, grown.jobLevel, grown.jobId, remaining), Is.EqualTo((5, 5, 100, 4000u)));
            Assert.That(grown.hasClassAbilities, Is.False);
            Assert.That(grown.TryBuildEligibleClassChange(Table, Catalog, out var changed), Is.True);
            Assert.That((changed.baseLevel, changed.jobLevel, changed.jobId), Is.EqualTo((5, 1, 105)));
            Assert.That(changed.learnedMagicIds, Is.EqualTo(new[] {101, 2401}));
            Assert.That(source.TryBuildEligibleClassChange(Table, Catalog, out _), Is.False);
        }

        [Test]
        public void LegacyUnknownLevelsAndInvalidStatesAreNotGuessedOrPartiallyAdvanced()
        {
            var legacy = new NativeCharacterProfile {name = "검증", jobId = 100};
            Assert.That(legacy.IsValid, Is.True);
            Assert.That(legacy.TryAdvanceLevels(Table, 9999, out var changed, out var remaining), Is.False);
            Assert.That(changed, Is.Null); Assert.That(remaining, Is.EqualTo(9999));
            Assert.That(legacy.Clone().hasLevels, Is.False);
            var source = legacy.Clone(); source.hasLevels = true; source.baseLevel = 1; source.jobLevel = 1;
            var missing = ScriptableObject.CreateInstance<ExperienceProgressionTable>();
            try {
                missing.Initialize(new[] {new ExperienceLevelEntry {jobId = 100, level = 1, nextJobId = 100, requiredXp = 500}});
                Assert.That(source.TryAdvanceLevels(missing, 500, out changed, out remaining), Is.False);
                Assert.That(changed, Is.Null); Assert.That(remaining, Is.EqualTo(500));
                source.baseLevel = 0; Assert.That(source.IsValid, Is.False);
                source.baseLevel = 1; source.jobLevel = 2; Assert.That(source.IsValid, Is.False);
                source.hasLevels = false; Assert.That(source.IsValid, Is.False);
            } finally { UnityEngine.Object.DestroyImmediate(missing); }
        }

        [Test]
        public void BattleReturnPublishesLevelsAndRemainingXpTogetherOrLeavesSessionUntouched()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try {
                var root = new GameObject("Level return check"); SceneManager.MoveGameObjectToScene(root, scene);
                var session = root.AddComponent<BattleTransitionController>();
                session.InitializeCharacter(new NativeCharacterProfile {name = "검증", jobId = 100,
                    hasLevels = true, baseLevel = 1, jobLevel = 1});
                var panel = root.AddComponent<DarkSaver.UI.TownServicesPanel>(); panel.ConfigureProgression(Table);
                var battle = root.AddComponent<BattleSceneRoot>();
                Assert.That(battle.RestorePlayerExperience(9000), Is.True);
                Assert.That(session.TryCaptureBattleReturn(battle, null, out var reason), Is.True, reason);
                var profile = session.CaptureCharacterProfile();
                Assert.That((profile.baseLevel, profile.jobLevel, session.PlayerExperience), Is.EqualTo((5, 5, 4000u)));
                panel.ConfigureProgression(null);
                Assert.That(session.TryCaptureBattleReturn(battle, null, out _), Is.False);
                Assert.That(session.PlayerExperience, Is.EqualTo(4000));
                Assert.That(session.CaptureCharacterProfile().baseLevel, Is.EqualTo(5));
            } finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void SeparateLevelsSurviveJsonAndNativeSessionRestore()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try {
                var root = new GameObject("Two levels save check"); SceneManager.MoveGameObjectToScene(root, scene);
                var session = root.AddComponent<BattleTransitionController>();
                Assert.That(session.InitializeCharacter(new NativeCharacterProfile {name = "검증", jobId = 100,
                    hasLevels = true, baseLevel = 12, jobLevel = 5}), Is.True);
                var json = JsonUtility.ToJson(session.CaptureSaveState("Kaosia_Map", "Town_Runtime", Vector2.one));
                var saved = JsonUtility.FromJson<NativeSaveData>(json);
                Assert.That(session.RestoreSaveState(saved), Is.True);
                saved.character.baseLevel = 999;
                var copy = session.CaptureCharacterProfile();
                Assert.That((copy.baseLevel, copy.jobLevel), Is.EqualTo((12, 5)));
                Assert.That(copy.TryBuildEligibleClassChange(Table, Catalog, out var changed), Is.True);
                Assert.That((changed.baseLevel, changed.jobLevel), Is.EqualTo((12, 1)));
            } finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void TownPreviewShowsNativeEdgeWithoutApplyingClassOrGrantingSpells()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("Class preview check"); SceneManager.MoveGameObjectToScene(root, scene);
                var session = root.AddComponent<BattleTransitionController>();
                Assert.That(session.InitializeCharacter(new NativeCharacterProfile {name = "검증", jobId = 100}), Is.True);
                var player = root.AddComponent<DarkSaver.Player.TopDownPlayerController>();
                root.AddComponent<DarkSaver.UI.MagicCatalogPanel>().Configure(Catalog, player);
                var panel = root.AddComponent<DarkSaver.UI.TownServicesPanel>(); panel.ConfigureProgression(Table);
                var rows = panel.CaptureClassChangeRows(); Assert.That(rows.Length, Is.GreaterThan(0));
                Assert.That(panel.TryPreviewClassChange(0, out var source, out var projected), Is.True);
                Assert.That((source.jobId, source.level, source.nextJobId, source.requiredXp), Is.EqualTo((100, 5, 105, 2500L)));
                Assert.That(projected.learnedMagicIds, Is.EqualTo(new[] {101, 2401}));
                Assert.That(session.PlayerJobId, Is.EqualTo(100)); Assert.That(session.PlayerExperience, Is.Zero);
                Assert.That(session.CaptureCharacterProfile().hasClassAbilities, Is.False);
                projected.name = "別"; Assert.That(session.PlayerName, Is.EqualTo("검증"));
                Assert.That(panel.TryPreviewClassChange(-1, out _, out _), Is.False);
                panel.ConfigureProgression(null); Assert.That(panel.TryPreviewClassChange(0, out _, out _), Is.False);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void ClassChangeRowsReturnCopiesAndDoNotTreatNpcAsPlayer()
        {
            var rows = Table.GetClassChangeRows(100); rows[0].nextJobId = 999;
            Assert.That(Table.GetClassChangeRows(100)[0].nextJobId, Is.EqualTo(105));
            Assert.That(Table.GetClassChangeRows(800), Is.Empty);
        }

        [Test]
        public void MergeRetainsAllKnownIdsIncludingEveryExplicitLevel()
        {
            Assert.That(Catalog.TryMergeLearnedMagic(new[] {101, 2401}, new[] {102, 101, 103}, out var ids), Is.True);
            Assert.That(ids, Is.EqualTo(new[] {101, 2401, 102, 103}));
            Assert.That(Catalog.TryMergeLearnedMagic(ids, new[] {102, 2401}, out var lower), Is.True);
            Assert.That(lower, Is.EqualTo(ids));
            Assert.That(Catalog.TryMergeLearnedMagic(ids, new[] {9999}, out var failed), Is.False);
            Assert.That(failed, Is.Empty);
        }

        [Test]
        public void NamesAndIdMagnitudeNeverAuthorizeDeletingLearnedSpells()
        {
            var catalog = ScriptableObject.CreateInstance<MagicCatalog>();
            MagicCatalogEntry Entry(int id, string name) {
                var values = new long[MagicCatalogEntry.NumericColumnCount]; values[0] = id;
                return new MagicCatalogEntry(values, name);
            }
            try
            {
                catalog.Initialize(new[] {Entry(900, "Spell(Lv1)"), Entry(100, "Spell(Lv3)"), Entry(901, "Other(Lv1)")});
                Assert.That(catalog.TryMergeLearnedMagic(new[] {900}, new[] {100, 901}, out var ids), Is.True);
                Assert.That(ids, Is.EqualTo(new[] {900, 100, 901}));
            }
            finally { UnityEngine.Object.DestroyImmediate(catalog); }
        }

        [Test]
        public void MagicMergeRejectsUnknownIdsAtomicallyAndDoesNotMutateInputOrAliasResult()
        {
            var previous=new[]{101,2401};var added=new[]{102,101,102};
            Assert.That(Catalog.TryMergeLearnedMagic(previous,added,out var merged),Is.True);
            Assert.That(merged,Is.EqualTo(new[]{101,2401,102}));
            merged[0]=103;
            Assert.That(previous,Is.EqualTo(new[]{101,2401}));Assert.That(added,Is.EqualTo(new[]{102,101,102}));
            foreach(int invalid in new[]{-1,0,9999,10000}) {
                Assert.That(Catalog.TryMergeLearnedMagic(previous,new[]{102,invalid},out var failed),Is.False);
                Assert.That(failed,Is.Empty);Assert.That(previous,Is.EqualTo(new[]{101,2401}));
            }
            Assert.That(Catalog.TryMergeLearnedMagic(null,added,out _),Is.False);
            Assert.That(Catalog.TryMergeLearnedMagic(previous,null,out _),Is.False);
        }
        [Test]
        public void NativeClassChangePreservesMagicAndUsesExactNewSkillList()
        {
            var original = new NativeCharacterProfile { name = "검증", jobId = 100 };
            Assert.That(original.TryBuildClassChange(Table, Catalog, 4, out _), Is.False);
            Assert.That(original.TryBuildClassChange(Table, Catalog, 5, out var changed), Is.True);
            Assert.That(changed.jobId, Is.EqualTo(105));
            Assert.That(changed.learnedMagicIds, Is.EqualTo(new[] {101, 2401}));
            Assert.That(changed.usableSkillIds, Is.EqualTo(new[] {10102, 10301, 21102}));
            Assert.That(original.jobId, Is.EqualTo(100)); Assert.That(original.hasClassAbilities, Is.False);
            Assert.That(changed.IsValid, Is.True);
            var clone = changed.Clone(); clone.learnedMagicIds[0] = 102;
            Assert.That(changed.learnedMagicIds[0], Is.EqualTo(101));
        }

        [Test]
        public void ChangedProfileSurvivesSaveSerializationSessionRestoreAndRecapture()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("Class save check"); SceneManager.MoveGameObjectToScene(root, scene);
                var session = root.AddComponent<BattleTransitionController>();
                var original = new NativeCharacterProfile { name = "검증", jobId = 100 };
                Assert.That(original.TryBuildClassChange(Table, Catalog, 5, out var changed), Is.True);
                Assert.That(session.InitializeCharacter(changed), Is.True);
                var save = session.CaptureSaveState("Kaosia_Map", "Town_Runtime", Vector2.one);
                var loaded = JsonUtility.FromJson<NativeSaveData>(JsonUtility.ToJson(save));
                Assert.That(NativeSaveStore.IsValid(loaded), Is.True);
                Assert.That(session.RestoreSaveState(loaded), Is.True);
                loaded.character.learnedMagicIds[0] = 102;
                var recaptured = session.CaptureSaveState("Kaosia_Map", "Town_Runtime", Vector2.one);
                Assert.That(recaptured.character.jobId, Is.EqualTo(105));
                Assert.That(recaptured.character.unspentClassChangeStatPoints,Is.EqualTo(20));
                Assert.That(recaptured.character.learnedMagicIds, Is.EqualTo(new[] {101, 2401}));
                Assert.That(recaptured.character.usableSkillIds, Does.Contain(21102));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void InvalidOrUnconfirmedAdvancedProfileIsRejected()
        {
            var profile = new NativeCharacterProfile { name = "검증", jobId = 105 };
            Assert.That(profile.IsValid, Is.False);
            profile.hasClassAbilities = true; profile.learnedMagicIds = new[] {101, 101}; profile.usableSkillIds = Array.Empty<int>();
            Assert.That(profile.IsValid, Is.False);
            Assert.That(profile.TryBuildClassChange(Table, Catalog, 10, out _), Is.False);
            Assert.That(NativeCharacterProfile.TryQueue("검증", 105, null, out _), Is.False);
        }
    }
}
