using System;
using System.IO;
using DarkSaver.Editor;
using DarkSaver.Items;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Tests
{
    public sealed class ItemDefinitionTests
    {
        [Test]
        public void IndividualAssetsPreserveEveryNativeFieldAndNeedNoOriginalFiles()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(ItemCatalogConverter.Output);
            ItemCatalogConverter.Verify(catalog, ItemCatalogConverter.Parse(File.ReadAllText("분석/Saver_Late/unity_catalogs/items.csv")));
            Assert.That(Directory.GetFiles(ItemDefinitionGenerator.Output, "Item_*.asset").Length, Is.EqualTo(catalog.Count));
            for (int i = 0; i < catalog.Count; i++) {
                var source = catalog.GetAt(i);
                ItemDefinitionGenerator.Verify(AssetDatabase.LoadAssetAtPath<ItemDefinition>(ItemDefinitionGenerator.PathFor(source.Id)), source);
            }
            foreach (int id in new[] {0, 1245, 1538}) {
                string path = ItemDefinitionGenerator.PathFor(id);
                foreach (string dependency in AssetDatabase.GetDependencies(path, true))
                    Assert.That(dependency == path || dependency.EndsWith(".cs", StringComparison.Ordinal), Is.True, dependency);
            }
        }

        [Test]
        public void SnapshotIsIndependentAndVerificationRejectsChangedValues()
        {
            var values = new long[ItemCatalogEntry.NumericColumnCount]; values[0] = 12; values[21] = -50;
            var source = new ItemCatalogEntry(values, "1E+11", "1,4", "이름", "설명", "weapon");
            var definition = ScriptableObject.CreateInstance<ItemDefinition>();
            try {
                definition.Initialize(source); values[21] = 900;
                Assert.That(definition.Data, Is.Not.SameAs(source));
                ItemDefinitionGenerator.Verify(definition, source);
                Assert.That(definition.Data.GetNumericValue(21), Is.EqualTo(-50));
                var modified = new ItemCatalogEntry(values, "1E+11", "1,4", "이름", "설명", "weapon");
                Assert.Throws<InvalidDataException>(() => ItemDefinitionGenerator.Verify(definition, modified));
                Assert.Throws<ArgumentNullException>(() => definition.Initialize(null));
                Assert.That(definition.Data.GetNumericValue(21), Is.EqualTo(-50));
            } finally { UnityEngine.Object.DestroyImmediate(definition); }
        }
    }
}
