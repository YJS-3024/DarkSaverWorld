using System;
using System.Collections.Generic;
using System.Linq;

namespace DarkSaver.Original.Tutorial
{
    [Serializable] public sealed class WaitingLine { public int id; public string text; public bool skipped; }
    [Serializable] public sealed class WaitingButton { public int id, x, y, sx, sy, width, height; }
    [Serializable] public sealed class WaitingWindowDefinition
    { public int id, scene, x, y, width, height; public WaitingButton[] buttons; }
    [Serializable] public sealed class WaitingDefinition
    { public TutorialMap map; public WaitingLine[] lines; public WaitingWindowDefinition[] windows; }
    public enum WaitingInputKind { Continue, Move, DoubleSelf, Key, Button, Restore, DragWindow, Equip, HoverDoor }
    // Saver.exe46A48D..46A52B: window-local origins in native equipment-index order.
    public static class WaitingEquipmentLayout
    {
        public const int Body = 1, LeftHand = 2, RightHand = 3, SlotCount = 7;
        public static TutorialTile Origin(int slot)
        {
            switch (slot)
            {
                case 0: return new TutorialTile(63, 42);
                case Body: return new TutorialTile(63, 86);
                case LeftHand: return new TutorialTile(26, 103);
                case RightHand: return new TutorialTile(100, 103);
                case 4: return new TutorialTile(26, 51);
                case 5: return new TutorialTile(63, 144);
                case 6: return new TutorialTile(100, 51);
                default: throw new ArgumentOutOfRangeException(nameof(slot));
            }
        }
        public static int SlotAt(int x, int y)
        {
            for (int slot = 0; slot < SlotCount; slot++)
            {
                var p = Origin(slot);
                if (x >= p.X && x < p.X + 32 && y >= p.Y && y < p.Y + 32) return slot;
            }
            return -1;
        }
    }
    public readonly struct WaitingInput
    {
        public readonly WaitingInputKind Kind;
        public readonly int Window, Value, X, Y;
        public WaitingInput(WaitingInputKind kind, int window = 0, int value = 0, int x = 0, int y = 0)
        { Kind = kind; Window = window; Value = value; X = x; Y = y; }
    }
    public sealed class WaitingWindow
    {
        public int Id, X, Y;
        public bool Visible, Minimized, Expanded;
        public WaitingWindow Copy() => (WaitingWindow)MemberwiseClone();
    }
    public sealed class WaitingView
    {
        public int ResourceId, Job, Direction, Sword, Armor, Hand, HandSlot, Body;
        public string Message;
        public double Clock, RenderX, RenderY;
        public TutorialTile Tile;
        public bool Moving, MovementAllowed, SwordEquipAllowed, ArmorEquipAllowed, Completed, SwordOnFloor, ArmorOnFloor, DoorHovered;
        public WaitingWindow[] Windows;
    }
    /// <summary>Transient Saver.exe waiting-room lesson; no currency or persistent item grants.</summary>
    public sealed class WaitingLesson
    {
        private readonly WaitingDefinition definition;
        private readonly int job;
        private readonly Dictionary<int, WaitingWindow> windows;
        private readonly Queue<TutorialTile> path = new Queue<TutorialTile>();
        private TutorialTile tile = new TutorialTile(2, 7), from;
        private int index = -1, direction = 1, sword = -1, armor = -1, hand = -1, body = -1;
        private int handSlot = WaitingEquipmentLayout.LeftHand;
        private double clock, moveClock;
        private bool swordFloor, armorFloor, completed, doorHovered;
        private int dragStartX, dragStartY;
        private int chatNormalY;
        public bool Completed => completed;
        // Walking is taught at these steps; window and equipment lessons retain the current tile.
        private bool MovementAllowed => !completed && path.Count == 0
            && (index == 2 || index == 3 || index == 28 || index == 29 || index == 35);
        private bool CanEquip(int item) => !completed && path.Count == 0 && windows[20].Visible && !windows[20].Minimized
            && ((index == 30 && item == 1 && sword == 1)
                || (index == 31 && item == (job == 103 ? 777 : 762) && armor == item));
        public WaitingLesson(WaitingDefinition definition, int job)
        {
            if (definition?.map == null || !definition.map.IsValid || definition.lines?.Length != 36
                || definition.windows?.Length != 4 || !new[] { 100, 101, 103 }.Contains(job))
                throw new ArgumentException("Invalid native waiting-room definition");
            for (int i = 0; i < 36; i++)
                if (definition.lines[i].id != 61216 + i || string.IsNullOrEmpty(definition.lines[i].text)
                    || definition.lines[i].skipped != (i >= 20 && i <= 22)) throw new ArgumentException("Invalid original lesson sequence");
            this.definition = definition; this.job = job;
            windows = definition.windows.ToDictionary(w => w.id, w => new WaitingWindow { Id = w.id, X = w.x, Y = w.y });
            if (!new[] { 18, 19, 20, 21 }.All(windows.ContainsKey)) throw new ArgumentException("Invalid waiting-room windows");
            from = tile;
        }
        public WaitingView Snapshot()
        {
            bool diagonal = path.Count > 0 && from.X != path.Peek().X && from.Y != path.Peek().Y;
            double phase = Math.Min(1, Math.Floor(moveClock / 80) * (diagonal ? 10.0 / 32 : .5));
            return new WaitingView { ResourceId = index < 0 ? 0 : 61216 + index, Job = job, Direction = direction,
                Message = index < 0 || completed ? "" : definition.lines[index].text, Clock = clock, Tile = tile,
                Moving = path.Count > 0, MovementAllowed = MovementAllowed,
                RenderX = path.Count == 0 ? tile.X : from.X + (path.Peek().X - from.X) * phase,
                RenderY = path.Count == 0 ? tile.Y : from.Y + (path.Peek().Y - from.Y) * phase,
                Sword = sword, Armor = armor, Hand = hand, HandSlot = handSlot, Body = body, SwordOnFloor = swordFloor,
                SwordEquipAllowed = CanEquip(1), ArmorEquipAllowed = CanEquip(job == 103 ? 777 : 762),
                ArmorOnFloor = armorFloor, Completed = completed, DoorHovered = doorHovered,
                Windows = windows.Values.Select(w => w.Copy()).ToArray() };
        }
        private double MoveDuration => path.Count > 0 && from.X != path.Peek().X && from.Y != path.Peek().Y ? 240 : 160;
        public void Advance(double milliseconds)
        {
            if (completed || double.IsNaN(milliseconds) || double.IsInfinity(milliseconds) || milliseconds < 0) return;
            clock += milliseconds;
            if (index < 0 && clock > 1000) Next();
            if (path.Count == 0) return;
            moveClock += milliseconds;
            while (path.Count > 0 && moveClock >= MoveDuration)
            {
                moveClock -= MoveDuration; tile = path.Dequeue(); from = tile;
                if (swordFloor && tile == new TutorialTile(16, 8)) { swordFloor = false; sword = 1; }
                if (armorFloor && tile == new TutorialTile(16, 9)) { armorFloor = false; armor = job == 103 ? 777 : 762; }
                Evaluate();
                if (path.Count > 0) Face(path.Peek());
            }
        }
        public bool Input(WaitingInput input)
        {
            if (completed || index < 0) return false;
            bool changed;
            switch (input.Kind)
            {
                case WaitingInputKind.Continue:
                    if (!new[] { 0, 1, 11, 19, 26, 27, 32, 33 }.Contains(index)) return false;
                    if (index == 27) swordFloor = true; // Native click handler435F2D..435F54: item1 at16,8.
                    Next(); return true;
                case WaitingInputKind.Move:
                    if (!MovementAllowed) return false;
                    var target = new TutorialTile(input.X, input.Y);
                    if (target == tile) return false;
                    var route = definition.map.Path(tile, target, 10000, new HashSet<TutorialTile> { new TutorialTile(16, 7) });
                    if (route == null || route.Count == 0) return false;
                    foreach (var p in route) path.Enqueue(p);
                    from = tile; moveClock = 0; Face(path.Peek()); return true;
                case WaitingInputKind.DoubleSelf:
                    if (path.Count > 0) return false;
                    windows[18].Visible = true; windows[18].Minimized = false; changed = true; break;
                case WaitingInputKind.Key:
                    int id = input.Value == 2 ? 18 : input.Value == 3 ? 21 : input.Value == 4 ? 20 : input.Value == 5 ? 19 : -1;
                    if (id < 0) return false;
                    var keyed = windows[id];
                    if (keyed.Minimized) { keyed.Minimized = false; keyed.Visible = true; }
                    else keyed.Visible = !keyed.Visible;
                    changed = true; break;
                case WaitingInputKind.Button: changed = Button(input.Window, input.Value); break;
                case WaitingInputKind.Restore:
                    if (!windows.TryGetValue(input.Window, out var mini) || !mini.Visible || !mini.Minimized) return false;
                    mini.Minimized = false; changed = true; break;
                case WaitingInputKind.DragWindow:
                    if (!windows.TryGetValue(input.Window, out var drag) || !drag.Visible || drag.Minimized) return false;
                    var layout = definition.windows.First(w => w.id == input.Window);
                    int x = Math.Max(0, Math.Min(640 - layout.width, input.X));
                    int height = drag.Id == 19 && drag.Expanded ? 401 : layout.height;
                    int y = Math.Max(0, Math.Min(480 - height, input.Y));
                    if (x == drag.X && y == drag.Y && input.Value != 1) return false;
                    drag.X = x; drag.Y = y; changed = true; break;
                case WaitingInputKind.Equip:
                    if (!CanEquip(input.Value)) return false;
                    // Equip carries the release point relative to window20, not an inferred item category.
                    int slot = WaitingEquipmentLayout.SlotAt(input.X, input.Y);
                    if (input.Value == 1 && sword == 1
                        && (slot == WaitingEquipmentLayout.LeftHand || slot == WaitingEquipmentLayout.RightHand))
                    { sword = -1; hand = 1; handSlot = slot; changed = true; }
                    else if (input.Value == (job == 103 ? 777 : 762) && armor == input.Value && slot == WaitingEquipmentLayout.Body)
                    { body = armor; armor = -1; changed = true; }
                    else return false;
                    break;
                case WaitingInputKind.HoverDoor:
                    doorHovered = input.X == 19 && input.Y == 7; changed = doorHovered; break;
                default: return false;
            }
            // Native435942 also waits for the drag flag to clear after the rectangle changed.
            if (changed && (input.Kind != WaitingInputKind.DragWindow || input.Value == 1)) Evaluate();
            return changed;
        }
        private bool Button(int id, int button)
        {
            if (!windows.TryGetValue(id, out var window) || !window.Visible || window.Minimized) return false;
            if ((id == 18 || id == 20) && button == 37) { window.Minimized = true; return true; }
            if (button == 38 || button == 62 || (id == 20 && button == 6)) { window.Visible = false; return true; }
            if ((id == 19 || id == 21) && button == 61) { window.Minimized = true; return true; }
            if (id == 19 && button == 83)
            {
                if (!window.Expanded) { chatNormalY = window.Y; window.Y = Math.Min(window.Y, 79); }
                else window.Y = chatNormalY;
                window.Expanded = !window.Expanded; return true;
            }
            int other = id == 18 && button == 31 ? 19 : id == 18 && button == 21 ? 21 : id == 18 && button == 20 ? 20 : -1;
            if (other < 0) return false;
            windows[other].Visible = true; windows[other].Minimized = false; return true;
        }
        private void Face(TutorialTile next) { direction = TutorialGeometry.FacingRow(next.X - from.X, next.Y - from.Y); }
        private void Next()
        {
            // A route can cross the objective before its requested endpoint. Stop on that tile
            // so movement from the previous instruction cannot continue into the next lesson.
            path.Clear(); from = tile; moveClock = 0;
            do { index++; } while (index < 36 && definition.lines[index].skipped);
            if (index >= 36) { completed = true; return; }
            if (index == 12) { dragStartX = windows[18].X; dragStartY = windows[18].Y; }
            if (index == 29) armorFloor = true;
        }
        private void Evaluate()
        {
            var main = windows[18]; var chat = windows[19];
            bool done;
            switch (index)
            {
                case 2: done = tile.X == 15 && tile.Y == 7; break;
                case 3: done = Math.Abs(tile.X - 2) < 3 && Math.Abs(tile.Y - 7) < 2; break;
                case 4: case 6: case 10: done = main.Visible && !main.Minimized; break;
                case 5: case 9: done = !main.Visible; break;
                case 7: done = main.Visible && main.Minimized; break;
                case 8: done = main.Visible && !main.Minimized; break;
                case 12: done = main.Visible && !main.Minimized && (main.X != dragStartX || main.Y != dragStartY); break;
                case 13: case 17: done = chat.Visible && !chat.Minimized; break;
                case 14: done = chat.Visible && !chat.Minimized && chat.Expanded; break;
                case 15: done = chat.Visible && !chat.Minimized && !chat.Expanded; break;
                case 16: case 18: done = !chat.Visible || chat.Minimized; break;
                case 23: done = windows[21].Visible; break;
                case 24: done = !windows[21].Visible; break;
                case 25: done = windows[20].Visible; break;
                case 28: done = sword == 1 || hand == 1; break;
                case 29: done = armor > 0 || body > 0; break;
                case 30: done = hand == 1; break;
                case 31: done = body > 0; break;
                case 34: done = doorHovered; break;
                case 35: done = tile.X == 19 && tile.Y == 7; break;
                default: done = false; break;
            }
            if (done) Next();
        }
    }
}
