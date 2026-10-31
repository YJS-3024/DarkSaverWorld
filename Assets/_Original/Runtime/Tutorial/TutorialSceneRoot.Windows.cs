using System;
using System.Collections.Generic;
using System.Linq;
using DarkSaver.Original.Entry;
using UnityEngine;
using UnityEngine.UI;

namespace DarkSaver.Original.Tutorial
{
    public sealed partial class TutorialSceneRoot
    {
        private sealed class WindowView
        {
            public RectTransform Root, Full, CollapsedPanel;
            public RawImage Body, Mini;
            public Text Title, Values, Details, AllySelf, AllyTeacher;
            public Image Ap, Hp, Mp;
            public Button Ally;
            public RectTransform PreviewContents;
            public readonly List<(RectTransform rect, double bottom)> PreviewDepth = new List<(RectTransform, double)>();
            public readonly Dictionary<int, RawImage> PreviewActors = new Dictionary<int, RawImage>();
            public bool FirstCloseConsumed, Collapsed;
        }
        private readonly Dictionary<int, RawImage> miniDots = new Dictionary<int, RawImage>();
        private void BuildWindows()
        {
            for (int i = 0; i < definition.windows.Length; i++)
            {
                var spec = definition.windows[i]; int key = spec.key;
                var window = new WindowView { Root = Group(windowsLayer, "F" + key, new Rect(spec.x, spec.y, spec.width, spec.height)) };
                window.Full = Group(window.Root, "Full panel", new Rect(0, 0, spec.width, spec.height));
                Picture(window.Full, "Native Glib scene" + spec.scene, new Rect(0, 0, spec.width, spec.height), art.windows[i]);
                // A native window owns its hit area; clicks must not select a map tile through it.
                var blocker = window.Full.gameObject.AddComponent<Image>(); blocker.color = Color.clear;
                foreach (var b in spec.buttons)
                {
                    int control = b.id; int controlY = b.y;
                    if (key == 3 && control == 47 && b.y != 72) continue; // Two synthetic tutorial alliance entries only.
                    var button = Hit(window.Full, "Native control " + control + " at " + b.y, new Rect(b.x, b.y, b.width, b.height), () => WindowControl(key, control, controlY));
                    var image = Picture((RectTransform)button.transform, "Native button states", new Rect(0, 0, b.width, b.height), art.buttons, new Rect(b.sx, b.sy, b.width, b.height));
                    button.gameObject.AddComponent<NativeEntryButton>().Configure(image, art.buttons, new Rect(b.sx, b.sy, b.width, b.height), new Rect(b.sx + b.width, b.sy, b.width, b.height), new Rect(b.sx + b.width * 2, b.sy, b.width, b.height));
                    if (key == 3 && control == 47) window.Ally = button;
                }
                var miniTexture = art.minimizedWindows[i];
                if (miniTexture != null)
                {
                    var restore = Hit(window.Root, "Restore native minimized panel", new Rect(0, 0, miniTexture.width, miniTexture.height), () =>
                    { var w = windowViews[key]; w.Collapsed = false; w.Full.gameObject.SetActive(true); w.CollapsedPanel.gameObject.SetActive(false); });
                    Picture((RectTransform)restore.transform, "Native minimized panel", new Rect(0, 0, miniTexture.width, miniTexture.height), miniTexture);
                    window.CollapsedPanel = (RectTransform)restore.transform; window.CollapsedPanel.gameObject.SetActive(false);
                }
                if (key == 1)
                {
                    var viewport = Group(window.Full, "Help text viewport", new Rect(12, 35, 562, 400)); viewport.gameObject.AddComponent<RectMask2D>();
                    window.Details = Label(viewport, "Original help.txt", new Rect(0, 0, 556, 20000), art.help == null ? "" : art.help.text, TextAnchor.UpperLeft, 12);
                    window.Details.rectTransform.sizeDelta = new Vector2(556, Mathf.Max(400, window.Details.preferredHeight));
                    var scroll = window.Full.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = (RectTransform)window.Details.transform;
                    scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 24; scroll.movementType = ScrollRect.MovementType.Clamped;
                }
                if (key == 2)
                {
                    window.Title = Label(window.Full, "Unit name", new Rect(8, 6, 68, 16), "", TextAnchor.MiddleCenter);
                    window.Body = Picture(window.Full, "32x32 live unit", new Rect(44, 79, 32, 32), art.actors[0]);
                    window.Ap = Gauge(window.Full, "Scripted action state", new Rect(30, 143, 80, 8), new Color(.6f, .7f, .9f));
                    window.Hp = Gauge(window.Full, "HP", new Rect(30, 156, 80, 8), Color.red);
                    window.Mp = Gauge(window.Full, "MP", new Rect(30, 169, 80, 8), Color.blue);
                    window.Values = Label(window.Full, "Raw HP MP", new Rect(30, 139, 80, 42), "", TextAnchor.MiddleCenter, 10);
                    window.Details = Label(window.Full, "Combat values", new Rect(30, 181, 80, 28), "", TextAnchor.UpperLeft, 10);
                }
                if (key == 3)
                {
                    window.AllySelf = Label(window.Full, "Native self label", new Rect(14, 51, 105, 19), "나", TextAnchor.MiddleLeft);
                    window.AllyTeacher = Label(window.Full, "Native teacher label", new Rect(14, 70, 105, 19), "사부", TextAnchor.MiddleLeft);
                    window.AllySelf.color = window.AllyTeacher.color = Color.black;
                    var ownCheck = spec.buttons.First(b => b.id == 47 && b.y == 53);
                    Picture(window.Full, "Self alliance checked", new Rect(ownCheck.x, ownCheck.y, ownCheck.width, ownCheck.height), art.buttons,
                        new Rect(ownCheck.sx + ownCheck.width, ownCheck.sy, ownCheck.width, ownCheck.height));
                }
                if (key == 4) window.Details = Label(window.Full, "SETVICTORY", new Rect(14, 31, 152, 28), "", TextAnchor.MiddleCenter);
                if (key == 5)
                {
                    window.Title = Label(window.Full, "Commander", new Rect(8, 6, 66, 16), "", TextAnchor.MiddleLeft);
                    window.Body = Picture(window.Full, "Commander live sprite", new Rect(8, 30, 32, 32), art.actors[0]);
                    window.Ap = Gauge(window.Full, "Action", new Rect(58, 34, 48, 8), new Color(.6f, .7f, .9f));
                    window.Hp = Gauge(window.Full, "HP", new Rect(58, 47, 48, 8), Color.red);
                    window.Mp = Gauge(window.Full, "MP", new Rect(58, 60, 48, 8), Color.blue);
                }
                if (key == 6)
                {
                    window.Mini = Picture(window.Full, "Original301hmap", new Rect(10, 26, 102, 102), art.minimap);
                    foreach (int id in new[] { 0, 100, 400, 410, 1, 2, 800 })
                    {
                        var dot = Picture(window.Full, "Unit marker " + id, new Rect(0, 0, 3, 3), Texture2D.whiteTexture);
                        dot.color = id < 100 ? Color.red : id == 100 ? new Color(.6f, .2f, 1) : Color.blue; miniDots[id] = dot;
                    }
                }
                if (key == 8)
                {
                    var clip = Group(window.Full, "Cursor tile viewport", new Rect(10, 36, 32, 32)); clip.gameObject.AddComponent<RectMask2D>();
                    window.PreviewContents = Group(clip, "Rendered map tile", new Rect(0, 0, 640, 480));
                    Picture(window.PreviewContents, "Ground", new Rect(0, 0, 640, 480), art.ground);
                    for (int d = 0; d < definition.decorations.Length; d++)
                    {
                        var deco = definition.decorations[d]; var image = Picture(window.PreviewContents, "Decoration " + d, new Rect(deco.x, deco.y, deco.width, deco.height), art.decorations[d]);
                        window.PreviewDepth.Add(((RectTransform)image.transform, deco.y + deco.height));
                    }
                    foreach (int id in new[] { 0, 100, 400, 410, 1, 2, 800 })
                        window.PreviewActors[id] = Picture(window.PreviewContents, "Unit " + id, new Rect(0, 0, 32, 32), art.actors[0]);
                    window.Details = Label(window.Full, "TileAttr attack defense movement", new Rect(76, 29, 30, 55), "", TextAnchor.MiddleCenter, 12);
                }
                if (key == 9)
                {
                    // Retreat is demonstrated, not accepted as permission to skip the lesson.
                    foreach (var child in window.Full.GetComponentsInChildren<Button>()) child.interactable = false;
                }
                windowViews[key] = window; window.Root.gameObject.SetActive(false);
            }
        }
        private Image Gauge(RectTransform parent, string name, Rect rect, Color color)
        {
            var image = Group(parent, name, rect).gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return image;
        }
        private static void Fill(Image image, float value, float width)
        { image.rectTransform.sizeDelta = new Vector2(width * Mathf.Clamp01(value), image.rectTransform.sizeDelta.y); }
        private void WindowControl(int key, int control, int y)
        {
            if (consumedPress || view.Completed) return;
            if (key == 3 && control == 47 && y == 72) { Send(new TutorialInput(TutorialInputKind.Ally, 100)); return; }
            if (control == 38 || control == 37)
            {
                var window = windowViews[key];
                if (!window.FirstCloseConsumed) { window.FirstCloseConsumed = true; return; }
                // Do not strand the required checkbox while KEYBOARD is disabled.
                if (key == 3 && view.Wait == "WAITALLYBUTTON") return;
                if (control == 38) Send(new TutorialInput(TutorialInputKind.CloseDialog, value: key));
                else if (window.CollapsedPanel != null) { window.Collapsed = !window.Collapsed; window.Full.gameObject.SetActive(!window.Collapsed); window.CollapsedPanel.gameObject.SetActive(window.Collapsed); }
            }
            if (key == 12)
            {
                int target = control == 52 ? 2 : control == 53 ? 3 : control == 54 ? 4 : control == 55 ? 6 : control == 56 ? 5 : control == 57 ? 8 : control == 58 ? 1 : control == 59 ? 9 : 0;
                if (target != 0) Send(new TutorialInput(TutorialInputKind.DialogButton, value: target));
            }
            if (key == 1 && windowViews[1].Details != null && (control == 39 || control == 40))
            {
                var rect = (RectTransform)windowViews[1].Details.transform; rect.anchoredPosition += new Vector2(0, control == 39 ? 24 : -24);
            }
        }
        private void PaintWindows()
        {
            foreach (var pair in windowViews)
            {
                int key = pair.Key; var window = pair.Value; bool visible = Array.IndexOf(view.Dialogs, key) >= 0;
                window.Root.gameObject.SetActive(visible); if (!visible) continue;
                var actor = view.Actors.FirstOrDefault(a => a.Id == (key == 5 ? 0 : view.HoverUnit));
                if (key == 2 || key == 5)
                {
                    if (actor == null) continue;
                    window.Title.text = actor.Id == 0 ? character.name : actor.Id == 100 ? "사부" : actor.Job.ToString();
                    window.Body.texture = art.Actor(actor.Job, actor.Id == 100 ? view.AllyMode : -1);
                    window.Body.uvRect = unitImages[actor.Id].uvRect;
                    float width = key == 2 ? 80 : 48;
                    // Normalize only the explicit demonstration literals to the largest literal in the script.
                    // This display scale is not a general AP maximum/recovery or action-cost rule.
                    Fill(window.Ap, actor.Tick == 99 ? 1 : actor.Tick <= 0 ? 0 : actor.Tick / (float)scriptedTickScale, width);
                    Fill(window.Hp, actor.MaxHp > 0 ? actor.Hp / (float)actor.MaxHp : 0, width);
                    Fill(window.Mp, actor.MaxMp > 0 ? actor.Mp / (float)actor.MaxMp : 0, width);
                    if (window.Values != null) window.Values.text = "\n" + actor.Hp + "/" + actor.MaxHp + "\n" + actor.Mp + "/" + actor.MaxMp;
                    if (window.Details != null) window.Details.text = "\n" + actor.Attack + "       " + actor.Defense;
                }
                else if (key == 3)
                {
                    window.Ally.interactable = view.Wait == "WAITALLYBUTTON";
                    // Native checkbox is a toggle state, not horizontal button hover strip.
                    var image = window.Ally.GetComponentInChildren<RawImage>();
                    var spec = Array.Find(definition.windows, w => w.key == 3).buttons.First(b => b.id == 47 && b.y == 72);
                    var behaviour = window.Ally.GetComponent<NativeEntryButton>(); behaviour.enabled = false;
                    image.uvRect = NativeEntryLayout.Uv(art.buttons, new Rect(spec.sx + (view.AllyMode == 0 ? spec.width : 0), spec.sy, spec.width, spec.height));
                }
                else if (key == 4) window.Details.text = view.Victory;
                else if (key == 6)
                {
                    foreach (var dot in miniDots.Values) dot.gameObject.SetActive(false);
                    foreach (var a in view.Actors)
                    { var dot = miniDots[a.Id]; dot.gameObject.SetActive(true); Place((RectTransform)dot.transform, new Rect(10 + (float)a.X / 20 * 102, 26 + (float)a.Y / 15 * 102, 3, 3)); }
                }
                else if (key == 8)
                {
                    var p = definition.map.Contains(pointerTile) ? pointerTile : new TutorialTile(10, 7);
                    window.PreviewContents.anchoredPosition = new Vector2(-p.X * 32, p.Y * 32);
                    foreach (var image in window.PreviewActors.Values) image.gameObject.SetActive(false);
                    var depth = new List<(RectTransform rect, double bottom)>(window.PreviewDepth);
                    foreach (var a in view.Actors)
                    {
                        var image = window.PreviewActors[a.Id]; image.gameObject.SetActive(true); image.texture = unitImages[a.Id].texture; image.uvRect = unitImages[a.Id].uvRect;
                        image.enabled = unitImages[a.Id].enabled;
                        Place((RectTransform)image.transform, new Rect((float)a.X * 32, (float)a.Y * 32, 32, 32));
                        depth.Add(((RectTransform)image.transform, a.Y * 32 + 32));
                    }
                    foreach (var item in depth.OrderBy(d => d.bottom)) item.rect.SetAsLastSibling();
                    window.Details.text = TerrainText(definition.map.Terrain(p, 0)) + "\n" + TerrainText(definition.map.Terrain(p, 1)) + "\n" + TerrainText(definition.map.Terrain(p, 2));
                }
            }
        }
        private static string TerrainText(int value) => Math.Abs((long)value) == 99 ? "**" : value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
