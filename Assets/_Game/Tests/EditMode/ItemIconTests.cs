using System;
using System.IO;
using DarkSaver.Editor;
using DarkSaver.Items;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Tests
{
    public sealed class ItemIconTests
    {
        [TestCase(0, 0, 288)]
        [TestCase(99, 3168, 288)]
        [TestCase(100, 0, 256)]
        [TestCase(999, 3168, 0)]
        public void CellsAreIndexedFromTopLeftWithoutMirroring(int id, int x, int y)
            => Assert.That(ItemIconSheetBuilder.CellRect(id), Is.EqualTo(new Rect(x, y, 32, 32)));

        [Test]
        public void DecoderRejectsMissingAndTruncatedDataAndInvalidCells()
        {
            Assert.Throws<InvalidDataException>(() => ItemIconSheetBuilder.Decode(null));
            Assert.Throws<InvalidDataException>(() => ItemIconSheetBuilder.Decode(new byte[50]));
            Assert.Throws<ArgumentOutOfRangeException>(() => ItemIconSheetBuilder.CellRect(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => ItemIconSheetBuilder.CellRect(1000));
        }

        [Test]
        public void GeneratedSheetMatchesAllOriginalPixelsAndImportSettings()
        {
            var texture = new Texture2D(2, 2);
            try {
                Assert.That(texture.LoadImage(File.ReadAllBytes(ItemIconSheetBuilder.Output)), Is.True);
                Assert.That((texture.width, texture.height), Is.EqualTo((3200, 320)));
                var expected = ItemIconSheetBuilder.Decode(File.ReadAllBytes(ItemIconSheetBuilder.Source));
                var actual = texture.GetPixels32();
                Assert.That(actual.Length, Is.EqualTo(expected.Length));
                for (int i = 0; i < actual.Length; i++)
                    if (!actual[i].Equals(expected[i])) Assert.Fail("Original pixel mismatch at " + i);
            } finally { UnityEngine.Object.DestroyImmediate(texture); }
            var importer = (TextureImporter)AssetImporter.GetAtPath(ItemIconSheetBuilder.Output);
            Assert.That(importer.maxTextureSize, Is.EqualTo(4096)); Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(importer.mipmapEnabled, Is.False); Assert.That(importer.isReadable, Is.False);
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
        }

        [Test]
        public void EveryItemPictureUsesNativeIndexAndCachedSpriteWithoutFallback()
        {
            Assert.That(ItemIcons.Get(-1), Is.Null); Assert.That(ItemIcons.Get(1000), Is.Null);
            Assert.That(ItemIcons.Get((ItemCatalogEntry)null), Is.Null);
            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(ItemCatalogConverter.Output);
            for (int i = 0; i < catalog.Count; i++) {
                var row = catalog.GetAt(i); var sprite = ItemIcons.Get(row);
                Assert.That(sprite, Is.Not.Null, "Item " + row.Id);
                Assert.That(sprite.rect, Is.EqualTo(ItemIconSheetBuilder.CellRect((int)row.GetNumericValue(1))));
                Assert.That(ItemIcons.Get(row), Is.SameAs(sprite));
                Assert.That(row.GetNumericValue(2), Is.Zero); // Preserved, not guessed as a second animation frame.
            }
            Assert.That(Resources.LoadAll<Sprite>("UI/ItemIcons").Length, Is.EqualTo(1000));
            foreach (string dependency in AssetDatabase.GetDependencies(ItemIconSheetBuilder.Output, true))
                Assert.That(dependency, Is.EqualTo(ItemIconSheetBuilder.Output));
        }
    }
}
