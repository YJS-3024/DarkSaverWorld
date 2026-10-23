using DarkSaver.Combat;
using DarkSaver.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
namespace DarkSaver.Tests
{
    public sealed class BattleMagicMenuLayoutTests
    {
        [Test] public void NativeMagicIconsFollowRecoveredCrossAndCornerOrder()
        {
            Assert.That(BattleMapPresentation.GetMagicChoiceIconRect(-1), Is.EqualTo(default(Rect)));
            Assert.That(BattleMapPresentation.GetMagicChoiceIconRect(9), Is.EqualTo(default(Rect)));
            var expected = new[] { new Vector2(32, 0), new Vector2(32, 64), new Vector2(0, 32), new Vector2(64, 32),
                new Vector2(0, 0), new Vector2(0, 64), new Vector2(64, 0), new Vector2(64, 64), new Vector2(32, 32) };
            for (int i = 0; i < 9; i++)
            {
                var rect = BattleMapPresentation.GetMagicChoiceIconRect(i);
                Assert.That(rect.position, Is.EqualTo(expected[i]));
                Assert.That(rect.size, Is.EqualTo(new Vector2(32, 32)));
                for (int j = 0; j < i; j++) Assert.That(rect.Overlaps(BattleMapPresentation.GetMagicChoiceIconRect(j)), Is.False);
            }
        }
        [Test] public void OverflowFallbackPreservesEveryLearnedIndexWithoutInventingSpells()
        {
            Assert.That(NativeBattleMagicMenu.PageCount(0), Is.Zero);
            Assert.That(NativeBattleMagicMenu.PageCount(9), Is.EqualTo(1));
            Assert.That(NativeBattleMagicMenu.PageCount(12), Is.EqualTo(2));
            for (int i = 0; i < 80; i++)
                Assert.That(NativeBattleMagicMenu.DisplayIndex(80, i / 9, i % 9), Is.EqualTo(i));
            Assert.That(NativeBattleMagicMenu.DisplayIndex(12, 1, 3), Is.EqualTo(-1));
            Assert.That(NativeBattleMagicMenu.DisplayIndex(12, -1, 0), Is.EqualTo(-1));
            Assert.That(NativeBattleMagicMenu.DisplayIndex(12, int.MaxValue, 0), Is.EqualTo(-1));
        }
        [Test] public void AnimatedMagicIconRetainsInputAndRendersAboveTransparentModalBlocker()
        {
            var go = new GameObject("Magic icon UI test");
            try
            {
                var owner = go.AddComponent<StartMenuController>(); InGameUi ui = null; InGameUi.Ensure(ref ui, owner);
                var root = ui.GetComponent<UIDocument>().rootVisualElement;
                ui.Begin(); ui.ModalBlocker("modal-input");
                ui.ActionButton("spell", BattleMapPresentation.GetMagicChoiceIconRect(7), "?", () => {}, true, "파이어볼 / MP 10",
                    BattleIconFrames.Magic(0, 0, 0)); ui.End();
                var parent = root.Q<VisualElement>("LogicalScreen");
                var button = root.Q<Button>("spell"); var icon = root.Q<VisualElement>("spell-icon");
                Assert.That(button.parent, Is.SameAs(parent)); Assert.That(icon.parent, Is.SameAs(parent));
                Assert.That(icon.pickingMode, Is.EqualTo(PickingMode.Ignore));
                Assert.That(button.tooltip, Is.EqualTo("파이어볼 / MP 10"));
                ui.Begin(); ui.ModalBlocker("modal-input");
                var next = BattleIconFrames.Magic(0, 0, GameConstants.BattleIconFrameSeconds);
                ui.ActionButton("spell", BattleMapPresentation.GetMagicChoiceIconRect(7), "?", () => {}, false, "시전 정보 미연결", next); ui.End();
                Assert.That(root.Q<Button>("spell"), Is.SameAs(button)); Assert.That(button.enabledSelf, Is.False);
                Assert.That(icon.style.backgroundImage.value.sprite, Is.SameAs(next)); Assert.That(icon.style.opacity.value, Is.EqualTo(.4f));
                ui.Begin(); ui.ActionButton("behind", new Rect(0, 0, 44, 44), "?", () => {}, true, "뒤쪽 명령", next);
                ui.ModalBlocker("modal-input");
                ui.ActionButton("spell", BattleMapPresentation.GetMagicChoiceIconRect(0), "?", () => {}, true, "마법", next);
                ui.Raise("modal-input"); ui.Raise("spell"); ui.Raise("spell-icon"); ui.End();
                var canvas = root.Q<VisualElement>("LogicalScreen"); var blocker = root.Q<VisualElement>("modal-input");
                Assert.That(blocker.pickingMode, Is.EqualTo(PickingMode.Position));
                Assert.That(canvas.IndexOf(blocker), Is.GreaterThan(canvas.IndexOf(root.Q<VisualElement>("behind-icon"))));
                Assert.That(canvas.IndexOf(root.Q<Button>("spell")), Is.GreaterThan(canvas.IndexOf(blocker)));
                Assert.That(canvas.IndexOf(icon), Is.GreaterThan(canvas.IndexOf(blocker)));
                ui.Begin(); ui.Button("behind", new Rect(0, 0, 44, 44), "이동", () => {}); ui.End();
                Assert.That(blocker.style.display.value, Is.EqualTo(DisplayStyle.None));
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
