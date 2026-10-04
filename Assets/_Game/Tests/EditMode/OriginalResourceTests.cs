using NUnit.Framework;
using UnityEngine;

namespace DarkSaver.Prototype.Tests
{
    public sealed class OriginalResourceTests
    {
        [TestCase("Original/Map/field_grass", 32, 32)]
        [TestCase("Original/Characters/party_bobyeong", 384, 256)]
        [TestCase("Original/Monsters/monster00", 576, 512)]
        [TestCase("Original/UI/interface", 2024, 366)]
        [TestCase("Original/Items/item", 3200, 320)]
        public void OriginalTexture_IsAvailableWithExpectedPixelSettings(
            string resourcePath, int expectedWidth, int expectedHeight)
        {
            var texture = Resources.Load<Texture2D>(resourcePath);

            Assert.That(texture, Is.Not.Null);
            Assert.That(texture.width, Is.EqualTo(expectedWidth));
            Assert.That(texture.height, Is.EqualTo(expectedHeight));
            Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point));
        }
    }
}
