using System;
using DarkSaver.Editor;
using DarkSaver.Jobs;
using NUnit.Framework;
using UnityEngine;

namespace DarkSaver.Tests
{
    public sealed class ClassRelationColorTests
    {
        [Test]
        public void RemapsOnlyNativeSixteenColorRegion()
        {
            foreach (int mode in new[] { 0, 2, 3 })
                for (int index = 0; index < 256; index++)
                    Assert.AreEqual(index >= 0xA0 && index <= 0xAF ? index + mode * 16 : index,
                        ClassArtworkConverter.RemapColorIndex(index, mode));
            Assert.Throws<ArgumentOutOfRangeException>(() => ClassArtworkConverter.RemapColorIndex(160, 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => ClassArtworkConverter.RemapColorIndex(-1, 0));
        }

        [Test]
        public void MissingRelationCatalogDoesNotSubstituteDefaultArtwork()
        {
            var catalog = ScriptableObject.CreateInstance<ClassArtworkCatalog>();
            try {
                Assert.IsFalse(catalog.TryApplyRelation(100, null, false, true));
                Assert.IsFalse(catalog.TryApplyRelation(100, null, false, false));
                Assert.Throws<ArgumentException>(() => catalog.ConfigureRelationColors(catalog, catalog));
            } finally { UnityEngine.Object.DestroyImmediate(catalog); }
        }
    }
}
