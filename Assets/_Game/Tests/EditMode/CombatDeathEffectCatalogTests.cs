using DarkSaver.Combat;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Tests
{
    public sealed class CombatDeathEffectCatalogTests
    {
        [Test]
        public void NativeCatalogMapsEveryCounterAndNeverReadsOriginalResources()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CombatDeathEffectCatalog>("Assets/_Game/Resources/Effects/CombatDeathEffectCatalog.asset");
            Assert.That(catalog, Is.Not.Null);
            for (var counter = 0; counter <= 18; counter++)
            {
                Assert.That(catalog.TryGetEffect(counter, out var sprite), Is.True);
                Assert.That(AssetDatabase.GetAssetPath(sprite), Is.EqualTo($"Assets/_Game/Art/Converted/Effects/Death/DeathEffect_{counter / 3}.png"));
                Assert.That(sprite.rect.width, Is.EqualTo(31)); Assert.That(sprite.rect.height, Is.EqualTo(31));
                Assert.That(sprite.pixelsPerUnit, Is.EqualTo(32)); Assert.That(sprite.pivot, Is.EqualTo(new Vector2(0, 31)));
                var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite));
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
                Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
                Assert.That(importer.mipmapEnabled, Is.False); Assert.That(importer.alphaIsTransparency, Is.True);
            }
            Assert.That(catalog.TryGetEffect(-1, out _), Is.False); Assert.That(catalog.TryGetEffect(19, out _), Is.False);
            foreach (var path in AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(catalog)))
                Assert.That(path.StartsWith("분석/") || path.EndsWith(".bmp"), Is.False);
        }

        [Test]
        public void ConfigurationCopiesInputAndRejectsIncompleteReplacement()
        {
            var catalog = ScriptableObject.CreateInstance<CombatDeathEffectCatalog>();
            try
            {
                Assert.That(catalog.TryGetEffect(0, out _), Is.False);
                var sprites = new Sprite[7];
                for (var i = 0; i < 7; i++) sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/_Game/Art/Converted/Effects/Death/DeathEffect_{i}.png");
                var first = sprites[0]; Assert.That(catalog.Configure(sprites), Is.True);
                sprites[0] = null; Assert.That(catalog.Configure(sprites), Is.False);
                Assert.That(catalog.Configure(null), Is.False); Assert.That(catalog.Configure(new Sprite[6]), Is.False);
                Assert.That(catalog.TryGetEffect(0, out var effect), Is.True); Assert.That(effect, Is.EqualTo(first));
            }
            finally { Object.DestroyImmediate(catalog); }
        }
    }
}
