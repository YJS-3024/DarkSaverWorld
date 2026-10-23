using System;
using System.Linq;
using DarkSaver.Editor;
using DarkSaver.Jobs;
using NUnit.Framework;
using UnityEditor;

namespace DarkSaver.Tests
{
    public sealed class NonPlayerJobCatalogTests
    {
        [TestCase(JobCatalogConverter.MonsterOutput, 324, 301, 799, "monster")]
        [TestCase(JobCatalogConverter.NpcOutput, 35, 800, 981, "npc_or_special")]
        public void CatalogPreservesCountsBoundsCategoryAndHasNoCsvDependency(string path, int count, int first, int last, string category)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<JobCatalog>(path);
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Count, Is.EqualTo(count));
            Assert.That(catalog.GetAt(0).Id, Is.EqualTo(first));
            Assert.That(catalog.GetAt(count - 1).Id, Is.EqualTo(last));
            for (var i = 0; i < count; i++) Assert.That(catalog.GetAt(i).CategoryLabel, Is.EqualTo(category));
            Assert.That(catalog.TryGet(first - 1, out var missing), Is.False);
            Assert.That(missing, Is.Null);
            Assert.That(catalog.TryGet(last + 1, out missing), Is.False);
            Assert.That(missing, Is.Null);
            foreach (var dependency in AssetDatabase.GetDependencies(path))
                Assert.That(dependency.EndsWith(".csv", StringComparison.OrdinalIgnoreCase), Is.False);
        }

        [Test]
        public void MonsterCatalogKeepsDifferentSpriteIdsNegativeSentinelsAndKisraValues()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<JobCatalog>(JobCatalogConverter.MonsterOutput);
            var rows = Enumerable.Range(0, catalog.Count).Select(catalog.GetAt).ToArray();
            Assert.That(rows.Count(row => row.Id != row.SpriteId), Is.EqualTo(123));
            Assert.That(rows.Any(row => Enumerable.Range(0, JobCatalogEntry.NumericColumnCount).Any(column => row.GetNumericValue(column) == -1)), Is.True);
            Assert.That(catalog.TryGet(701, out var kisra), Is.True);
            Assert.That(kisra.Name, Is.EqualTo("키스라"));
            Assert.That(kisra.SpriteId, Is.EqualTo(701));
            Assert.That(kisra.GetNumericValue(2), Is.EqualTo(300));
            Assert.That(kisra.GetNumericValue(4), Is.EqualTo(150));
            Assert.That(kisra.GetNumericValue(6), Is.EqualTo(126));
            Assert.That(kisra.GetNumericValue(8), Is.EqualTo(55));
            Assert.That(kisra.HorizontalTransitionText, Is.EqualTo("-1"));
            Assert.That(catalog.TryGet(799, out var sharedSprite), Is.True);
            Assert.That(sharedSprite.Name, Is.EqualTo("미안투"));
            Assert.That(sharedSprite.SpriteId, Is.EqualTo(713));
            Assert.That(sharedSprite.HorizontalTransitionText, Is.EqualTo("100"));
            Assert.That(rows.SelectMany(row => Enumerable.Range(0, JobCatalogEntry.NumericColumnCount).Select(row.GetNumericValue)).Max(), Is.EqualTo(50000000L));
        }

        [Test]
        public void NpcSpecialCatalogKeepsTextAndDoesNotInferBehaviorFromCategory()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<JobCatalog>(JobCatalogConverter.NpcOutput);
            Assert.That(catalog.TryGet(800, out var first), Is.True);
            Assert.That(first.Name, Is.EqualTo("파이터"));
            Assert.That(first.Description, Is.Empty);
            Assert.That(first.HorizontalTransitionText, Is.EqualTo("100"));
            Assert.That(first.GetNumericValue(32), Is.EqualTo(1000));
            Assert.That(catalog.TryGet(981, out var last), Is.True);
            Assert.That(last.Name, Is.EqualTo("물고기"));
            Assert.That(last.HorizontalTransitionText, Is.EqualTo("-1"));
            Assert.That(last.CategoryLabel, Is.EqualTo("npc_or_special"));
        }

        [Test]
        public void SeparateCatalogsDoNotOverlapOrReplacePlayerJobs()
        {
            var paths = new[] { JobCatalogConverter.Output, JobCatalogConverter.MonsterOutput, JobCatalogConverter.NpcOutput };
            var ids = new System.Collections.Generic.HashSet<int>();
            foreach (var path in paths)
            {
                var catalog = AssetDatabase.LoadAssetAtPath<JobCatalog>(path);
                for (var i = 0; i < catalog.Count; i++) Assert.That(ids.Add(catalog.GetAt(i).Id), Is.True);
            }
            Assert.That(ids.Count, Is.EqualTo(429));
            var players = AssetDatabase.LoadAssetAtPath<JobCatalog>(JobCatalogConverter.Output);
            Assert.That(players.Count, Is.EqualTo(70));
            Assert.That(players.TryGet(100, out var fighter), Is.True);
            Assert.That(fighter.Name, Is.EqualTo("파이터"));
            Assert.That(fighter.GetNumericValue(2), Is.EqualTo(200));
            Assert.That(players.TryGet(701, out _), Is.False);
            Assert.That(players.TryGet(800, out _), Is.False);
        }
    }
}
