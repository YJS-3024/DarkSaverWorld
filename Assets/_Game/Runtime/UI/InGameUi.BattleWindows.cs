using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DarkSaver.UI
{
    public sealed partial class InGameUi
    {
        private NativeBattleWindows libraryButtons;
        private sealed class LibraryButtonState
        {
            public Texture2D[] frames;
            public bool hover, pressed;
            public void Paint(Button button) => button.style.backgroundImage = new StyleBackground(frames[pressed ? 2 : hover ? 1 : 0]);
        }
        private readonly System.Collections.Generic.Dictionary<string, LibraryButtonState> libraryButtonStates = new();

        // The converted catalog also contains the shared GameLib.ltn controls.
        // Draw at native pixel size; do not stretch the artwork to a text-button rectangle.
        public void NativeLibraryButton(string key, Vector2 position, int nativeId, Action action, bool enabled, string tooltip)
        {
            if (libraryButtons == null) libraryButtons = Resources.Load<NativeBattleWindows>("UI/BattleWindows/NativeBattleWindows");
            NativeBattleWindows.Control control = null;
            if (libraryButtons != null)
                foreach (var window in libraryButtons.windows) {
                    foreach (var candidate in window.controls)
                        if (candidate.nativeId == nativeId) { control = candidate; break; }
                    if (control != null) break;
                }
            if (control?.frames == null || control.frames.Length != 3)
                throw new InvalidOperationException("Missing converted GameLib.ltn control " + nativeId);
            NativeImageButton(key, position, control, action, enabled, tooltip);
        }

        public void NativeTexturePanel(string key, Rect rect, Texture2D texture)
        {
            var panel = Element<VisualElement>(key, rect);
            panel.style.backgroundImage = new StyleBackground(texture);
            panel.style.backgroundColor = Color.clear;
            panel.BringToFront();
        }

        public void NativeImageButton(string key, Vector2 position, NativeBattleWindows.Control control, Action action, bool enabled, string tooltip)
        {
            if (control?.frames == null || control.frames.Length != 3) throw new ArgumentException("Native button needs three frames.", nameof(control));
            bool returning = !elements.TryGetValue(key, out var previous) || previous.style.display.value == DisplayStyle.None;
            Button(key, new Rect(position, control.rect.size), "", action, enabled, tooltip: tooltip);
            var button = (Button)elements[key];
            button.style.backgroundColor = Color.clear;
            button.style.borderTopWidth = button.style.borderBottomWidth = button.style.borderLeftWidth = button.style.borderRightWidth = 0;
            button.style.paddingTop = button.style.paddingBottom = button.style.paddingLeft = button.style.paddingRight = 0;
            button.style.borderTopLeftRadius = button.style.borderTopRightRadius = button.style.borderBottomLeftRadius = button.style.borderBottomRightRadius = 0;
            if (!libraryButtonStates.TryGetValue(key, out var state)) {
                state = new LibraryButtonState(); libraryButtonStates.Add(key, state);
                var captured = state;
                button.RegisterCallback<PointerEnterEvent>(_ => { captured.hover = button.enabledInHierarchy; captured.Paint(button); });
                button.RegisterCallback<PointerLeaveEvent>(_ => { captured.hover = captured.pressed = false; captured.Paint(button); });
                button.RegisterCallback<PointerDownEvent>(_ => { captured.pressed = button.enabledInHierarchy; captured.Paint(button); }, TrickleDown.TrickleDown);
                button.RegisterCallback<PointerUpEvent>(_ => { captured.pressed = false; captured.Paint(button); }, TrickleDown.TrickleDown);
                button.RegisterCallback<PointerCaptureOutEvent>(_ => { captured.pressed = false; captured.Paint(button); });
            }
            if (!enabled || returning || state.frames != control.frames) state.hover = state.pressed = false;
            state.frames = control.frames;
            state.Paint(button); button.BringToFront();
        }

        public void NativeBattleWindowText(string key, Rect rect, string text)
        {
            var parent = Scroll(key + "-scroll", rect, 0);
            parent.style.height = StyleKeyword.Auto;
            parent.style.width = Length.Percent(100);
            ((ScrollView)elements[key + "-scroll"]).verticalScrollerVisibility = ScrollerVisibility.Hidden;
            var label = Element<Label>(key, new Rect(0, 0, rect.width, 0), parent);
            label.text = text ?? string.Empty;
            label.tooltip = label.text;
            elements[key + "-scroll"].tooltip = label.text;
            label.pickingMode = PickingMode.Ignore;
            label.style.position = Position.Relative;
            label.style.width = Length.Percent(100);
            label.style.height = StyleKeyword.Auto;
            label.style.marginTop = label.style.marginBottom = 0;
            label.style.paddingTop = label.style.paddingBottom = 0;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.fontSize = 14;
            label.style.color = new Color(.17f, .09f, .035f);
            elements[key + "-scroll"].BringToFront();
        }

        public void NativeMinimizedWindow(string key, Rect rect, Texture2D background, Action restore)
        {
            Button(key, rect, "", restore, tooltip: "크게 보기");
            var button = elements[key];
            button.style.backgroundImage = new StyleBackground(background);
            button.style.backgroundColor = Color.clear;
            button.style.borderTopWidth = button.style.borderBottomWidth = button.style.borderLeftWidth = button.style.borderRightWidth = 0;
            button.BringToFront();
        }

        public void NativeBattleWindow(string key, Rect rect, NativeBattleWindows.Window window, Action close, Action minimize = null)
        {
            var panel = Element<VisualElement>(key, rect);
            panel.style.backgroundImage = new StyleBackground(window.background);
            panel.style.backgroundColor = Color.clear;
            panel.BringToFront();
            for (int i = 0; i < window.controls.Length; i++)
            {
                var control = window.controls[i];
                bool closes = control.nativeId == 19 || control.nativeId == 38 || control.nativeId == 66;
                bool minimizes = control.nativeId == 37 && minimize != null;
                var r = control.rect; r.position += rect.position;
                string buttonKey = key + "-control-" + i;
                bool fresh = !elements.ContainsKey(buttonKey);
                Button(buttonKey, r, "", closes ? close : minimizes ? minimize : null, closes || minimizes,
                    tooltip: closes ? "닫기" : minimizes ? "작게 보기" : "내용/기능 연결 대기");
                var button = (Button)elements[buttonKey];
                button.style.backgroundColor = Color.clear;
                button.style.borderTopWidth = button.style.borderBottomWidth =
                    button.style.borderLeftWidth = button.style.borderRightWidth = 0;
                button.style.paddingTop = button.style.paddingBottom = button.style.paddingLeft = button.style.paddingRight = 0;
                if (fresh)
                {
                    bool hover = false, pressed = false;
                    void Paint() => button.style.backgroundImage = new StyleBackground(control.frames[pressed ? 2 : hover ? 1 : 0]);
                    button.RegisterCallback<PointerEnterEvent>(_ => { hover = true; Paint(); });
                    button.RegisterCallback<PointerLeaveEvent>(_ => { hover = pressed = false; Paint(); });
                    button.RegisterCallback<PointerDownEvent>(_ => { pressed = true; Paint(); });
                    button.RegisterCallback<PointerUpEvent>(_ => { pressed = false; Paint(); });
                    button.RegisterCallback<PointerCaptureOutEvent>(_ => { pressed = false; Paint(); });
                    Paint();
                }
                button.BringToFront();
            }
        }
    }
}
