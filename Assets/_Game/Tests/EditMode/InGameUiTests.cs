using DarkSaver.UI;
using NUnit.Framework;
using UnityEngine;
using DarkSaver.EditorTools.CanvasTesting;

namespace DarkSaver.Tests
{
    public sealed class InGameUiTests
    {
        [Test]
        public void CharacterPortraitsUseRecoveredJobImagesAndOriginalNoFaceFallback()
        {
            var catalog=Resources.Load<CharacterPortraitCatalog>("UI/CharacterPortraits/CharacterPortraits");
            Assert.That(catalog, Is.Not.Null); Assert.That(catalog.Count, Is.EqualTo(70));
            var unique=new System.Collections.Generic.HashSet<Texture2D>();
            for(int job=100;job<=172;job++) {
                Assert.That(catalog.TryGet(job,out var image), Is.True);
                Assert.That(image.width, Is.EqualTo(64)); Assert.That(image.height, Is.EqualTo(64));
                Assert.That(image.filterMode, Is.EqualTo(FilterMode.Point)); Assert.That(image.mipmapCount, Is.EqualTo(1));
                if(job<=169) Assert.That(image, Is.Not.SameAs(catalog.NoFace)); else Assert.That(image, Is.SameAs(catalog.NoFace));
                unique.Add(image);
            }
            Assert.That(unique.Count, Is.EqualTo(71));
            Assert.That(catalog.TryGet(99,out _), Is.False); Assert.That(catalog.TryGet(173,out _), Is.False);
            foreach(var dependency in UnityEditor.AssetDatabase.GetDependencies(UnityEditor.AssetDatabase.GetAssetPath(catalog)))
                Assert.That(dependency.StartsWith("Assets/"), Is.True, dependency);
        }
        [Test]
        public void CharacterAdjustmentPreservesOriginalShellAndButtons()
        {
            var catalog=Resources.Load<NativeBattleWindows>("UI/TownAbilityAdjustment/NativeTownAbilityAdjustment");
            Assert.That(catalog,Is.Not.Null); var window=catalog.Find(23);
            Assert.That(window.size,Is.EqualTo(new Vector2Int(540,435)));
            Assert.That(window.background.width,Is.EqualTo(540)); Assert.That(window.background.height,Is.EqualTo(435));
            Assert.That(window.controls.Length,Is.EqualTo(21));
            int[] ids={37,38,77,79,78,78,78,78,78,78,80,80,80,80,80,80,56,57,81,19,54};
            for(int i=0;i<21;i++) {
                var control=window.controls[i]; Assert.That(control.nativeId,Is.EqualTo(ids[i]));
                Assert.That(control.frames.Length,Is.EqualTo(3));
                foreach(var frame in control.frames) { Assert.That(frame,Is.Not.Null); Assert.That(frame.mipmapCount,Is.EqualTo(1)); Assert.That(frame.filterMode,Is.EqualTo(FilterMode.Point)); }
            }
            foreach(var dependency in UnityEditor.AssetDatabase.GetDependencies(UnityEditor.AssetDatabase.GetAssetPath(catalog))) Assert.That(dependency.StartsWith("Assets/"),Is.True,dependency);
        }
        [TestCase("UI/TownCharacterInfo/NativeTownCharacterInfo",335,274,3)]
        [TestCase("UI/TownCharacterInfoMinimized/NativeTownCharacterInfoMinimized",34,22,0)]
        public void CharacterInfoWindowAssetsPreserveOriginalGeometry(string path, int width, int height, int controlCount)
        {
            var catalog = Resources.Load<NativeBattleWindows>(path); Assert.That(catalog, Is.Not.Null);
            var window = catalog.Find(6);
            Assert.That(window.size, Is.EqualTo(new Vector2Int(width,height)));
            Assert.That(window.background.width, Is.EqualTo(width)); Assert.That(window.background.height, Is.EqualTo(height));
            Assert.That(window.controls.Length, Is.EqualTo(controlCount));
            if(controlCount>0) {
                int[] ids = {61,62,82}; Vector2[] positions = {new(286,16),new(303,16),new(29,230)};
                for(int i=0;i<3;i++) {
                    var control = window.controls[i];
                    Assert.That(control.nativeId, Is.EqualTo(ids[i])); Assert.That(control.rect.position, Is.EqualTo(positions[i]));
                    Assert.That(control.frames.Length, Is.EqualTo(3));
                    foreach(var frame in control.frames) {
                        Assert.That(frame, Is.Not.Null); Assert.That(frame.filterMode, Is.EqualTo(FilterMode.Point));
                        Assert.That(frame.mipmapCount, Is.EqualTo(1));
                    }
                }
            }
            foreach(var dependency in UnityEditor.AssetDatabase.GetDependencies(UnityEditor.AssetDatabase.GetAssetPath(catalog)))
                Assert.That(dependency.StartsWith("Assets/"), Is.True, dependency);
        }
        [Test]
        public void ItemDragNeedsMovementAndOppositeRegionAndCancelsOnRebindOrHide()
        {
            var go = new GameObject("Item drag fixture");
            try {
                var owner = go.AddComponent<StartMenuController>(); InGameUi ui = null; InGameUi.Ensure(ref ui, owner);
                int drops = 0;
                void Draw(int id = 1) {
                    ui.Begin(); ui.ItemSlotButton("slot", new Rect(0,0,32,32), () => {}, null, 1, false, "");
                    ui.ItemDragSource("slot", id, null, new Rect(100,0,50,50), value => drops += value); ui.End();
                }
                Draw(); var root = CanvasProbe.For(ui);
                var button = root.Q<Button>("slot"); var parent = button.parent;
                void Down(Vector2 p) { using var e = PointerDownEvent.GetPooled(new Event {type=EventType.MouseDown,button=0,mousePosition=parent.LocalToWorld(p)}); e.target=button; button.SendEvent(e); }
                void Move(Vector2 p) { using var e = PointerMoveEvent.GetPooled(new Event {type=EventType.MouseDrag,button=0,mousePosition=parent.LocalToWorld(p)}); e.target=button; button.SendEvent(e); }
                void Up(Vector2 p) { using var e = PointerUpEvent.GetPooled(new Event {type=EventType.MouseUp,button=0,mousePosition=parent.LocalToWorld(p)}); e.target=button; button.SendEvent(e); }
                Down(new Vector2(16,16)); Up(new Vector2(16,16)); Assert.That(drops, Is.Zero);
                Down(new Vector2(16,16)); Move(new Vector2(125,16)); Up(new Vector2(125,16)); Assert.That(drops, Is.EqualTo(1));
                Up(new Vector2(125,16)); Assert.That(drops, Is.EqualTo(1));
                Down(new Vector2(16,16)); Move(new Vector2(60,60)); Up(new Vector2(60,60)); Assert.That(drops, Is.EqualTo(1));
                Down(new Vector2(16,16)); Move(new Vector2(125,16)); Draw(2); Up(new Vector2(125,16)); Assert.That(drops, Is.EqualTo(1));
                Down(new Vector2(16,16)); Move(new Vector2(125,16)); ui.Hide(); Draw(2); Up(new Vector2(125,16)); Assert.That(drops, Is.EqualTo(1));
                Down(new Vector2(16,16)); Move(new Vector2(125,16)); button.SetEnabled(false); Up(new Vector2(125,16)); Assert.That(drops, Is.EqualTo(1));
                button.SetEnabled(true); Draw(2);
                const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                Down(new Vector2(16,16)); Move(new Vector2(125,16)); typeof(InGameUi).GetMethod("OnApplicationFocus", flags).Invoke(ui, new object[] {false}); Up(new Vector2(125,16)); Assert.That(drops, Is.EqualTo(1));
                Down(new Vector2(16,16)); Move(new Vector2(125,16)); typeof(InGameUi).GetMethod("OnApplicationPause", flags).Invoke(ui, new object[] {true}); Up(new Vector2(125,16)); Assert.That(drops, Is.EqualTo(1));
                Down(new Vector2(16,16)); Move(new Vector2(125,16)); Up(new Vector2(125,16)); Assert.That(drops, Is.EqualTo(3));
            } finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void NativeDescriptionScrollReusesOnlyVisibleRowsAndResetsForNewText()
        {
            var go = new GameObject("Native description fixture");
            try {
                var owner = go.AddComponent<StartMenuController>(); InGameUi ui = null; InGameUi.Ensure(ref ui, owner);
                var window = Resources.Load<NativeBattleWindows>("UI/TownShop/NativeTownShop").Find(10);
                var lines = new string[100]; for (int i = 0; i < lines.Length; i++) lines[i] = "가나다라마바사아자차카타파하";
                string text = string.Join("\n", lines); var rect = new Rect(0,0,219,60);
                void Draw(string value, bool reset = false) {
                    ui.Begin(); ui.NativeBitmapScrollText("detail", rect, value, window.controls[0], window.controls[1], reset); ui.End();
                }
                Draw(text);
                var root = CanvasProbe.For(ui);
                var scroll = root.Q<ScrollView>("detail-scroll");
                Assert.That(scroll.verticalScrollerVisibility, Is.EqualTo(ScrollerVisibility.Hidden));
                Assert.That(scroll.contentContainer.style.height.value.value, Is.EqualTo(1600));
                Assert.That(scroll.contentContainer.childCount, Is.LessThanOrEqualTo(70));
                Assert.That(root.Q<Button>("detail-up").enabledSelf, Is.False);
                var down = root.Q<Button>("detail-down");
                using (var e = NavigationSubmitEvent.GetPooled()) { e.target = down; down.SendEvent(e); }
                Assert.That(scroll.scrollOffset.y, Is.EqualTo(16));
                Draw(text);
                var up = root.Q<Button>("detail-up"); Assert.That(up.enabledSelf, Is.True);
                using (var e = NavigationSubmitEvent.GetPooled()) { e.target = up; up.SendEvent(e); }
                Assert.That(scroll.scrollOffset.y, Is.Zero);
                for (int i = 0; i < 20; i++) { scroll.scrollOffset = new Vector2(0, i * 64); Draw(text); }
                Assert.That(scroll.contentContainer.childCount, Is.LessThanOrEqualTo(70));
                Draw("가");
                Assert.That(scroll.scrollOffset.y, Is.Zero);
                Assert.That(root.Q<Button>("detail-down").enabledSelf, Is.False);
                Assert.That(root.Q<VisualElement>("detail-row-1-glyph-0").style.display.value, Is.EqualTo(DisplayStyle.None));
                Assert.That(scroll.tooltip, Is.EqualTo("가"));
            } finally { Object.DestroyImmediate(go); }
        }

        [TestCase("가나A", 28, "가나|A")]
        [TestCase("ABC", 14, "AB|C")]
        [TestCase("A 가", 26, "A 가")]
        [TestCase("가\r\n나", 28, "가|나")]
        [TestCase("가\n", 28, "가|")]
        [TestCase(null, 28, "")]
        public void NativeGlyphWrappingUsesOriginalAdvances(string text, int width, string expected)
        {
            Assert.That(string.Join("|", NativeStatusFont.WrapLines(text, width)), Is.EqualTo(expected));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => NativeStatusFont.WrapLines(text, 13));
        }

