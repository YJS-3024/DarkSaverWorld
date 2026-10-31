using System.Collections.Generic;
using System.Linq;
using DarkSaver.Original.Entry;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DarkSaver.Original.Tutorial
{
    public sealed partial class TutorialSceneRoot
    {
        private GameObject waitingCanvas;
        private RectTransform waitingScreen, waitingDialogue, waitingLayers;
        private WaitingDefinition waitingDefinition;
        private WaitingView waitingView;
        private RawImage waitingPlayer, waitingTeacher, waitingSword, waitingArmor, waitingGhost;
        private RawImage waitingCursor;
        private bool waitingCursorOwned, previousCursorVisible;
        private Text waitingMessage, waitingName;
        private bool waitingActive;
        private Vector2? waitingDragOffset;
        private int waitingHoverX = int.MinValue, waitingHoverY, waitingHoverResource;
        private readonly Dictionary<int, WaitingPanel> waitingPanels = new Dictionary<int, WaitingPanel>();
        private readonly List<Button> waitingMoveButtons = new List<Button>();
        private sealed class WaitingPanel
        {
            public RectTransform Root, Full, Mini;
            public RawImage Background, Commander, Sword, Armor, Hand, Body;
            public Text Name, Job;
            public readonly Dictionary<int, RectTransform> Controls = new Dictionary<int, RectTransform>();
        }
        private void BeginWaitingLesson()
        {
            var reply = server.Request_BeginWaitingLesson();
            if (!reply.Accepted) { Debug.LogError("Original waiting lesson rejected: " + reply.Failure, this); return; }
            waitingDefinition = art.WaitingDefinition;
            if (waitingCanvas != null) { waitingCanvas.SetActive(false); Destroy(waitingCanvas); waitingPanels.Clear(); }
            if (canvasObject != null) canvasObject.SetActive(false); if (music != null) music.Stop();
            waitingDragOffset = null; waitingHoverX = int.MinValue; BuildWaitingLesson();
            waitingCanvas.SetActive(true); waitingView = reply.View; waitingActive = true; active = false;
            previousCursorVisible = Cursor.visible; waitingCursorOwned = true;
            consumedPress = true; PaintWaitingLesson();
        }
        private void BuildWaitingLesson()
        {
            waitingCanvas = new GameObject("Original Waiting Lesson Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = waitingCanvas.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 200; canvas.pixelPerfect = true;
            var scaler = waitingCanvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640, 480); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var black = Group((RectTransform)canvas.transform, "Letterbox", new Rect(0, 0, 640, 480)).gameObject.AddComponent<Image>();
            black.color = Color.black; black.raycastTarget = false;
            var br = (RectTransform)black.transform; br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one; br.offsetMin = br.offsetMax = Vector2.zero;
            waitingScreen = Group((RectTransform)canvas.transform, "Native640x480", new Rect(0, 0, 640, 480));
            waitingScreen.anchorMin = waitingScreen.anchorMax = new Vector2(.5f, .5f); waitingScreen.anchoredPosition = new Vector2(-320, 240);
            waitingScreen.gameObject.AddComponent<RectMask2D>();
            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont(new[] { "굴림", "Gulim", "Malgun Gothic", "Apple SD Gothic Neo", "Noto Sans CJK KR" }, 12);
                ownsFont = font != null; if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            Picture(waitingScreen, "Map1500", new Rect(0, 0, 640, 480), art.waitingGround);
            waitingMoveButtons.Clear();
            for (int y = 0; y < 15; y++) for (int x = 0; x < 20; x++)
            {
                int tx = x, ty = y;
                waitingMoveButtons.Add(Hit(waitingScreen, "Move " + x + "," + y, new Rect(x * 32, y * 32, 32, 32),
                    () => WaitingSend(new WaitingInput(WaitingInputKind.Move, x: tx, y: ty))));
            }
            // Saver42635F..426408 scales the32px inventory source into a16px floor surface.
            waitingSword = Picture(waitingScreen, "Dropped item1", new Rect(520, 264, 16, 16), art.waitingItems, new Rect(0, 0, 32, 32));
            waitingArmor = Picture(waitingScreen, "Dropped native armor", new Rect(520, 296, 16, 16), art.waitingItems);
            var player = Hit(waitingScreen, "Double click commander", new Rect(64, 224, 32, 32), () => { });
            var clicks = player.gameObject.AddComponent<EventTrigger>();
            var click = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            click.callback.AddListener(e => { var p = (PointerEventData)e; if (p.button == PointerEventData.InputButton.Left && p.clickCount == 2) WaitingSend(new WaitingInput(WaitingInputKind.DoubleSelf)); });
            clicks.triggers.Add(click);
            waitingPlayer = Picture((RectTransform)player.transform, "Commander32px", new Rect(0, 0, 32, 32), art.Actor(character.job, -1));
            waitingName = Label((RectTransform)player.transform, "Commander name", new Rect(-30, -16, 92, 16), character.name, TextAnchor.MiddleCenter, 12);
            waitingName.color = Color.yellow;
            waitingTeacher = Picture(waitingScreen, "Teacher125", new Rect(512, 224, 32, 32), art.Actor(125, -1));
            var teacherName = Label(waitingScreen, "사부", new Rect(488, 208, 80, 16), "사부", TextAnchor.MiddleCenter, 12); teacherName.color = Color.yellow;
            waitingLayers = Group(waitingScreen, "Native Wlib windows", new Rect(0, 0, 640, 480));
            for (int i = 0; i < waitingDefinition.windows.Length; i++) BuildWaitingPanel(waitingDefinition.windows[i], i);
            waitingDialogue = Group(waitingScreen, "Original RT_STRING lesson", new Rect(40, 20, 562, 84));
            Picture(waitingDialogue, "Wlib scene38", new Rect(0, 0, 562, 84), art.waitingDialogue);
            Picture(waitingDialogue, "Teacher portrait", new Rect(14, 9, 64, 64), art.face);
            waitingMessage = Label(waitingDialogue, "Original lesson text", new Rect(85, 7, 466, 72), "", TextAnchor.UpperLeft, 12);
            waitingMessage.color = new Color(.72f, .72f, .72f, 1);
            Hit(waitingDialogue, "Continue where native handler permits", new Rect(0, 0, 562, 84), () => WaitingSend(new WaitingInput(WaitingInputKind.Continue)));
            waitingGhost = Picture(waitingScreen, "Dragged native item", new Rect(0, 0, 32, 32), art.waitingItems); waitingGhost.gameObject.SetActive(false);
            waitingCursor = Picture(waitingScreen, "Native sword or door cursor", new Rect(0, 0, 32, 32), art.waitingCursors);
        }
        private void BuildWaitingPanel(WaitingWindowDefinition spec, int index)
        {
            int id = spec.id;
            var p = new WaitingPanel { Root = Group(waitingLayers, "Wlib window" + id, new Rect(spec.x, spec.y, spec.width, spec.height)) };
            waitingPanels.Add(id, p);
            p.Full = Group(p.Root, "Full", new Rect(0, 0, spec.width, spec.height));
            var blocker = p.Full.gameObject.AddComponent<Image>(); blocker.color = Color.clear;
            p.Background = Picture(p.Full, "Native scene" + spec.scene, new Rect(0, 0, spec.width, spec.height), art.waitingWindows[index]);
            foreach (var b in spec.buttons)
            {
                int buttonId = b.id;
                var button = Hit(p.Full, "Native button" + b.id, new Rect(b.x, b.y, b.width, b.height), () => WaitingSend(new WaitingInput(WaitingInputKind.Button, id, buttonId)));
                var image = Picture((RectTransform)button.transform, "Original button states", new Rect(0, 0, b.width, b.height), art.waitingButtons);
                button.gameObject.AddComponent<NativeEntryButton>().Configure(image, art.waitingButtons,
                    new Rect(b.sx, b.sy, b.width, b.height), new Rect(b.sx + b.width, b.sy, b.width, b.height), new Rect(b.sx + b.width * 2, b.sy, b.width, b.height));
                // Repeated character-slot buttons share ID48; only header controls need lookup.
                p.Controls[buttonId] = (RectTransform)button.transform;
            }
            var header = Hit(p.Full, "Drag native ornament", new Rect(id == 18 ? 100 : 15, id == 18 ? 0 : 3, id == 18 ? 60 : 230, id == 18 ? 40 : 12), () => { });
            header.gameObject.AddComponent<NativeTutorialDrag>().Configure(waitingScreen, (point, end) =>
            {
                var w = server.WaitingSnapshot?.Windows.FirstOrDefault(a => a.Id == id); if (w == null) return;
                if (!waitingDragOffset.HasValue) waitingDragOffset = point - new Vector2(w.X, w.Y);
                var pos = point - waitingDragOffset.Value;
                WaitingSend(new WaitingInput(WaitingInputKind.DragWindow, id, value: end ? 1 : 0, x: Mathf.RoundToInt(pos.x), y: Mathf.RoundToInt(pos.y)));
                if (end) waitingDragOffset = null;
            });
            var minimized = art.waitingMinimizedWindows[index]; var miniRect = new Rect(0, 0, minimized.width, minimized.height);
            p.Mini = Group(p.Root, "Minimized", miniRect);
            var restore = Hit(p.Mini, "Restore", miniRect, () => WaitingSend(new WaitingInput(WaitingInputKind.Restore, id)));
            Picture((RectTransform)restore.transform, "Native minimized icon", miniRect, minimized);
            if (id == 18)
            {
                p.Commander = Picture(p.Full, "Portrait cell", new Rect(38, 86, 32, 32), art.Actor(character.job, -1), new Rect(32, 32, 32, 32));
                p.Name = WaitingField(p.Full, "Name", new Rect(116, 73, 108, 16), character.name);
                p.Job = WaitingField(p.Full, "Job", new Rect(116, 92, 108, 16), JobName(character.job));
                for (int row = 0; row < 3; row++) for (int column = 0; column < 3; column++)
                { var cell = Group(p.Full, "Black character cell", new Rect(41 + column * 38, 140 + row * 50, 32, 32)).gameObject.AddComponent<Image>(); cell.color = Color.black; cell.raycastTarget = false; }
                WaitingField(p.Full, "Original empty balance", new Rect(84, 300, 58, 18), "0", TextAnchor.MiddleRight, Color.white);
            }
            if (id == 21)
            {
                p.Name = WaitingField(p.Full, "Name", new Rect(146, 39, 170, 16), character.name);
                p.Job = WaitingField(p.Full, "Job", new Rect(146, 56, 170, 16), JobName(character.job));
                var face = character.job == 100 ? art.waitingFighterFace : character.job == 101 ? art.waitingKnightFace : art.waitingMagicianFace;
                p.Commander = Picture(p.Full, "Native64x64 commander face", new Rect(29, 46, 64, 64), face);
                var badge = Group(p.Full, "Empty native badge", new Rect(29, 178, 32, 32)).gameObject.AddComponent<Image>(); badge.color = Color.black; badge.raycastTarget = false;
            }
            if (id == 20)
            {
                for (int row = 0; row < 4; row++) for (int column = 0; column < 7; column++)
                { var cell = Group(p.Full, "Black inventory cell", new Rect(146 + column * 35, 42 + row * 35, 32, 32)).gameObject.AddComponent<Image>(); cell.color = Color.black; cell.raycastTarget = false; }
                foreach (int slot in new[] { WaitingEquipmentLayout.LeftHand, WaitingEquipmentLayout.RightHand, WaitingEquipmentLayout.Body })
                { var cell = Group(p.Full, "Black equipment cell", WaitingEquipmentRect(slot)).gameObject.AddComponent<Image>(); cell.color = Color.black; cell.raycastTarget = false; }
                p.Sword = WaitingItemSlot(p.Full, "Dagger in inventory", new Rect(146, 42, 32, 32), 1);
                p.Armor = WaitingItemSlot(p.Full, "Armor in inventory", new Rect(181, 42, 32, 32), character.job == 103 ? 777 : 762);
                p.Hand = Picture(p.Full, "Hand equipment", WaitingEquipmentRect(WaitingEquipmentLayout.LeftHand), art.waitingItems, new Rect(0, 0, 32, 32));
                p.Body = Picture(p.Full, "Body equipment", WaitingEquipmentRect(WaitingEquipmentLayout.Body), art.waitingItems);
            }
        }
        private static Rect WaitingEquipmentRect(int slot)
        { var p = WaitingEquipmentLayout.Origin(slot); return new Rect(p.X, p.Y, 32, 32); }
        private Text WaitingField(RectTransform parent, string name, Rect rect, string text,
            TextAnchor alignment = TextAnchor.MiddleLeft, Color? color = null)
        {
            // Bound OS-font glyphs to the native field: long names must not paint labels/borders.
            var field = Group(parent, name + " bounds", rect); field.gameObject.AddComponent<RectMask2D>();
            var label = Label(field, name, new Rect(0, 0, rect.width, rect.height), text, alignment, 12);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate; label.color = color ?? Color.black;
            return label;
        }
        private string JobName(int job) => definition.map.abilities.First(a => a.job == job).name;
        private RawImage WaitingItemSlot(RectTransform parent, string name, Rect rect, int item)
        {
            var button = Hit(parent, name, rect, () => { });
            var image = Picture((RectTransform)button.transform, "Native item", new Rect(0, 0, 32, 32), art.waitingItems, WaitingItemSource(item));
            button.gameObject.AddComponent<NativeTutorialDrag>().Configure(waitingScreen, (point, end) =>
            {
                waitingGhost.gameObject.SetActive(!end); waitingGhost.uvRect = NativeEntryLayout.Uv(art.waitingItems, WaitingItemSource(item));
                Place((RectTransform)waitingGhost.transform, new Rect(point.x - 16, point.y - 16, 32, 32));
                if (!end) return;
                var p = server.WaitingSnapshot.Windows.First(w => w.Id == 20); var local = point - new Vector2(p.X, p.Y);
                WaitingSend(new WaitingInput(WaitingInputKind.Equip, value: item, x: Mathf.FloorToInt(local.x), y: Mathf.FloorToInt(local.y)));
            }, () => server.WaitingSnapshot != null && (item == 1 ? server.WaitingSnapshot.SwordEquipAllowed : server.WaitingSnapshot.ArmorEquipAllowed));
            return image;
        }
        private static Rect WaitingItemSource(int item) => new Rect(item == 1 ? 0 : item == 762 ? 32 : 64, 0, 32, 32);
        private void WaitingSend(WaitingInput input) { if (waitingActive) server.Request_WaitingLessonInput(input); }
        private void UpdateWaitingLesson()
        {
            if (server.WaitingSnapshot == null) { LateWaitingLesson(); return; }
            consumedPress = false;
            for (int key = 2; key <= 5; key++) if (Input.GetKeyDown((KeyCode)((int)KeyCode.F1 + key - 1)))
                WaitingSend(new WaitingInput(WaitingInputKind.Key, value: key));
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(waitingScreen, Input.mousePosition, null, out var local))
            {
                var pos = new Vector2(local.x, -local.y);
                var covered = server.WaitingSnapshot.Windows.Any(w => w.Visible && WaitingPanelBounds(w).Contains(pos));
                int x = covered ? -1 : Mathf.FloorToInt(pos.x / 32), y = Mathf.FloorToInt(pos.y / 32);
                int resource = server.WaitingSnapshot.ResourceId;
                if (x != waitingHoverX || y != waitingHoverY || resource != waitingHoverResource)
                {
                    waitingHoverX = x; waitingHoverY = y; waitingHoverResource = resource;
                    WaitingSend(new WaitingInput(WaitingInputKind.HoverDoor, x: x, y: y));
                }
            }
        }
        private Rect WaitingPanelBounds(WaitingWindow w)
        {
            int index = System.Array.FindIndex(waitingDefinition.windows, a => a.id == w.Id);
            var spec = waitingDefinition.windows[index]; var mini = art.waitingMinimizedWindows[index];
            return new Rect(w.X, w.Y, w.Minimized ? mini.width : spec.width,
                w.Minimized ? mini.height : w.Id == 19 && w.Expanded ? 401 : spec.height);
        }
        private void LateWaitingLesson()
        {
            waitingView = server.WaitingSnapshot;
            if (waitingView == null) { waitingActive = false; waitingCanvas.SetActive(false); RestoreWaitingCursor(); return; }
            if (waitingView.Completed)
            {
                waitingActive = false; waitingCanvas.SetActive(false); waitingGhost.gameObject.SetActive(false); RestoreWaitingCursor(); BeginBattleLesson(); return;
            }
            PaintWaitingLesson();
        }
        private void PaintWaitingLesson()
        {
            if (waitingView == null) return;
            var v = waitingView; int frame = new[] { 0, 1, 2, 1 }[(int)(v.Clock / TutorialPresentationTiming.IdleFrameMs) % 4];
            foreach (var button in waitingMoveButtons) button.interactable = v.MovementAllowed;
            var playerRoot = (RectTransform)waitingPlayer.transform.parent;
            Place(playerRoot, new Rect((float)v.RenderX * 32, (float)v.RenderY * 32, 32, 32));
            waitingPlayer.uvRect = NativeEntryLayout.Uv(waitingPlayer.texture, new Rect(frame * 32, v.Direction * 32, 32, 32));
            waitingTeacher.uvRect = NativeEntryLayout.Uv(waitingTeacher.texture, new Rect(frame * 32, 32, 32, 32));
            waitingSword.gameObject.SetActive(v.SwordOnFloor); waitingArmor.gameObject.SetActive(v.ArmorOnFloor);
            waitingArmor.uvRect = NativeEntryLayout.Uv(art.waitingItems, WaitingItemSource(character.job == 103 ? 777 : 762));
            waitingMessage.text = v.Message; waitingDialogue.gameObject.SetActive(!string.IsNullOrEmpty(v.Message));
            bool cursorInside = RectTransformUtility.ScreenPointToLocalPointInRectangle(waitingScreen, Input.mousePosition, null, out var mouse)
                && new Rect(0, 0, 640, 480).Contains(new Vector2(mouse.x, -mouse.y)) && Application.isFocused;
            waitingCursor.gameObject.SetActive(cursorInside); if (waitingCursorOwned) Cursor.visible = !cursorInside && previousCursorVisible;
            if (cursorInside)
            {
                Place((RectTransform)waitingCursor.transform, new Rect(mouse.x - 12, -mouse.y - 12, 32, 32));
                waitingCursor.uvRect = NativeEntryLayout.Uv(art.waitingCursors, new Rect((v.DoorHovered ? 1 + frame : 0) * 32, 0, 32, 32));
            }
            foreach (var w in v.Windows)
            {
                var p = waitingPanels[w.Id]; var spec = waitingDefinition.windows.First(a => a.id == w.Id);
                p.Root.gameObject.SetActive(w.Visible); p.Full.gameObject.SetActive(!w.Minimized); p.Mini.gameObject.SetActive(w.Minimized);
                int height = w.Id == 19 && w.Expanded ? 401 : spec.height;
                Place(p.Root, new Rect(w.X, w.Y, spec.width, height)); Place(p.Full, new Rect(0, 0, spec.width, height));
                if (w.Id == 19)
                {
                    p.Background.texture = w.Expanded ? art.waitingChatExpanded : art.waitingWindows[1];
                    Place((RectTransform)p.Background.transform, new Rect(0, 0, spec.width, height));
                    foreach (var b in spec.buttons)
                        Place(p.Controls[b.id], new Rect(b.x, b.id == 59 && w.Expanded ? 379 : b.y, b.width, b.height));
                }
                if (w.Id == 20)
                {
                    p.Sword.transform.parent.gameObject.SetActive(v.Sword > 0); p.Armor.transform.parent.gameObject.SetActive(v.Armor > 0);
                    p.Hand.gameObject.SetActive(v.Hand > 0); p.Body.gameObject.SetActive(v.Body > 0);
                    Place((RectTransform)p.Hand.transform, WaitingEquipmentRect(v.HandSlot));
                    p.Body.uvRect = NativeEntryLayout.Uv(art.waitingItems, WaitingItemSource(character.job == 103 ? 777 : 762));
                }
            }
        }
        private void RestoreWaitingCursor()
        { if (waitingCursorOwned) Cursor.visible = previousCursorVisible; waitingCursorOwned = false; }
        private void OnApplicationFocus(bool focused)
        { if (!focused && waitingCursorOwned) Cursor.visible = previousCursorVisible; }
    }
}
