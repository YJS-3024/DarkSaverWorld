using System;
using System.IO;
using DarkSaver.Combat;
using DarkSaver.Editor;
using NUnit.Framework;
using UnityEngine;

namespace DarkSaver.Tests
{
    public sealed class ExperienceProgressionTests
    {
        private const string Header = "jobId,jobName,level,requiredXp,nextJobId,rewardGold,rewardXp,learnedMagic,usableSkills\n";

        [Test]
        public void ClassEntryReadsOnlyFirstRowAndPreservesNonMagicSkillIds()
        {
            var table = ScriptableObject.CreateInstance<ExperienceProgressionTable>();
            try
            {
                table.Initialize(new[] {
                    new ExperienceLevelEntry { jobId = 145, level = 1, learnedMagic = "701,801", usableSkills = "10104,21106," },
                    new ExperienceLevelEntry { jobId = 145, level = 21, learnedMagic = "901", usableSkills = "10105" }
                });
                Assert.That(table.TryGetClassEntryAbilities(145, out var spells, out var skills), Is.True);
                Assert.That(spells, Is.EqualTo(new[] {701, 801}));
                Assert.That(skills, Is.EqualTo(new[] {10104, 21106}));
                spells[0] = 1;
                table.TryGetClassEntryAbilities(145, out spells, out _);
                Assert.That(spells[0], Is.EqualTo(701));
                Assert.That(table.TryGetClassEntryAbilities(999, out _, out _), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(table); }
        }

        [TestCase(null)] [TestCase("")] [TestCase("101,,102")] [TestCase("0,101")]
        [TestCase("-1")] [TestCase("2147483648")]
        public void UnknownOrMalformedClassAbilitiesDoNotReturnPartialLists(string raw)
        {
            var table = ScriptableObject.CreateInstance<ExperienceProgressionTable>();
            try
            {
                table.Initialize(new[] { new ExperienceLevelEntry {
                    jobId = 100, level = 1, learnedMagic = "101", usableSkills = raw } });
                Assert.That(table.TryGetClassEntryAbilities(100, out var spells, out var skills), Is.False);
                Assert.That(spells, Is.Empty); Assert.That(skills, Is.Empty);
            }
            finally { UnityEngine.Object.DestroyImmediate(table); }
        }

        [Test]
        public void ConvertedSourceMatchesAndAllSeventyClassEntryListsAreReadable()
        {
            var source = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                "분석/Saver_1.2/catalogs/experience_progression.csv");
            var rows = ExperienceProgressionConverter.Parse(File.ReadAllText(source));
            var table = UnityEditor.AssetDatabase.LoadAssetAtPath<ExperienceProgressionTable>(ExperienceProgressionConverter.Output);
            ExperienceProgressionConverter.Verify(table, rows);
            for (int id = 100; id <= 169; id++)
                Assert.That(table.TryGetClassEntryAbilities(id, out _, out _), Is.True, "job=" + id);
            Assert.That(table.TryGetClassEntryAbilities(166, out _, out var skills), Is.True);
            Assert.That(skills, Does.Contain(21705));
        }

        [Test]
        public void NativeClassChangeEdgeUsesNewClassMagicAndUpgradedSkillIds()
        {
            var table = UnityEditor.AssetDatabase.LoadAssetAtPath<ExperienceProgressionTable>(ExperienceProgressionConverter.Output);
            Assert.That(table.TryGetClassChangeAbilities(100, 5, out var next, out var spells, out var skills), Is.True);
            Assert.That(next, Is.EqualTo(105));
            Assert.That(spells, Is.EqualTo(new[] {2401}));
            Assert.That(skills, Is.EqualTo(new[] {10102, 10301, 21102}));
            Assert.That(table.TryGetClassChangeAbilities(100, 4, out next, out spells, out skills), Is.False);
            Assert.That(next, Is.Zero); Assert.That(spells, Is.Empty); Assert.That(skills, Is.Empty);
            // A later level's spell field is not a class-change edge or automatic grant.
            Assert.That(table.TryGetClassChangeAbilities(155, 21, out _, out _, out _), Is.False);
        }

        [Test]
        public void ConfirmedLevelEligibilityUsesSourceThresholdNotExperience()
        {
            var table = UnityEditor.AssetDatabase.LoadAssetAtPath<ExperienceProgressionTable>(ExperienceProgressionConverter.Output);
            Assert.That(table.TryGetEligibleClassChange(100, 4, out _), Is.False);
            Assert.That(table.TryGetEligibleClassChange(100, 5, out var row), Is.True);
            Assert.That(row.nextJobId, Is.EqualTo(105)); Assert.That(row.level, Is.EqualTo(5));
            Assert.That(table.TryGetEligibleClassChange(100, 6, out row), Is.True);
            Assert.That(row.nextJobId, Is.EqualTo(105));
            Assert.That(table.TryGetEligibleClassChange(105, 9, out _), Is.False);
            Assert.That(table.TryGetEligibleClassChange(105, 10, out row), Is.True);
            Assert.That(row.nextJobId, Is.EqualTo(115));
            Assert.That(table.TryGetEligibleClassChange(100, 0, out _), Is.False);
            Assert.That(table.TryGetEligibleClassChange(100, 101, out _), Is.False);
        }