        [Test]
        public void NativeBitmapTextClipsReusesGlyphsAndKeepsFullTooltip()
        {
            var go = new GameObject("Native town text fixture");
            try {
                var owner = go.AddComponent<StartMenuController>(); InGameUi ui = null; InGameUi.Ensure(ref ui, owner);
                var root = CanvasProbe.For(ui);
                ui.Begin(); ui.NativeBitmapText("paper", new Rect(0,0,28,32), "가나A", Color.black, 2); ui.End();
                var clip = root.Q<VisualElement>("paper");
                Assert.That(clip.pickingMode, Is.EqualTo(PickingMode.Ignore));
                Assert.That(clip.tooltip, Is.EqualTo("가나A"));
                Assert.That(root.Q<VisualElement>("paper-glyph-1-0").style.top.value.value, Is.EqualTo(17));
                var first = root.Q<VisualElement>("paper-glyph-0-0");
                ui.Begin(); ui.NativeBitmapText("paper", new Rect(0,0,28,32), "A", Color.yellow, 1, TextAnchor.MiddleRight); ui.End();
                Assert.That(root.Q<VisualElement>("paper-glyph-0-0"), Is.SameAs(first));
                Assert.That(first.style.left.value.value, Is.EqualTo(21));
                Assert.That(first.style.unityBackgroundImageTintColor.value, Is.EqualTo(Color.yellow));
                Assert.That(root.Q<VisualElement>("paper-glyph-1-0").style.display.value, Is.EqualTo(DisplayStyle.None));
                Assert.That(first.pickingMode, Is.EqualTo(PickingMode.Ignore));
            } finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void NativeTradeConfirmationPreservesOriginalSizeAndYesNoControls()
        {
            var catalog = Resources.Load<NativeBattleWindows>("UI/TownTradeConfirmation/NativeTradeConfirmation");
            Assert.That(catalog, Is.Not.Null);
            var window = catalog.Find(29);
            Assert.That(window.size, Is.EqualTo(new Vector2Int(528, 75)));
            Assert.That(window.background.width, Is.EqualTo(528)); Assert.That(window.background.height, Is.EqualTo(75));
            Assert.That(window.controls.Length, Is.EqualTo(2));
            Assert.That(window.controls[0].nativeId, Is.EqualTo(27));
            Assert.That(window.controls[0].rect.position, Is.EqualTo(new Vector2(170, 40)));
            Assert.That(window.controls[1].nativeId, Is.EqualTo(28));
            Assert.That(window.controls[1].rect.position, Is.EqualTo(new Vector2(302, 40)));
            foreach (var control in window.controls) {
                Assert.That(control.frames.Length, Is.EqualTo(3));
                foreach (var frame in control.frames) Assert.That(frame, Is.Not.Null);
            }
            foreach (string dependency in UnityEditor.AssetDatabase.GetDependencies(UnityEditor.AssetDatabase.GetAssetPath(catalog)))
                Assert.That(dependency.EndsWith(".lsc") || dependency.EndsWith(".bmp") || dependency.EndsWith(".bin"), Is.False);
        }

        [Test]
        public void TownShopUsesConvertedOriginalLayoutAndNotRawRuntimeFiles()
        {
            var catalog = Resources.Load<NativeBattleWindows>("UI/TownShop/NativeTownShop");
            Assert.That(catalog, Is.Not.Null);
            var window = catalog.Find(10);
            Assert.That(window.size, Is.EqualTo(new Vector2Int(419, 303)));
            Assert.That(window.background.width, Is.EqualTo(419)); Assert.That(window.background.height, Is.EqualTo(303));
            Assert.That(window.background.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(window.controls.Length, Is.EqualTo(3));
            Assert.That(window.controls[0].nativeId, Is.EqualTo(40));
            Assert.That(window.controls[0].rect.position, Is.EqualTo(new Vector2(384, 35)));
            Assert.That(window.controls[1].nativeId, Is.EqualTo(39));
            Assert.That(window.controls[1].rect.position, Is.EqualTo(new Vector2(384, 161)));
            Assert.That(window.controls[2].nativeId, Is.EqualTo(19));
            Assert.That(window.controls[2].rect.position, Is.EqualTo(new Vector2(345, 264)));
            foreach (string dependency in UnityEditor.AssetDatabase.GetDependencies(UnityEditor.AssetDatabase.GetAssetPath(catalog))) {
                StringAssert.StartsWith("Assets/_Game/", dependency);
                Assert.That(dependency.EndsWith(".lsc") || dependency.EndsWith(".bin") || dependency.EndsWith(".bmp"), Is.False);
            }
            foreach (var control in window.controls) foreach (var frame in control.frames) {
                Assert.That(frame, Is.Not.Null); Assert.That(frame.width, Is.EqualTo(control.rect.width));
                Assert.That(frame.height, Is.EqualTo(control.rect.height)); Assert.That(frame.filterMode, Is.EqualTo(FilterMode.Point));
            }
        }

        [Test]
        public void SharedNativeButtonsKeepPixelSizeStatesAndReboundActions()
        {
            var go = new GameObject("Town native button fixture");
            try {
                var owner = go.AddComponent<StartMenuController>(); InGameUi ui = null;
                InGameUi.Ensure(ref ui, owner);
                var catalog = Resources.Load<NativeBattleWindows>("UI/BattleWindows/NativeBattleWindows");
                var close = catalog.Find(0).controls[1]; Assert.That(close.nativeId, Is.EqualTo(38));
                int calls = 0;
                ui.Begin(); ui.NativeLibraryButton("close", Vector2.one, 38, () => calls++, true, "닫기"); ui.End();
                var button = CanvasProbe.For(ui).Q<Button>("close");
                Assert.That(button.style.width.value.value, Is.EqualTo(close.rect.width));
                Assert.That(button.style.height.value.value, Is.EqualTo(close.rect.height));
                Assert.That(button.style.backgroundImage.value.texture, Is.SameAs(close.frames[0]));
                using (var e = PointerEnterEvent.GetPooled()) { e.target = button; button.SendEvent(e); }
                Assert.That(button.style.backgroundImage.value.texture, Is.SameAs(close.frames[1]));
                using (var e = PointerDownEvent.GetPooled(new Event {type = EventType.MouseDown, button = 0})) { e.target = button; button.SendEvent(e); }
                Assert.That(button.style.backgroundImage.value.texture, Is.SameAs(close.frames[2]));
                using (var e = PointerLeaveEvent.GetPooled()) { e.target = button; button.SendEvent(e); }
                Assert.That(button.style.backgroundImage.value.texture, Is.SameAs(close.frames[0]));
                ui.Begin(); ui.NativeLibraryButton("close", Vector2.one, 38, () => calls += 10, false, "닫기"); ui.End();
                Assert.That(button.enabledSelf, Is.False);
                Assert.That(button.style.backgroundImage.value.texture, Is.SameAs(close.frames[0]));
                ui.Hide(); ui.Begin(); ui.NativeLibraryButton("close", Vector2.one, 38, () => calls += 10, true, "닫기"); ui.End();
                using (var e = NavigationSubmitEvent.GetPooled()) { e.target = button; button.SendEvent(e); }
                Assert.That(calls, Is.EqualTo(10));
                Assert.That(button.style.borderTopWidth.value, Is.Zero);
            } finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void WorldNamesUseMeasuredNativeGlyphsWithoutPointerBlocking()
        {
            Assert.AreEqual(40, NativeStatusFont.Measure("A 가나"));
            Assert.AreEqual(0, NativeStatusFont.Measure(null));
            Assert.NotNull(NativeStatusFont.WorldGlyph('가'));
            Assert.AreEqual(new Vector2(14,14), NativeStatusFont.WorldGlyph('가').rect.size);
            var go = new GameObject("World name UI test");
            try
            {
                var owner = go.AddComponent<StartMenuController>(); InGameUi ui = null;
                InGameUi.Ensure(ref ui,owner);
                ui.Begin(); ui.NativeWorldName("name",new Vector2(100,100),"가나"); ui.End();
                var root = CanvasProbe.For(ui);
                var name = root.Q<VisualElement>("name");
                Assert.AreEqual(28,name.style.width.value.value);
                Assert.AreEqual(86,name.style.left.value.value);
                Assert.AreEqual(86,name.style.top.value.value);
                Assert.AreEqual(PickingMode.Ignore,name.pickingMode);
                Assert.AreEqual(PickingMode.Ignore,root.Q<VisualElement>("name-glyph-0").pickingMode);
                Assert.IsNull(root.Q<Label>("name"));
                ui.Begin(); ui.NativeWorldName("name",new Vector2(100,100),"가"); ui.End();
                Assert.AreEqual(DisplayStyle.None,root.Q<VisualElement>("name-glyph-1").style.display.value);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void ReactivatedDocumentReattachesRetainedBattleControls()
        {
            var go = new GameObject("Reactivated battle UI test");
            try
            {
                var owner = go.AddComponent<StartMenuController>(); InGameUi ui = null;
                InGameUi.Ensure(ref ui, owner);
                var document = CanvasProbe.For(ui);
                var calls = 0;
                ui.Begin(); ui.Button("attack", new Rect(0,0,32,32), "공격", () => calls++); ui.End();
                var button = document.rootVisualElement.Q<Button>("attack");
                for (int i = 0; i < 2; i++)
                {
                    go.SetActive(false); go.SetActive(true);
                    ui.Begin(); ui.Button("attack", new Rect(0,0,32,32), "공격", () => calls++); ui.End();
                    Assert.AreSame(button, document.rootVisualElement.Q<Button>("attack"));
                    Assert.AreSame(document.rootVisualElement, button.parent.parent);
                    Assert.AreEqual(PickingMode.Ignore, document.rootVisualElement.pickingMode);
                    using var click = NavigationSubmitEvent.GetPooled();
                    click.target = button; button.SendEvent(click);
                    Assert.AreEqual(i + 1, calls);
                }
                ui.Hide();
                Assert.AreEqual(DisplayStyle.None, button.parent.style.display.value);
                ui.Begin(); ui.End();
                Assert.AreEqual(DisplayStyle.None, button.style.display.value);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void BattleAndWorldPanelsUseConvertedPaperResources()
        {
            Assert.NotNull(Resources.Load<Texture2D>("UI/BattlePaper"));
            Assert.NotNull(Resources.Load<Sprite>("UI/BattlePortraitFrame"));
            var go = new GameObject("Battle skin test");
            try
            {
                var owner = go.AddComponent<StartMenuController>(); InGameUi ui = null;
                InGameUi.Ensure(ref ui, owner);
                var root = CanvasProbe.For(ui);
                ui.Begin();
                ui.BattlePanel("battle-test", new Rect(0, 0, 220, 260), "전투");
                ui.BattlePortrait("portrait-test", new Rect(20, 40, 64, 64), null);
                ui.Panel("world-test", new Rect(240, 0, 220, 260), "마을");
                ui.End();
                Assert.NotNull(root.Q<VisualElement>("battle-test").style.backgroundImage.value.texture);
                Assert.AreSame(Resources.Load<Texture2D>("UI/BattlePaper"),root.Q<VisualElement>("world-test").style.backgroundImage.value.texture);
                Assert.NotNull(root.Q<VisualElement>("portrait-test-frame").style.backgroundImage.value.sprite);
                Assert.AreEqual(PickingMode.Ignore, root.Q<VisualElement>("portrait-test-frame").pickingMode);
                Assert.Less(root.Q<Label>("battle-test-title").style.color.value.r, .3f);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void RetainedControlsReuseElementsAndHideUnusedCommands()
        {
            var go = new GameObject("Native UI test");
            try
            {
                var owner = go.AddComponent<StartMenuController>();
                InGameUi ui = null;
                InGameUi.Ensure(ref ui, owner);
                var document = CanvasProbe.For(ui);
                Assert.NotNull(ui.GetComponent<UnityEngine.UI.GraphicRaycaster>());
                Assert.AreEqual(RenderMode.ScreenSpaceOverlay,ui.Canvas.renderMode);
                Assert.AreEqual(PickingMode.Ignore, document.rootVisualElement.pickingMode);
                ui.Begin();
                ui.Panel("panel", new Rect(12, 12, 200, 100), "전투");
                ui.Button("move", new Rect(20, 42, 180, 28), "이동", () => { }, false);
                ui.Label("feedback", new Rect(20, 72, 180, 28), "AP 회복 중");
                ui.End();
                var first = document.rootVisualElement.Q<Button>("move");
                Assert.IsFalse(first.enabledSelf);
                ui.Begin();
                ui.Button("move", new Rect(20, 42, 180, 28), "이동 AP4", () => { }, true, true);
                ui.End();
                Assert.AreSame(first, document.rootVisualElement.Q<Button>("move"));
                Assert.IsTrue(first.enabledSelf);
                Assert.AreEqual("이동 AP4", first.text);
                Assert.AreEqual(DisplayStyle.None, document.rootVisualElement.Q<Label>("feedback").style.display.value);
                ui.Hide();
                Assert.AreEqual(DisplayStyle.None, document.rootVisualElement.Q<VisualElement>("LogicalScreen").style.display.value);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void DungeonResultUsesNativeLayoutAndHidesAfterAcknowledgement()
        {
            var go = new GameObject("Dungeon result UI test");
            try
            {
                var owner = go.AddComponent<StartMenuController>();
                InGameUi ui = null;
                InGameUi.Ensure(ref ui, owner);
                var root = CanvasProbe.For(ui);
                var rect = new Rect(40, 80, 560, 112);
                ui.Begin();
                ui.DungeonResult("outcome", rect, "던전을 완료했습니다.\n획득 경험치 123", () => { });
                ui.End();
                var panel = root.Q<VisualElement>("outcome");
                Assert.AreEqual(434f, panel.style.width.value.value);
                Assert.AreEqual(344f, panel.style.height.value.value);
                Assert.AreEqual(0f, panel.style.borderLeftWidth.value);
                Assert.AreEqual(0f, panel.style.borderTopLeftRadius.value.value);
                Assert.IsNotNull(panel.style.backgroundImage.value.texture);
                Assert.AreEqual(TextAnchor.UpperLeft, root.Q<Label>("outcome-text").style.unityTextAlign.value);
                // A second retained-mode draw must keep every dynamic value above the raised background.
                for(int frame=0;frame<3;frame++)
                {
                    ui.Begin();ui.DungeonResult("outcome",rect,"아군",()=>{},"처치32","1151","승리");ui.End();
                    var background=ui.Find<UnityEngine.UI.RawImage>("outcome").transform;
                    foreach(var field in new[]{"outcome-allies-scroll","outcome-enemies-scroll","outcome-experience","outcome-status"})
                        Assert.Greater(ui.Find<UnityEngine.Component>(field).transform.GetSiblingIndex(),background.GetSiblingIndex(),field);
                    Assert.AreEqual("1151",ui.Find<UnityEngine.UI.Text>("outcome-experience").text);
                    Assert.AreEqual("승리",ui.Find<UnityEngine.UI.Text>("outcome-status").text);
                }
                Assert.AreEqual("확인", root.Q<Button>("outcome-confirm").tooltip);
                Assert.AreEqual(46f, root.Q<Button>("outcome-confirm").style.width.value.value);
                Assert.AreEqual(DisplayStyle.Flex, root.Q<VisualElement>("outcome-blocker").style.display.value);
                Assert.GreaterOrEqual(CanvasProbe.For(ui).sortingOrder, 100);
                Assert.GreaterOrEqual(CanvasProbe.For(ui).panelSettings.sortingOrder, 100);
                ui.Begin();
                ui.DungeonResult("outcome", rect, "전투에서 패배했습니다.", () => { });
                ui.End();
                Assert.AreSame(panel, root.Q<VisualElement>("outcome"));
                ui.Begin(); ui.End();
                Assert.AreEqual(DisplayStyle.None, panel.style.display.value);
                Assert.AreEqual(20, CanvasProbe.For(ui).sortingOrder);
                Assert.AreEqual(20, CanvasProbe.For(ui).panelSettings.sortingOrder);
                Assert.AreEqual(DisplayStyle.None, root.Q<Button>("outcome-confirm").style.display.value);
                Assert.AreEqual(DisplayStyle.None, root.Q<VisualElement>("outcome-blocker").style.display.value);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void ResultScrollKeepsCurrentPositionButResetsForChangedOrReopenedResults()
        {
            var go = new GameObject("Result scroll reuse test");
            try {
                var owner = go.AddComponent<StartMenuController>();
                InGameUi ui = null; InGameUi.Ensure(ref ui, owner);
                var root = CanvasProbe.For(ui);
                string names = string.Join("\n", new string[20]);
                void Draw(string text) { ui.Begin(); ui.DungeonResult("result", default, text, () => { }, names); ui.End(); }
                Draw(names);
                var allies = root.Q<ScrollView>("result-allies-scroll");
                var enemies = root.Q<ScrollView>("result-enemies-scroll");
                void ScrollDown() {
                    // ScrollRect derives its range from viewport/content geometry.
                    allies.scrollOffset = enemies.scrollOffset = new Vector2(0, 80);
                    Assert.That(allies.scrollOffset.y, Is.EqualTo(80));
                    Assert.That(enemies.scrollOffset.y, Is.EqualTo(80));
                }
                ScrollDown(); Draw(names);
                Assert.That(allies.scrollOffset.y, Is.EqualTo(80));
                Assert.That(enemies.scrollOffset.y, Is.EqualTo(80));
                Draw("새 아군\n" + names);
                Assert.That(allies.scrollOffset, Is.EqualTo(Vector2.zero));
                Assert.That(enemies.scrollOffset.y, Is.EqualTo(80));
                ScrollDown(); ui.Begin(); ui.End(); Draw("새 아군\n" + names);
                Assert.That(allies.scrollOffset, Is.EqualTo(Vector2.zero));
                Assert.That(enemies.scrollOffset, Is.EqualTo(Vector2.zero));
                ScrollDown(); ui.Hide(); Draw("새 아군\n" + names);
                Assert.That(allies.scrollOffset, Is.EqualTo(Vector2.zero));
                Assert.That(enemies.scrollOffset, Is.EqualTo(Vector2.zero));
            } finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void ResultListsScrollAndShrinkWithoutLeakingOldNamesOrRewards()
        {
            var go = new GameObject("Result lists test");
            try
            {
                var owner = go.AddComponent<StartMenuController>();
                InGameUi ui = null; InGameUi.Ensure(ref ui, owner);
                var root = CanvasProbe.For(ui);
                ui.Begin();
                ui.DungeonResult("result", default, "아군1\n아군2\n아군3\n아군4\n아군5", () => { },
                    "적1\n적2\n적3\n적4", "123", "승리");
                ui.End();
                Assert.AreEqual("아군1", root.Q<Label>("result-text").text);
                Assert.AreEqual("아군5", root.Q<Label>("result-text-4").text);
                Assert.AreEqual("적4", root.Q<Label>("result-enemies-3").text);
                Assert.AreEqual(100f, root.Q<ScrollView>("result-allies-scroll").contentContainer.style.height.value.value);
                Assert.AreEqual("123", root.Q<Label>("result-experience").text);
                Assert.AreEqual("승리", root.Q<Label>("result-status").text);
                ui.Begin(); ui.DungeonResult("result", default, "아군", () => { }, "적", "", "패배"); ui.End();
                Assert.AreEqual(DisplayStyle.None, root.Q<Label>("result-text-4").style.display.value);
                Assert.AreEqual(DisplayStyle.None, root.Q<Label>("result-enemies-3").style.display.value);
                Assert.AreEqual(string.Empty, root.Q<Label>("result-experience").text);
                Assert.AreEqual("패배", root.Q<Label>("result-status").text);
                ui.Begin(); ui.End();
                Assert.AreEqual(DisplayStyle.None, root.Q<ScrollView>("result-allies-scroll").style.display.value);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void NativeWindowDescriptionWrapsInsideBodyAndClearsWhenUnknown()
        {
            var go = new GameObject("Victory condition text test");
            try
            {
                var owner = go.AddComponent<StartMenuController>();
                InGameUi ui = null; InGameUi.Ensure(ref ui, owner);
                var root = CanvasProbe.For(ui);
                var rect = new Rect(100, 120, 164, 37);
                const string description = "설정된 목표 설명입니다.\n긴 목표 설명도 창 밖으로 넘치지 않고 스크롤합니다.";
                ui.Begin(); ui.NativeBattleWindowText("goal", rect, description); ui.End();
                var label = root.Q<Label>("goal");
                Assert.AreEqual(description, label.text);
                Assert.AreEqual(description, label.tooltip);
                Assert.AreEqual(PickingMode.Ignore, label.pickingMode);
                Assert.GreaterOrEqual(ui.Find<UnityEngine.UI.Text>("goal").rectTransform.sizeDelta.y,ui.Find<UnityEngine.UI.Text>("goal").preferredHeight);
                Assert.AreEqual(WhiteSpace.Normal, label.style.whiteSpace.value);
                Assert.AreEqual(37f, root.Q<ScrollView>("goal-scroll").style.height.value.value);
                Assert.AreEqual(ScrollerVisibility.Hidden, root.Q<ScrollView>("goal-scroll").verticalScrollerVisibility);
                Assert.AreEqual(description, root.Q<ScrollView>("goal-scroll").tooltip);
                ui.Begin(); ui.NativeBattleWindowText("goal", rect, string.Empty); ui.End();
                Assert.AreEqual(string.Empty, label.text); Assert.AreEqual(string.Empty, label.tooltip);
                ui.Begin(); ui.End();
                Assert.AreEqual(DisplayStyle.None, root.Q<ScrollView>("goal-scroll").style.display.value);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void TextInputUpdatesModelWithoutLosingTheRetainedField()
        {
            var go = new GameObject("Character name UI test");
            try
            {
                var owner = go.AddComponent<StartMenuController>();
                InGameUi ui = null; InGameUi.Ensure(ref ui, owner);
                var name = "";
                ui.Begin(); ui.TextInput("name", new Rect(0, 0, 200, 32), name, 12, value => name = value); ui.End();
                var field = CanvasProbe.For(ui).Q<TextField>("name");
                field.value = "용병";
                Assert.AreEqual("용병", name);
                ui.Begin(); ui.TextInput("name", new Rect(0, 0, 200, 32), name, 12, value => name = value); ui.End();
                Assert.AreSame(field, CanvasProbe.For(ui).Q<TextField>("name"));
                Assert.AreEqual("용병", field.value);
                var replacement = "";
                ui.Begin(); ui.TextInput("name", new Rect(0, 0, 200, 32), name, 12, value => replacement = value); ui.End();
                field.value = "새 용병";
                Assert.AreEqual("새 용병", replacement);
                Assert.AreEqual("용병", name);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
