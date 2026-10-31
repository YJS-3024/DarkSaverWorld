using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using DarkSaver.Original.Campaign;

namespace DarkSaver.Original.Editor.Scenarios
{
    public sealed partial class ScenarioEditorWindow
    {
        private const string GatewayPath=BasePath+"Data/Campaign/UserGateways.json";
        [SerializeField] private ScenarioGatewaySettings gatewayDocument;
        [SerializeField] private bool dirtyGateways;
        [SerializeField] private string gatewayHash,selectedGateway;
        private GatewayMapCatalog gatewayCatalog;
        private bool pickNpc,pickSpawn,dragGateway,gatewayRemembered;
        private Vector2 gatewayDragStart;
        private int gatewayStartX,gatewayStartY;
        private readonly System.Collections.Generic.List<byte[]> gatewayUndo=new System.Collections.Generic.List<byte[]>(),gatewayRedo=new System.Collections.Generic.List<byte[]>();
        private GatewayMapInfo MapInfo(string key)=>gatewayCatalog.maps.First(m=>m.key==key);
        private void LoadGateways(bool preserve)
        {
            gatewayCatalog=JsonUtility.FromJson<GatewayMapCatalog>(File.ReadAllText(BasePath+"Data/Campaign/GatewayMaps.json"));
            if(preserve)return;
            var bytes=File.ReadAllBytes(GatewayPath);gatewayDocument=ScenarioGatewayData.Decode(bytes);ScenarioGatewayData.Validate(gatewayDocument,gatewayCatalog);
            gatewayHash=ScenarioGatewayData.Hash(bytes);dirtyGateways=false;
        }
        private void RememberGateway()
        {
            gatewayUndo.Add(ScenarioGatewayData.Encode(gatewayDocument));if(gatewayUndo.Count>100)gatewayUndo.RemoveAt(0);gatewayRedo.Clear();
            dirtyGateways=hasUnsavedChanges=true;message="NPC 입구: 저장하지 않은 변경";Repaint();
        }
        private void GatewayHistory(bool back)
        {
            var from=back?gatewayUndo:gatewayRedo;var to=back?gatewayRedo:gatewayUndo;if(from.Count==0)return;
            to.Add(ScenarioGatewayData.Encode(gatewayDocument));gatewayDocument=ScenarioGatewayData.Decode(from[from.Count-1]);from.RemoveAt(from.Count-1);
            dirtyGateways=hasUnsavedChanges=true;dragGateway=pickNpc=pickSpawn=false;Repaint();
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
                dirtyGateways=false;hasUnsavedChanges=dirty;AssetDatabase.Refresh();message="NPC 입구 저장: "+GatewayPath;return true;
            }
            catch(Exception e){message=e.Message;return false;}
        }
        private void ReloadGateways()
        {
            if(dirtyGateways&&!EditorUtility.DisplayDialog("NPC 입구 다시 열기","저장하지 않은 입구 편집을 버리고 저장본을 불러올까요?","불러오기","취소"))return;
            try{LoadGateways(false);gatewayUndo.Clear();gatewayRedo.Clear();hasUnsavedChanges=dirty;message="NPC 입구를 다시 불러왔습니다.";}catch(Exception e){message=e.Message;}
        }
        private void DrawGatewayInspector(Rect rect)
        {
            GUILayout.BeginArea(rect,EditorStyles.helpBox);inspectorScroll=EditorGUILayout.BeginScrollView(inspectorScroll);
            GUILayout.Label("시나리오 NPC 입구",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("시나리오당 NPC 1명\nNPC 클릭 → 방 목록 → 참가\n사용자 기억에 따른 연결이며 원작 좌표가 아닙니다.",MessageType.Info);
            bool apply=EditorGUILayout.ToggleLeft("저장한 연결을 로컬 서버에서 사용",gatewayDocument.applyToGame);
            if(apply!=gatewayDocument.applyToGame){RememberGateway();gatewayDocument.applyToGame=apply;}
            GUILayout.Label("적용은 다음 Play/프로필 진입부터",EditorStyles.miniLabel);
            var map=MapInfo(mapKey);
            using(new EditorGUI.DisabledScope(map.kind!="world"&&map.kind!="town"))
                if(GUILayout.Button("이 맵에 NPC 입구 추가"))
                {RememberGateway();var g=new ScenarioGateway{id=Guid.NewGuid().ToString("N"),fromMap=map.id,fromHash=map.hash,targetMap=-1,npcJob=-1,x=-1,y=-1,spawnX=-1,spawnY=-1,name=""};gatewayDocument.gates=gatewayDocument.gates.Concat(new[]{g}).ToArray();selectedGateway=g.id;}
            foreach(var link in gatewayDocument.gates)
                if(GUILayout.Button((link.id==selectedGateway?"● ":"")+link.fromMap+" → "+link.targetMap+" "+link.name,EditorStyles.miniButton))selectedGateway=link.id;
            var gate=gatewayDocument.gates.FirstOrDefault(g=>g.id==selectedGateway);
            if(gate!=null)
            {
                GUILayout.Space(7);GUILayout.Label("선택 입구",EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();string name=EditorGUILayout.TextField("NPC 이름",gate.name);bool enabled=EditorGUILayout.Toggle("이 NPC 연결 사용",gate.enabled);
                if(EditorGUI.EndChangeCheck()){RememberGateway();gate.name=name;gate.enabled=enabled;}
                var npcs=gatewayCatalog.npcJobs;var options=new[]{"외형을 선택하세요"}.Concat(npcs.Select(id=>jobs.TryGetValue(id,out var j)?j.ToString():id.ToString())).ToArray();
                int npcAt=Array.IndexOf(npcs,gate.npcJob)+1,npc=EditorGUILayout.Popup("NPC 외형",npcAt,options);
                if(npc!=npcAt){RememberGateway();gate.npcJob=npc==0?-1:npcs[npc-1];}
                var targets=gatewayCatalog.maps.Where(m=>m.kind=="scenario").OrderBy(m=>m.id).ToArray();
                int at=Array.FindIndex(targets,m=>m.id==gate.targetMap)+1,next=EditorGUILayout.Popup("대상 시나리오",at,new[]{"시나리오를 선택하세요"}.Concat(targets.Select(m=>m.key+" "+m.title)).ToArray());
                if(next!=at){RememberGateway();gate.targetMap=next==0?-1:targets[next-1].id;gate.targetHash=next==0?"":targets[next-1].hash;gate.spawnX=gate.spawnY=-1;}
                EditorGUI.BeginChangeCheck();int x=EditorGUILayout.DelayedIntField("NPC X",gate.x),y=EditorGUILayout.DelayedIntField("NPC Y",gate.y);
                int sx=EditorGUILayout.DelayedIntField("도착 X",gate.spawnX),sy=EditorGUILayout.DelayedIntField("도착 Y",gate.spawnY);
                if(EditorGUI.EndChangeCheck()){RememberGateway();gate.x=x;gate.y=y;gate.spawnX=sx;gate.spawnY=sy;}
                if(GUILayout.Button("출발 맵에서 NPC 위치 찍기")){pickNpc=true;pickSpawn=false;SelectMap(gatewayCatalog.maps.First(m=>m.id==gate.fromMap).key);}
                using(new EditorGUI.DisabledScope(gate.targetMap<0))
                    if(GUILayout.Button("도착 맵에서 진입 위치 찍기")){pickSpawn=true;pickNpc=false;SelectMap(gatewayCatalog.maps.First(m=>m.id==gate.targetMap).key);}
                if(GUILayout.Button("선택 NPC 입구 삭제")){RememberGateway();gatewayDocument.gates=gatewayDocument.gates.Where(g=>g.id!=selectedGateway).ToArray();selectedGateway=null;}
            }
            GUILayout.BeginHorizontal();using(new EditorGUI.DisabledScope(gatewayUndo.Count==0))if(GUILayout.Button("되돌리기"))GatewayHistory(true);
            using(new EditorGUI.DisabledScope(gatewayRedo.Count==0))if(GUILayout.Button("다시 실행"))GatewayHistory(false);GUILayout.EndHorizontal();
            if(GUILayout.Button("NPC 입구 연결 저장"))SaveGateways();
            if(pickNpc||pickSpawn)EditorGUILayout.HelpBox(pickNpc?"지도에서 NPC가 설 타일을 클릭하세요.":"지도에서 캐릭터가 도착할 타일을 클릭하세요.",MessageType.Info);
            if(!string.IsNullOrEmpty(message))EditorGUILayout.HelpBox(message,MessageType.None);
            EditorGUILayout.EndScrollView();GUILayout.EndArea();
        }
        private void ProcessGatewayInput(Event e,bool inside,Vector2 local)
        {
            if(e.type==EventType.KeyDown&&(e.control||e.command)&&GUIUtility.keyboardControl==0&&e.keyCode==KeyCode.Z){GatewayHistory(!e.shift);e.Use();return;}
            var gate=gatewayDocument.gates.FirstOrDefault(g=>g.id==selectedGateway);
            if(e.type==EventType.MouseDown&&e.button==0&&inside&&hoverX>=0)
            {
                if(gate!=null&&((pickNpc&&gate.fromMap==int.Parse(mapKey))||(pickSpawn&&gate.targetMap==int.Parse(mapKey))))
                {RememberGateway();if(pickNpc){gate.x=hoverX;gate.y=hoverY;}else{gate.spawnX=hoverX;gate.spawnY=hoverY;}pickNpc=pickSpawn=false;}
                else
                {gate=gatewayDocument.gates.LastOrDefault(g=>g.fromMap==int.Parse(mapKey)&&g.x==hoverX&&g.y==hoverY);if(gate!=null){selectedGateway=gate.id;dragGateway=true;gatewayRemembered=false;gatewayDragStart=local;gatewayStartX=gate.x;gatewayStartY=gate.y;}}
                e.Use();Repaint();
            }
            if(e.type==EventType.MouseDrag&&e.button==0&&dragGateway&&gate!=null)
            {
                if(!gatewayRemembered){RememberGateway();gatewayRemembered=true;}
                gate.x=Mathf.Clamp(gatewayStartX+Mathf.RoundToInt((local.x-gatewayDragStart.x)/(32*zoom)),0,document.width-1);gate.y=Mathf.Clamp(gatewayStartY+Mathf.RoundToInt((local.y-gatewayDragStart.y)/(32*zoom)),0,document.height-1);e.Use();Repaint();
            }
            if(e.type==EventType.MouseUp){dragGateway=false;gatewayRemembered=false;}
        }
        private void DrawGateways()
        {
            foreach(var unit in source.layout.units)DrawActor(unit.unit,unit.job.ToString(),unit.x,unit.y,new Color(.6f,.6f,.6f),false);
            foreach(var gate in gatewayDocument.gates)
            {
                if(gate.fromMap==int.Parse(mapKey)&&gate.x>=0&&gate.y>=0)
                {
                    var r=TileRect(gate.x,gate.y);var texture=ActorPicture(gate.npcJob.ToString());if(texture!=null)GUI.DrawTexture(r,texture,ScaleMode.ScaleToFit,true);
                    Outline(r,gate.id==selectedGateway?Color.yellow:Color.cyan,2);GUI.Label(new Rect(r.x,r.y-15,150,15),gate.name+" → "+gate.targetMap,small);
                }
                if(gate.targetMap==int.Parse(mapKey)&&gate.spawnX>=0&&gate.spawnY>=0)DrawMarker(new ScenarioMarker{x=gate.spawnX,y=gate.spawnY,kind="local",label="사용자 지정 도착 칸",file=GatewayPath},Color.green);
            }
        }
    }
}
