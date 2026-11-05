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
        private const string OriginalRosterPath=BasePath+"Data/Placements/scenario_monster_placements.json";
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
        private bool pickBattleEntry;
        private int entryJob;
        private Rect viewport;
        private bool fitRequested;
        [SerializeField] private bool layoutReadOnly;
        private string layoutNotice="";
        private string failedMapKey="",mapLoadStage="",directMapKey="";
        private int queuedSave;
        private bool saveScheduled;
        private void QueueEditorSave(bool gatewaysOnly)
        {
            queuedSave=Math.Max(queuedSave,gatewaysOnly?1:2);
            GUI.FocusControl(null);EditorGUIUtility.editingTextField=false;Repaint();
        }
        private void DispatchQueuedSave()
        {
            // DelayedIntField/other editors have now seen focus loss during Repaint.
            if(queuedSave==0||saveScheduled||Event.current.type!=EventType.Repaint)return;
            int action=queuedSave;queuedSave=0;saveScheduled=true;
            EditorApplication.delayCall+=()=>
            {
                if(this==null)return;
                try{if(action==1)SaveGateways();else SaveChanges();}
                finally{saveScheduled=false;Repaint();}
            };
        }

        private const string MapNamesPath=BasePath+"Data/Tables/scene_maps.csv";
        private Dictionary<string,string> mapNames=new Dictionary<string,string>();
        private long mapNamesStamp=long.MinValue,mapNamesSize=-1;
        private double mapNamesNextPoll;
        private string mapNamesError="";
        private void RefreshMapNames(bool force=false)
        {
            try
            {
                var file=new FileInfo(MapNamesPath);long stamp=file.Exists?file.LastWriteTimeUtc.Ticks:0,size=file.Exists?file.Length:0;
                if(!force&&stamp==mapNamesStamp&&size==mapNamesSize)return;
                mapNamesStamp=stamp;mapNamesSize=size;
                mapNames=file.Exists?ScenarioLayoutData.ReadMapNames(File.ReadAllText(MapNamesPath)):new Dictionary<string,string>();
                mapNamesError="";
            }
            catch(Exception e)when(e is IOException||e is UnauthorizedAccessException||e is ArgumentException)
            {mapNamesError="맵 이름표를 읽지 못해 이전 이름을 유지합니다. "+e.Message;}
            Repaint();
        }
        private void OnInspectorUpdate()
        {
            if(EditorApplication.timeSinceStartup<mapNamesNextPoll)return;
            mapNamesNextPoll=EditorApplication.timeSinceStartup+.5;RefreshMapNames();
        }


        [MenuItem("DarkSaver/원작 도구/시나리오 편집툴")]
        public static void Open()
        { var window = GetWindow<ScenarioEditorWindow>(); window.titleContent = new GUIContent("시나리오 편집툴"); window.minSize = new Vector2(1000, 650); window.Show(); }
        private static string ReadOptional(string path) => File.Exists(path) ? File.ReadAllText(path) : null;
        private string Category(string key)
        {
            int id=int.Parse(key);
            if(id>=101&&id<=107)return "수련장";
            if(id>=201&&id<=207)return "대련장";
            if(id==301)return "튜토리얼";
            var map=gatewayCatalog?.maps?.FirstOrDefault(m=>m.key==key);
            if(map?.kind=="town")return "마을";
            if(map?.kind=="interior")return "실내";
            return id>=1000?"월드/필드":"시나리오";
        }
        private string Title(string key)
        {
            string label=Category(key)+" "+key;
            var map=gatewayCatalog?.maps?.FirstOrDefault(m=>m.key==key);
            string title=mapNames.TryGetValue(key,out var custom)?custom:map?.title;
            return string.IsNullOrWhiteSpace(title)||title==key?label:title+" ["+label+"]";
        }
        // Unity Popup treats ASCII '/' as a submenu delimiter, even in map titles.
        private static string FlatPopupLabel(string value) => (value??"").Replace("/"," · ");
        private string MapLabel(int id)
        {
            var map=gatewayCatalog?.maps?.FirstOrDefault(m=>m.id==id);
            return map==null?(id<0?"맵 미선택":"맵 "+id):Title(map.key);
        }
        private void OnEnable()
        {
            titleContent = new GUIContent("시나리오 편집툴"); minSize = new Vector2(1000,650);
            RefreshMapNames(true);
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
        { if (mapTexture != null) DestroyImmediate(mapTexture); mapTexture = null; pictures.Clear(); }
        private ScenarioSource ReadMapSource(string key,byte[] map,int width,int height)
        {
            string folder=DecodedFolder+key+"set.arc/",prefix=folder+key;
            string set=ReadOptional(prefix+".set.txt"),pos=ReadOptional(prefix+".pos.txt");
            if((set==null||pos==null)&&ScenarioLayoutData.Hash(map)!=MapInfo(key).hash)throw new IOException("원본 배치 목록과 대조할 지도 버전이 다릅니다.");
            return ScenarioLayoutData.ReadSourceWithCatalog(key,width,height,map,set,pos,ReadOptional(prefix+".evt.txt"),
                (Directory.Exists(folder)?Directory.GetFiles(folder,"*.de?.txt"):Array.Empty<string>()).Where(p=>p.EndsWith(".deo.txt",StringComparison.Ordinal)||p.EndsWith(".dee.txt",StringComparison.Ordinal)).OrderBy(p=>p.EndsWith(".deo.txt")?0:1).ToArray(),
                (set==null||pos==null)&&File.Exists(OriginalRosterPath)?File.ReadAllBytes(OriginalRosterPath):null,new HashSet<int>(jobs.Keys));
        }
        private void LoadMap(string key, bool preserve = false)
        {
            Texture2D pendingTexture=null;
            try
            {
                mapLoadStage="지도 파일: "+MapInfo(key).nativePath;
                var map = File.ReadAllBytes(MapInfo(key).nativePath); var size = NativeMapDecoder.Dimensions(map);
                mapLoadStage="원본 배치/지도 해시 확인";
                var nextSource = ReadMapSource(key,map,size.width,size.height);
                string path = LayoutFolder + "/" + key + ".json"; var bytes = File.Exists(path) ? File.ReadAllBytes(path) : null;
                mapLoadStage="사용자 배치 읽기: "+path;
                bool readOnly=false;string notice="";
                var next = preserve ? document : ScenarioLayoutData.LoadForPreview(bytes,nextSource.layout,new HashSet<int>(jobs.Keys),out readOnly,out notice);
                ScenarioLayoutData.Validate(next, nextSource.layout, new HashSet<int>(jobs.Keys));
                mapLoadStage="타일/장식 렌더";
                var pixels = NativeMapDecoder.Render(map, id =>
                {
                    string graphics=(MapInfo(key).legacy?BasePath+"SourceData/KaosiaLegacy/":NativeFolder)+"mdsr"+id+".grs.bytes";
                    mapLoadStage="타일/장식 파일: "+graphics;return File.ReadAllBytes(graphics);
                });
                mapLoadStage="미리보기 텍스처 "+pixels.width+"×"+pixels.height+" (GPU 최대 "+SystemInfo.maxTextureSize+")";
                if(pixels.width>SystemInfo.maxTextureSize||pixels.height>SystemInfo.maxTextureSize)throw new IOException("지도 크기가 현재 GPU의 최대 텍스처 크기를 초과합니다.");
                var texture = new Texture2D(pixels.width, pixels.height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
                pendingTexture=texture;
                var flipped = new byte[pixels.rgba.Length]; int stride = pixels.width * 4;
                for (int y = 0; y < pixels.height; y++) Buffer.BlockCopy(pixels.rgba, y * stride, flipped, (pixels.height - 1 - y) * stride, stride);
                texture.LoadRawTextureData(flipped); texture.Apply(false, true);
                if (mapTexture != null) DestroyImmediate(mapTexture); mapTexture = texture;
                source = nextSource; document = next; mapKey = key; dragging = false;pickBattleEntry=false;
                if (!preserve) { savedHash = bytes == null ? null : ScenarioLayoutData.Hash(bytes); dirty = false; layoutReadOnly=readOnly;layoutNotice=notice;undo.Clear(); redo.Clear(); selected = -1; Fit(); }
                localEntry = null;
                if (key == "101")
                {
                    var world = JsonUtility.FromJson<WorldDefinition>(File.ReadAllText(BasePath + "Data/World/KaosiaWorld.json"));
                    var gate = world.Map(1000).portals.FirstOrDefault(p => p.targetMap == 101);
                    if (gate != null) localEntry = new ScenarioMarker { x = gate.spawnX, y = gate.spawnY, kind = "local", label = "현재 로컬 수련장 진입점", file = "KaosiaWorld.json" };
                }
                mapLoadStage="연출 대본 읽기";
                scriptIndex = 0; ReadScript(); loadError = failedMapKey = ""; message = layoutNotice; hasUnsavedChanges = dirty || dirtyGateways;
                saveChangesMessage = "시나리오 " + mapKey + "의 편집 배치를 저장할까요? 원본 SET/POS는 변경하지 않습니다.";
                Repaint();
            }
            catch(ExitGUIException){throw;}
            catch (Exception e)
            {
                if(pendingTexture!=null&&pendingTexture!=mapTexture)DestroyImmediate(pendingTexture);
                failedMapKey=key;
                loadError="맵 "+key+" 선택 실패\n단계: "+mapLoadStage+"\n"+e.GetType().Name+": "+e.Message;
                message=loadError;Debug.LogException(new IOException(loadError,e));Repaint();
            }
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
        private void SelectMap(string key) { if ((key != mapKey || loadError.Length>0) && MayLeave()) LoadMap(key); }
        private void Remember()
        { undo.Add(ScenarioLayoutData.Encode(document)); if (undo.Count > 100) undo.RemoveAt(0); redo.Clear(); MarkDirty(); }
        private void MarkDirty() { dirty = hasUnsavedChanges = true; message = "저장하지 않은 변경"; Repaint(); }
        private void History(bool back)
        {
            if(layoutReadOnly)return;
            var from = back ? undo : redo; var to = back ? redo : undo; if (from.Count == 0) return;
            to.Add(ScenarioLayoutData.Encode(document)); document = ScenarioLayoutData.Decode(from[from.Count - 1]); from.RemoveAt(from.Count - 1); selected = -1; dragging = false; MarkDirty();
        }
        private bool Save()
        {
            if (source == null || loadError.Length > 0) return false;
            if(layoutReadOnly){message="기존 사용자 배치 파일을 보호하고 있습니다. 원본 배치 불러오기 후 저장하세요.";return false;}
            bool layoutSaved=false;
            try
            {
                if (ScenarioLayoutData.Hash(File.ReadAllBytes(MapInfo(mapKey).nativePath)) != source.layout.mapHash)
                    throw new IOException("원본 맵이 변경되었습니다. 다시 불러온 뒤 편집하세요.");
                string prefix = DecodedFolder + mapKey + "set.arc/" + mapKey;
                foreach (var pair in new[] { (ext: "set", hash: source.layout.setHash), (ext: "pos", hash: source.layout.posHash) })
                { string text = ReadOptional(prefix + "." + pair.ext + ".txt"); if ((text == null ? "" : ScenarioLayoutData.Hash(Encoding.UTF8.GetBytes(text))) != pair.hash) throw new IOException("원본 SET/POS가 변경되었습니다. 다시 불러온 뒤 편집하세요."); }
                if(source.layout.catalogHash!=null&&(!File.Exists(OriginalRosterPath)||ScenarioLayoutData.Hash(File.ReadAllBytes(OriginalRosterPath))!=source.layout.catalogHash))
                    throw new IOException("원본 배치 목록이 변경되었습니다. 다시 불러온 뒤 편집하세요.");
                savedHash = ScenarioLayoutData.Save(LayoutFolder, document, source.layout, new HashSet<int>(jobs.Keys), string.IsNullOrEmpty(savedHash) ? null : savedHash);
                layoutSaved=true;
                document.edited = !ScenarioLayoutData.SameUnits(document, source.layout);
                if(int.TryParse(mapKey,out int trainingMap)&&trainingMap>=101&&trainingMap<=107)
                {
                    string testFolder=BasePath+"Resources/TrainingTestLayouts";
                    Directory.CreateDirectory(testFolder);
                    string exportPath=testFolder+"/"+mapKey+".json";
                    string previousExport=File.Exists(exportPath)?ScenarioLayoutData.Hash(File.ReadAllBytes(exportPath)):null;
                    string exportedHash=ScenarioLayoutData.Save(testFolder,document,source.layout,new HashSet<int>(jobs.Keys),previousExport);
                    if(exportedHash!=savedHash)throw new IOException("편집 배치와 실행용 배치의 저장 내용이 다릅니다.");
                    AssetDatabase.ImportAsset(exportPath,ImportAssetOptions.ForceUpdate);
                    string catalogPath=BasePath+"Resources/TrainingTestLayoutCatalog.asset";
                    var catalog=AssetDatabase.LoadAssetAtPath<DarkSaver.Original.Entry.TrainingTestLayoutCatalog>(catalogPath);
                    if(catalog==null){catalog=ScriptableObject.CreateInstance<DarkSaver.Original.Entry.TrainingTestLayoutCatalog>();AssetDatabase.CreateAsset(catalog,catalogPath);}
                    catalog.Set(trainingMap,AssetDatabase.LoadAssetAtPath<TextAsset>(exportPath));
                    EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
                }
                dirty = false; hasUnsavedChanges = dirtyGateways;
                message = "저장: " + LayoutFolder + "/" + mapKey + ".json";
                try {AssetDatabase.Refresh();}
                catch(Exception e){message+=" / 파일 저장 완료, 에셋 새로고침 경고: "+e.Message;}
                return true;
            }
            catch (Exception e)
            {
                if(layoutSaved){dirty=hasUnsavedChanges=true;message="편집 배치는 저장했지만 실행용 배치 내보내기에 실패했습니다. 다시 저장하세요: "+e.Message;}
                else message=e.Message;
                return false;
            }
        }
        public override void SaveChanges() { if(dirty&&!Save())return;if(dirtyGateways&&!SaveGateways())return;base.SaveChanges(); }
        public override void DiscardChanges() { dirty = dirtyGateways = false; base.DiscardChanges(); }
        private void DeleteSelected()
        {
            if (mode != 0 || layoutReadOnly || document == null || !document.units.Any(u => u.unit == selected)) return;
            Remember(); document.units = document.units.Where(u => u.unit != selected).ToArray(); selected = -1; dragging = false;
        }
        private void AddAt(int x, int y)
        {
            if (mode != 0 || layoutReadOnly || !jobs.ContainsKey(addJob)) return;
            Remember(); var unit = new ScenarioPlacement { unit = ScenarioLayoutData.NextUnit(document), job = addJob, x = x, y = y };
            document.units = document.units.Concat(new[] { unit }).ToArray(); selected = unit.unit;
        }
        private void RestoreOriginalLayout()
        {
            try
            {
                var map=File.ReadAllBytes(MapInfo(mapKey).nativePath);var size=NativeMapDecoder.Dimensions(map);
                var original=ReadMapSource(mapKey,map,size.width,size.height);
                ScenarioLayoutData.Validate(original.layout,original.layout,new HashSet<int>(jobs.Keys));
                if(original.layout.units.Length==0)
                {
                    message=original.layout.source=="original-missing"?mapKey+": 원본 SET/POS 미확보 ("+DecodedFolder+mapKey+"set.arc/). 현재 편집 배치는 유지합니다.":mapKey+": 원본 SET/POS의 전투 배치가 비어 있습니다. 현재 편집 배치는 유지합니다.";
                    Repaint();return;
                }
                if(original.layout.mapHash!=source.layout.mapHash)throw new IOException("원본 지도가 변경되었습니다. 다시 열기를 먼저 실행하세요.");
                Remember();source=original;document=original.layout.Copy();selected=-1;mode=0;adding=dragging=pickNpc=pickSpawn=dragGateway=false;
                pictures.Clear();scriptIndex=0;ReadScript();loadError="";layoutReadOnly=false;layoutNotice="";Fit();message=mapKey+": 원본 전투 "+document.units.Length+"개 재로딩 · 화면 맞춤 · 저장 전";
            }
            catch(Exception e){message="원본 배치 재로딩 실패: "+e.Message;}
        }
        private Texture2D ActorPicture(string job)
        {
            int id; if (!int.TryParse(job, out id)) return null; // COM0 is not an invented class.
            if (!jobs.TryGetValue(id, out var info) || !picturePaths.ContainsKey(info.picture)) return null;
            if (pictures.TryGetValue(info.picture, out var cached)) return cached;
            // Reuse the verified black-keyed native sheets; do not decode/read GPU pixels in OnGUI.
            var prepared=BasePath+"Art/Campaign/Picture"+info.picture+".png";
            var result=AssetDatabase.LoadAssetAtPath<Texture2D>(prepared);
            if(result!=null)pictures[info.picture]=result;
            return result;
        }
        private static void DrawActorPicture(Rect rect,Texture2D sheet)
        {
            int w=sheet.width/3,h=sheet.height/4;
            GUI.DrawTextureWithTexCoords(rect,sheet,new Rect(0,1f-(float)(2*h)/sheet.height,(float)w/sheet.width,(float)h/sheet.height),true);
        }
    }
}
