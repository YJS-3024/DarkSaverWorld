using System;
using DarkSaver.Original.Maps;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using DarkSaver.Original.World;
using DarkSaver.Original.Campaign;

namespace DarkSaver.Original.Editor.Scenarios
{
    public sealed partial class ScenarioEditorWindow : EditorWindow
    {
        internal const string BasePath = "Assets/_Original/";
        internal const string LayoutFolder = BasePath + "Editor/ScenarioLayouts";
        private const string NativeFolder = BasePath + "SourceData/Native/map/";
        private const string DecodedFolder = BasePath + "SourceData/Decoded/map/";
        [SerializeField] private string mapKey = "01", savedHash;
        [SerializeField] private ScenarioLayout document;
        [SerializeField] private bool dirty;
        [SerializeField] private int mode, category, selected = -1;
        [SerializeField] private float zoom = 1;
        [SerializeField] private Vector2 pan;
        private ScenarioSource source;
        private string[] keys = Array.Empty<string>();
        private readonly Dictionary<int, ScenarioJob> jobs = new Dictionary<int, ScenarioJob>();
        private readonly Dictionary<int, string> picturePaths = new Dictionary<int, string>();
        private readonly Dictionary<int, Texture2D> pictures = new Dictionary<int, Texture2D>();
        private readonly List<byte[]> undo = new List<byte[]>(), redo = new List<byte[]>();
        private Texture2D mapTexture;
        private string search = "", jobSearch = "", message = "", loadError = "", scriptText = "";
        private int scriptIndex, scriptLine, addJob = 401;
        private Vector2 mapScroll, inspectorScroll, unitScroll;
        private bool grid = true, labels = true, entries = true, chests = true, quests = true, groups = true, allJobs;
        private ScenarioActor[] actors = Array.Empty<ScenarioActor>();
        private ScenarioMarker localEntry;
        private Rect viewport;

