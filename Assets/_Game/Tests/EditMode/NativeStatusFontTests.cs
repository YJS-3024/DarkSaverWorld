using DarkSaver.UI;
using NUnit.Framework;
using UnityEngine;
using DarkSaver.EditorTools.CanvasTesting;

namespace DarkSaver.Tests
{
    public sealed class NativeStatusFontTests
    {
        [Test] public void RecoveredFontResolvesKoreanAsciiAndNativeAdvances()
        {
            foreach (char c in "전투검증키스라ABC123?")
                Assert.That(NativeStatusFont.Glyph(c).name, Is.EqualTo("U_" + ((int)c).ToString("X4")));
            Assert.That(NativeStatusFont.Glyph('전').rect.size, Is.EqualTo(new Vector2(14,14)));
            Assert.That(NativeStatusFont.Advance('전'), Is.EqualTo(14));
            Assert.That(NativeStatusFont.Advance('A'), Is.EqualTo(7));
            Assert.That(NativeStatusFont.Advance(' '), Is.EqualTo(5));
            Assert.That(NativeStatusFont.Glyph('\u2603'), Is.SameAs(NativeStatusFont.Glyph('?')));
        }
        [Test] public void NativeNameUsesBitmapGlyphsAndClipsWithoutCapturingPointer()
        {
            var go = new GameObject("Native font test");
            try
            {
                var owner = go.AddComponent<StartMenuController>(); InGameUi ui = null; InGameUi.Ensure(ref ui, owner);
                ui.Begin(); ui.NativeStatusName("name", new Rect(9,7,68,14), "전투A B"); ui.End();
                var root = CanvasProbe.For(ui);
                var clip = root.Q<VisualElement>("name");
                Assert.That(clip.style.overflow.value, Is.EqualTo(Overflow.Hidden));
                Assert.That(clip.pickingMode, Is.EqualTo(PickingMode.Ignore));
                Assert.That(root.Q<VisualElement>("name-glyph-1").style.left.value.value, Is.EqualTo(14));
                Assert.That(root.Q<VisualElement>("name-glyph-4").style.left.value.value, Is.EqualTo(40));
                Assert.That(root.Q<VisualElement>("name-glyph-0").parent, Is.SameAs(clip));
                ui.Begin(); ui.NativeStatusName("name", new Rect(9,7,68,14), "A"); ui.End();
                Assert.That(root.Q<VisualElement>("name-glyph-0").style.backgroundImage.value.sprite, Is.SameAs(NativeStatusFont.Glyph('A')));
                Assert.That(root.Q<VisualElement>("name-glyph-4").style.display.value, Is.EqualTo(DisplayStyle.None));
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
