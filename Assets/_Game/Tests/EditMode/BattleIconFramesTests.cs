using DarkSaver.UI;
using NUnit.Framework;
using UnityEngine;
using DarkSaver.EditorTools.CanvasTesting;

namespace DarkSaver.Tests
{
    public sealed class BattleIconFramesTests
    {
        [Test]
        public void MercenariesExposeOnlyMovementAttackAndRest()
        {
            for(int i=0;i<8;i++)
            {
                Assert.That(NativeBattleActionMenu.AllowsIndex(0,i),Is.True);
                Assert.That(NativeBattleActionMenu.AllowsIndex(1,i),Is.EqualTo(i==0 || i==1 || i==3));
                Assert.That(NativeBattleActionMenu.AllowsIndex(2,i),Is.EqualTo(i==0 || i==1 || i==3));
            }
            Assert.That(NativeBattleActionMenu.AllowsIndex(-1,0),Is.False);
        }

        [Test]
        public void NativeCommanderMenuUsesEightAdjacentCellsAndLeavesCharacterCellEmpty()
        {
            var bounds = new Rect(100, 200, NativeBattleActionMenu.Size, NativeBattleActionMenu.Size);
            var expected = new[] { new Vector2(100, 200), new Vector2(164, 200), new Vector2(164, 232),
                new Vector2(100, 264), new Vector2(100, 232), new Vector2(164, 264),
                new Vector2(132, 264), new Vector2(132, 200) };
            var columns = new[] { 0, 1, 12, 8, 2, 5, 4, 3 };
            var character = new Rect(132, 232, 32, 32);
            for (int i = 0; i < NativeBattleActionMenu.Count; i++)
            {
                var rect = NativeBattleActionMenu.ButtonRect(bounds, i);
                Assert.That(rect.position, Is.EqualTo(expected[i]));
                Assert.That(rect.size, Is.EqualTo(new Vector2(32, 32)));
                Assert.That(rect.Overlaps(character), Is.False);
                Assert.That(NativeBattleActionMenu.IconColumn(i), Is.EqualTo(columns[i]));
                for (int j = 0; j < i; j++) Assert.That(rect.Overlaps(NativeBattleActionMenu.ButtonRect(bounds, j)), Is.False);
            }
            Assert.That(GameConstants.BattleRestIconColumn, Is.EqualTo(8));
        }

        [Test]
        public void FrameClockAlternatesTwoGroupsAndWraps()
        {
            var step = GameConstants.BattleIconFrameSeconds;
            Assert.That(step, Is.EqualTo(.3));
            Assert.That(BattleIconFrames.FrameAt(0), Is.EqualTo(1));
            Assert.That(BattleIconFrames.FrameAt(step * .99), Is.EqualTo(1));
            Assert.That(BattleIconFrames.FrameAt(step), Is.Zero);
            Assert.That(BattleIconFrames.FrameAt(step * 2), Is.EqualTo(1));
            Assert.That(BattleIconFrames.FrameAt(double.NaN), Is.Zero);
            Assert.That(BattleIconFrames.Action(-1, 0), Is.Null);
            Assert.That(BattleIconFrames.FromMagicCoordinates(0, 0, 0), Is.Null);
            Assert.That(BattleIconFrames.FromMagicCoordinates(1, 32, 0), Is.Null);
        }
        [Test]
        public void NativeFramesAndButtonReusePreserveInput()
        {
            var first = BattleIconFrames.Action(0, 0);
            var second = BattleIconFrames.Action(0, GameConstants.BattleIconFrameSeconds);
            Assert.That(first, Is.Not.Null); Assert.That(second, Is.Not.Null);
            Assert.That(first.name, Is.EqualTo("Action_00_Frame_1"));
            Assert.That(second.name, Is.EqualTo("Action_00_Frame_0"));
            Assert.That(first.rect.size, Is.EqualTo(new Vector2(32, 32)));
            Assert.That(Mathf.Abs(first.rect.y - second.rect.y), Is.EqualTo(160));
            Assert.That(BattleIconFrames.Magic(0, 0, 0).name, Is.EqualTo("Magic_0_00_Frame_1"));
            Assert.That(BattleIconFrames.FromMagicCoordinates(0, 192, 0), Is.SameAs(BattleIconFrames.Magic(0, 0, 0)));
            var go = new GameObject("Animated icon test");
            try
            {
                var owner = go.AddComponent<StartMenuController>(); InGameUi ui = null;
                InGameUi.Ensure(ref ui, owner);
                var root = CanvasProbe.For(ui);
                var rect = new Rect(10, 10, 48, 48);
                ui.Begin(); ui.ActionButton("move", rect, "이동", () => {}, true, "이동", first); ui.End();
                var button = root.Q<Button>("move");
                var image = root.Q<VisualElement>("move-icon");
                Assert.That(button.style.backgroundColor.value.a, Is.Zero);
                Assert.That(button.style.borderTopWidth.value, Is.Zero);
                Assert.That(image.pickingMode, Is.EqualTo(PickingMode.Ignore));
                Assert.That(image.style.backgroundImage.value.sprite, Is.SameAs(first));
                ui.Begin(); ui.ActionButton("move", rect, "이동", () => {}, true, "이동", second); ui.End();
                Assert.That(root.Q<Button>("move"), Is.SameAs(button));
                Assert.That(image.style.backgroundImage.value.sprite, Is.SameAs(second));
                ui.Begin(); ui.ActionButton("move", rect, "이동", () => {}, false, "AP 부족", second); ui.End();
                Assert.That(button.enabledSelf, Is.False);
                Assert.That(image.style.opacity.value, Is.EqualTo(.4f));
                Assert.That(image.pickingMode, Is.EqualTo(PickingMode.Ignore));
                ui.Begin(); ui.ActionButton("move", rect, "이동", () => {}, true, "이동"); ui.End();
                Assert.That(button.text, Is.EqualTo("이동"));
                Assert.That(button.enabledSelf, Is.True);
                Assert.That(button.style.borderTopWidth.value, Is.EqualTo(2));
                Assert.That(image.style.display.value, Is.EqualTo(DisplayStyle.None));
                ui.Begin(); ui.Label("unit-name", rect, "키스라", alignment: TextAnchor.MiddleCenter); ui.End();
                Assert.That(root.Q<Label>("unit-name").style.unityTextAlign.value, Is.EqualTo(TextAnchor.MiddleCenter));
                ui.Begin(); ui.Button("roster-row", rect, "긴 이름 AP 10", () => {}, singleLine: true, fontSize: 12); ui.End();
                var row = root.Q<Button>("roster-row");
                Assert.That(row.style.whiteSpace.value, Is.EqualTo(WhiteSpace.NoWrap));
                Assert.That(row.style.textOverflow.value, Is.EqualTo(TextOverflow.Clip));
                Assert.That(row.style.fontSize.value.value, Is.EqualTo(12));
                ui.Begin(); ui.Button("roster-row", rect, "일반 버튼", () => {}); ui.End();
                Assert.That(row.style.whiteSpace.value, Is.EqualTo(WhiteSpace.Normal));
                Assert.That(row.style.fontSize.value.value, Is.EqualTo(14));
                ui.Begin(); ui.BattleLabel("message", rect, "긴 전투 메시지", singleLine: true); ui.End();
                var message = root.Q<Label>("message");
                Assert.That(message.style.whiteSpace.value, Is.EqualTo(WhiteSpace.NoWrap));
                Assert.That(message.tooltip, Is.EqualTo("긴 전투 메시지"));
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