        [Test]
        public void EligibilityRejectsMissingOrAmbiguousNextClassWithoutPartialResult()
        {
            var table = ScriptableObject.CreateInstance<ExperienceProgressionTable>();
            try {
                table.Initialize(new[] {
                    new ExperienceLevelEntry {jobId = 100, level = 5, nextJobId = 105},
                    new ExperienceLevelEntry {jobId = 100, level = 6, nextJobId = 106},
                    new ExperienceLevelEntry {jobId = 105, level = 1, nextJobId = 105},
                    new ExperienceLevelEntry {jobId = 106, level = 1, nextJobId = 106}});
                Assert.That(table.TryGetEligibleClassChange(100, 6, out var row), Is.False);
                Assert.That(row.nextJobId, Is.Zero);
                table.Initialize(new[] {new ExperienceLevelEntry {jobId = 100, level = 5, nextJobId = 105}});
                Assert.That(table.TryGetEligibleClassChange(100, 5, out row), Is.False);
                Assert.That(row.nextJobId, Is.Zero);
            } finally { UnityEngine.Object.DestroyImmediate(table); }
        }

        [Test]
        public void UnresolvedNextClassLeavesAllOutputsEmpty()
        {
            var table = ScriptableObject.CreateInstance<ExperienceProgressionTable>();
            try
            {
                table.Initialize(new[] { new ExperienceLevelEntry {
                    jobId = 100, level = 5, nextJobId = 105, learnedMagic = "0", usableSkills = "10101" } });
                Assert.That(table.TryGetClassChangeAbilities(100, 5, out var next, out var spells, out var skills), Is.False);
                Assert.That(next, Is.Zero); Assert.That(spells, Is.Empty); Assert.That(skills, Is.Empty);
            }
            finally { UnityEngine.Object.DestroyImmediate(table); }
        }

        [Test]
        public void ParserPreservesLargeXpAndQuotedLists()
        {
            var rows = ExperienceProgressionConverter.Parse(Header +
                "100,\"파이터\"\"이름\",100,3477000000,105,150,500,101,\"10101,21101\"\n");
            Assert.That(rows.Length, Is.EqualTo(1));
            Assert.That(rows[0].requiredXp, Is.EqualTo(3477000000L));
            Assert.That(rows[0].jobName, Is.EqualTo("파이터\"이름"));
            Assert.That(rows[0].usableSkills, Is.EqualTo("10101,21101"));
        }

        [TestCase("100,name,1,-1,100,0,0,0,0")]
        [TestCase("100,name,1,abc,100,0,0,0,0")]
        [TestCase("100,name,1,9223372036854775808,100,0,0,0,0")]
        [TestCase("100,name,1,500,100,0,0,0")]
        [TestCase("100,\"name,1,500,100,0,0,0,0")]
        [TestCase("100,\"name\"x,1,500,100,0,0,0,0")]
        public void ParserRejectsMalformedRows(string row)
        {
            Assert.Throws<InvalidDataException>(() => ExperienceProgressionConverter.Parse(Header + row));
        }

        [Test]
        public void LookupSortsAndDistinguishesMissingFromZero()
        {
            var table = ScriptableObject.CreateInstance<ExperienceProgressionTable>();
            try
            {
                table.Initialize(new[]
                {
                    new ExperienceLevelEntry { jobId = 105, level = 100, requiredXp = 3477000000L },
                    new ExperienceLevelEntry { jobId = 100, level = 2, requiredXp = 500 },
                    new ExperienceLevelEntry { jobId = 100, level = 1, requiredXp = 0 }
                });
                Assert.That(table.TryGet(100, 1, out var zero), Is.True);
                Assert.That(zero.requiredXp, Is.Zero);
                Assert.That(table.TryGet(105, 100, out var large), Is.True);
                Assert.That(large.requiredXp, Is.EqualTo(3477000000L));
                Assert.That(table.TryGet(999, 1, out _), Is.False);
                Assert.That(table.TryGet(100, 0, out _), Is.False);
                Assert.That(table.TryGet(100, 101, out _), Is.False);
                Assert.That(table.TryGet(100, 3, out _), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(table); }
        }

        [Test]
        public void InvalidInitializationLeavesExistingDataUnchanged()
        {
            var table = ScriptableObject.CreateInstance<ExperienceProgressionTable>();
            try
            {
                var valid = new ExperienceLevelEntry { jobId = 100, level = 1, requiredXp = 500 };
                table.Initialize(new[] { valid });
                var conflicting = valid; conflicting.rewardXp = 1;
                Assert.Throws<ArgumentException>(() => table.Initialize(new[] { valid, conflicting }));
                Assert.Throws<ArgumentException>(() => table.Initialize(new[]
                {
                    valid, new ExperienceLevelEntry { jobId = 100, level = 2, requiredXp = 499 }
                }));
                Assert.Throws<ArgumentException>(() => table.Initialize(Array.Empty<ExperienceLevelEntry>()));
                Assert.Throws<ArgumentException>(() => table.Initialize(new[]
                {
                    new ExperienceLevelEntry { jobId = 100, level = 0 }
                }));
                Assert.That(table.Count, Is.EqualTo(1));
                Assert.That(table.TryGet(100, 1, out var actual), Is.True);
                Assert.That(actual.requiredXp, Is.EqualTo(500));
            }
            finally { UnityEngine.Object.DestroyImmediate(table); }
        }

        [Test]
        public void IdenticalSourceDuplicatesArePreserved()
        {
            var table = ScriptableObject.CreateInstance<ExperienceProgressionTable>();
            try
            {
                var row = new ExperienceLevelEntry { jobId = 163, level = 35, requiredXp = 754000 };
                table.Initialize(new[] { row, row });
                Assert.That(table.Count, Is.EqualTo(2));
                Assert.That(table.TryGet(163, 35, out var actual), Is.True);
                Assert.That(actual, Is.EqualTo(row));
            }
            finally { UnityEngine.Object.DestroyImmediate(table); }
        }
    }
}
