using NUnit.Framework;
using UnityEngine;

namespace DarkSaver.Prototype.Tests
{
    public sealed class OriginalScreenLayoutTests
    {
        [Test]
        public void Create_FitsFourByThreeInsideWideWindowsScreen()
        {
            var layout = OriginalScreenLayout.Create(
                1920, 1080, new Rect(0, 0, 1920, 1080), 640, 480);

            Assert.That(layout.Scale, Is.EqualTo(2.25f));
            Assert.That(layout.Offset, Is.EqualTo(new Vector2(240, 0)));
        }

        [Test]
        public void Create_RespectsMobileSafeArea()
        {
            var layout = OriginalScreenLayout.Create(
                2400, 1080, new Rect(100, 0, 2200, 1080), 640, 480);

            Assert.That(layout.Scale, Is.EqualTo(2.25f));
            Assert.That(layout.Offset, Is.EqualTo(new Vector2(480, 0)));
        }

        [Test]
        public void ScreenToReference_ConvertsBottomLeftTouchCoordinates()
        {
            var layout = OriginalScreenLayout.Create(
                1920, 1080, new Rect(0, 0, 1920, 1080), 640, 480);

            var result = layout.ScreenToReference(new Vector2(960, 540), 1080);

            Assert.That(result.x, Is.EqualTo(320).Within(.01f));
            Assert.That(result.y, Is.EqualTo(240).Within(.01f));
        }
    }
}
