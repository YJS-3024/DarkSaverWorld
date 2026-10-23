using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DarkSaver.UI
{
    public sealed partial class InGameUi
    {
        private sealed class ItemDragState
        {
            public Button source;
            public int itemId, pointerId;
            public Vector2 start, position;
            public bool armed, dragging;
            public Sprite icon;
            public Rect destination;
            public Action<int> dropped;
        }
        private readonly Dictionary<string, ItemDragState> itemDrags = new();

        public void ItemDragSource(string key, int itemId, Sprite icon, Rect destination, Action<int> dropped)
        {
            if (!elements.TryGetValue(key, out var element) || element is not Button button)
                throw new ArgumentException("Create an item slot before binding its drag source.", nameof(key));
            if (!itemDrags.TryGetValue(key, out var state)) {
                state = new ItemDragState {source = button}; itemDrags.Add(key, state);
                var captured = state;
                button.RegisterCallback<PointerDownEvent>(e => {
                    if (e.button != 0 || captured.itemId <= 0 || !button.enabledInHierarchy) return;
                    CancelItemDrags();
                    captured.armed = true; captured.pointerId = e.pointerId;
                    captured.start = captured.position = e.position;
                }, TrickleDown.TrickleDown);
                button.RegisterCallback<PointerMoveEvent>(e => {
                    if (!captured.armed || captured.pointerId != e.pointerId) return;
                    captured.position = e.position;
                    if (!captured.dragging && Vector2.Distance(captured.start, captured.position) >= GameConstants.TownItemDragThreshold)
                        captured.dragging = true;
                    if (!captured.dragging) return;
                    button.CapturePointer(e.pointerId); e.StopImmediatePropagation();
                }, TrickleDown.TrickleDown);
                button.RegisterCallback<PointerUpEvent>(e => {
                    if (!captured.armed || captured.pointerId != e.pointerId || e.button != 0) return;
                    bool wasDragging = captured.dragging;
                    bool accept = wasDragging && owner != null && owner.isActiveAndEnabled && button.enabledInHierarchy && used.Contains(key)
                        && captured.destination.Contains(root.WorldToLocal(e.position));
                    int id = captured.itemId; var callback = captured.dropped;
                    if (!wasDragging) { captured.armed = false; return; }
                    ResetDrag(captured);
                    e.StopImmediatePropagation();
                    if (accept) callback?.Invoke(id);
                }, TrickleDown.TrickleDown);
                button.RegisterCallback<PointerCaptureOutEvent>(_ => { captured.armed = captured.dragging = false; });
                button.RegisterCallback<PointerCancelEvent>(_ => ResetDrag(captured));
            }
            if (state.itemId != itemId || !button.enabledInHierarchy) ResetDrag(state);
            state.itemId = itemId; state.icon = icon; state.destination = destination; state.dropped = dropped;
        }
        private static void ResetDrag(ItemDragState state)
        {
            state.armed = state.dragging = false;
            if (state.source.HasPointerCapture(state.pointerId)) state.source.ReleasePointer(state.pointerId);
        }
        public void CancelItemDrags()
        {
            foreach (var state in itemDrags.Values) ResetDrag(state);
        }
        private void OnApplicationFocus(bool focused) { if (!focused) CancelItemDrags(); }
        private void OnApplicationPause(bool paused) { if (paused) CancelItemDrags(); }
        private void DrawItemDrag()
        {
            foreach (var pair in itemDrags) {
                var state = pair.Value;
                if (!used.Contains(pair.Key)) { ResetDrag(state); continue; }
                if (!state.dragging || state.icon == null) continue;
                var position = root.WorldToLocal(state.position) - new Vector2(16,16);
                Portrait("item-drag-preview", new Rect(position, new Vector2(32,32)), state.icon);
                Raise("item-drag-preview");
            }
        }
    }
}
