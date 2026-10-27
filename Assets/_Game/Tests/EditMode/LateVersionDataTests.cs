using System;
using DarkSaver.Combat;
using DarkSaver.Core;
using DarkSaver.Editor;
using DarkSaver.Jobs;
using DarkSaver.Magic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Tests
{
    public sealed class LateVersionDataTests
    {
        [TestCase(167,170)]
        [TestCase(168,171)]
        [TestCase(169,172)]
        public void LateClassChangeAndLevel300SurviveProfileRoundtrip(int previous,int next)
        {
            var table=AssetDatabase.LoadAssetAtPath<ExperienceProgressionTable>(ExperienceProgressionConverter.Output);
            var magic=AssetDatabase.LoadAssetAtPath<MagicCatalog>(MagicCatalogConverter.Output);
            Assert.That(table.Count,Is.EqualTo(3177));
            var profile=new NativeCharacterProfile {name="후기검증",jobId=previous,hasLevels=true,baseLevel=300,jobLevel=100,
                hasClassAbilities=true,learnedMagicIds=new[]{101},usableSkillIds=Array.Empty<int>()};
            Assert.That(profile.TryBuildEligibleClassChange(table,magic,out var changed),Is.True);
            Assert.That(changed.jobId,Is.EqualTo(next));
            Assert.That(changed.learnedMagicIds,Does.Contain(101));
            Assert.That(profile.jobId,Is.EqualTo(previous)); // Candidate creation is not an in-place mutation.
            changed.jobLevel=299;changed.baseLevel=600;
            Assert.That(changed.TryAdvanceLevels(table,uint.MaxValue,out var advanced,out _),Is.True);
            Assert.That(advanced.jobLevel,Is.EqualTo(300));
            var restored=JsonUtility.FromJson<NativeCharacterProfile>(JsonUtility.ToJson(advanced));
            Assert.That(restored.IsValid,Is.True);Assert.That(restored.jobLevel,Is.EqualTo(300));
            restored.jobLevel=301;Assert.That(restored.IsValid,Is.False);
            profile.jobLevel=101;Assert.That(profile.IsValid,Is.False);
        }

        [Test]
        public void LateAbilitiesPreserveKnownCoefficientsAndBlockUnknownCurism()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<MagicCatalog>(MagicCatalogConverter.Output);
            Assert.That(catalog.Count,Is.EqualTo(440));
            int[] ea={-3,-5,-7,-9};
            for(int i=0;i<4;i++) {
                Assert.That(catalog.TryGet(11201+i,out var entry),Is.True);
                Assert.That(entry.GetNumericValue(9),Is.EqualTo(ea[i]));
                Assert.That(UnresolvedAbilityRules.TryCreate(entry,out var rule),Is.True);
                Assert.That(rule.Effect,Is.EqualTo(AbilityEffect.Regeneration));
            }
            Assert.That(catalog.TryGet(30901,out var unknown),Is.True);
            Assert.That(UnresolvedAbilityRules.TryCreate(unknown,out _),Is.False);
        }

        [Test]
        public void LateArtworkHasAllPlayerClassesWithoutInventingMissingAlliedPalette()
        {
            var normal=AssetDatabase.LoadAssetAtPath<ClassArtworkCatalog>(ClassArtworkConverter.Output);
            var other=AssetDatabase.LoadAssetAtPath<ClassArtworkCatalog>("Assets/_Game/Runtime/Jobs/ClassArtworkCatalog_Color2.asset");
            var allied=AssetDatabase.LoadAssetAtPath<ClassArtworkCatalog>("Assets/_Game/Runtime/Jobs/ClassArtworkCatalog_Color3.asset");
            Assert.That(normal.Count,Is.EqualTo(73));Assert.That(other.Count,Is.EqualTo(73));Assert.That(allied.Count,Is.EqualTo(72));
            foreach(int job in new[]{169,170,171,172}) {
                Assert.That(normal.TryGet(job,out var art),Is.True);
                for(int i=0;i<12;i++) {
                    Assert.That(art.GetFrame(i).rect.width,Is.EqualTo(32));
                    Assert.That(art.GetFrame(i).rect.height,Is.EqualTo(32));
                }
            }
            Assert.That(allied.TryGet(170,out _),Is.False);
        }
    }
}
