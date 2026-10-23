using System;
using System.IO;
using DarkSaver.Editor;
using DarkSaver.Items;
using NUnit.Framework;
using UnityEngine;

namespace DarkSaver.Tests
{
    public sealed class ItemDefinitionPublisherTests
    {
        private static ItemCatalogEntry Row(int id, long attack, string name)
        {
            var values = new long[ItemCatalogEntry.NumericColumnCount]; values[0] = id; values[20] = attack;
            return new ItemCatalogEntry(values, "1E+11", "1,4", name, "설명", "weapon");
        }

        [Test]
        public void ApplyKeepsCatalogObjectAndPublishesIndependentEditedValues()
        {
            var catalog = ScriptableObject.CreateInstance<ItemCatalog>();
            var definition = ScriptableObject.CreateInstance<ItemDefinition>();
            try {
                catalog.Initialize(new[] {Row(1, 1, "before")});
                var sameReference = catalog;
                definition.Initialize(Row(1, 123, "edited"));
                ItemDefinitionPublisher.Apply(catalog, new[] {definition});
                Assert.That(catalog, Is.SameAs(sameReference));
                Assert.That(catalog.TryGet(1, out var actual), Is.True);
                Assert.That(actual.Name, Is.EqualTo("edited")); Assert.That(actual.GetNumericValue(20), Is.EqualTo(123));
                Assert.That(actual.PriceNotation, Is.EqualTo("1E+11")); Assert.That(actual.DamageTargets, Is.EqualTo("1,4"));
                Assert.That(actual, Is.Not.SameAs(definition.Data));
                definition.Initialize(Row(1, 999, "unpublished"));
                Assert.That(actual.Name, Is.EqualTo("edited")); Assert.That(actual.GetNumericValue(20), Is.EqualTo(123));
            } finally { UnityEngine.Object.DestroyImmediate(definition); UnityEngine.Object.DestroyImmediate(catalog); }
        }

        [Test]
        public void MissingDuplicateAndUninitializedDefinitionsNeverChangeCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<ItemCatalog>();
            var first = ScriptableObject.CreateInstance<ItemDefinition>();
            var second = ScriptableObject.CreateInstance<ItemDefinition>();
            var blank = ScriptableObject.CreateInstance<ItemDefinition>();
            try {
                catalog.Initialize(new[] {Row(1, 1, "one"), Row(2, 2, "two")});
                first.Initialize(Row(1, 50, "edited")); second.Initialize(Row(2, 60, "edited two"));
                Assert.Throws<InvalidDataException>(() => ItemDefinitionPublisher.Apply(catalog, new[] {first}));
                Assert.Throws<InvalidDataException>(() => ItemDefinitionPublisher.Apply(catalog, new[] {first, first, second}));
                Assert.Throws<InvalidDataException>(() => ItemDefinitionPublisher.Apply(catalog, new[] {first, second, blank}));
                Assert.Throws<InvalidDataException>(() => ItemDefinitionPublisher.Apply(catalog, new ItemDefinition[] {first, second, null}));
                Assert.That(catalog.Count, Is.EqualTo(2));
                Assert.That(catalog.GetAt(0).Name, Is.EqualTo("one")); Assert.That(catalog.GetAt(1).GetNumericValue(20), Is.EqualTo(2));
                var candidate = ItemDefinitionPublisher.BuildCandidate(catalog, new[] {second, first});
                Assert.That(candidate[0].Id, Is.EqualTo(1)); Assert.That(candidate[1].Id, Is.EqualTo(2));
                Assert.That(catalog.GetAt(0).Name, Is.EqualTo("one"));
            } finally {
                UnityEngine.Object.DestroyImmediate(first); UnityEngine.Object.DestroyImmediate(second);
                UnityEngine.Object.DestroyImmediate(blank); UnityEngine.Object.DestroyImmediate(catalog);
            }
        }
    }
}
