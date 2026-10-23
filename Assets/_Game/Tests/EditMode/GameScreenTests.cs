using DarkSaver.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace DarkSaver.Tests
{
    public sealed class GameScreenTests
    {
        [Test] public void WorldSpritePixelsMatchUiAtReferenceResolution()
        {
            var go = new GameObject("Pixel scale camera check");
            var target = new RenderTexture(640,480,16);
            try
            {
                var camera = go.AddComponent<Camera>(); camera.orthographic = true;
                camera.targetTexture = target;
                var follow = go.AddComponent<DarkSaver.CameraSystem.CameraFollow2D>();
                follow.Configure(go.transform, new Vector2(40,30));
                camera.aspect = 640f / 480;
                go.transform.position = new Vector3(0,0,-10);
                Assert.That(camera.orthographicSize, Is.EqualTo(7.5f));
                // A native 32px/32PPU sprite occupies one world unit and one 32px UI cell.
                var origin = camera.WorldToScreenPoint(Vector3.zero);
                Assert.That(camera.WorldToScreenPoint(Vector3.right).x-origin.x, Is.EqualTo(32).Within(.001f));
                Assert.That(camera.WorldToScreenPoint(Vector3.up).y-origin.y, Is.EqualTo(32).Within(.001f));
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(target); }
        }

        [TestCase(640, 480, 0, 0, 640, 480)]
        [TestCase(1920, 1080, 240, 0, 1440, 1080)]
        [TestCase(480, 640, 0, 140, 480, 360)]
        [TestCase(320, 240, 0, 0, 320, 240)]
        public void AspectFitPreservesFourByThree(float w, float h, float x, float y, float vw, float vh)
        { Assert.That(GameScreen.Fit(w, h), Is.EqualTo(new Rect(x, y, vw, vh))); }
        [Test] public void PointerConversionRejectsLetterboxAndPreservesLogicalCoordinates()
        {
            var size = new Vector2(1920, 1080);
            Assert.That(GameScreen.TryScreenToGui(new Vector2(960, 540), size, out var center), Is.True);
            Assert.That(center, Is.EqualTo(new Vector2(320, 240)));
            Assert.That(GameScreen.TryScreenToGui(new Vector2(240, 1080), size, out var corner), Is.True);
            Assert.That(corner, Is.EqualTo(Vector2.zero));
            Assert.That(GameScreen.TryScreenToGui(new Vector2(239, 540), size, out _), Is.False);
            Assert.That(GameScreen.TryScreenToGui(new Vector2(1680, 540), size, out _), Is.False);
            Assert.That(GameScreen.TryScreenToGui(new Vector2(200, 20), new Vector2(480, 640), out _), Is.False);
            Assert.That(GameScreen.TryScreenToGui(Vector2.zero, Vector2.zero, out _), Is.False);
            var logical = new Vector2(122, 333);
            Assert.That(GameScreen.TryScreenToGui(GameScreen.GuiToScreen(logical), out var roundtrip), Is.True);
            Assert.That(Vector2.Distance(logical, roundtrip), Is.LessThan(.001f));
        }
    }
}
