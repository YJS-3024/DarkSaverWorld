using System;
using System.Collections.Generic;
using System.Linq;
using DarkSaver.Original.Entry;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DarkSaver.Original.Tutorial
{
    [DisallowMultipleComponent, RequireComponent(typeof(EntrySceneRoot), typeof(LocalServerManager))]
    public sealed partial class TutorialSceneRoot : MonoBehaviour
    {
        [SerializeField] private NativeTutorialArt art = null;
        private LocalServerManager server;
        private EntrySceneRoot entry;
        private TutorialDefinition definition;
        private GameObject canvasObject;
        private RectTransform screen, world, rangesLayer, unitsLayer, windowsLayer, menuLayer, dialogueLayer;
        private Font font;
        private bool ownsFont;
        private readonly List<Text> messageLines = new List<Text>();
        private Text faultText;
        private Button retrySave;
        private readonly Dictionary<int, RawImage> unitImages = new Dictionary<int, RawImage>();
        private readonly Dictionary<int, RawImage> unitEffects = new Dictionary<int, RawImage>();
        private readonly Dictionary<int, WindowView> windowViews = new Dictionary<int, WindowView>();
        private readonly List<RawImage> rangeImages = new List<RawImage>();
        private readonly List<(RectTransform rect, int bottom)> decorationOrder = new List<(RectTransform, int)>();
        private readonly List<(TutorialAction action, Button button, RawImage image)> actionIcons = new List<(TutorialAction, Button, RawImage)>();
        private RectTransform actionMenu, magicMenu, orderMenu;
        private RawImage magicFx, displayPosition, spellIcon;
        private readonly List<RawImage> orderIcons = new List<RawImage>();
        private AudioSource music;
        private int musicRepeats, rangeKey = int.MinValue;
        private bool active, consumedPress, completionAttempted, delivered;
        private Vector2 previousPointer;
        private EntryCharacter character;
        private TutorialView view;
        public event Action<EntryCharacter> TutorialCompleted;
        public event Action<EntryCharacter> WorldEntryReady;
        public bool IsRunning => waitingActive || (active && view != null && !view.Completed);
        private void Awake()
        {
            entry = GetComponent<EntrySceneRoot>(); server = GetComponent<LocalServerManager>();
            entry.CharacterReady += Begin;
        }
        private void Begin(EntryCharacter value)
        {
            if (value == null) return;
            character = value.Copy(); completionAttempted = delivered = false;
            if (value.tutorialCompleted) { WorldEntryReady?.Invoke(value.Copy()); Debug.Log("Original client: tutorial already completed; world entry boundary reached.", this); return; }
            if (art == null || !art.IsConfigured || !art.WaitingConfigured) { Debug.LogError("Original tutorial assets are missing.", this); return; }
            definition = art.Definition; server.ConfigureTutorial(art);
            BeginWaitingLesson();
        }
        private void BeginBattleLesson()
        {
            var reply = server.Request_BeginTutorial();
            if (!reply.Accepted) { Debug.LogError("Original tutorial entry rejected: " + reply.Failure, this); return; }
            if (canvasObject == null) Build();
            canvasObject.SetActive(true); active = true; consumedPress = true; view = reply.View;
            previousPointer = Input.mousePosition; Paint();
        }
        private void Build()
        {
            canvasObject = new GameObject("Original Tutorial Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 200; canvas.pixelPerfect = true;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640, 480); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var black = Group((RectTransform)canvas.transform, "Letterbox", new Rect(0, 0, 640, 480)).gameObject.AddComponent<Image>();
            black.color = Color.black; black.raycastTarget = false;
            var br = (RectTransform)black.transform; br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one; br.offsetMin = br.offsetMax = Vector2.zero;
            screen = Group((RectTransform)canvas.transform, "Native 640x480", new Rect(0, 0, 640, 480));
            screen.anchorMin = screen.anchorMax = screen.pivot = new Vector2(.5f, .5f); screen.anchoredPosition = Vector2.zero;
            screen.gameObject.AddComponent<RectMask2D>();
            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont(new[] { "굴림", "Gulim", "Malgun Gothic", "Apple SD Gothic Neo", "Noto Sans CJK KR" }, 12);
                ownsFont = font != null;
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            world = Group(screen, "Map301", new Rect(0, 0, 640, 480));
            Picture(world, "Native ground", new Rect(0, 0, 640, 480), art.ground);
            for (int y = 0; y < definition.map.height; y++) for (int x = 0; x < definition.map.width; x++)
            {
                int tileX = x, tileY = y;
                Hit(world, "Tile " + x + "," + y, new Rect(x * 32, y * 32, 32, 32), () => Send(new TutorialInput(TutorialInputKind.Tile, x: tileX, y: tileY)));
            }
            rangesLayer = Group(screen, "Native ranges", new Rect(0, 0, 640, 480));
            for (int i = 0; i < 300; i++) { var image = Picture(rangesLayer, "Range " + i, new Rect(0, 0, 31, 31), art.ranges[0]); image.gameObject.SetActive(false); rangeImages.Add(image); }
            displayPosition = Picture(rangesLayer, "DISPLAYPOS", new Rect(0, 0, 31, 31), art.ranges[3]);
            unitsLayer = Group(screen, "Units and native decorations", new Rect(0, 0, 640, 480));
            for (int i = 0; i < definition.decorations.Length; i++)
            {
                var d = definition.decorations[i]; var image = Picture(unitsLayer, "Decoration " + i, new Rect(d.x, d.y, d.width, d.height), art.decorations[i]);
                decorationOrder.Add(((RectTransform)image.transform, d.y + d.height));
            }
            foreach (int id in new[] { 0, 100, 400, 410, 1, 2, 800 })
            {
                int unit = id;
                var button = Hit(unitsLayer, "Unit " + id, new Rect(0, 0, 32, 32), () => Send(new TutorialInput(TutorialInputKind.Unit, unit)));
                var image = Picture((RectTransform)button.transform, "Native 32x32 cell", new Rect(0, 0, 32, 32), art.actors[0]);
                unitImages[id] = image;
                unitEffects[id] = Picture((RectTransform)button.transform, "Native effect", new Rect(0, 0, 31, 31), art.effects);
            }
            magicFx = Picture(screen, "Pattern2", new Rect(0, 0, 32, 32), art.effects);
            windowsLayer = Group(screen, "Glib windows", new Rect(0, 0, 640, 480));
            BuildWindows();
            dialogueLayer = Group(screen, "MESSAGE", new Rect(40, 50, 562, 104));
            Picture(dialogueLayer, "ToneSurface frame", new Rect(0, 0, 562, 104), art.dialogue);
            Picture(dialogueLayer, "Face125", new Rect(20, 20, 64, 64), art.face);
            for (int line = 0; line < 3; line++)
            {
                var label = Label(dialogueLayer, "Original line " + line, new Rect(110, 23 + line * 20, 437, 18), "", TextAnchor.UpperLeft, 12);
                label.horizontalOverflow = HorizontalWrapMode.Overflow; messageLines.Add(label);
            }
            // Menus keep actor-relative positions; status windows take input/draw priority above this layer.
            menuLayer = Group(screen, "Actor-relative actions below windows", new Rect(0, 0, 640, 480));
            BuildMenus();
            windowsLayer.SetAsLastSibling();
            dialogueLayer.SetAsLastSibling(); // Keep the current lesson readable while a large F1 window is demonstrated.
            var fault = Group(screen, "Storage failure", new Rect(80, 180, 480, 96));
            var bg = fault.gameObject.AddComponent<Image>(); bg.color = new Color(0, 0, 0, .95f);
            faultText = Label(fault, "Failure text", new Rect(12, 8, 456, 42), "", TextAnchor.MiddleCenter, 12);
            retrySave = Hit(fault, "Retry completion save", new Rect(170, 56, 140, 28), TryComplete);
            Label((RectTransform)retrySave.transform, "Label", new Rect(0, 0, 140, 28), "완료 기록 저장 재시도", TextAnchor.MiddleCenter, 12);
            fault.gameObject.SetActive(false);
            music = canvasObject.AddComponent<AudioSource>(); music.playOnAwake = false; music.loop = false; music.spatialBlend = 0; music.clip = art.music;
        }
        private void BuildMenus()
        {
            actionMenu = Group(menuLayer, "Actions at actor", new Rect(0, 0, 96, 96));
            var actions = new[] { TutorialAction.Move, TutorialAction.Item, TutorialAction.Attack, TutorialAction.Magic, TutorialAction.Special, TutorialAction.Rest, TutorialAction.Order, TutorialAction.Options };
            var positions = new[] { new Vector2(0, 0), new Vector2(32, 0), new Vector2(64, 0), new Vector2(0, 32), new Vector2(64, 32), new Vector2(0, 64), new Vector2(32, 64), new Vector2(64, 64) };
            for (int i = 0; i < actions.Length; i++)
            {
                var action = actions[i]; var button = Hit(actionMenu, action.ToString(), new Rect(positions[i], new Vector2(32, 32)), () => Send(new TutorialInput(TutorialInputKind.Action, view.MenuUnit, (int)action)));
                var image = Picture((RectTransform)button.transform, "Native icon", new Rect(0, 0, 32, 32), art.icons); actionIcons.Add((action, button, image));
            }
            magicMenu = Group(menuLayer, "Magic selection", new Rect(0, 0, 32, 32));
            var magicButton = Hit(magicMenu, "Fireball101", new Rect(0, 0, 32, 32), () => Send(new TutorialInput(TutorialInputKind.Magic, 0, 101)));
            spellIcon = Picture((RectTransform)magicButton.transform, "Native fireball icon", new Rect(0, 0, 32, 32), art.icons, new Rect(0, 32, 32, 32));
            orderMenu = Group(menuLayer, "Mercenary orders", new Rect(0, 0, 96, 96));
            for (int i = 0; i < 3; i++)
            {
                int order = i + 1;
                var button = Hit(orderMenu, "Order " + order, new Rect(i == 0 ? 32 : i == 1 ? 0 : 64, i == 0 ? 0 : 64, 32, 32), () => Send(new TutorialInput(TutorialInputKind.Order, 0, order)));
                // Micon native order column: direct, auto attack, auto defense.
                orderIcons.Add(Picture((RectTransform)button.transform, "Native order icon", new Rect(0, 0, 32, 32), art.icons, new Rect(i * 32, 64, 32, 32)));
            }
        }
        private void Update()
        {
            if (waitingActive) { UpdateWaitingLesson(); return; }
            if (!active || view == null) return;
            if (Input.GetMouseButtonDown(0))
            {
                consumedPress = view.Wait == "WAITMOUSEBUTTON";
                if (consumedPress) server.Request_TutorialInput(new TutorialInput(TutorialInputKind.Continue));
            }
            if (Input.GetMouseButtonDown(1)) Send(new TutorialInput(TutorialInputKind.Back));
            for (int i = 1; i <= 12; i++) if (Input.GetKeyDown((KeyCode)((int)KeyCode.F1 + i - 1))) Send(new TutorialInput(TutorialInputKind.Key, value: i));
            if ((Vector2)Input.mousePosition != previousPointer)
            {
                previousPointer = Input.mousePosition;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(screen, Input.mousePosition, null, out var p))
                {
                    // Screen pivot is centered; convert to top-left logical coordinates.
                    pointerTile = new TutorialTile(Mathf.FloorToInt((p.x + 320) / 32), Mathf.FloorToInt((240 - p.y) / 32));
                    var hit = view.Actors.LastOrDefault(a => p.x + 320 >= a.X * 32 && p.x + 320 < a.X * 32 + 32 && 240 - p.y >= a.Y * 32 && 240 - p.y < a.Y * 32 + 32);
                    if (hit != null) server.Request_TutorialInput(new TutorialInput(TutorialInputKind.HoverUnit, hit.Id));
                }
            }
        }
        private TutorialTile pointerTile = new TutorialTile(10, 7);
        private void LateUpdate()
        {
            if (waitingActive) { LateWaitingLesson(); return; }
            if (!active) return;
            view = server.TutorialSnapshot; if (view == null) { active = false; canvasObject.SetActive(false); music.Stop(); return; }
            Paint();
            if (view.Error.Length > 0) { ShowFault(view.Error, false); return; }
            if (view.Completed && !completionAttempted) TryComplete();
            if (view.MusicStarted && musicRepeats <= 1000 && !music.isPlaying) { music.Stop(); music.Play(); musicRepeats++; }
        }
        private void Send(TutorialInput input)
        {
            if (!active || view == null || view.Completed || (consumedPress && input.Kind != TutorialInputKind.Key && input.Kind != TutorialInputKind.Back)) return;
            server.Request_TutorialInput(input);
        }
        private void TryComplete()
        {
            completionAttempted = true; var reply = server.Request_CompleteTutorial();
            if (!reply.Accepted) { ShowFault("튜토리얼 완료 기록을 저장하지 못했습니다.\n" + reply.Failure, true); return; }
            faultText.transform.parent.gameObject.SetActive(false);
            if (!delivered)
            {
                delivered = true; character = reply.Character; TutorialCompleted?.Invoke(character.Copy()); WorldEntryReady?.Invoke(character.Copy());
                Debug.Log("Original tutorial completed and saved. World entry boundary reached; no town or reward rules have been inferred.", this);
            }
        }
        private void ShowFault(string text, bool canRetry)
        { faultText.text = text; faultText.transform.parent.gameObject.SetActive(true); retrySave.gameObject.SetActive(canRetry); }
        private void Paint()
        {
            dialogueLayer.gameObject.SetActive(!string.IsNullOrWhiteSpace(view.Message));
            var lines = view.Message.Split('|');
            for (int i = 0; i < messageLines.Count; i++) messageLines[i].text = i < lines.Length ? lines[i].Trim(' ') : "";
            foreach (var pair in unitImages) pair.Value.transform.parent.gameObject.SetActive(false);
            var ordered = new List<(RectTransform rect, double bottom)>();
            ordered.AddRange(decorationOrder.Select(d => (d.rect, (double)d.bottom)));
            foreach (var actor in view.Actors)
            {
                var image = unitImages[actor.Id]; var rt = (RectTransform)image.transform.parent; rt.gameObject.SetActive(true);
                Place(rt, new Rect((float)actor.X * 32, (float)actor.Y * 32, 32, 32));
                var texture = art.Actor(actor.Job, actor.Id == 100 ? view.AllyMode : -1);
                int frame = new[] { 0, 1, 2, 1 }[(int)(view.Clock / TutorialPresentationTiming.IdleFrameMs) % 4];
                int row = actor.Direction;
                bool corpse = actor.Dying && !(view.MagicAge >= 0 && (view.MagicCaster == actor.Id || view.MagicTarget == actor.Id));
                if (corpse) { row = actor.DeathCounter < 8 ? 32 : actor.DeathCounter % 4; frame = 0; }
                // Native row32 lies outside these sheets. Do not invent a fallen-body frame.
                image.enabled = texture != null && row * 32 < texture.height;
                image.texture = texture;
                if (image.enabled) image.uvRect = NativeEntryLayout.Uv(texture, new Rect(frame * 32, row * 32, 32, 32));
                var fx = unitEffects[actor.Id]; fx.gameObject.SetActive(corpse || actor.ShowsAttackEffect);
                int fxColumn = corpse ? Math.Min(6, actor.DeathCounter / 3) : actor.AttackCounter / 2 % 4;
                fx.texture = corpse ? art.effects : art.attackEffects;
                fx.uvRect = NativeEntryLayout.Uv(fx.texture, new Rect(fxColumn * 32, corpse ? 0 : 32, corpse ? 31 : 32, corpse ? 31 : 32));
                float fxX = !corpse ? (float)(actor.AttackFxX - actor.X) * 32 : 0;
                float fxY = !corpse ? (float)(actor.AttackFxY - actor.Y) * 32 : 0;
                Place((RectTransform)fx.transform, new Rect(fxX, fxY, corpse ? 31 : 32, corpse ? 31 : 32));
                ordered.Add((rt, actor.Y * 32 + 32));
            }
            foreach (var item in ordered.OrderBy(v => v.bottom)) item.rect.SetAsLastSibling();
            magicFx.gameObject.SetActive(view.MagicAge >= TutorialPresentationTiming.MagicPhaseMs);
            if (view.MagicAge >= 0)
            {
                var from = view.Actors.FirstOrDefault(a => a.Id == view.MagicCaster); var to = view.Actors.FirstOrDefault(a => a.Id == view.MagicTarget);
                if (from != null && to != null)
                {
                    int counter = (int)(view.MagicAge / TutorialPresentationTiming.MagicPhaseMs); bool impact = counter >= 10;
                    float t = Mathf.Clamp01(counter / 10f), a = 1 - t;
                    var source = new Vector2((float)from.X * 32, (float)from.Y * 32);
                    var target = new Vector2((float)to.X * 32, (float)to.Y * 32);
                    var control = (source + target) * .5f + new Vector2(0, -50);
                    var point = source * (a * a * a) + control * (3 * a * a * t + 3 * a * t * t) + target * (t * t * t);
                    Place((RectTransform)magicFx.transform, new Rect(point.x, point.y, 32, 32));
                    int count = impact ? definition.magicImpactFrames : definition.magicTravelFrames;
                    int column = (impact ? counter - 10 : counter) % Math.Max(1, count);
                    magicFx.uvRect = NativeEntryLayout.Uv(art.effects, new Rect((impact ? definition.magicImpactX : definition.magicTravelX) + column * 32, impact ? definition.magicImpactY : definition.magicTravelY, 32, 32));
                }
            }
            displayPosition.gameObject.SetActive(view.DisplayPosition.HasValue);
            if (view.DisplayPosition.HasValue) Place((RectTransform)displayPosition.transform, new Rect(view.DisplayPosition.Value.X * 32, view.DisplayPosition.Value.Y * 32, 31, 31));
            PaintRanges(); PaintMenus(); PaintWindows();
        }
        private void PaintRanges()
        {
            // Only rebuild reachable cells when range, script state or occupancy changes.
            int key = view.Cursor * 31 + (view.Range.HasValue ? (int)view.Range.Value : 0);
            foreach (var a in view.Actors) key = unchecked(key * 31 + a.Tile.GetHashCode() + a.Id);
            if (key == rangeKey) return; rangeKey = key;
            foreach (var image in rangeImages) image.gameObject.SetActive(false);
            if (!view.Range.HasValue) return;
            var unit = view.Actors.FirstOrDefault(a => a.Id == view.RangeUnit); if (unit == null) return;
            var occupied = new HashSet<TutorialTile>(view.Actors.Where(a => a.Id != unit.Id && !a.Dying).Select(a => a.Tile)); int index = 0;
            for (int y = 0; y < definition.map.height; y++) for (int x = 0; x < definition.map.width; x++)
            {
                var tile = new TutorialTile(x, y); int distance = TutorialGeometry.Distance(unit.Tile, tile);
                bool move = view.Range == TutorialAction.Move;
                bool show = move ? definition.map.Path(unit.Tile, tile, 12, occupied) != null : distance <= (view.Range == TutorialAction.Magic ? 3 : 1);
                if (!show || tile == unit.Tile) continue;
                bool target = !move && view.Actors.Any(a => a.Id >= 100 && (a.Id != 100 || view.AllyMode != 0) && !a.Dying && a.Tile == tile);
                var image = rangeImages[index++]; image.gameObject.SetActive(true); image.texture = art.ranges[target ? 3 : move ? 0 : view.Range == TutorialAction.Attack ? 1 : 2];
                Place((RectTransform)image.transform, new Rect(x * 32, y * 32, 31, 31));
            }
        }
        private void PaintMenus()
        {
            var actor = view.Actors.FirstOrDefault(a => a.Id == view.MenuUnit);
            actionMenu.gameObject.SetActive(actor != null && !view.MagicVisible && !view.OrderVisible);
            magicMenu.gameObject.SetActive(view.MagicVisible); orderMenu.gameObject.SetActive(view.OrderVisible);
            int iconPhase = (int)(view.Clock / 300) % 2 * 160;
            spellIcon.uvRect = NativeEntryLayout.Uv(art.icons, new Rect(0, 32 + iconPhase, 32, 32));
            for (int i = 0; i < orderIcons.Count; i++) orderIcons[i].uvRect = NativeEntryLayout.Uv(art.icons, new Rect(i * 32, 64 + iconPhase, 32, 32));
            if (actor == null) return;
            Place(actionMenu, new Rect((float)actor.X * 32 - 32, (float)actor.Y * 32 - 32, 96, 96));
            Place(magicMenu, new Rect((float)actor.X * 32, (float)actor.Y * 32 - 32, 32, 32));
            Place(orderMenu, new Rect((float)actor.X * 32 - 32, (float)actor.Y * 32 - 32, 96, 96));
            foreach (var item in actionIcons)
            {
                bool allowed = TutorialSession.ActionAllowed(actor, item.action);
                int normal = item.action == TutorialAction.Move ? 0 : item.action == TutorialAction.Attack ? 1 : item.action == TutorialAction.Magic ? 2 : item.action == TutorialAction.Item ? 3 : item.action == TutorialAction.Order ? 4 : item.action == TutorialAction.Options ? 5 : item.action == TutorialAction.Rest ? 8 : 12;
                int grey = item.action == TutorialAction.Move ? 4 : item.action == TutorialAction.Attack ? 5 : item.action == TutorialAction.Magic ? 6 : item.action == TutorialAction.Item ? 7 : item.action == TutorialAction.Rest ? 8 : item.action == TutorialAction.Special ? 9 : item.action == TutorialAction.Order ? 10 : 11;
                item.image.uvRect = NativeEntryLayout.Uv(art.icons, new Rect((allowed ? normal : grey) * 32, allowed ? ((int)(view.Clock / 300) % 2) * 160 : 128, 32, 32));
                item.button.interactable = view.MouseEnabled && allowed;
            }
        }
        private static RectTransform Group(RectTransform parent, string name, Rect rect)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1); Place(rt, rect); return rt;
        }
        private static void Place(RectTransform rt, Rect rect) { rt.anchoredPosition = new Vector2(rect.x, -rect.y); rt.sizeDelta = rect.size; }
        private static RawImage Picture(RectTransform parent, string name, Rect rect, Texture texture, Rect? source = null)
        {
            var image = Group(parent, name, rect).gameObject.AddComponent<RawImage>(); image.texture = texture; image.raycastTarget = false;
            if (source.HasValue) image.uvRect = NativeEntryLayout.Uv(texture, source.Value); return image;
        }
        private Text Label(RectTransform parent, string name, Rect rect, string text, TextAnchor alignment = TextAnchor.UpperLeft, int size = 12)
        {
            var label = Group(parent, name, rect).gameObject.AddComponent<Text>(); label.font = font; label.fontSize = size; label.color = Color.white;
            label.alignment = alignment; label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Overflow;
            label.supportRichText = false; label.raycastTarget = false; label.text = text; return label;
        }
        private Button Hit(RectTransform parent, string name, Rect rect, Action action)
        {
            var image = Group(parent, name, rect).gameObject.AddComponent<Image>(); image.color = Color.clear;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => { if (!consumedPress) action(); }); return button;
        }
        private void OnDisable()
        {
            RestoreWaitingCursor();
            waitingActive = false; if (waitingCanvas != null) waitingCanvas.SetActive(false);
            active = false; if (canvasObject != null) canvasObject.SetActive(false); if (music != null) music.Stop();
        }
        private void OnDestroy()
        {
            RestoreWaitingCursor();
            if (entry != null) entry.CharacterReady -= Begin;
            if (canvasObject != null) Destroy(canvasObject);
            if (waitingCanvas != null) Destroy(waitingCanvas);
            if (ownsFont && font != null) Destroy(font);
        }
    }
}
