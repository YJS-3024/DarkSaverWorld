using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DarkSaver.UI
{
    // Retained runtime UI. Coordinates use the same top-left screen space as pointer guards.
    public sealed partial class InGameUi : MonoBehaviour
    {
        private readonly Dictionary<string, VisualElement> elements = new();
        private readonly HashSet<string> used = new();
        private readonly Dictionary<string, Action> actions = new();
        private readonly Dictionary<string, Action<string>> textChanges = new();
        private readonly Dictionary<string, (string Text, string[] Rows)> resultRows = new();
        private UIDocument document;
        private PanelSettings settings;
        private int normalSortingOrder;
        private Font font;
        private Texture2D battlePaper;
        private Texture2D battleResult;
        private Sprite battlePortraitFrame;
        private Behaviour owner;
        private VisualElement root;
        private string hoveredStatKey;
        private static readonly Color Ink = new(.91f, .88f, .77f);
        private static readonly Color Gold = new(.55f, .43f, .22f);

        public static InGameUi Ensure(ref InGameUi ui, Behaviour owner, int order = 20)
        {
            if (ui != null) return ui;
            var child = new GameObject("InGameUI", typeof(UIDocument));
            child.transform.SetParent(owner.transform, false);
            ui = child.AddComponent<InGameUi>();
            ui.owner = owner;
            ui.document = child.GetComponent<UIDocument>();
            ui.settings = ScriptableObject.CreateInstance<PanelSettings>();
            ui.settings.scaleMode = PanelScaleMode.ConstantPixelSize;
            ui.settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("UI/InGameTheme");
            ui.document.panelSettings = ui.settings;
            ui.document.sortingOrder = order;
            ui.settings.sortingOrder = order;
            ui.normalSortingOrder = order;
            ui.document.rootVisualElement.pickingMode = PickingMode.Ignore;
            ui.root = new VisualElement { name = "LogicalScreen", pickingMode = PickingMode.Ignore };
            ui.document.rootVisualElement.Add(ui.root);
            ui.root.pickingMode = PickingMode.Ignore;
            ui.root.style.position = Position.Absolute;
            ui.root.style.width = Rendering.GameScreen.Width; ui.root.style.height = Rendering.GameScreen.Height;
            ui.root.style.transformOrigin = new TransformOrigin(0, 0, 0);
            ui.root.style.overflow = Overflow.Hidden;
            ui.SyncViewport();
            ui.font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 16);
            ui.root.style.unityFontDefinition = FontDefinition.FromFont(ui.font);
            ui.root.style.fontSize = 14;
            ui.root.style.color = Ink;
            return ui;
        }

        private void Update() { if (owner == null || !owner.isActiveAndEnabled) Hide(); }
        public void Hide()
        {
            CancelItemDrags();
            hoveredStatKey = null;
            // Hiding the document does not hide each retained child. Invalidate result rows
            // so reopening also resets its scroll position, even when the names are unchanged.
            resultRows.Clear();
            foreach (var state in libraryButtonStates.Values) state.hover = state.pressed = false;
            if (root != null) root.style.display = DisplayStyle.None;
        }
        public void Begin()
        {
            document.sortingOrder = normalSortingOrder;
            settings.sortingOrder = normalSortingOrder;
            // UIDocument rebuilds its document root after disable/enable. Our retained
            // controls survive, but must be attached to the current root again.
            var documentRoot = document.rootVisualElement;
            if (root.parent != documentRoot)
            {
                documentRoot.pickingMode = PickingMode.Ignore;
                documentRoot.Add(root);
            }
            SyncViewport(); used.Clear(); root.style.display = DisplayStyle.Flex;
        }
        void SyncViewport()
        {
            var viewport = Rendering.GameScreen.Viewport;
            root.style.left = viewport.x; root.style.top = viewport.y;
            root.style.scale = new Scale(new Vector3(viewport.width / Rendering.GameScreen.Width, viewport.height / Rendering.GameScreen.Height, 1));
        }
        public void End()
        {
            DrawItemDrag();
            if (hoveredStatKey != null && used.Contains(hoveredStatKey) && elements.TryGetValue(hoveredStatKey, out var stat))
            {
                // Runtime-only explanatory overlay, not an inferred original buff panel.
                var rect = new Rect(Mathf.Clamp(stat.style.left.value.value + 8, 0, Rendering.GameScreen.Width - 260),
                    Mathf.Clamp(stat.style.top.value.value - 60, 0, Rendering.GameScreen.Height - 56), 260, 56);
                Panel("native-stat-detail", rect);
                Label("native-stat-detail-text", new Rect(rect.x + 8, rect.y + 5, rect.width - 16, rect.height - 10), stat.tooltip, 12);
                elements["native-stat-detail"].pickingMode = PickingMode.Ignore;
                elements["native-stat-detail-text"].pickingMode = PickingMode.Ignore;
                Raise("native-stat-detail"); Raise("native-stat-detail-text");
            }
            else hoveredStatKey = null;
            foreach (var pair in elements)
                pair.Value.style.display = used.Contains(pair.Key) ? DisplayStyle.Flex : DisplayStyle.None;
        }
        private T Element<T>(string key, Rect rect, VisualElement parent = null) where T : VisualElement, new()
        {
            if (!elements.TryGetValue(key, out var element))
            {
                element = new T { name = key };
                elements.Add(key, element);
                (parent ?? root).Add(element);
            }
            used.Add(key);
            element.style.display = DisplayStyle.Flex;
            element.style.position = Position.Absolute;
            element.style.left = rect.x; element.style.top = rect.y;
            element.style.width = Mathf.Max(0, rect.width); element.style.height = Mathf.Max(0, rect.height);
            return (T)element;
        }
        private static void Border(VisualElement element)
        {
            element.style.borderTopWidth = element.style.borderBottomWidth = element.style.borderLeftWidth = element.style.borderRightWidth = 1;
            element.style.borderTopColor = element.style.borderBottomColor = element.style.borderLeftColor = element.style.borderRightColor = Gold;
            element.style.borderTopLeftRadius = element.style.borderTopRightRadius = element.style.borderBottomLeftRadius = element.style.borderBottomRightRadius = 4;
        }
        public void SolidRect(string key,Rect rect,Color color)
        {
            var fill=Element<VisualElement>(key,rect);
            fill.pickingMode=PickingMode.Ignore; fill.style.backgroundColor=color;
            fill.style.borderTopWidth=fill.style.borderBottomWidth=fill.style.borderLeftWidth=fill.style.borderRightWidth=0;
            fill.BringToFront();
        }
        public void Panel(string key, Rect rect, string title = "")
        {
            var panel = Element<VisualElement>(key, rect);
            panel.style.backgroundColor = new Color(.055f, .07f, .085f, .96f);
            Border(panel);
            if (!string.IsNullOrEmpty(title)) Label(key + "-title", new Rect(rect.x + 12, rect.y + 4, rect.width - 24, 22), title, 15);
        }
        public void BattlePanel(string key, Rect rect, string title = "")
        {
            Panel(key, rect, title);
            if (battlePaper == null) battlePaper = Resources.Load<Texture2D>("UI/BattlePaper");
            var panel = elements[key];
            panel.style.backgroundImage = battlePaper == null ? StyleKeyword.None : new StyleBackground(battlePaper);
            panel.style.backgroundColor = new Color(.72f, .52f, .29f);
            panel.style.borderTopLeftRadius = panel.style.borderTopRightRadius =
                panel.style.borderBottomLeftRadius = panel.style.borderBottomRightRadius = 0;
            panel.style.borderTopWidth = panel.style.borderBottomWidth =
                panel.style.borderLeftWidth = panel.style.borderRightWidth = 2;
            if (!string.IsNullOrEmpty(title)) elements[key + "-title"].style.color = new Color(.17f, .09f, .035f);
        }
        public void NativeStatusPanel(string key, Rect rect, int rosterRows = 0)
        {
            var panel = Element<VisualElement>(key, rect);
            var texture = Resources.Load<Texture2D>(rosterRows == -1 ? "UI/NativeRosterCollapsed"
                : rosterRows == 0 ? "UI/NativeStatusPanel" : "UI/NativeRoster" + rosterRows);
            panel.style.backgroundImage = texture == null ? StyleKeyword.None : new StyleBackground(texture);
            panel.style.backgroundColor = Color.clear;
            panel.style.borderTopWidth = panel.style.borderBottomWidth = panel.style.borderLeftWidth = panel.style.borderRightWidth = 0;
            panel.style.borderTopLeftRadius = panel.style.borderTopRightRadius = panel.style.borderBottomLeftRadius = panel.style.borderBottomRightRadius = 0;
        }

        public void NativeStatusName(string key, Rect rect, string text)
        {
            var clip = Element<VisualElement>(key, rect); clip.pickingMode = PickingMode.Ignore;
            clip.style.overflow = Overflow.Hidden; clip.tooltip = text ?? "";
            int pen = 0;
            for (int i = 0; text != null && i < text.Length && pen < rect.width; i++)
            {
                if (text[i] != ' ') Portrait(key + "-glyph-" + i, new Rect(pen, 0, 14, 14), NativeStatusFont.Glyph(text[i]), clip);
                pen += NativeStatusFont.Advance(text[i]);
            }
        }

        private readonly Dictionary<string, (string Text, int Width, int MaximumLines, string[] Lines)> bitmapTextLines = new();
        public void NativeBitmapText(string key, Rect rect, string text, Color color, int maximumLines = 1,
            TextAnchor alignment = TextAnchor.MiddleLeft, float textScale = 1f)
        {
            textScale=Mathf.Clamp(textScale,.5f,1f);
            int width = Mathf.Max(14, Mathf.FloorToInt(rect.width/textScale));
            if (!bitmapTextLines.TryGetValue(key, out var cached) || cached.Text != text || cached.Width != width || cached.MaximumLines != maximumLines) {
                cached = (text, width, maximumLines, maximumLines == 1 ? new[] {text ?? ""} : NativeStatusFont.WrapLines(text, width)); bitmapTextLines[key] = cached;
            }
            var clip = Element<VisualElement>(key, rect);
            clip.pickingMode = PickingMode.Ignore; clip.style.overflow = Overflow.Hidden; clip.tooltip = text ?? "";
            int rows = Mathf.Min(Mathf.Min(cached.Lines.Length, Mathf.Max(0, maximumLines)), Mathf.FloorToInt(rect.height / (14*textScale)));
            float top = Mathf.Max(0, Mathf.Floor((rect.height - (rows * 16 - 2)*textScale) / 2));
            for (int row = 0; row < rows; row++) {
                string line = cached.Lines[row]; float measured = NativeStatusFont.Measure(line)*textScale;
                float pen = alignment == TextAnchor.MiddleRight ? rect.width - measured
                    : alignment == TextAnchor.MiddleCenter ? Mathf.Floor((rect.width - measured) / 2) : 0;
                for (int i = 0; i < line.Length; i++) {
                    if (line[i] != ' ') {
                        string glyphKey = key + "-glyph-" + row + "-" + i;
                        Portrait(glyphKey, new Rect(pen, top + row * 16*textScale, 14*textScale, 14*textScale), NativeStatusFont.WorldGlyph(line[i]), clip);
                        elements[glyphKey].style.unityBackgroundImageTintColor = color;
                    }
                    pen += NativeStatusFont.Advance(line[i])*textScale;
                }
            }
            clip.BringToFront();
        }

        public void NativeBitmapScrollText(string key, Rect rect, string text,
            NativeBattleWindows.Control up, NativeBattleWindows.Control down, bool resetPosition = false)
        {
            const int lineHeight = 16;
            int width = Mathf.Max(14, Mathf.FloorToInt(rect.width - 19));
            bool changed = !bitmapTextLines.TryGetValue(key, out var cached) || cached.Text != text || cached.Width != width || cached.MaximumLines != -1;
            if (changed) {
                cached = (text, width, -1, NativeStatusFont.WrapLines(text, width)); bitmapTextLines[key] = cached;
            }
            float height = Mathf.Max(rect.height, cached.Lines.Length * lineHeight);
            var content = Scroll(key + "-scroll", new Rect(rect.x, rect.y, width, rect.height), height, resetPosition || changed);
            var scroll = (ScrollView)elements[key + "-scroll"];
            scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden; scroll.tooltip = text ?? "";
            float maximum = Mathf.Max(0, height - rect.height);
            float offset = Mathf.Clamp(scroll.scrollOffset.y, 0, maximum);
            if (offset != scroll.scrollOffset.y) scroll.scrollOffset = new Vector2(0, offset);
            int first = Mathf.Clamp(Mathf.FloorToInt(offset / lineHeight), 0, cached.Lines.Length - 1);
            int visible = Mathf.Min(cached.Lines.Length - first, Mathf.CeilToInt(rect.height / lineHeight) + 1);
            // Reuse viewport-row keys, not absolute line indices: long text cannot grow the visual tree.
            for (int row = 0; row < visible; row++) {
                string line = cached.Lines[first + row]; int pen = 0;
                for (int i = 0; i < line.Length; i++) {
                    if (line[i] != ' ') {
                        string glyphKey = key + "-row-" + row + "-glyph-" + i;
                        Portrait(glyphKey, new Rect(pen, (first + row) * lineHeight, 14, 14), NativeStatusFont.Glyph(line[i]), content);
                    }
                    pen += NativeStatusFont.Advance(line[i]);
                }
            }
            scroll.BringToFront();
            NativeImageButton(key + "-up", new Vector2(rect.xMax - up.rect.width, rect.y), up,
                () => scroll.scrollOffset = new Vector2(0, Mathf.Max(0, scroll.scrollOffset.y - lineHeight)), offset > .5f, "설명 위로");
            NativeImageButton(key + "-down", new Vector2(rect.xMax - down.rect.width, rect.yMax - down.rect.height), down,
                () => scroll.scrollOffset = new Vector2(0, Mathf.Min(maximum, scroll.scrollOffset.y + lineHeight)), offset < maximum - .5f, "설명 아래로");
        }

        public void NativeWorldName(string key, Vector2 bottomCenter, string text)
        {
            var width = NativeStatusFont.Measure(text);
            var rect = new Rect(Mathf.Round(bottomCenter.x - width * .5f), Mathf.Round(bottomCenter.y - 14), width, 14);
            var clip = Element<VisualElement>(key, rect); clip.pickingMode = PickingMode.Ignore;
            clip.style.overflow = Overflow.Hidden;
            int pen = 0;
            for (int i = 0; text != null && i < text.Length; i++)
            {
                if (text[i] != ' ')
                {
                    var glyphKey = key + "-glyph-" + i;
                    Portrait(glyphKey, new Rect(pen, 0, 14, 14), NativeStatusFont.WorldGlyph(text[i]), clip);
                    elements[glyphKey].style.unityBackgroundImageTintColor = Ink;
                }
                pen += NativeStatusFont.Advance(text[i]);
            }
        }

        public void NativeStatusCloseButton(string key, Rect rect, Action close, bool minimize = false)
        {
            bool isNew = !elements.ContainsKey(key);
            Button(key, rect, "", close, tooltip: minimize ? "최소화" : "닫기");
            var button = (Button)elements[key];
            button.style.backgroundColor = Color.clear;
            button.style.borderTopWidth = button.style.borderBottomWidth = button.style.borderLeftWidth = button.style.borderRightWidth = 0;
            button.style.borderTopLeftRadius = button.style.borderTopRightRadius = button.style.borderBottomLeftRadius = button.style.borderBottomRightRadius = 0;
            button.style.paddingLeft = button.style.paddingRight = button.style.paddingTop = button.style.paddingBottom = 0;
            if (!isNew) return;
            bool hovered = false, pressed = false;
            void Paint() => button.style.backgroundImage = new StyleBackground(NativeBattleStatus.CloseButton(NativeBattleStatus.CloseButtonState(hovered, pressed), minimize));
            button.RegisterCallback<PointerEnterEvent>(_ => { hovered = true; Paint(); });
            button.RegisterCallback<PointerLeaveEvent>(_ => { hovered = pressed = false; Paint(); });
            button.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { pressed = true; Paint(); } });
            button.RegisterCallback<PointerUpEvent>(e => { if (e.button == 0) { pressed = false; Paint(); } });
            button.RegisterCallback<PointerCaptureOutEvent>(_ => { pressed = false; Paint(); });
            Paint();
        }

        public void ModalBlocker(string key)
        {
            bool isNew = !elements.ContainsKey(key);
            var blocker = Element<VisualElement>(key, new Rect(0, 0, Rendering.GameScreen.Width, Rendering.GameScreen.Height));
            blocker.pickingMode = PickingMode.Position; blocker.style.backgroundColor = Color.clear;
            if (isNew)
            {
                blocker.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
                blocker.RegisterCallback<PointerUpEvent>(e => e.StopPropagation());
                blocker.RegisterCallback<WheelEvent>(e => e.StopPropagation());
            }
            blocker.BringToFront();
        }
        public void Raise(string key) { if (elements.TryGetValue(key, out var element)) element.BringToFront(); }
        public void BattleLabel(string key, Rect rect, string text, int size = 14, VisualElement parent = null, bool singleLine = false, string tooltip = null)
        {
            Label(key, rect, text, size, parent);
            elements[key].style.color = new Color(.17f, .09f, .035f);
            elements[key].style.whiteSpace = singleLine ? WhiteSpace.NoWrap : WhiteSpace.Normal;
            elements[key].style.overflow = Overflow.Hidden;
            elements[key].style.textOverflow = singleLine ? TextOverflow.Ellipsis : TextOverflow.Clip;
            elements[key].tooltip = tooltip ?? (singleLine ? text ?? "" : "");
        }
        public void BattlePortrait(string key, Rect rect, Sprite portrait)
        {
            if (battlePortraitFrame == null) battlePortraitFrame = Resources.Load<Sprite>("UI/BattlePortraitFrame");
            Portrait(key + "-frame", rect, battlePortraitFrame);
            Portrait(key, new Rect(rect.x + 3, rect.y + 3, rect.width - 6, rect.height - 6), portrait);
        }
        // gscene17 native static layout. Dynamic contents are local battle data, not recovered formulas.
        public void DungeonResult(string key, Rect rect, string text, Action acknowledge,
            string enemies = "", string experience = "", string outcome = "")
        {
            // The native notice owns its size; legacy callers still pass the old 560x112 notice rect.
            rect = new Rect((Rendering.GameScreen.Width - 434f) * .5f,
                (Rendering.GameScreen.Height - 344f) * .5f, 434, 344);
            // The map presentation owns a separate UIDocument for names/damage glyphs.
            document.sortingOrder = Mathf.Max(normalSortingOrder, 100);
            settings.sortingOrder = Mathf.Max(normalSortingOrder, 100);
            ModalBlocker(key + "-blocker");
            var panel = Element<VisualElement>(key, rect);
            if (battleResult == null) battleResult = Resources.Load<Texture2D>("UI/NativeBattleResult");
            panel.style.backgroundImage = battleResult == null ? StyleKeyword.None : new StyleBackground(battleResult);
            panel.style.backgroundColor = Color.clear;
            panel.style.borderTopWidth = panel.style.borderBottomWidth =
                panel.style.borderLeftWidth = panel.style.borderRightWidth = 0;
            panel.style.borderTopLeftRadius = panel.style.borderTopRightRadius =
                panel.style.borderBottomLeftRadius = panel.style.borderBottomRightRadius = 0;
            ResultNames(key + "-text", key + "-allies-scroll", new Rect(rect.x + 20, rect.y + 90, 394, 70), text);
            ResultNames(key + "-enemies", key + "-enemies-scroll", new Rect(rect.x + 20, rect.y + 202, 394, 60), enemies);
            BattleLabel(key + "-experience", new Rect(rect.x + 238, rect.y + 62, 52, 20), experience, 14, singleLine: true);
            BattleLabel(key + "-status", new Rect(rect.x + 85, rect.y + 271, 40, 22), outcome, 14, singleLine: true);
            bool isNew = !elements.ContainsKey(key + "-confirm");
            Button(key + "-confirm", new Rect(rect.x + 372, rect.y + 311, 46, 19), "", acknowledge, tooltip: "확인");
            var button = (Button)elements[key + "-confirm"];
            button.style.backgroundColor = Color.clear;
            button.style.borderTopWidth = button.style.borderBottomWidth =
                button.style.borderLeftWidth = button.style.borderRightWidth = 0;
            button.style.paddingLeft = button.style.paddingRight = button.style.paddingTop = button.style.paddingBottom = 0;
            if (isNew)
            {
                var frames = new[] { Resources.Load<Texture2D>("UI/NativeResultConfirm0"),
                    Resources.Load<Texture2D>("UI/NativeResultConfirm1"), Resources.Load<Texture2D>("UI/NativeResultConfirm2") };
                bool hovered = false, pressed = false;
                void Paint() => button.style.backgroundImage = new StyleBackground(frames[pressed ? 2 : hovered ? 1 : 0]);
                button.RegisterCallback<PointerEnterEvent>(_ => { hovered = true; Paint(); });
                button.RegisterCallback<PointerLeaveEvent>(_ => { hovered = pressed = false; Paint(); });
                button.RegisterCallback<PointerDownEvent>(_ => { pressed = true; Paint(); });
                button.RegisterCallback<PointerUpEvent>(_ => { pressed = false; Paint(); });
                button.RegisterCallback<PointerCaptureOutEvent>(_ => { pressed = false; Paint(); });
                Paint();
            }
            // Retained controls may predate later HUD elements. Keep the notice above them.
            panel.BringToFront();
            elements[key + "-allies-scroll"].BringToFront();
            elements[key + "-enemies-scroll"].BringToFront();
            elements[key + "-experience"].BringToFront();
            elements[key + "-status"].BringToFront();
            elements[key + "-confirm"].BringToFront();
        }

        private void ResultNames(string key, string scrollKey, Rect rect, string text)
        {
            text ??= string.Empty;
            bool changed = !resultRows.TryGetValue(scrollKey, out var cached) || cached.Text != text;
            if (changed) { cached = (text, text.Split('\n')); resultRows[scrollKey] = cached; }
            var rows = cached.Rows;
            bool reopened = !elements.TryGetValue(scrollKey, out var previous)
                || previous.style.display.value == DisplayStyle.None;
            var parent = Scroll(scrollKey, rect, Mathf.Max(rect.height, rows.Length * 20));
            // Keep the user's position while viewing one result, but never inherit an older result's position.
            if (changed || reopened) ((ScrollView)elements[scrollKey]).scrollOffset = Vector2.zero;
            for (int i = 0; i < rows.Length; i++)
            {
                string rowKey = i == 0 ? key : key + "-" + i;
                BattleLabel(rowKey, new Rect(0, i * 20, rect.width - 20, 20), rows[i], 14, parent, singleLine: true);
                elements[rowKey].style.unityTextAlign = TextAnchor.UpperLeft;
            }
        }

        public void Label(string key, Rect rect, string text, int size = 14, VisualElement parent = null,
            TextAnchor alignment = TextAnchor.MiddleLeft, Color? color = null)
        {
            var label = Element<Label>(key, rect, parent);
            label.text = text ?? ""; label.pickingMode = PickingMode.Ignore;
            label.style.color = color ?? Ink;
            label.style.whiteSpace = WhiteSpace.Normal; label.style.fontSize = size;
            label.style.unityTextAlign = alignment;
        }
        public void Button(string key, Rect rect, string text, Action action, bool enabled = true,
            bool selected = false, string tooltip = "", VisualElement parent = null, Sprite icon = null,
            bool singleLine = false, int fontSize = 14)
        {
            var isNew = !elements.ContainsKey(key);
            var button = Element<Button>(key, rect, parent);
            actions[key] = action;
            if (isNew) button.clicked += () => { if (owner != null && owner.isActiveAndEnabled && actions.TryGetValue(key, out var callback)) callback?.Invoke(); };
            button.text = text; button.tooltip = tooltip; button.SetEnabled(enabled);
            button.style.marginLeft = button.style.marginRight = button.style.marginTop = button.style.marginBottom = 0;
            button.style.backgroundColor = selected ? new Color(.32f, .25f, .12f) : new Color(.12f, .15f, .17f);
            button.style.color = enabled ? Ink : new Color(.46f, .47f, .45f);
            button.style.whiteSpace = singleLine ? WhiteSpace.NoWrap : WhiteSpace.Normal;
            button.style.overflow = Overflow.Hidden; button.style.textOverflow = TextOverflow.Ellipsis;
            button.style.fontSize = fontSize; Border(button);
            button.style.paddingLeft = icon == null ? 0 : 36;
            button.style.paddingRight = 0;
            if (icon != null)
            {
                Portrait(key + "-list-icon", new Rect(rect.x, rect.y, 32, 32), icon, parent);
                elements[key + "-list-icon"].BringToFront();
            }
        }
        public void IconTextButton(string key, Rect rect, string text, Action action, Sprite icon)
        {
            Button(key, rect, text, action);
            var button = elements[key];
            button.style.unityTextAlign = TextAnchor.MiddleLeft;
            button.style.paddingLeft = icon == null ? 4 : 32;
            button.BringToFront();
            if (icon != null) {
                Portrait(key + "-icon", new Rect(rect.x + 4, rect.center.y - 12, 24, 24), icon);
                elements[key + "-icon"].BringToFront();
            }
        }

        public void ItemSlotButton(string key, Rect rect, Action action, Sprite icon, int quantity, bool selected, string tooltip)
        {
            Button(key, rect, quantity > 0 && icon == null ? "?" : "", action, quantity > 0, selected, tooltip);
            elements[key].BringToFront();
            if (icon != null && quantity > 0) {
                Portrait(key + "-icon", rect, icon); elements[key + "-icon"].BringToFront();
            }
            if (quantity > 1) {
                Label(key + "-quantity", new Rect(rect.x, rect.yMax - 12, rect.width - 2, 12), quantity.ToString(), 10,
                    alignment: TextAnchor.MiddleRight);
                elements[key + "-quantity"].BringToFront();
            }
        }

        public void ActionButton(string key, Rect rect, string text, Action action, bool enabled, string tooltip, Sprite icon = null, VisualElement parent = null, bool dimDisabledIcon = true)
        {
            Button(key, rect, icon == null ? text : "", action, enabled, tooltip: tooltip, parent: parent);
            var button = elements[key];
            // Native Micon pixels carry the artwork; do not wrap them in invented circles.
            bool nativeIcon = icon != null;
            button.style.backgroundColor = nativeIcon ? Color.clear : new Color(.19f, .12f, .055f, .95f);
            button.style.borderTopColor = button.style.borderBottomColor =
                button.style.borderLeftColor = button.style.borderRightColor = nativeIcon ? Color.clear : new Color(.8f, .63f, .28f);
            button.style.borderTopWidth = button.style.borderBottomWidth =
                button.style.borderLeftWidth = button.style.borderRightWidth = nativeIcon ? 0 : 2;
            button.style.borderTopLeftRadius = button.style.borderTopRightRadius =
                button.style.borderBottomLeftRadius = button.style.borderBottomRightRadius = nativeIcon ? 0 : rect.width * .5f;
            button.style.unityTextAlign = TextAnchor.MiddleCenter;
            button.BringToFront();
            if (icon != null)
            {
                var iconSize = Mathf.Min(32, Mathf.Min(rect.width, rect.height));
                Portrait(key + "-icon", new Rect(rect.center.x - iconSize * .5f, rect.center.y - iconSize * .5f, iconSize, iconSize), icon, parent);
                elements[key + "-icon"].BringToFront();
                elements[key + "-icon"].style.opacity = enabled || !dimDisabledIcon ? 1 : .4f;
            }
        }
        public void TextInput(string key, Rect rect, string value, int maximum, Action<string> changed, VisualElement parent = null, bool password = false)
        {
            var isNew = !elements.ContainsKey(key);
            var field = Element<TextField>(key, rect, parent);
            field.maxLength = maximum;
            field.isPasswordField = password;field.maskChar = '*';
            textChanges[key] = changed;
            if (isNew) field.RegisterValueChangedCallback(e => {
                if (owner != null && owner.isActiveAndEnabled && textChanges.TryGetValue(key, out var callback)) callback?.Invoke(e.newValue);
            });
            if (field.value != value) field.SetValueWithoutNotify(value ?? "");
        }
        public VisualElement Scroll(string key, Rect rect, float contentHeight, bool resetPosition = false)
        {
            var scroll = Element<ScrollView>(key, rect);
            if (resetPosition) scroll.scrollOffset = Vector2.zero;
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.contentContainer.style.height = contentHeight;
            return scroll.contentContainer;
        }
        public void Bar(string key, Rect rect, string label, int value, int maximum, Color color, int fontSize = 14)
        {
            var back = Element<VisualElement>(key, rect); back.pickingMode = PickingMode.Ignore;
            back.style.backgroundColor = new Color(.12f, .13f, .14f);
            var fill = Element<VisualElement>(key + "-fill", new Rect(rect.x, rect.y,
                rect.width * (maximum > 0 ? Mathf.Clamp01((float)value / maximum) : 0), rect.height));
            fill.pickingMode = PickingMode.Ignore; fill.style.backgroundColor = color;
            Label(key + "-text", rect, $" {label} {value}/{maximum}", fontSize);
        }
        public void NativeStatusGauge(string key, Vector2 position, int value, int maximum, int row, bool showNumbers = true, int gaugeWidth = 80)
        {
            int width = NativeBattleStatus.FillWidth(value, maximum, gaugeWidth);
            if (width > 0)
            {
                var clip = Element<VisualElement>(key + "-clip", new Rect(position.x, position.y, width, 9));
                clip.pickingMode = PickingMode.Ignore; clip.style.overflow = Overflow.Hidden;
                // Crop the full 80px strip; do not squash its texture to the remaining fill width.
                Portrait(key + "-fill", new Rect(0, 0, 80, 9), NativeBattleStatus.Gauge(row), clip);
            }
            if (!showNumbers || maximum <= 0) return;
            var text = System.Math.Max(0, value).ToString(System.Globalization.CultureInfo.InvariantCulture) + "/"
                + maximum.ToString(System.Globalization.CultureInfo.InvariantCulture);
            NativeStatusNumber(key, new Vector2(position.x + 4, position.y + 1), text);
        }
        public void NativeRosterSelection(string key, Rect rect, Action select, bool enabled, string tooltip)
        {
            Button(key, rect, "", select, enabled, tooltip: tooltip);
            var button = elements[key];
            button.style.backgroundColor = Color.clear;
            button.style.borderTopWidth = button.style.borderBottomWidth = button.style.borderLeftWidth = button.style.borderRightWidth = 0;
            button.style.opacity = enabled ? 1 : .4f;
        }
        public void NativeStatusNumber(string key, Vector2 position, string text, string tooltip = null)
        {
            if (text == null) return;
            for (int i = 0; i < text.Length; i++)
                Portrait(key + "-digit-" + i, new Rect(position.x + i * 5, position.y, 5, 7), NativeBattleStatus.Digit(text[i]));
            if (!string.IsNullOrEmpty(tooltip) && text.Length > 0)
            {
                var hitKey = key + "-tooltip";
                bool isNew = !elements.ContainsKey(hitKey);
                var hit = Element<VisualElement>(hitKey, new Rect(position.x, position.y, text.Length * 5, 7));
                hit.pickingMode = PickingMode.Position; hit.tooltip = tooltip;
                if (isNew)
                {
                    hit.RegisterCallback<PointerEnterEvent>(_ => TryShowNativeStatusDetails(key));
                    hit.RegisterCallback<PointerLeaveEvent>(_ => { if (hoveredStatKey == hitKey) hoveredStatKey = null; });
                }
            }
        }

        public bool TryShowNativeStatusDetails(string key)
        {
            var hitKey = key + "-tooltip";
            if (!used.Contains(hitKey) || !elements.ContainsKey(hitKey)) return false;
            hoveredStatKey = hitKey; return true;
        }

        public void Portrait(string key, Rect rect, Sprite sprite, VisualElement parent = null)
        {
            var element = Element<VisualElement>(key, rect, parent); element.pickingMode = PickingMode.Ignore;
            element.style.backgroundImage = sprite == null ? StyleKeyword.None : new StyleBackground(sprite);
        }
        public void PortraitTexture(string key, Rect rect, Texture2D texture)
        {
            var element = Element<VisualElement>(key, rect); element.pickingMode = PickingMode.Ignore;
            element.style.backgroundImage = texture == null ? StyleKeyword.None : new StyleBackground(texture);
        }
        private void OnDestroy()
        {
            if (settings != null) { if (Application.isPlaying) Destroy(settings); else DestroyImmediate(settings); }
            if (font != null) { if (Application.isPlaying) Destroy(font); else DestroyImmediate(font); }
        }
    }
}
