using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using DarkSaver.Core;
using DarkSaver.Events;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace DarkSaver.Editor
{
    // Preview scenes never replace or save the user's open scenes.
    public sealed class ScenarioPlacementWindow : EditorWindow
    {
        [Serializable] private sealed class UnitList { public Scenario01Unit[] units; }
        [Serializable] private sealed class OriginalIndex { public OriginalMap[] maps=Array.Empty<OriginalMap>(); }
        [Serializable] private sealed class OriginalMap { public string key="",title="",mapScene="";public int width=0,height=0; }
        [Serializable] private sealed class PartyPosition { public int id=0,x=0,y=0; }
        [Serializable] private sealed class OriginalDetails { public PartyPosition[] partyPositions=Array.Empty<PartyPosition>();public bool placementEdited=false; }
        [Serializable] private sealed class OriginalUnitList { public OriginalUnit[] units; }
        [Serializable] private sealed class OriginalUnit
        {
            public int id,job,x,y,hp,mp,attack,defense,magicAttack,magicDefense;public uint experience;public string name;
            public static OriginalUnit From(Scenario01Unit u)=>new OriginalUnit{id=u.id,job=u.job,x=u.x,y=u.y,hp=u.hp,mp=u.mp,
                attack=u.attack,defense=u.defense,magicAttack=u.magicAttack,magicDefense=u.magicDefense,experience=u.experience,name=u.name};
        }
        [SerializeField] private int scenarioIndex;
        private int selected=-1,job=301;
        [SerializeField] private string path,original;
        private string error;
        [SerializeField] private Scenario01Definition data;
        private Scene preview;
        private Camera cameraView;
        private RenderTexture texture;
        private Tilemap ground;
        private Vector2 scroll;
        [SerializeField] private bool dirty;
        private bool dragging,showLocations=true,showLabels,showAi;
        private ScenarioPlacementTroops.AiRule[] aiRules=Array.Empty<ScenarioPlacementTroops.AiRule>();
        [SerializeField] private int placementMode; // 0: battle, 1: intro (exclusive)
        private float zoom=1;
        private bool showGroups=true;
        private int referenceFormation;
        private bool showFormationReference;
        private string isolatedGroup;
        private Rect canvas;
        private string[] names;
        private ScenarioDefinition[] maps;
        private OriginalMap[] originalMaps=Array.Empty<OriginalMap>();
        private PartyPosition[] partyPositions=Array.Empty<PartyPosition>();
        private bool originalEdited;
        private bool IsOriginalLayout=>maps!=null && maps[scenarioIndex].Key.StartsWith("Original",StringComparison.Ordinal);
        private bool IsBattleLayout=>maps!=null && (maps[scenarioIndex].Key.StartsWith("Training",StringComparison.Ordinal) || maps[scenarioIndex].Key.StartsWith("Duel",StringComparison.Ordinal));
        private string SourceKey=>IsOriginalLayout?maps[scenarioIndex].Key.Substring(8):maps[scenarioIndex].Key;
        private GUIStyle headingStyle,sectionStyle;
        private void DrawHeading()
        {
            var r=GUILayoutUtility.GetRect(1,52,GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(r,new Color(.085f,.115f,.16f));
            EditorGUI.DrawRect(new Rect(r.x,r.y,4,r.height),new Color(.2f,.7f,.8f));
            headingStyle ??=new GUIStyle(EditorStyles.boldLabel){fontSize=17,normal={textColor=Color.white}};
            GUI.Label(new Rect(r.x+16,r.y+7,r.width-24,24),"SCENARIO  /  BATTLE EDITOR",headingStyle);
            GUI.Label(new Rect(r.x+16,r.y+31,r.width-24,18),"시나리오 · 수련장 · 대련장   |   휠 확대 · 가운데 드래그 이동",EditorStyles.whiteMiniLabel);
        }
        private void Section(string title)
        {
            EditorGUILayout.Space(6);
            sectionStyle ??=new GUIStyle(EditorStyles.boldLabel){fontSize=12};
            EditorGUILayout.LabelField(title,sectionStyle);
        }
        private readonly Dictionary<int,Sprite> sprites=new Dictionary<int,Sprite>();
        [MenuItem("DarkSaver/시나리오 편집툴")]
        public static void Open()=>GetWindow<ScenarioPlacementWindow>("시나리오 편집툴");
        private void OnEnable()
        {
            minSize=new Vector2(860,560);
            string indexPath="Assets/_Game/Editor/BattleLayouts/OriginalIndex.json";
            originalMaps=File.Exists(indexPath)?JsonUtility.FromJson<OriginalIndex>(File.ReadAllText(indexPath))?.maps ?? Array.Empty<OriginalMap>():Array.Empty<OriginalMap>();
            maps=ScenarioCatalog.All.Concat(Enumerable.Range(101,7).Select(id=>new ScenarioDefinition("Training"+id,"수련장 "+id,"Map_"+id+"_;","",20,15,"")))
                .Concat(Enumerable.Range(201,7).Select(id=>new ScenarioDefinition("Duel"+id,"대련장 "+id,"Map_"+id+"_;","",30,30,"")))
                .Concat(originalMaps.Select(m=>new ScenarioDefinition("Original"+m.key,m.title,m.mapScene,"",m.width,m.height,""))).ToArray();
            names=maps.Select((s,i)=>(s.Key.StartsWith("Original",StringComparison.Ordinal)?"[원본 SET/POS] "+s.Key.Substring(8)+" · ":i<ScenarioCatalog.All.Count?"[기존 콘텐츠] "+s.Key+" · ":"")+s.Title).ToArray();
            if(data==null && path==null && originalMaps.Length>0)scenarioIndex=ScenarioCatalog.All.Count+14;
            scenarioIndex=Mathf.Clamp(scenarioIndex,0,names.Length-1);
            if(data!=null)
            {
                var edited=data;var baseline=original;bool changed=dirty;dirty=false;
                Load(Mathf.Clamp(scenarioIndex,0,names.Length-1));
                if(changed){data=edited;original=baseline;dirty=true;}
            }
        }
        public override void SaveChanges(){Save();if(dirty)throw new IOException(error ?? "배치 저장 실패");base.SaveChanges();}
        public override void DiscardChanges(){dirty=false;base.DiscardChanges();}
        private bool Discard()=>!dirty || EditorUtility.DisplayDialog("저장하지 않은 배치","변경을 버리고 진행할까요?","버리기","계속 편집");
        private void OnDisable(){Release();}
        private void Release()
        {
            if(cameraView!=null)DestroyImmediate(cameraView.gameObject);
            if(preview.IsValid())EditorSceneManager.ClosePreviewScene(preview);
            if(texture!=null){texture.Release();DestroyImmediate(texture);}
            preview=default;cameraView=null;texture=null;ground=null;sprites.Clear();
        }
        private void Load(int index)
        {
            if(!Discard())return;
            Release();data=null;dirty=false;selected=-1;error=null;scenarioIndex=index;aiRules=Array.Empty<ScenarioPlacementTroops.AiRule>();isolatedGroup=null;partyPositions=Array.Empty<PartyPosition>();originalEdited=false;
            try
            {
                var scenario=maps[index];
                path=IsBattleLayout || IsOriginalLayout?"Assets/_Game/Editor/BattleLayouts/"+scenario.Key+".json":"Assets/_Game/Resources/Scenarios/"+scenario.Key+"/Content.json";
                original=File.Exists(path)?File.ReadAllText(path):null;
                if(IsOriginalLayout && original==null)throw new FileNotFoundException("원본 배치 파일이 없습니다: "+path);
                data=original!=null?JsonUtility.FromJson<Scenario01Definition>(original):new Scenario01Definition
                {
                    version=2,scenarioKey=scenario.Key,width=scenario.Width,height=scenario.Height,
                    entryX=scenario.Width/2,entryY=scenario.Height/2,sourceHash=new string('0',64),profileHash=new string('0',64),
                    intro=Array.Empty<Scenario01Command>(),outro=Array.Empty<Scenario01Command>(),events=Array.Empty<Scenario01Event>(),
                    units=Array.Empty<Scenario01Unit>(),characters=Array.Empty<Scenario01Unit>(),traversalCells=Array.Empty<int>()
                };
                if(data==null || data.width<=0 || data.height<=0)throw new InvalidDataException("잘못된 시나리오 크기");
                if(data.units==null)data.units=Array.Empty<Scenario01Unit>();
                if(IsOriginalLayout)
                {
                    var details=JsonUtility.FromJson<OriginalDetails>(original);
                    partyPositions=details?.partyPositions ?? Array.Empty<PartyPosition>();originalEdited=details!=null && details.placementEdited;
                }
                if(!IsBattleLayout)
                {
                    string aiPath="분석/Saver_Late/decoded/map/"+SourceKey+"set.arc/"+SourceKey+".ai";
                    if(File.Exists(aiPath))
                        aiRules=ScenarioPlacementTroops.ParseAi(File.ReadAllText(aiPath));
                }
                string scenePath=AssetDatabase.FindAssets("t:Scene").Select(AssetDatabase.GUIDToAssetPath)
                    .FirstOrDefault(p=>Path.GetFileNameWithoutExtension(p)==scenario.MapScene);
                if(scenePath==null)throw new FileNotFoundException("맵 씬을 찾을 수 없습니다: "+scenario.MapScene);
                preview=EditorSceneManager.OpenPreviewScene(scenePath);
                var tilemaps=preview.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Tilemap>(true)).ToArray();
                ground=tilemaps.FirstOrDefault(t=>t.name=="Ground") ?? tilemaps.FirstOrDefault();
                if(ground==null)throw new InvalidDataException("타일맵이 없습니다.");
                var go=new GameObject("Scenario placement preview camera");SceneManager.MoveGameObjectToScene(go,preview);
                cameraView=go.AddComponent<Camera>();cameraView.enabled=false;cameraView.orthographic=true;
                cameraView.clearFlags=CameraClearFlags.SolidColor;cameraView.backgroundColor=new Color(.08f,.08f,.08f);
                cameraView.scene=preview;
                var a=ground.CellToWorld(Vector3Int.zero);var b=ground.CellToWorld(new Vector3Int(data.width,data.height,0));
                go.transform.position=new Vector3((a.x+b.x)/2,(a.y+b.y)/2,-100);
                cameraView.orthographicSize=Mathf.Abs(b.y-a.y)/2+.5f;
            }
            catch(Exception e){error=e.Message;Release();}
            Repaint();
        }
        private Vector3 World(int x,int y)=>ground.GetCellCenterWorld(new Vector3Int(x,data.height-1-y,0));
        private Vector2 Screen(int x,int y)
        {
            var p=cameraView.WorldToViewportPoint(World(x,y));return new Vector2(canvas.x+p.x*canvas.width,canvas.y+(1-p.y)*canvas.height);
        }
        private bool Cell(Vector2 mouse,out Vector2Int cell)
        {
            cell=default;if(!canvas.Contains(mouse))return false;
            var p=cameraView.ViewportToWorldPoint(new Vector3((mouse.x-canvas.x)/canvas.width,1-(mouse.y-canvas.y)/canvas.height,100));
            var c=ground.WorldToCell(p);cell=new Vector2Int(c.x,data.height-1-c.y);
            return cell.x>=0 && cell.y>=0 && cell.x<data.width && cell.y<data.height;
        }
        private Sprite SpriteFor(int id)
        {
            if(sprites.TryGetValue(id,out var s))return s;
            if(IsOriginalLayout)
            {
                s=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Editor/BattleLayouts/Art/"+id+".png");
                if(s!=null)sprites[id]=s;return s;
            }
            string prefix=maps[scenarioIndex].Key=="01"?"Scenarios/01/Characters/":"Scenarios/Shared/Characters/";
            s=Resources.Load<Sprite>(prefix+id+"_Down_0");sprites[id]=s;return s;
        }
        private float MarkerSize => Mathf.Max(3,Vector2.Distance(Screen(0,0),Screen(1,0)));
        private void Marker(Vector2 pos,Color color,string label,Sprite sprite,bool active)
        {
            float size=MarkerSize;
            var rect=new Rect(pos.x-size/2,pos.y-size/2,size,size);
            if(!rect.Overlaps(canvas))return;
            EditorGUI.DrawRect(rect,active?Color.white:color);
            if(sprite!=null)
            {
                var r=sprite.textureRect;float inset=Mathf.Min(1,size*.05f);
                GUI.DrawTextureWithTexCoords(new Rect(rect.x+inset,rect.y+inset,size-inset*2,size-inset*2),sprite.texture,
                    new Rect(r.x/sprite.texture.width,r.y/sprite.texture.height,r.width/sprite.texture.width,r.height/sprite.texture.height));
            }
            GUI.Label(rect,new GUIContent("",label));
            if(showLabels || active)GUI.Label(new Rect(rect.xMax+2,pos.y-10,200,22),label,EditorStyles.whiteMiniLabel);
        }
        // Code-backed ID relationship. AI movement references never redefine membership.
        private static int GroupAnchor(Scenario01Unit unit)=>ScenarioPlacementTroops.CommanderId(unit.id);
        private string GroupKey(Scenario01Unit unit)=>GroupAnchor(unit).ToString();
        private bool Visible(Scenario01Unit unit)=>isolatedGroup==null || GroupKey(unit)==isolatedGroup;
        private Color GroupColor(Scenario01Unit unit)=>Color.HSVToRGB(Mathf.Repeat(GroupAnchor(unit)*.0618034f,1),.65f,.95f);
        private void DrawGroupLinks()
        {
            foreach(var group in data.units.Where(Visible).GroupBy(GroupKey))
            {
                var members=group.OrderBy(u=>u.id).ToArray();var first=members[0];
                var leader=ScenarioPlacementTroops.Commander(members,first);
                Color color=GroupColor(first);float half=MarkerSize*.7f;
                var points=members.Select(u=>Screen(u.x,u.y)).ToArray();
                var box=Rect.MinMaxRect(points.Min(p=>p.x)-half,points.Min(p=>p.y)-half,points.Max(p=>p.x)+half,points.Max(p=>p.y)+half);
                EditorGUI.DrawRect(box,new Color(color.r,color.g,color.b,.09f));
                EditorGUI.DrawRect(new Rect(box.x,box.y,box.width,2),color);EditorGUI.DrawRect(new Rect(box.x,box.yMax-2,box.width,2),color);
                EditorGUI.DrawRect(new Rect(box.x,box.y,2,box.height),color);EditorGUI.DrawRect(new Rect(box.xMax-2,box.y,2,box.height),color);
                foreach(var member in members)
                {
                    if(leader!=null)DrawLink(Screen(leader.x,leader.y),Screen(member.x,member.y),new Color(color.r,color.g,color.b,.65f));
                }
                GUI.Label(new Rect(box.x,box.y-18,310,18),"지휘관 "+GroupAnchor(first)+" · "+members.Length+"인 · "+ScenarioPlacementTroops.FormationName(members),EditorStyles.whiteMiniLabel);
            }
        }
        private static void DrawLink(Vector2 a,Vector2 b,Color color,bool dotted=false)
        {
            var delta=b-a;if(delta.sqrMagnitude<1)return;
            var matrix=GUI.matrix;
            try
            {
                GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg,a);
                if(dotted)for(float x=0;x<delta.magnitude;x+=9)EditorGUI.DrawRect(new Rect(a.x+x,a.y,Mathf.Min(4,delta.magnitude-x),1),color);
                else EditorGUI.DrawRect(new Rect(a.x,a.y,delta.magnitude,1),color);
            }
            finally{GUI.matrix=matrix;}
        }
        private Scenario01Unit SelectedUnit=>selected>=0 && selected<data.units.Length?data.units[selected]:null;
        private void DrawAiReferences()
        {
            foreach(var unit in data.units.Where(u=>Visible(u) && u.id==GroupAnchor(u)))
            foreach(var rule in aiRules.Where(a=>a.unit==unit.id))
            {
                Color tint=new Color(.2f,.8f,.9f,.7f);
                if(rule.followsUnit)
                {
                    var target=data.units.FirstOrDefault(u=>u.id==rule.target);
                    if(target==null)continue;
                    DrawLink(Screen(unit.x,unit.y),Screen(target.x,target.y),tint,true);
                    Marker(Screen(target.x,target.y),new Color(.2f,.8f,.9f,.25f),"SOILDER "+unit.id+" → "+rule.target+" (현재 좌표를 따르는 이동 기준)",null,false);
                }
                else
                {
                    var center=Screen(rule.x,rule.y);float radius=MarkerSize*rule.radius;
                    EditorGUI.DrawRect(new Rect(center.x-radius,center.y-radius,radius*2,1),tint);
                    EditorGUI.DrawRect(new Rect(center.x-radius,center.y+radius,radius*2,1),tint);
                    EditorGUI.DrawRect(new Rect(center.x-radius,center.y-radius,1,radius*2),tint);
                    EditorGUI.DrawRect(new Rect(center.x+radius,center.y-radius,1,radius*2),tint);
                    Marker(center,new Color(.2f,.8f,.9f,.25f),"AREA "+unit.id+" r="+rule.radius+" (이동 영역, 출현 좌표 아님)",null,false);
                }
            }
        }
        private void DrawFormationReference()
        {
            if(!showFormationReference || SelectedUnit==null)return;
            var leader=ScenarioPlacementTroops.Commander(data.units,SelectedUnit);if(leader==null)return;
            foreach(var offset in ScenarioPlacementTroops.Formation(referenceFormation))
            {
                int x=leader.x+offset.x,y=leader.y+offset.y;
                var p=Screen(x,y);float half=MarkerSize*.45f;Color color=new Color(1,.8f,.3f,.8f);
                EditorGUI.DrawRect(new Rect(p.x-half,p.y-half,half*2,1),color);EditorGUI.DrawRect(new Rect(p.x-half,p.y+half,half*2,1),color);
                EditorGUI.DrawRect(new Rect(p.x-half,p.y-half,1,half*2),color);EditorGUI.DrawRect(new Rect(p.x+half,p.y-half,1,half*2),color);
            }
        }
        private float FitSize => Mathf.Max(data.height*Mathf.Abs(ground.cellSize.y),data.width*Mathf.Abs(ground.cellSize.x)/cameraView.aspect)/2+.6f;
        private void ResetView()
        {
            zoom=1;
            var a=ground.CellToWorld(Vector3Int.zero);var b=ground.CellToWorld(new Vector3Int(data.width,data.height,0));
            cameraView.transform.position=new Vector3((a.x+b.x)/2,(a.y+b.y)/2,-100);Repaint();
        }
        private void Navigate()
        {
            if(!GUI.enabled)return;
            var e=Event.current;
            if(!canvas.Contains(e.mousePosition))return;
            if(e.type==EventType.ScrollWheel)
            {
                var pointer=new Vector3((e.mousePosition.x-canvas.x)/canvas.width,1-(e.mousePosition.y-canvas.y)/canvas.height,100);
                var before=cameraView.ViewportToWorldPoint(pointer);
                zoom=Mathf.Clamp(zoom*Mathf.Exp(-e.delta.y*.08f),.5f,8);
                cameraView.orthographicSize=FitSize/zoom;
                var after=cameraView.ViewportToWorldPoint(pointer);
                cameraView.transform.position+=before-after;e.Use();Repaint();
            }
            else if(e.type==EventType.MouseDrag && e.button==2)
            {
                float perPixel=cameraView.orthographicSize*2/canvas.height;
                cameraView.transform.position+=new Vector3(-e.delta.x*perPixel,e.delta.y*perPixel,0);
                dragging=false;e.Use();Repaint();
            }
        }
        private IEnumerable<(int x,int y,string label,bool chest)> Locations()
        {
            if(data.events==null)yield break;
            foreach(var item in data.events)
            {
                var actions=item.actions ?? Array.Empty<Scenario01Command>();
                bool chest=actions.Any(a=>a.name=="MESSAGE" && (a.text ?? "").Contains("상자"));
                var changes=actions.Where(a=>a.name=="CHANGESPRITE" && a.args!=null && a.args.Length>=2).ToArray();
                var positions=chest && changes.Length>0?changes:(item.conditions ?? Array.Empty<Scenario01Command>()).Where(c=>c.name=="CHARPOS" && c.args!=null && c.args.Length==2).ToArray();
                var seen=new HashSet<Vector2Int>();
                foreach(var command in positions)
                    if(int.TryParse(command.args[0],out int x) && int.TryParse(command.args[1],out int y)
                        && x>=0 && y>=0 && x<data.width && y<data.height && seen.Add(new Vector2Int(x,y)))
                        yield return (x,y,(chest?"상자":"퀘스트/이벤트 조건")+" #"+item.id+" ("+x+","+y+") · "+item.scope+" / "+item.mode,chest);
            }
        }
        private void OnGUI()
        {
            if(names==null)OnEnable();
            hasUnsavedChanges=dirty;saveChangesMessage="변경한 시나리오 배치를 저장할까요?";
            DrawHeading();
            using(new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                using(new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    int index=EditorGUILayout.Popup(scenarioIndex,names,GUILayout.MinWidth(260));
                    if(index!=scenarioIndex || data==null && error==null)Load(index);
                    if(GUILayout.Button("다시 불러오기",EditorStyles.toolbarButton))Load(scenarioIndex);
                    if(originalMaps.Length>0 && GUILayout.Button("원본 배치 보기",EditorStyles.toolbarButton))Load(ScenarioCatalog.All.Count+14);
                    if(GUILayout.Button(dirty?"저장 *":original==null?"새 배치 저장":"저장",EditorStyles.toolbarButton,GUILayout.Width(90)))Save();
                    showLocations=GUILayout.Toggle(showLocations,"상자/이벤트",EditorStyles.toolbarButton);
                    showLabels=GUILayout.Toggle(showLabels,"이름",EditorStyles.toolbarButton);
                    if(!IsBattleLayout)showAi=GUILayout.Toggle(showAi,"AI 이동 기준",EditorStyles.toolbarButton);

                }
                int mode=GUILayout.Toolbar(placementMode,new[]{"전투 배치","연출 배치"});
                if(placementMode==0 && data!=null)
                {
                    using(new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                    {
                        showGroups=GUILayout.Toggle(showGroups,"무리 경계",EditorStyles.toolbarButton,GUILayout.Width(75));
                        GUILayout.Label("지휘관 ID / 부하 슬롯 기준",EditorStyles.miniLabel,GUILayout.Width(180));
                        var keys=data.units.Select(GroupKey).Distinct().OrderBy(k=>k).ToArray();
                        if(isolatedGroup!=null && !keys.Contains(isolatedGroup))isolatedGroup=null;
                        int current=isolatedGroup==null?0:Array.IndexOf(keys,isolatedGroup)+1;
                        var options=new[]{"모든 부대"}.Concat(keys.Select(k=>"지휘관 "+k)).ToArray();
                        int chosen=EditorGUILayout.Popup(Mathf.Max(0,current),options);
                        string nextGroup=chosen==0?null:keys[chosen-1];
                        if(nextGroup!=isolatedGroup){selected=-1;dragging=false;isolatedGroup=nextGroup;}
                        var visibleUnits=data.units.Where(Visible).ToArray();int stacked=ScenarioPlacementTroops.StackedCells(visibleUnits);
                        GUILayout.Label(new GUIContent("표시 "+visibleUnits.Length+" / 전체 "+data.units.Length+(stacked>0?" · 겹침 "+stacked+"칸":""),
                            "표시 수는 필터를 통과한 전체 몬스터 수입니다. 같은 칸의 몬스터는 겹쳐 그려지므로 우측 부대 목록으로 각각 확인하세요."),EditorStyles.miniLabel,GUILayout.Width(200));
                        if(GUILayout.Button("전체 몬스터 보기",EditorStyles.toolbarButton,GUILayout.Width(120)))
                        {isolatedGroup=null;selected=-1;dragging=false;}
                    }
                    if(data.units.Length>0 && !data.units.Any(Visible))EditorGUILayout.HelpBox("배치 데이터는 있지만 현재 표시 필터에 해당하는 몬스터가 없습니다. 전체 몬스터 보기로 필터를 해제할 수 있습니다.",MessageType.Warning);
                    if(SelectedUnit!=null && !Visible(SelectedUnit)){selected=-1;dragging=false;}
                }
                if(mode!=placementMode){placementMode=mode;selected=-1;dragging=false;Repaint();}
                if(error!=null)EditorGUILayout.HelpBox(error,MessageType.Error);
                if(IsBattleLayout)EditorGUILayout.HelpBox("수련장·대련장은 편집용 배치 데이터입니다. 원작 서버 배치를 추측해서 생성하지 않으며 로컬 서버 통지에는 자동 적용하지 않습니다.",MessageType.Info);
                if(IsOriginalLayout)EditorGUILayout.HelpBox("원본 SET/POS의 유닛·직업·좌표입니다. "+(dirty?"미저장 편집이 있습니다.":originalEdited?"편집 변경을 저장한 상태입니다.":"원문 배치 그대로입니다.")+" 파란 아군 슬롯은 원본 POS의 위치이며 캐릭터 종류는 미정입니다.",MessageType.Info);
                if(data==null || cameraView==null)return;
                EditorGUILayout.HelpBox("빨강: 전투 몬스터 · 파랑: 아군/진입 위치 · 노랑: 상자 · 보라: 이벤트 · 청록: AI 이동 기준"+(showFormationReference?" · 노랑 윤곽: 대형 참고":"")+". 원본 좌표(좌상단 0,0). "+(IsBattleLayout || IsOriginalLayout?"저장: 편집 전용 BattleLayouts JSON":"저장: 실제 Content.json"),MessageType.Info);
                using(new EditorGUILayout.HorizontalScope())
                {
                    zoom=EditorGUILayout.Slider("확대",zoom,.5f,8);
                    if(GUILayout.Button("전체 보기",GUILayout.Width(85)))ResetView();
                }
                using(new EditorGUILayout.HorizontalScope())
                {
                    float width=Mathf.Max(160,position.width-295),height=Mathf.Max(140,position.height-235);
                    canvas=GUILayoutUtility.GetRect(width,height,GUILayout.Width(width),GUILayout.Height(height));
                    DrawMap();
                    using(new EditorGUILayout.VerticalScope(EditorStyles.helpBox,GUILayout.Width(270)))DrawInspector();
                }
            }
        }
        private void DrawMap()
        {
            int w=Mathf.Max(1,(int)canvas.width),h=Mathf.Max(1,(int)canvas.height);
            if(texture==null || texture.width!=w || texture.height!=h)
            {
                if(texture!=null){texture.Release();DestroyImmediate(texture);}
                texture=new RenderTexture(w,h,24){hideFlags=HideFlags.HideAndDontSave};texture.Create();
            }
            cameraView.aspect=(float)w/h;
            cameraView.orthographicSize=FitSize/zoom;
            Navigate();
            Rect viewport=canvas;
            GUI.BeginGroup(viewport);
            canvas=new Rect(0,0,viewport.width,viewport.height);
            try
            {
                if(Event.current.type==EventType.Repaint)
                {cameraView.targetTexture=texture;try{cameraView.Render();}finally{cameraView.targetTexture=null;}GUI.DrawTexture(canvas,texture);}
                Color gridColor=new Color(1,1,1,.13f);
                Vector2 dx=Screen(1,0)-Screen(0,0),dy=Screen(0,1)-Screen(0,0);
                Vector2 corner=Screen(0,0)-(dx+dy)/2;
                for(int x=0;x<=data.width;x++)EditorGUI.DrawRect(new Rect(corner.x+dx.x*x,corner.y,1,dy.y*data.height),gridColor);
                for(int y=0;y<=data.height;y++)EditorGUI.DrawRect(new Rect(corner.x,corner.y+dy.y*y,dx.x*data.width,1),gridColor);

                if(placementMode==0 && showGroups)DrawGroupLinks();
                if(placementMode==0 && showAi && !IsBattleLayout)DrawAiReferences();
                if(placementMode==0 && showLocations)
                    foreach(var marker in Locations())Marker(Screen(marker.x,marker.y),marker.chest?new Color(.9f,.65f,.1f):new Color(.7f,.3f,.85f),marker.label,null,false);
                if(placementMode==0)
                for(int i=0;i<data.units.Length;i++)
                {
                    var u=data.units[i];if(!Visible(u))continue;
                    Marker(Screen(u.x,u.y),showGroups?GroupColor(u):new Color(.8f,.18f,.15f),
                        (u.id==GroupAnchor(u)?"★ 지휘관 ":"부하 슬롯 "+ScenarioPlacementTroops.Slot(u.id)+" / ")+u.id+" · "+u.name,SpriteFor(u.job),selected==i);
                    if(showGroups && u.id==GroupAnchor(u))
                        GUI.Label(new Rect(Screen(u.x,u.y).x-MarkerSize/2,Screen(u.x,u.y).y-MarkerSize/2,20,20),"★",EditorStyles.whiteBoldLabel);
                }
                if(placementMode==0)DrawFormationReference();
                if(placementMode==0 && IsOriginalLayout)
                    foreach(var slot in partyPositions)Marker(Screen(slot.x,slot.y),new Color(.1f,.4f,.95f),"원본 아군 슬롯 "+slot.id+" (종류 미정, 읽기 전용)",null,false);
                else if(placementMode==0)Marker(Screen(data.entryX,data.entryY),new Color(.1f,.4f,.95f),"캐릭터 진입",null,selected==-2);
                else foreach(var command in IntroPlacements())
                {
                    int.TryParse(command.args[0],out int introJob);
                    Marker(Screen(int.Parse(command.args[2]),int.Parse(command.args[3])),new Color(.1f,.65f,.2f),
                        "연출 "+command.args[0]+" / "+command.args[1],SpriteFor(introJob),false);
                }
            }
            finally{canvas=viewport;GUI.EndGroup();}
            if(!GUI.enabled || placementMode!=0)return;
            var e=Event.current;
            if(e.type==EventType.MouseDown && e.button==0 && canvas.Contains(e.mousePosition))
            {
                selected=-1;float best=MarkerSize*.7f;
                for(int i=data.units.Length-1;i>=0;i--){var u=data.units[i];if(!Visible(u))continue;float d=Vector2.Distance(e.mousePosition,Screen(u.x,u.y));if(d<best){best=d;selected=i;}}
                if(!IsOriginalLayout && Vector2.Distance(e.mousePosition,Screen(data.entryX,data.entryY))<MarkerSize/2)selected=-2;
                dragging=selected!=-1;GUI.FocusControl(null);e.Use();Repaint();
            }
            if(e.type==EventType.MouseDrag && e.button==0 && dragging && Cell(e.mousePosition,out var cell))
            {
                if(selected==-2){data.entryX=cell.x;data.entryY=cell.y;}else{data.units[selected].x=cell.x;data.units[selected].y=cell.y;}
                dirty=true;e.Use();Repaint();
            }
            if(e.type==EventType.MouseUp)dragging=false;
            if(e.type==EventType.KeyDown && e.keyCode==KeyCode.Delete && selected>=0 && !EditorGUIUtility.editingTextField){Delete();e.Use();}
        }
        private void Delete(){if(selected<0)return;data.units=data.units.Where((u,i)=>i!=selected).ToArray();selected=-1;dirty=true;}
        private IEnumerable<Scenario01Command> IntroPlacements()
            => (data.intro ?? Array.Empty<Scenario01Command>()).Where(c=>c.name=="CHARPOS" && c.args!=null && c.args.Length>=4
                && int.TryParse(c.args[2],out _) && int.TryParse(c.args[3],out _));
        private void DrawInspector()
        {
            scroll=EditorGUILayout.BeginScrollView(scroll,GUILayout.Height(canvas.height));
            try{DrawInspectorContent();}
            finally{EditorGUILayout.EndScrollView();}
        }
        private void DrawInspectorContent()
        {
            if(placementMode==1)
            {
                EditorGUILayout.HelpBox("도입 연출의 CHARPOS 배치입니다 (읽기 전용). 전투 몬스터와 진입점은 표시하지 않습니다.",MessageType.Info);
                foreach(var command in IntroPlacements())
                    if(GUILayout.Button(command.args[0]+" / "+command.args[1]+" ("+command.args[2]+","+command.args[3]+")"))
                    {var pos=World(int.Parse(command.args[2]),int.Parse(command.args[3]));cameraView.transform.position=new Vector3(pos.x,pos.y,cameraView.transform.position.z);Repaint();}
                return;
            }
            Section("MONSTERS  /  몬스터");
            job=EditorGUILayout.IntField("추가할 직업 ID",job);
            int commanderId=ScenarioPlacementTroops.NextCommanderId(data.units);
            using(new EditorGUI.DisabledScope(commanderId<0))
                if(GUILayout.Button(new GUIContent("새 지휘관 추가","400~790 중 사용하지 않는 ID 10개 구간을 선택합니다.")))AddUnit(commanderId,data.entryX,data.entryY);
            int memberId=ScenarioPlacementTroops.NextMemberId(data.units,SelectedUnit);
            using(new EditorGUI.DisabledScope(memberId<0))
                if(GUILayout.Button(new GUIContent("선택 부대에 부하 추가","같은 지휘관의 빈 슬롯 1~9에 추가합니다. 지휘관이 없거나 슬롯이 모두 차면 사용할 수 없습니다.")))
                {var leader=ScenarioPlacementTroops.Commander(data.units,SelectedUnit);AddUnit(memberId,leader.x,leader.y);}
            Section("DETAILS  /  선택 배치");
            EditorGUI.BeginChangeCheck();
            if(selected==-2){data.entryX=EditorGUILayout.IntField("진입 X",data.entryX);data.entryY=EditorGUILayout.IntField("진입 Y",data.entryY);}
            else if(selected>=0 && selected<data.units.Length)
            {
                var u=data.units[selected];u.id=EditorGUILayout.IntField("유닛 ID",u.id);u.name=EditorGUILayout.TextField("이름",u.name);u.job=EditorGUILayout.IntField("직업 ID",u.job);
                u.x=EditorGUILayout.IntField("X",u.x);u.y=EditorGUILayout.IntField("Y",u.y);
                u.hp=EditorGUILayout.IntField("HP",u.hp);u.mp=EditorGUILayout.IntField("MP",u.mp);u.attack=EditorGUILayout.IntField("공격",u.attack);u.defense=EditorGUILayout.IntField("방어",u.defense);
                u.magicAttack=EditorGUILayout.IntField("마법 공격",u.magicAttack);u.magicDefense=EditorGUILayout.IntField("마법 방어",u.magicDefense);
                if(GUILayout.Button("선택 몬스터 삭제"))Delete();
            }
            if(EditorGUI.EndChangeCheck())dirty=true;
            DrawTroopInspector();
            if(!IsBattleLayout && !IsOriginalLayout)EditorGUILayout.HelpBox("저장 시 배치 해시가 변경됩니다. 기존 진행 저장은 콘텐츠 불일치로 거부될 수 있으므로 새 검증 저장을 사용하세요. 삭제한 ID를 참조하는 이벤트/대사는 자동 변경하지 않습니다.",MessageType.Warning);
            if(IsOriginalLayout)EditorGUILayout.HelpBox("원문은 분석 자료에 보존됩니다. 저장은 Editor/BattleLayouts의 편집본만 변경하며 실제 씬·전투 출현은 자동 변경하지 않습니다. 수치는 원본 능력치 표의 기본값입니다.",MessageType.Info);
            else if(!IsBattleLayout)EditorGUILayout.HelpBox("기존 보완 편성을 보존해 표시합니다. 01~17의 원작 초기 몬스터 종류·수량·좌표를 확정한 자료가 아닙니다. 연출 배치는 별도 모드입니다.",MessageType.Info);
            else EditorGUILayout.LabelField("저장 위치: Editor/BattleLayouts",EditorStyles.miniLabel);
            Section("LAYOUT  /  배치 목록");
            using(new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(data.width+" × "+data.height,GUILayout.Width(70));
                EditorGUILayout.LabelField(data.units.Length+" 유닛",GUILayout.Width(70));
                EditorGUILayout.LabelField(data.units.Select(GroupKey).Distinct().Count()+" 부대");
            }
            foreach(var group in data.units.Select((u,i)=>(unit:u,index:i)).Where(p=>Visible(p.unit)).GroupBy(p=>GroupKey(p.unit)))
            {
                var first=group.First().unit;
                EditorGUILayout.LabelField("지휘관 "+GroupAnchor(first),EditorStyles.boldLabel);
                foreach(var entry in group.OrderBy(p=>p.unit.id))
                {
                    var u=entry.unit;string role=ScenarioPlacementTroops.Slot(u.id)==0?"★":"└ "+ScenarioPlacementTroops.Slot(u.id);
                    if(GUILayout.Button(role+" · "+u.id+" "+u.name+" ("+u.x+","+u.y+")"))selected=entry.index;
                }
            }
            if(showLocations)
            {
                EditorGUILayout.LabelField("상자 / 퀘스트·이벤트 (읽기 전용)");
                foreach(var marker in Locations())
                    if(GUILayout.Button(marker.label))
                    {var world=World(marker.x,marker.y);cameraView.transform.position=new Vector3(world.x,world.y,cameraView.transform.position.z);Repaint();}
            }
        }
        private void AddUnit(int id,int x,int y)
        {
            // Free nearby editor cell; no original spawn or formation policy is inferred.
            var occupied=new HashSet<int>(data.units.Select(u=>u.y*data.width+u.x));
            var cells=from cy in Enumerable.Range(0,data.height) from cx in Enumerable.Range(0,data.width)
                      where !occupied.Contains(cy*data.width+cx)
                      orderby Math.Abs(cx-x)+Math.Abs(cy-y),cy,cx select new Vector2Int(cx,cy);
            if(!cells.Any()){error="맵에 빈 배치 칸이 없습니다.";return;}
            var cell=cells.First();
            var unit=new Scenario01Unit{id=id,job=job,name="새 몬스터",x=cell.x,y=cell.y,hp=1};
            data.units=data.units.Concat(new[]{unit}).ToArray();selected=data.units.Length-1;isolatedGroup=null;dirty=true;error=null;
        }
        private void DrawTroopInspector()
        {
            var unit=SelectedUnit;if(unit==null)return;
            Section("TROOP  /  부대 구조");
            var members=ScenarioPlacementTroops.Members(data.units,unit);var leader=ScenarioPlacementTroops.Commander(members,unit);
            EditorGUILayout.LabelField("지휘관 ID",GroupAnchor(unit).ToString());
            EditorGUILayout.LabelField("역할",ScenarioPlacementTroops.Slot(unit.id)==0?"지휘관":"부하 슬롯 "+ScenarioPlacementTroops.Slot(unit.id));
            EditorGUILayout.LabelField("현재 대형",ScenarioPlacementTroops.FormationName(members));
            if(leader==null)EditorGUILayout.HelpBox("배치에 지휘관 "+GroupAnchor(unit)+"이 없습니다. 부하를 지휘관으로 대체 표시하지 않습니다.",MessageType.Warning);
            else
            {
                foreach(var member in members)
                    EditorGUILayout.LabelField(member.id+" / 슬롯 "+ScenarioPlacementTroops.Slot(member.id),"Δ("+(member.x-leader.x)+","+(member.y-leader.y)+")");
                foreach(var rule in aiRules.Where(a=>a.unit==leader.id))
                    if(rule.followsUnit)
                        EditorGUILayout.LabelField("SOILDER 이동 기준",rule.target+"번"+(data.units.Any(u=>u.id==rule.target)?"의 현재 위치":" (배치에 없음)"));
                    else if(rule.x!=leader.x || rule.y!=leader.y)
                        EditorGUILayout.HelpBox("현재 좌표 ("+leader.x+","+leader.y+")와 AREA 중심 ("+rule.x+","+rule.y+")이 다릅니다. 좌표를 자동 보정하지 않습니다.",MessageType.Info);
                referenceFormation=EditorGUILayout.Popup("원본 대형 참고",referenceFormation,ScenarioPlacementTroops.FormationNames);
                showFormationReference=EditorGUILayout.Toggle("맵에 참고선 표시",showFormationReference);
                var rect=GUILayoutUtility.GetRect(100,92,GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(rect,new Color(.07f,.1f,.14f));var center=rect.center;float step=16;
                foreach(var offset in ScenarioPlacementTroops.Formation(referenceFormation))
                    EditorGUI.DrawRect(new Rect(center.x+offset.x*step-4,center.y+offset.y*step-4,8,8),new Color(.35f,.8f,.9f));
                EditorGUI.DrawRect(new Rect(center.x-5,center.y-5,10,10),new Color(1,.8f,.3f));
                EditorGUILayout.HelpBox("원본에서 관측한 5인 대형의 형태 참고입니다. 선택/표시로 현재 수량·좌표·슬롯을 변경하지 않습니다.",MessageType.Info);
            }
        }
        private static string ReplaceArray(string source,string json)
        {
            var match=Regex.Match(source,"\\\"units\\\"\\s*:");if(!match.Success)throw new InvalidDataException("units 필드 없음");
            int start=source.IndexOf('[',match.Index+match.Length),depth=0;bool quoted=false,escape=false;
            for(int i=start;i<source.Length;i++)
            {
                char c=source[i];if(quoted){if(escape)escape=false;else if(c=='\\')escape=true;else if(c=='\"')quoted=false;continue;}
                if(c=='\"')quoted=true;else if(c=='[')depth++;else if(c==']' && --depth==0)return source.Substring(0,start)+json+source.Substring(i+1);
            }
            throw new InvalidDataException("units 배열 끝 없음");
        }
        private void Save()
        {
            if(data==null || !dirty && original!=null)return;
            try
            {
                bool Inside(int x,int y)=>x>=0 && y>=0 && x<data.width && y<data.height;
                if(!Inside(data.entryX,data.entryY) || data.units.Any(u=>!Inside(u.x,u.y) || u.id<=0 || u.id>short.MaxValue || u.job<0) || data.units.Select(u=>u.id).Distinct().Count()!=data.units.Length)
                    throw new InvalidDataException("좌표 범위·중복/잘못된 ID·직업을 확인하세요.");
                if((original==null && File.Exists(path)) || (original!=null && (!File.Exists(path) || File.ReadAllText(path)!=original)))throw new IOException("외부에서 파일이 변경되었습니다. 다시 불러온 후 편집하세요.");
                string wrapper=IsOriginalLayout?JsonUtility.ToJson(new OriginalUnitList{units=data.units.Select(OriginalUnit.From).ToArray()},true):JsonUtility.ToJson(new UnitList{units=data.units},true);
                string array=wrapper.Substring(wrapper.IndexOf('['));array=array.Substring(0,array.LastIndexOf(']')+1);
                string updated=ReplaceArray(original ?? JsonUtility.ToJson(data,true),array);
                if(IsOriginalLayout)updated=Regex.Replace(updated,"\\\"placementEdited\\\"\\s*:\\s*(?:true|false)","\"placementEdited\": true");
                updated=Regex.Replace(updated,"(\\\"entryX\\\"\\s*:\\s*)-?\\d+",m=>m.Groups[1].Value+data.entryX);
                updated=Regex.Replace(updated,"(\\\"entryY\\\"\\s*:\\s*)-?\\d+",m=>m.Groups[1].Value+data.entryY);
                using(var sha=SHA256.Create())
                {string hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(array+"|"+data.entryX+"|"+data.entryY))).Replace("-","").ToLowerInvariant();updated=Regex.Replace(updated,"(\\\"profileHash\\\"\\s*:\\s*\\\")[^\\\"]*(\\\")",m=>m.Groups[1].Value+hash+m.Groups[2].Value);}
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string temp=path+".placement-tmp";File.WriteAllText(temp,updated,new UTF8Encoding(false));
                if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);
                original=updated;dirty=false;if(IsOriginalLayout)originalEdited=true;AssetDatabase.ImportAsset(path);error=null;
            }
            catch(Exception e){error=e.Message;}
        }
    }
}
