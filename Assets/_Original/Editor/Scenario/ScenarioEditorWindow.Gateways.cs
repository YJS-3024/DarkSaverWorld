using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using DarkSaver.Original.Campaign;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Editor.Scenarios
{
    public sealed partial class ScenarioEditorWindow
    {
        private const string GatewayPath=BasePath+"Data/Campaign/UserGateways.json";
        [SerializeField] private ScenarioGatewaySettings gatewayDocument;
        [SerializeField] private bool dirtyGateways;
        [SerializeField] private string gatewayHash,selectedGateway;
        private GatewayMapCatalog gatewayCatalog;
        private WorldDefinition gatewayWorld;
        private string selectedNativePortal;
        private bool sizingGateway;
        private bool sizingArrival,dragArrival;
        private int areaStartX,areaStartY;
        private bool pickNpc,pickSpawn,dragGateway,gatewayRemembered;
        private Vector2 gatewayDragStart;
        private int gatewayStartX,gatewayStartY;
        private readonly System.Collections.Generic.List<byte[]> gatewayUndo=new System.Collections.Generic.List<byte[]>(),gatewayRedo=new System.Collections.Generic.List<byte[]>();
        private GatewayMapInfo MapInfo(string key)=>gatewayCatalog.maps.First(m=>m.key==key);
        private void LoadGateways(bool preserve)
        {
            gatewayCatalog=JsonUtility.FromJson<GatewayMapCatalog>(File.ReadAllText(BasePath+"Data/Campaign/GatewayMaps.json"));
            gatewayWorld=JsonUtility.FromJson<CampaignDefinition>(File.ReadAllText(BasePath+"Data/Campaign/CampaignCatalog.json")).Merge(
                JsonUtility.FromJson<WorldDefinition>(File.ReadAllText(BasePath+"Data/World/KaosiaWorld.json")));
            if(preserve)return;
            var bytes=File.ReadAllBytes(GatewayPath);gatewayDocument=ScenarioGatewayData.Decode(bytes);ScenarioGatewayData.Validate(gatewayDocument,gatewayCatalog);
            gatewayHash=ScenarioGatewayData.Hash(bytes);dirtyGateways=false;
        }
        private void RememberGateway()
        {
            gatewayUndo.Add(ScenarioGatewayData.Encode(gatewayDocument));if(gatewayUndo.Count>100)gatewayUndo.RemoveAt(0);gatewayRedo.Clear();
            dirtyGateways=hasUnsavedChanges=true;message="입구 연결: 저장하지 않은 변경";Repaint();
        }
        private void GatewayHistory(bool back)
        {
            var from=back?gatewayUndo:gatewayRedo;var to=back?gatewayRedo:gatewayUndo;if(from.Count==0)return;
            to.Add(ScenarioGatewayData.Encode(gatewayDocument));gatewayDocument=ScenarioGatewayData.Decode(from[from.Count-1]);from.RemoveAt(from.Count-1);
            dirtyGateways=hasUnsavedChanges=true;dragGateway=pickNpc=pickSpawn=sizingGateway=false;Repaint();
        }
        private bool SaveGateways()
        {
            try
            {
                foreach(var g in gatewayDocument.gates)
                    foreach(int id in new[]{g.fromMap,g.targetMap})
                    {var info=gatewayCatalog.maps.FirstOrDefault(m=>m.id==id);if(info==null||ScenarioGatewayData.Hash(File.ReadAllBytes(info.nativePath))!=info.hash)throw new IOException("맵이 변경됐거나 선택되지 않았습니다. 입구 설정을 확인하세요.");}
                // Validate actual runtime placement too, without changing its cached native data.
                var world=JsonUtility.FromJson<DarkSaver.Original.World.WorldDefinition>(File.ReadAllText(BasePath+"Data/World/KaosiaWorld.json"));
                var art=AssetDatabase.LoadAssetAtPath<NativeCampaignArt>(BasePath+"Data/Campaign/NativeCampaignArt.asset");
                ScenarioGatewayData.Apply(gatewayDocument,gatewayCatalog,art.Definition.Merge(world));
                gatewayHash=ScenarioGatewayData.Save(GatewayPath,gatewayDocument,gatewayCatalog,string.IsNullOrEmpty(gatewayHash)?null:gatewayHash);
                dirtyGateways=false;hasUnsavedChanges=dirty;AssetDatabase.Refresh();message="입구 저장: "+GatewayPath;return true;
            }
            catch(Exception e){message=e.Message;return false;}
        }
        private void ReloadGateways()
        {
            if(dirtyGateways&&!EditorUtility.DisplayDialog("입구 다시 열기","저장하지 않은 입구 편집을 버리고 저장본을 불러올까요?","불러오기","취소"))return;
            try{LoadGateways(false);gatewayUndo.Clear();gatewayRedo.Clear();hasUnsavedChanges=dirty;dragGateway=pickNpc=pickSpawn=sizingGateway=false;message="입구를 다시 불러왔습니다.";}catch(Exception e){message=e.Message;}
        }
        private void AddGateway(bool walk)
        {
            var map=MapInfo(mapKey);RememberGateway();
            var gate=new ScenarioGateway{id=Guid.NewGuid().ToString("N"),trigger=walk?"walk":"npc",width=walk?3:1,height=1,
                fromMap=map.id,fromHash=map.hash,targetMap=-1,npcJob=-1,x=-1,y=-1,spawnX=-1,spawnY=-1,spawnWidth=1,spawnHeight=1,
                returnX=-1,returnY=-1,name=walk?"새 이동 입구":"새 NPC 입구"};
            gatewayDocument.gates=gatewayDocument.gates.Concat(new[]{gate}).ToArray();selectedGateway=gate.id;selectedNativePortal=null;
            pickNpc=pickSpawn=dragGateway=sizingGateway=false;
        }
        private void EditNativePortal(WorldPortal portal)
        {
            var existing=gatewayDocument.gates.FirstOrDefault(g=>g.fromMap==int.Parse(mapKey)&&g.replacesPortal==portal.id);
            if(existing!=null){selectedGateway=existing.id;selectedNativePortal=null;return;}
            int x=portal.cells.Min(c=>c.x),y=portal.cells.Min(c=>c.y),w=portal.cells.Max(c=>c.x)-x+1,h=portal.cells.Max(c=>c.y)-y+1;
            if(portal.cells.Select(c=>c.x+":"+c.y).Distinct().Count()!=w*h){message="이 출입구는 사각형이 아닙니다. 새 이동 입구로 영역을 지정하세요.";return;}
            var from=MapInfo(mapKey);var target=gatewayCatalog.maps.First(m=>m.id==portal.targetMap);RememberGateway();
            var gate=new ScenarioGateway{id=Guid.NewGuid().ToString("N"),trigger="walk",replacesPortal=portal.id,
                fromMap=from.id,fromHash=from.hash,targetMap=target.id,targetHash=target.hash,x=x,y=y,width=w,height=h,
                spawnX=portal.spawnX,spawnY=portal.spawnY,spawnWidth=Math.Max(1,portal.spawnWidth),spawnHeight=Math.Max(1,portal.spawnHeight),returnX=portal.returnX,returnY=portal.returnY,name=portal.id,enabled=true};
            gatewayDocument.gates=gatewayDocument.gates.Concat(new[]{gate}).ToArray();selectedGateway=gate.id;selectedNativePortal=null;
        }
        private void PickGatewayMap(ScenarioGateway gate,bool arrival)
        {
            int id=arrival?gate.targetMap:gate.fromMap;SelectMap(gatewayCatalog.maps.First(m=>m.id==id).key);
            if(int.Parse(mapKey)!=id)return;
            pickSpawn=arrival;pickNpc=!arrival;dragGateway=sizingGateway=false;
        }
        private void DrawGatewayInspector(Rect rect)
        {
            GUILayout.BeginArea(rect,EditorStyles.helpBox);inspectorScroll=EditorGUILayout.BeginScrollView(inspectorScroll);
            GUILayout.Label("맵 입구 연결",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("이동 입구: 영역을 밟아 맵 이동\nNPC 입구: 클릭 → 방 목록 → 참가\n한 맵에 여러 입구를 설정합니다.\n돌아오는 방향은 별도 입구로 추가하세요.",MessageType.Info);
            bool apply=EditorGUILayout.ToggleLeft("저장한 연결을 로컬 서버에서 사용",gatewayDocument.applyToGame);
            if(apply!=gatewayDocument.applyToGame){RememberGateway();gatewayDocument.applyToGame=apply;}
            GUILayout.Label("적용은 다음 Play/프로필 진입부터",EditorStyles.miniLabel);
            var map=MapInfo(mapKey);
            if(GUILayout.Button("이 맵에 이동 입구 추가"))AddGateway(true);
            using(new EditorGUI.DisabledScope(map.kind!="world"&&map.kind!="town"))
                if(GUILayout.Button("이 맵에 NPC 입구 추가"))AddGateway(false);
            GUILayout.Label("기존 출입구 (현재 지도)",EditorStyles.boldLabel);
            foreach(var portal in gatewayWorld.Map(map.id).portals)
            {
                bool overridden=gatewayDocument.gates.Any(g=>g.fromMap==map.id&&g.replacesPortal==portal.id);
                if(GUILayout.Button((selectedNativePortal==portal.id?"● ":"")+portal.id+" → "+portal.targetMap+" ("+portal.cells.Length+"칸)"+(overridden?" [편집본]":""),EditorStyles.miniButton))
                {selectedNativePortal=portal.id;selectedGateway=null;pickNpc=pickSpawn=dragGateway=sizingGateway=false;}
            }
            var native=gatewayWorld.Map(map.id).portals.FirstOrDefault(p=>p.id==selectedNativePortal);
            if(native!=null)
            {
                EditorGUILayout.HelpBox("기존 도착: "+native.targetMap+" / "+native.spawnX+","+native.spawnY+" ("+Math.Max(1,native.spawnWidth)+"×"+Math.Max(1,native.spawnHeight)+")\n수정본 저장 전까지 원본은 유지됩니다.",MessageType.None);
                if(GUILayout.Button("이 출입구를 편집본으로 가져오기"))EditNativePortal(native);
            }
            DrawGatewayList();
            var gate=gatewayDocument.gates.FirstOrDefault(g=>g.id==selectedGateway);
            if(gate!=null)
            {
                GUILayout.Space(7);GUILayout.Label("선택 입구",EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();string name=EditorGUILayout.TextField("입구 이름",gate.name);bool enabled=EditorGUILayout.Toggle("이 연결 사용",gate.enabled);
                if(EditorGUI.EndChangeCheck()){RememberGateway();gate.name=name;gate.enabled=enabled;}
                DrawGatewayGroupField(gate);
                GUILayout.Label(gate.IsWalk?"방식: 영역 진입 (자동 이동)":"방식: NPC 클릭 (방 참가)");
                GUILayout.Label("출발: "+gate.fromMap+" → 도착: "+gate.targetMap,EditorStyles.boldLabel);
                if(!string.IsNullOrEmpty(gate.replacesPortal))GUILayout.Label("기존 출입구 수정: "+gate.replacesPortal,EditorStyles.wordWrappedMiniLabel);
                if(!gate.IsWalk){var npcs=gatewayCatalog.npcJobs;var options=new[]{"외형을 선택하세요"}.Concat(npcs.Select(id=>jobs.TryGetValue(id,out var j)?j.ToString():id.ToString())).ToArray();
                int npcAt=Array.IndexOf(npcs,gate.npcJob)+1,npc=EditorGUILayout.Popup("NPC 외형",npcAt,options);
                if(npc!=npcAt){RememberGateway();gate.npcJob=npc==0?-1:npcs[npc-1];}
                }
                var targets=gatewayCatalog.maps.Where(m=>m.id!=gate.fromMap&&(gate.IsWalk||m.kind=="scenario")).OrderBy(m=>m.id).ToArray();
                int at=Array.FindIndex(targets,m=>m.id==gate.targetMap)+1,next=EditorGUILayout.Popup("도착 맵",at,new[]{"도착 맵을 선택하세요"}.Concat(targets.Select(m=>m.key+" "+m.title)).ToArray());
                if(next!=at){RememberGateway();gate.targetMap=next==0?-1:targets[next-1].id;gate.targetHash=next==0?"":targets[next-1].hash;gate.spawnX=gate.spawnY=-1;pickNpc=pickSpawn=dragGateway=sizingGateway=false;}
                EditorGUI.BeginChangeCheck();int x=EditorGUILayout.DelayedIntField("출발 X (좌상단)",gate.x),y=EditorGUILayout.DelayedIntField("출발 Y (좌상단)",gate.y);
                int width=gate.IsWalk?EditorGUILayout.DelayedIntField("가로 칸 수",gate.Width):1,height=gate.IsWalk?EditorGUILayout.DelayedIntField("세로 칸 수",gate.Height):1;
                int sx=EditorGUILayout.DelayedIntField("도착 X (좌상단)",gate.spawnX),sy=EditorGUILayout.DelayedIntField("도착 Y (좌상단)",gate.spawnY);
                int sw=EditorGUILayout.DelayedIntField("도착 가로 칸 수",gate.SpawnWidth),sh=EditorGUILayout.DelayedIntField("도착 세로 칸 수",gate.SpawnHeight);
                if(EditorGUI.EndChangeCheck()){RememberGateway();gate.x=x;gate.y=y;gate.width=width;gate.height=height;gate.spawnX=sx;gate.spawnY=sy;gate.spawnWidth=Math.Max(1,sw);gate.spawnHeight=Math.Max(1,sh);}
                GUILayout.BeginHorizontal();if(GUILayout.Button("도착 2 × 1")){RememberGateway();gate.spawnWidth=2;gate.spawnHeight=1;}if(GUILayout.Button("도착 1 × 2")){RememberGateway();gate.spawnWidth=1;gate.spawnHeight=2;}GUILayout.EndHorizontal();
                if(gate.SpawnWidth>1||gate.SpawnHeight>1)EditorGUILayout.HelpBox("출발 칸 순서에 비례해 도착 칸을 선택합니다. 도착한 출구는 영역을 벗어난 뒤 다시 진입할 때 작동합니다.",MessageType.None);
                if(gate.IsWalk)
                {
                    GUILayout.BeginHorizontal();if(GUILayout.Button("3칸 × 1")){RememberGateway();gate.width=3;gate.height=1;}if(GUILayout.Button("6칸 × 1")){RememberGateway();gate.width=6;gate.height=1;}GUILayout.EndHorizontal();
                    if(gatewayWorld.Map(gate.targetMap)?.training==true)
                    {
                        EditorGUI.BeginChangeCheck();int rx=EditorGUILayout.DelayedIntField("카오시아 복귀 X",gate.returnX),ry=EditorGUILayout.DelayedIntField("카오시아 복귀 Y",gate.returnY);
                        if(EditorGUI.EndChangeCheck()){RememberGateway();gate.returnX=rx;gate.returnY=ry;}
                    }
                }
                if(GUILayout.Button(gate.IsWalk?"출발 맵에서 입구 영역 그리기":"출발 맵에서 NPC 위치 찍기"))PickGatewayMap(gate,false);
                using(new EditorGUI.DisabledScope(gate.targetMap<0))
                    if(GUILayout.Button("도착 맵에서 출구 영역 그리기"))PickGatewayMap(gate,true);
                if(GUILayout.Button("출발 지도 보기")){pickNpc=pickSpawn=dragGateway=sizingGateway=false;SelectMap(gatewayCatalog.maps.First(m=>m.id==gate.fromMap).key);}
                using(new EditorGUI.DisabledScope(gate.targetMap<0))if(GUILayout.Button("도착 지도 보기")){pickNpc=pickSpawn=dragGateway=sizingGateway=false;SelectMap(gatewayCatalog.maps.First(m=>m.id==gate.targetMap).key);}
                if(GUILayout.Button("선택 입구 삭제")){RememberGateway();gatewayDocument.gates=gatewayDocument.gates.Where(g=>g.id!=selectedGateway).ToArray();selectedGateway=null;pickNpc=pickSpawn=dragGateway=sizingGateway=false;}
            }
            GUILayout.BeginHorizontal();using(new EditorGUI.DisabledScope(gatewayUndo.Count==0))if(GUILayout.Button("되돌리기"))GatewayHistory(true);
            using(new EditorGUI.DisabledScope(gatewayRedo.Count==0))if(GUILayout.Button("다시 실행"))GatewayHistory(false);GUILayout.EndHorizontal();
            if(GUILayout.Button("입구 연결 저장"))SaveGateways();
            if(pickNpc||pickSpawn||sizingGateway)EditorGUILayout.HelpBox(pickSpawn||sizingGateway&&sizingArrival?"도착 지도입니다. 클릭하면 현재 크기로 놓고, 드래그하면 도착 출구 영역을 그립니다.":gate?.IsWalk==true?"출발 지도에서 클릭하면 현재 크기로 배치합니다. 드래그하면 사각 영역을 그립니다.":"지도에서 NPC가 설 타일을 클릭하세요.",MessageType.Info);
            if(!string.IsNullOrEmpty(message))EditorGUILayout.HelpBox(message,MessageType.None);
            EditorGUILayout.EndScrollView();GUILayout.EndArea();
        }
        private void ProcessGatewayInput(Event e,bool inside,Vector2 local)
        {
            if(e.type==EventType.KeyDown&&(e.control||e.command)&&GUIUtility.keyboardControl==0&&e.keyCode==KeyCode.Z){GatewayHistory(!e.shift);e.Use();return;}
            if(e.type==EventType.KeyDown&&(e.control||e.command)&&GUIUtility.keyboardControl==0&&e.keyCode==KeyCode.Y){GatewayHistory(false);e.Use();return;}
            var gate=gatewayDocument.gates.FirstOrDefault(g=>g.id==selectedGateway);
            if(e.type==EventType.MouseDown&&e.button==0&&inside&&hoverX>=0)
            {
                if(gate!=null&&((pickNpc&&gate.fromMap==int.Parse(mapKey))||(pickSpawn&&gate.targetMap==int.Parse(mapKey))))
                {RememberGateway();sizingArrival=pickSpawn;areaStartX=hoverX;areaStartY=hoverY;if(pickNpc){gate.x=hoverX;gate.y=hoverY;sizingGateway=gate.IsWalk;}else{gate.spawnX=hoverX;gate.spawnY=hoverY;sizingGateway=true;}pickNpc=pickSpawn=false;}
                else
                {
                    dragArrival=gate!=null&&gate.targetMap==int.Parse(mapKey)&&gate.SpawnContains(hoverX,hoverY);
                    if(!dragArrival)gate=gatewayDocument.gates.LastOrDefault(g=>g.fromMap==int.Parse(mapKey)&&g.Contains(hoverX,hoverY));
                    if(gate==null){gate=gatewayDocument.gates.LastOrDefault(g=>g.targetMap==int.Parse(mapKey)&&g.SpawnContains(hoverX,hoverY));dragArrival=gate!=null;}
                    if(gate!=null){selectedGateway=gate.id;selectedNativePortal=null;dragGateway=true;gatewayRemembered=false;gatewayDragStart=local;gatewayStartX=dragArrival?gate.spawnX:gate.x;gatewayStartY=dragArrival?gate.spawnY:gate.y;}
                    else{var portal=gatewayWorld.Map(int.Parse(mapKey)).portals.LastOrDefault(p=>p.cells.Any(c=>c.x==hoverX&&c.y==hoverY));selectedNativePortal=portal?.id;selectedGateway=null;}
                }
                e.Use();Repaint();
            }
            if(e.type==EventType.MouseDrag&&e.button==0&&sizingGateway&&gate!=null&&inside&&hoverX>=0)
            {
                int left=Math.Min(areaStartX,hoverX),top=Math.Min(areaStartY,hoverY),w=Math.Abs(hoverX-areaStartX)+1,h=Math.Abs(hoverY-areaStartY)+1;
                if(sizingArrival){gate.spawnX=left;gate.spawnY=top;gate.spawnWidth=w;gate.spawnHeight=h;}
                else{gate.x=left;gate.y=top;gate.width=w;gate.height=h;}e.Use();Repaint();
            }
            if(e.type==EventType.MouseDrag&&e.button==0&&dragGateway&&gate!=null)
            {
                if(!gatewayRemembered){RememberGateway();gatewayRemembered=true;}
                int x=Mathf.Clamp(gatewayStartX+Mathf.RoundToInt((local.x-gatewayDragStart.x)/(32*zoom)),0,Math.Max(0,document.width-(dragArrival?gate.SpawnWidth:gate.Width)));
                int y=Mathf.Clamp(gatewayStartY+Mathf.RoundToInt((local.y-gatewayDragStart.y)/(32*zoom)),0,Math.Max(0,document.height-(dragArrival?gate.SpawnHeight:gate.Height)));
                if(dragArrival){gate.spawnX=x;gate.spawnY=y;}else{gate.x=x;gate.y=y;}e.Use();Repaint();
            }
            if(e.type==EventType.MouseUp){dragGateway=sizingGateway=false;gatewayRemembered=false;}
            if(e.type==EventType.KeyDown&&e.keyCode==KeyCode.Escape){dragGateway=sizingGateway=pickNpc=pickSpawn=false;e.Use();}
        }
        private void DrawGateways()
        {
            foreach(var unit in source.layout.units)DrawActor(unit.unit,unit.job.ToString(),unit.x,unit.y,new Color(.6f,.6f,.6f),false);
            foreach(var portal in gatewayWorld.Map(int.Parse(mapKey)).portals)
            {
                if(gatewayDocument.gates.Any(g=>g.fromMap==int.Parse(mapKey)&&g.replacesPortal==portal.id))continue;
                foreach(var cell in portal.cells)Outline(TileRect(cell.x,cell.y),selectedNativePortal==portal.id?Color.yellow:new Color(.4f,.8f,1,.6f),2);
                var cell0=portal.cells.First();var r=TileRect(cell0.x,cell0.y);GUI.Label(new Rect(r.x,r.y-15,200,15),"기존 "+portal.id+" → "+portal.targetMap,small);
            }
            foreach(var gate in gatewayDocument.gates)
            {
                if(gate.fromMap==int.Parse(mapKey)&&gate.x>=0&&gate.y>=0)
                {
                    var r=TileRect(gate.x,gate.y);
                    if(gate.IsWalk){r.width*=Math.Max(1,gate.Width);r.height*=Math.Max(1,gate.Height);EditorGUI.DrawRect(r,new Color(0,.8f,1,.18f));}
                    else{var texture=ActorPicture(gate.npcJob.ToString());if(texture!=null)DrawActorPicture(r,texture);}
                    Outline(r,gate.id==selectedGateway?Color.yellow:gate.enabled?Color.cyan:Color.gray,2);GUI.Label(new Rect(r.x,r.y-15,220,15),gate.name+" → "+gate.targetMap+(gate.IsWalk?" ("+gate.Width+"×"+gate.Height+")":""),small);
                }
                if(gate.targetMap==int.Parse(mapKey)&&gate.spawnX>=0&&gate.spawnY>=0)
                {
                    var r=TileRect(gate.spawnX,gate.spawnY);r.width*=Math.Max(1,gate.SpawnWidth);r.height*=Math.Max(1,gate.SpawnHeight);
                    var color=gate.id==selectedGateway?Color.green:new Color(.3f,.65f,.4f);EditorGUI.DrawRect(r,new Color(color.r,color.g,color.b,.18f));Outline(r,color,2);
                    GUI.Label(new Rect(r.x,r.y-15,260,15),gate.name+" 도착 "+gate.SpawnWidth+"×"+gate.SpawnHeight+" (출발 "+gate.fromMap+")",small);
                }
            }
        }
    }
}