        [MenuItem("DarkSaver/원작 도구/시나리오 편집툴")]
        public static void Open()
        { var window = GetWindow<ScenarioEditorWindow>(); window.titleContent = new GUIContent("시나리오 편집툴"); window.minSize = new Vector2(1000, 650); window.Show(); }
        private static string ReadOptional(string path) => File.Exists(path) ? File.ReadAllText(path) : null;
        private static string Category(string key)
        { int id = int.Parse(key); return id >= 101 && id <= 107 ? "수련장" : id >= 201 && id <= 207 ? "대련장" : id == 301 ? "튜토리얼" : id >= 1000 ? "월드/필드" : "시나리오"; }
        private static string Title(string key) => Category(key) + " " + key;
        private void OnEnable()
        {
            titleContent = new GUIContent("시나리오 편집툴"); minSize = new Vector2(1000,650);
            try
            {
                LoadGateways(dirtyGateways && gatewayDocument != null);
                keys = gatewayCatalog.maps.Select(m=>m.key).OrderBy(int.Parse).ToArray();
                jobs.Clear(); var rows = ScenarioLayoutData.Csv(File.ReadAllText(BasePath + "Data/Tables/units_all.csv")).ToArray();
                int job = Array.IndexOf(rows[0], "직업"), picture = Array.IndexOf(rows[0], "직그림"), name = Array.IndexOf(rows[0], "이름"), kind = Array.IndexOf(rows[0], "category");
                foreach (var r in rows.Skip(1)) if (r.Length == rows[0].Length)
                { int id = int.Parse(r[job]); jobs[id] = new ScenarioJob { id = id, picture = int.Parse(r[picture]), name = r[name].Replace('_', ' '), category = r[kind] }; }
                picturePaths.Clear();
                foreach (var file in Directory.GetFiles(BasePath + "Art/Archives/gameres", "*r.bmp.png", SearchOption.AllDirectories))
                    if (int.TryParse(Path.GetFileName(file).Split('r')[0], out int id)) picturePaths[id] = file;
                if (!keys.Contains(mapKey)) mapKey = keys.First();
                LoadMap(mapKey, dirty && document != null);
            }
            catch (Exception e) { loadError = e.Message; }
        }
        private void OnDisable()
        { if (mapTexture != null) DestroyImmediate(mapTexture); mapTexture = null; foreach (var t in pictures.Values) if (t != null) DestroyImmediate(t); pictures.Clear(); }
        private void LoadMap(string key, bool preserve = false)
        {
            try
            {
                var map = File.ReadAllBytes(MapInfo(key).nativePath); var size = NativeMapDecoder.Dimensions(map);
                string folder = DecodedFolder + key + "set.arc/", prefix = folder + key;
                var nextSource = ScenarioLayoutData.ReadSource(key, size.width, size.height, map, ReadOptional(prefix + ".set.txt"), ReadOptional(prefix + ".pos.txt"), ReadOptional(prefix + ".evt.txt"),
                    (Directory.Exists(folder) ? Directory.GetFiles(folder, "*.de?.txt") : Array.Empty<string>()).Where(p => p.EndsWith(".deo.txt", StringComparison.Ordinal) || p.EndsWith(".dee.txt", StringComparison.Ordinal)).OrderBy(p => p.EndsWith(".deo.txt") ? 0 : 1).ToArray());
                string path = LayoutFolder + "/" + key + ".json"; var bytes = File.Exists(path) ? File.ReadAllBytes(path) : null;
                var next = preserve ? document : bytes == null ? nextSource.layout.Copy() : ScenarioLayoutData.Decode(bytes);
                ScenarioLayoutData.Validate(next, nextSource.layout, new HashSet<int>(jobs.Keys));
                var pixels = NativeMapDecoder.Render(map, id => File.ReadAllBytes((MapInfo(key).legacy ? BasePath+"SourceData/KaosiaLegacy/" : NativeFolder) + "mdsr" + id + ".grs.bytes"));
                var texture = new Texture2D(pixels.width, pixels.height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
                var flipped = new byte[pixels.rgba.Length]; int stride = pixels.width * 4;
                for (int y = 0; y < pixels.height; y++) Buffer.BlockCopy(pixels.rgba, y * stride, flipped, (pixels.height - 1 - y) * stride, stride);
                texture.LoadRawTextureData(flipped); texture.Apply(false, true);
                if (mapTexture != null) DestroyImmediate(mapTexture); mapTexture = texture;
                source = nextSource; document = next; mapKey = key; dragging = false;
                if (!preserve) { savedHash = bytes == null ? null : ScenarioLayoutData.Hash(bytes); dirty = false; undo.Clear(); redo.Clear(); selected = -1; Fit(); }
                localEntry = null;
                if (key == "101")
                {
                    var world = JsonUtility.FromJson<WorldDefinition>(File.ReadAllText(BasePath + "Data/World/KaosiaWorld.json"));
                    var gate = world.Map(1000).portals.FirstOrDefault(p => p.targetMap == 101);
                    if (gate != null) localEntry = new ScenarioMarker { x = gate.spawnX, y = gate.spawnY, kind = "local", label = "현재 로컬 수련장 진입점", file = "KaosiaWorld.json" };
                }
                scriptIndex = 0; ReadScript(); loadError = ""; message = ""; hasUnsavedChanges = dirty || dirtyGateways;
                saveChangesMessage = "시나리오 " + mapKey + "의 편집 배치를 저장할까요? 원본 SET/POS는 변경하지 않습니다.";
                Repaint();
            }
            catch (Exception e) { loadError = e.Message; Repaint(); }
        }
        private void ReadScript()
        {
            scriptText = source.scripts.Length == 0 ? "" : File.ReadAllText(source.scripts[Mathf.Clamp(scriptIndex, 0, source.scripts.Length - 1)]);
            var lines = ScenarioLayoutData.Lines(scriptText);
            int first = Array.FindIndex(lines, line => ScenarioLayoutData.Tokens(line).FirstOrDefault() == "CHARPOS");
            scriptLine = first < 0 ? 0 : first + 1; UpdateActors();
        }
        private void UpdateActors() => actors = source.scripts.Length == 0 ? Array.Empty<ScenarioActor>() : ScenarioLayoutData.Cutscene(scriptText, Path.GetFileName(source.scripts[scriptIndex]), mapKey, scriptLine);
        private bool MayLeave()
        {
            if (!dirty) return true;
            int choice = EditorUtility.DisplayDialogComplex("저장하지 않은 배치", "시나리오 " + mapKey + "의 편집을 저장할까요?", "저장", "취소", "저장하지 않음");
            return choice == 0 ? Save() : choice == 2;
        }
        private void SelectMap(string key) { if (key != mapKey && MayLeave()) LoadMap(key); }
        private void Remember()
        { undo.Add(ScenarioLayoutData.Encode(document)); if (undo.Count > 100) undo.RemoveAt(0); redo.Clear(); MarkDirty(); }
        private void MarkDirty() { dirty = hasUnsavedChanges = true; message = "저장하지 않은 변경"; Repaint(); }
        private void History(bool back)
        {
            var from = back ? undo : redo; var to = back ? redo : undo; if (from.Count == 0) return;
            to.Add(ScenarioLayoutData.Encode(document)); document = ScenarioLayoutData.Decode(from[from.Count - 1]); from.RemoveAt(from.Count - 1); selected = -1; dragging = false; MarkDirty();
        }
        private bool Save()
        {
            if (source == null || loadError.Length > 0) return false;
            try
            {
                if (ScenarioLayoutData.Hash(File.ReadAllBytes(MapInfo(mapKey).nativePath)) != source.layout.mapHash)
                    throw new IOException("원본 맵이 변경되었습니다. 다시 불러온 뒤 편집하세요.");
                string prefix = DecodedFolder + mapKey + "set.arc/" + mapKey;
                foreach (var pair in new[] { (ext: "set", hash: source.layout.setHash), (ext: "pos", hash: source.layout.posHash) })
                { string text = ReadOptional(prefix + "." + pair.ext + ".txt"); if ((text == null ? "" : ScenarioLayoutData.Hash(Encoding.UTF8.GetBytes(text))) != pair.hash) throw new IOException("원본 SET/POS가 변경되었습니다. 다시 불러온 뒤 편집하세요."); }
                savedHash = ScenarioLayoutData.Save(LayoutFolder, document, source.layout, new HashSet<int>(jobs.Keys), string.IsNullOrEmpty(savedHash) ? null : savedHash);
                document.edited = !ScenarioLayoutData.SameUnits(document, source.layout); dirty = false; hasUnsavedChanges = dirtyGateways;
                AssetDatabase.Refresh(); message = "저장: " + LayoutFolder + "/" + mapKey + ".json"; return true;
            }
            catch (Exception e) { message = e.Message; return false; }
        }
        public override void SaveChanges() { if(dirty&&!Save())return;if(dirtyGateways&&!SaveGateways())return;base.SaveChanges(); }
        public override void DiscardChanges() { dirty = dirtyGateways = false; base.DiscardChanges(); }
        private void DeleteSelected()
        {
            if (mode != 0 || document == null || !document.units.Any(u => u.unit == selected)) return;
            Remember(); document.units = document.units.Where(u => u.unit != selected).ToArray(); selected = -1; dragging = false;
        }
        private void AddAt(int x, int y)
        {
            if (mode != 0 || !jobs.ContainsKey(addJob)) return;
            Remember(); var unit = new ScenarioPlacement { unit = ScenarioLayoutData.NextUnit(document), job = addJob, x = x, y = y };
            document.units = document.units.Concat(new[] { unit }).ToArray(); selected = unit.unit;
        }
        private Texture2D ActorPicture(string job)
        {
            int id; if (!int.TryParse(job, out id)) return null; // COM0 is not an invented class.
            if (!jobs.TryGetValue(id, out var info) || !picturePaths.TryGetValue(info.picture, out var path)) return null;
            if (pictures.TryGetValue(info.picture, out var cached)) return cached;
            var sheet = new Texture2D(2,2,TextureFormat.RGBA32,false) { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                if (!ImageConversion.LoadImage(sheet, File.ReadAllBytes(path), false)) return null;
                int w = sheet.width / 3, h = sheet.height / 4; var pixels = sheet.GetPixels32(); var frame = new Color32[w * h];
                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                { var c = pixels[(sheet.height - h * 2 + y) * sheet.width + x]; if (c.r == 0 && c.g == 0 && c.b == 0) c.a = 0; frame[y * w + x] = c; }
                var result = new Texture2D(w,h,TextureFormat.RGBA32,false) { filterMode = FilterMode.Point, hideFlags = HideFlags.HideAndDontSave };
                result.SetPixels32(frame); result.Apply(false, true); pictures[info.picture] = result; return result;
            }
            finally { DestroyImmediate(sheet); }
        }
    }
}
