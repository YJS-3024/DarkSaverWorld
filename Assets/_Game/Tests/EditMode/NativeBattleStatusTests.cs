using DarkSaver.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace DarkSaver.Tests
{
    public sealed class NativeBattleStatusTests
    {
        [Test] public void RosterWindowTransitionsReleaseBoundsWithoutChangingCombat()
        {
            var go = new GameObject("Roster window transitions");
            try
            {
                var battle = go.AddComponent<DarkSaver.Combat.BattleSceneRoot>();
                Assert.That(battle.TryGetUnit(100, out var before), Is.True);
                var selection = battle.SelectedUnitId;
                var expanded = battle.AlliedRosterRect;
                Assert.That(battle.IsAlliedRosterVisible, Is.True);
                battle.MinimizeAlliedRoster();
                Assert.That(battle.IsAlliedRosterMinimized, Is.True);
                Assert.That(battle.AlliedRosterRect.size, Is.EqualTo(new Vector2(33,22)));
                Assert.That(battle.AlliedRosterRect.Contains(new Vector2(100,60)), Is.False);
                battle.OpenAlliedRoster();
                Assert.That(battle.IsAlliedRosterMinimized, Is.False);
                Assert.That(battle.AlliedRosterRect, Is.EqualTo(expanded));
                battle.CloseAlliedRoster();
                Assert.That(battle.IsAlliedRosterVisible, Is.False);
                Assert.That(battle.AlliedRosterRect, Is.EqualTo(default(Rect)));
                battle.MinimizeAlliedRoster();
                Assert.That(battle.IsAlliedRosterMinimized, Is.False);
                battle.OpenAlliedRoster();
                Assert.That(battle.AlliedRosterRect, Is.EqualTo(expanded));
                Assert.That(battle.TryGetUnit(100, out var after), Is.True);
                Assert.That(after.Ap, Is.EqualTo(before.Ap));
                Assert.That(after.Hp, Is.EqualTo(before.Hp));
                Assert.That(battle.SelectedUnitId, Is.EqualTo(selection));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test] public void RosterPanelsKeepNativeDimensionsAndFortyEightPixelGaugeCrop()
        {
            var collapsed = Resources.Load<Texture2D>("UI/NativeRosterCollapsed");
            Assert.That(collapsed, Is.Not.Null);
            Assert.That(collapsed.width, Is.EqualTo(33)); Assert.That(collapsed.height, Is.EqualTo(22));
            for (int state = 0; state < 3; state++)
                Assert.That(NativeBattleStatus.CloseButton(state, true).rect.size, Is.EqualTo(new Vector2(15,15)));
            for (int rows = 1; rows <= 4; rows++)
            {
                var texture = Resources.Load<Texture2D>("UI/NativeRoster" + rows);
                Assert.That(texture, Is.Not.Null);
                Assert.That(texture.width, Is.EqualTo(120));
                Assert.That(texture.height, Is.EqualTo(44 + 41 * rows));
                Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point));
            }
            Assert.That(NativeBattleStatus.FillWidth(1, 3, 48), Is.EqualTo(16));
            Assert.That(NativeBattleStatus.FillWidth(int.MaxValue, int.MaxValue, 48), Is.EqualTo(48));
            var go = new GameObject("Native roster test");
            try
            {
                var owner = go.AddComponent<StartMenuController>(); InGameUi ui = null; InGameUi.Ensure(ref ui, owner);
                ui.Begin();
                ui.NativeStatusPanel("roster", new Rect(12,12,120,85), 1);
                ui.NativeRosterSelection("select", new Rect(17,46,110,38), () => {}, true, "unit");
                ui.NativeStatusGauge("hp", new Vector2(77,60), 1, 3, 1, true, 48);
                ui.End();
                var root = ui.GetComponent<UIDocument>().rootVisualElement;
                Assert.That(root.Q<VisualElement>("roster").style.backgroundImage.value.texture,
                    Is.SameAs(Resources.Load<Texture2D>("UI/NativeRoster1")));
                Assert.That(root.Q<Button>("select").style.backgroundColor.value.a, Is.Zero);
                Assert.That(root.Q<VisualElement>("hp-clip").style.width.value.value, Is.EqualTo(16));
                Assert.That(root.Q<VisualElement>("hp-fill").style.width.value.value, Is.EqualTo(80));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test] public void CloseButtonUsesNativeFifteenPixelStatesAndPreservesRetainedButton()
        {
            for (int state = 0; state < 3; state++)
            {
                var sprite = NativeBattleStatus.CloseButton(state);
                Assert.That(sprite, Is.Not.Null);
                Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(15,15)));
            }
            var go = new GameObject("Native close test");
            try
            {
                var owner = go.AddComponent<StartMenuController>(); InGameUi ui = null; InGameUi.Ensure(ref ui, owner);
                var root = ui.GetComponent<UIDocument>().rootVisualElement;
                var rect = new Rect(108,253,15,15);
                ui.Begin(); ui.NativeStatusCloseButton("close", rect, () => {}); ui.End();
                var button = root.Q<Button>("close");
                Assert.That(button.text, Is.Empty);
                Assert.That(button.style.borderTopWidth.value, Is.Zero);
                Assert.That(button.style.backgroundImage.value.sprite, Is.SameAs(NativeBattleStatus.CloseButton(0)));
                // Detached EditMode UIDocuments cannot dispatch real pointer events.
                Assert.That(NativeBattleStatus.CloseButtonState(false, false), Is.Zero);
                Assert.That(NativeBattleStatus.CloseButtonState(true, false), Is.EqualTo(1));
                Assert.That(NativeBattleStatus.CloseButtonState(true, true), Is.EqualTo(2));
                Assert.That(NativeBattleStatus.CloseButtonState(false, true), Is.Zero);
                ui.Begin(); ui.NativeStatusCloseButton("close", rect, () => {}); ui.End();
                Assert.That(root.Q<Button>("close"), Is.SameAs(button));
                Assert.That(button.style.backgroundImage.value.sprite, Is.SameAs(NativeBattleStatus.CloseButton(0)));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test] public void NativePanelUsesBakedScenePixelsWithoutInventedCssBorder()
        {
            var texture = Resources.Load<Texture2D>("UI/NativeStatusPanel");
            Assert.That(texture, Is.Not.Null);
            Assert.That(texture.width, Is.EqualTo(120)); Assert.That(texture.height, Is.EqualTo(213));
            Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point));
            var go = new GameObject("Native panel test");
            try
            {
                var owner = go.AddComponent<StartMenuController>(); InGameUi ui = null; InGameUi.Ensure(ref ui, owner);
                ui.Begin(); ui.NativeStatusPanel("inspect", new Rect(12,247,120,213));
                ui.Button("close", new Rect(108,253,18,18), "×", () => {}, fontSize: 10);
                ui.NativeStatusNumber("attack", new Vector2(44,444), "65", "공격 기본55 / 임시+10"); ui.End();
                var root = ui.GetComponent<UIDocument>().rootVisualElement;
                var panel = root.Q<VisualElement>("inspect");
                Assert.That(panel.style.backgroundImage.value.texture, Is.SameAs(texture));
                Assert.That(panel.style.borderTopWidth.value, Is.Zero);
                Assert.That(panel.style.backgroundColor.value.a, Is.Zero);
                Assert.That(root.Q<Button>("close").style.paddingRight.value.value, Is.Zero);
                Assert.That(root.Q<Button>("close").style.fontSize.value.value, Is.EqualTo(10));
                Assert.That(root.Q<VisualElement>("attack-digit-0").style.backgroundImage.value.sprite, Is.SameAs(NativeBattleStatus.Digit('6')));
                var tooltip = root.Q<VisualElement>("attack-tooltip");
                Assert.AreEqual("공격 기본55 / 임시+10", tooltip.tooltip);
                Assert.AreEqual(PickingMode.Position, tooltip.pickingMode);
                Assert.AreEqual(PickingMode.Ignore, root.Q<VisualElement>("attack-digit-0").pickingMode);
                Assert.IsFalse(ui.TryShowNativeStatusDetails("missing"));
                using (var enter = PointerEnterEvent.GetPooled()) { enter.target = tooltip; tooltip.SendEvent(enter); }
                ui.Begin(); ui.NativeStatusNumber("attack", new Vector2(44,444), "55", "공격 기본55"); ui.End();
                Assert.AreEqual("공격 기본55", tooltip.tooltip);
                Assert.AreEqual("공격 기본55", root.Q<Label>("native-stat-detail-text").text);
                Assert.AreEqual(PickingMode.Ignore, root.Q<VisualElement>("native-stat-detail").pickingMode);
                ui.Begin(); ui.NativeStatusNumber("attack", new Vector2(44,444), "55"); ui.End();
                Assert.AreEqual(DisplayStyle.None, tooltip.style.display.value);
                Assert.AreEqual(DisplayStyle.None, root.Q<VisualElement>("native-stat-detail").style.display.value);
                Assert.IsFalse(ui.TryShowNativeStatusDetails("attack"));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test] public void FillUsesNativeIntegerEightyPixelRatioWithoutOverflow()
        {
            Assert.That(NativeBattleStatus.FillWidth(100, 100), Is.EqualTo(80));
            Assert.That(NativeBattleStatus.FillWidth(50, 100), Is.EqualTo(40));
            Assert.That(NativeBattleStatus.FillWidth(1, 3), Is.EqualTo(26));
            Assert.That(NativeBattleStatus.FillWidth(200, 100), Is.EqualTo(80));
            Assert.That(NativeBattleStatus.FillWidth(-1, 100), Is.Zero);
            Assert.That(NativeBattleStatus.FillWidth(10, 0), Is.Zero);
            Assert.That(NativeBattleStatus.FillWidth(int.MaxValue, int.MaxValue), Is.EqualTo(80));
        }
        [Test] public void NativeSpritesAndUiClipPreserveStripPixelsAndBitmapDigits()
        {
            for (int i = 0; i < 3; i++) Assert.That(NativeBattleStatus.Gauge(i).rect.size, Is.EqualTo(new Vector2(80, 9)));
            foreach (var c in "0123456789/") Assert.That(NativeBattleStatus.Digit(c).rect.size, Is.EqualTo(new Vector2(5, 7)));
            Assert.That(NativeBattleStatus.Digit('?'), Is.Null);
            var go = new GameObject("Native gauge test");
            try
            {
                var owner = go.AddComponent<StartMenuController>(); InGameUi ui = null; InGameUi.Ensure(ref ui, owner);
                var root = ui.GetComponent<UIDocument>().rootVisualElement;
                ui.Begin(); ui.NativeStatusGauge("hp", new Vector2(30,156), 50, 100, 1); ui.End();
                var clip = root.Q<VisualElement>("hp-clip"); var fill = root.Q<VisualElement>("hp-fill");
                Assert.That(clip.style.width.value.value, Is.EqualTo(40));
                Assert.That(clip.style.overflow.value, Is.EqualTo(Overflow.Hidden));
                Assert.That(fill.style.width.value.value, Is.EqualTo(80));
                Assert.That(fill.parent, Is.SameAs(clip));
                Assert.That(fill.pickingMode, Is.EqualTo(PickingMode.Ignore));
                Assert.That(root.Q<VisualElement>("hp-digit-2").style.backgroundImage.value.sprite, Is.SameAs(NativeBattleStatus.Digit('/')));
                ui.Begin(); ui.NativeStatusGauge("hp", new Vector2(30,156), 0, 100, 1); ui.End();
                Assert.That(clip.style.display.value, Is.EqualTo(DisplayStyle.None));
                ui.Begin(); ui.NativeStatusGauge("hp", new Vector2(30,156), 100, 100, 1); ui.End();
                Assert.That(root.Q<VisualElement>("hp-clip"), Is.SameAs(clip));
                Assert.That(clip.style.width.value.value, Is.EqualTo(80));
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
