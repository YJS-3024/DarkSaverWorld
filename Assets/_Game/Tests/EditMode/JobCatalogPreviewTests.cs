using System;
using System.Linq;
using DarkSaver.Editor;
using DarkSaver.Jobs;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Tests
{
    public sealed class JobCatalogPreviewTests
    {
        [TestCase(JobCatalogConverter.Output, 73)]
        [TestCase(JobCatalogConverter.MonsterOutput, 337)]
        [TestCase(JobCatalogConverter.NpcOutput, 37)]
        public void DefaultFilterReadsSelectedNativeCatalog(string path, int count)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<JobCatalog>(path);
            Assert.That(catalog, Is.Not.Null);
            Assert.That(JobCatalogPreviewWindow.GetMatches(catalog, null, false, null).Count, Is.EqualTo(count));
        }

        [Test]
        public void MismatchFilterKeepsJobAndSpriteIdsSeparate()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<JobCatalog>(JobCatalogConverter.MonsterOutput);
            var rows = JobCatalogPreviewWindow.GetMatches(catalog, "", true, "");
            Assert.That(rows.Count, Is.EqualTo(130));
            Assert.That(rows.All(row => row.Id != row.SpriteId), Is.True);
            Assert.That(rows.Single(row => row.Id == 799).SpriteId, Is.EqualTo(713));
            foreach (var path in new[] { JobCatalogConverter.Output, JobCatalogConverter.NpcOutput })
                Assert.That(JobCatalogPreviewWindow.GetMatches(AssetDatabase.LoadAssetAtPath<JobCatalog>(path), "", true, "").Count, Is.Zero);
        }

        [Test]
        public void ExactSpriteFilterReturnsAllMatchingJobsWithoutSubstitution()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<JobCatalog>(JobCatalogConverter.MonsterOutput);
            var rows = JobCatalogPreviewWindow.GetMatches(catalog, "", false, " 713 ");
            Assert.That(rows.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(rows.All(row => row.SpriteId == 713), Is.True);
            Assert.That(rows.Any(row => row.Id == 799), Is.True);
            rows = JobCatalogPreviewWindow.GetMatches(catalog, "799", true, "713");
            Assert.That(rows.Count, Is.EqualTo(1));
            Assert.That(rows[0].Id, Is.EqualTo(799));
            Assert.That(JobCatalogPreviewWindow.GetMatches(catalog, "799", true, "701").Count, Is.Zero);
        }

        [TestCase("not-an-id")]
        [TestCase("713.0")]
        [TestCase("1E+2")]
        [TestCase("9223372036854775808")]
        public void InvalidSpriteIdDoesNotFallBackToAllRows(string filter)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<JobCatalog>(JobCatalogConverter.MonsterOutput);
            Assert.That(JobCatalogPreviewWindow.GetMatches(catalog, "", false, filter).Count, Is.Zero);
        }

        [Test]
        public void NameSearchNormalizesOnlyPresentationAndHandlesEmptySource()
        {
            var catalog = ScriptableObject.CreateInstance<JobCatalog>();
            try
            {
                var values = new long[JobCatalogEntry.NumericColumnCount]; values[0] = 2; values[1] = 713;
                var row = new JobCatalogEntry(values, "-1", "기본_직업", "설명_원문", "annotation");
                catalog.Initialize(new[] { row });
                Assert.That(JobCatalogPreviewWindow.GetMatches(catalog, " 기본 직업 ", false, "").Single(), Is.SameAs(row));
                Assert.That(JobCatalogPreviewWindow.GetMatches(catalog, "기본_직업", false, "").Single(), Is.SameAs(row));
                Assert.That(row.Name, Is.EqualTo("기본_직업"));
                Assert.That(row.Description, Is.EqualTo("설명_원문"));
                Assert.That(row.HorizontalTransitionText, Is.EqualTo("-1"));
                Assert.That(JobCatalogPreviewWindow.GetMatches(null, "", false, "").Count, Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(catalog); }
        }

        [Test]
        public void SelectionRetainsVisibleIdAndClearsMissingResults()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<JobCatalog>(JobCatalogConverter.MonsterOutput);
            var rows = JobCatalogPreviewWindow.GetMatches(catalog, "", false, "");
            Assert.That(JobCatalogPreviewWindow.GetSelected(rows, 799).Id, Is.EqualTo(799));
            Assert.That(JobCatalogPreviewWindow.GetSelected(rows, -1).Id, Is.EqualTo(301));
            Assert.That(JobCatalogPreviewWindow.GetSelected(JobCatalogPreviewWindow.GetMatches(catalog, "no-such-name-xyz", false, ""), 799), Is.Null);
            Assert.That(JobCatalogPreviewWindow.GetSelected(null, 799), Is.Null);
        }

        [Test]
        public void NumericLabelsMatchPreservedColumnPositions()
        {
            Assert.That(JobCatalogPreviewWindow.GetNumericColumnLabel(0), Is.EqualTo("직업"));
            Assert.That(JobCatalogPreviewWindow.GetNumericColumnLabel(1), Is.EqualTo("직그림"));
            Assert.That(JobCatalogPreviewWindow.GetNumericColumnLabel(2), Is.EqualTo("체력수"));
            Assert.That(JobCatalogPreviewWindow.GetNumericColumnLabel(33), Is.EqualTo("가격"));
            Assert.Throws<ArgumentOutOfRangeException>(() => JobCatalogPreviewWindow.GetNumericColumnLabel(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => JobCatalogPreviewWindow.GetNumericColumnLabel(34));
        }

        [Test]
        public void FiltersDoNotModifyNativeCatalogs()
        {
            foreach (var path in new[] { JobCatalogConverter.Output, JobCatalogConverter.MonsterOutput, JobCatalogConverter.NpcOutput })
            {
                var catalog = AssetDatabase.LoadAssetAtPath<JobCatalog>(path);
                var before = EditorJsonUtility.ToJson(catalog);
                var wasDirty = EditorUtility.IsDirty(catalog);
                JobCatalogPreviewWindow.GetMatches(catalog, "키스라", false, "");
                JobCatalogPreviewWindow.GetMatches(catalog, "", true, "713");
                JobCatalogPreviewWindow.GetSelected(JobCatalogPreviewWindow.GetMatches(catalog, "800", false, ""), 799);
                Assert.That(EditorJsonUtility.ToJson(catalog), Is.EqualTo(before));
                Assert.That(EditorUtility.IsDirty(catalog), Is.EqualTo(wasDirty));
            }
        }
    }
}
