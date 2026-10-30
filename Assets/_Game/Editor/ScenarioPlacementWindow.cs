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
        [SerializeField] private int scenarioIndex;
        private int selected=-1,wave,job=301;
        [SerializeField] private string path,original;
        private string error;
        [SerializeField] private Scenario01Definition data;
        private Scene preview;
        private Camera cameraView;
        private RenderTexture texture;
        private Tilemap ground;
        private Vector2 scroll;
        [SerializeField] private bool dirty;
        private bool dragging,showAll=true,showIntro=true;
        private float zoom=1;
        private Rect canvas;
        private string[] names;
        private readonly Dictionary<int,Sprite> sprites=new Dictionary<int,Sprite>();
        [MenuItem("DarkSaver/시나리오 편집툴")]
        public static void Open()=>GetWindow<ScenarioPlacementWindow>("시나리오 편집툴");
        private void OnEnable()
        {
            names=ScenarioCatalog.All.Select(s=>s.Key+" · "+s.Title).ToArray();
            if(data!=null)
            {
                var edited=data;var baseline=original;bool changed=dirty;dirty=false;
                Load(Mathf.Clamp(scenarioIndex,0,names.Length-1));data=edited;original=baseline;dirty=changed;
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
            Release();data=null;dirty=false;selected=-1;error=null;scenarioIndex=index;
            try
            {
                var scenario=ScenarioCatalog.All[index];path="Assets/_Game/Resources/Scenarios/"+scenario.Key+"/Content.json";
                original=File.ReadAllText(path);data=JsonUtility.FromJson<Scenario01Definition>(original);
                if(data==null || data.width<=0 || data.height<=0)throw new InvalidDataException("잘못된 시나리오 크기");
                if(data.units==null)data.units=Array.Empty<Scenario01Unit>();
                string scenePath=AssetDatabase.FindAssets("t:Scene").Select(AssetDatabase.GUIDToAssetPath)
                    .FirstOrDefault(p=>Path.GetFileNameWithoutExtension(p)==scenario.MapScene);
                if(scenePath==null)throw new FileNotFoundException("맵 씬을 찾을 수 없습니다: "+scenario.MapScene);
                preview=EditorSceneManager.OpenPreviewScene(scenePath);
                var maps=preview.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Tilemap>(true)).ToArray();
                ground=maps.FirstOrDefault(t=>t.name=="Ground") ?? maps.FirstOrDefault();
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
            string prefix=ScenarioCatalog.All[scenarioIndex].Key=="01"?"Scenarios/01/Characters/":"Scenarios/Shared/Characters/";
            s=Resources.Load<Sprite>(prefix+id+"_Down_0");sprites[id]=s;return s;
        }
        private void Marker(Vector2 pos,Color color,string label,Sprite sprite,bool active)
        {
            var rect=new Rect(pos.x-12,pos.y-12,24,24);EditorGUI.DrawRect(rect,active?Color.white:color);
            if(sprite!=null){var r=sprite.textureRect;GUI.DrawTextureWithTexCoords(new Rect(rect.x+1,rect.y+1,22,22),sprite.texture,new Rect(r.x/sprite.texture.width,r.y/sprite.texture.height,r.width/sprite.texture.width,r.height/sprite.texture.height));}
            GUI.Label(new Rect(pos.x+14,pos.y-10,170,22),label,EditorStyles.whiteMiniLabel);
        }
        private void OnGUI()
        {
            if(names==null)OnEnable();
            hasUnsavedChanges=dirty;saveChangesMessage="변경한 시나리오 배치를 저장할까요?";
            using(new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                using(new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    int index=EditorGUILayout.Popup(scenarioIndex,names,GUILayout.Width(260));
                    if(index!=scenarioIndex || data==null && error==null)Load(index);
                    if(GUILayout.Button("다시 불러오기",EditorStyles.toolbarButton))Load(scenarioIndex);
                    if(GUILayout.Button(dirty?"저장 *":"저장",EditorStyles.toolbarButton))Save();
                    showAll=GUILayout.Toggle(showAll,"모든 웨이브",EditorStyles.toolbarButton);
                    showIntro=GUILayout.Toggle(showIntro,"연출 배치",EditorStyles.toolbarButton);
                    wave=EditorGUILayout.IntField(wave,GUILayout.Width(45));
                }
                if(error!=null)EditorGUILayout.HelpBox(error,MessageType.Error);
                if(data==null || cameraView==null)return;
                EditorGUILayout.HelpBox("빨강: 몬스터 · 파랑: 플레이어 진입점. 클릭 선택 / 드래그 이동 / Delete 삭제. 좌표는 원본 기준(좌상단 0,0). 저장은 실제 Content.json에 반영됩니다.",MessageType.Info);
                zoom=EditorGUILayout.Slider("확대 (맵 중앙)",zoom,1,4);
                using(new EditorGUILayout.HorizontalScope())
                {
                    float width=Mathf.Max(160,position.width-295),height=Mathf.Max(140,position.height-130);
                    canvas=GUILayoutUtility.GetRect(width,height,GUILayout.Width(width),GUILayout.Height(height));
                    DrawMap();
                    using(new EditorGUILayout.VerticalScope(GUILayout.Width(270)))DrawInspector();
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
            var size=ground.cellSize;cameraView.orthographicSize=Mathf.Max(data.height*Mathf.Abs(size.y),data.width*Mathf.Abs(size.x)/cameraView.aspect)/2/zoom+.6f;
            if(Event.current.type==EventType.Repaint){cameraView.targetTexture=texture;cameraView.Render();cameraView.targetTexture=null;GUI.DrawTexture(canvas,texture);}
            Handles.BeginGUI();Handles.color=new Color(1,1,1,.13f);
            Vector2 dx=Screen(1,0)-Screen(0,0),dy=Screen(0,1)-Screen(0,0);
            Vector2 corner=Screen(0,0)-(dx+dy)/2;
            for(int x=0;x<=data.width;x++)Handles.DrawLine(corner+dx*x,corner+dx*x+dy*data.height);
            for(int y=0;y<=data.height;y++)Handles.DrawLine(corner+dy*y,corner+dy*y+dx*data.width);
            Handles.EndGUI();
            for(int i=0;i<data.units.Length;i++)
            {
                var u=data.units[i];if(!showAll && u.wave!=wave)continue;
                Marker(Screen(u.x,u.y),new Color(.8f,.18f,.15f),u.id+" · "+u.name+" [W"+u.wave+"]",SpriteFor(u.job),selected==i);
            }
            if(showIntro && data.intro!=null)
                foreach(var command in data.intro)
                    if(command.name=="CHARPOS" && command.args!=null && command.args.Length>=4
                        && int.TryParse(command.args[2],out int cx) && int.TryParse(command.args[3],out int cy))
                    {
                        int.TryParse(command.args[0],out int introJob);
                        Marker(Screen(cx,cy),new Color(.1f,.65f,.2f),"연출 "+command.args[0]+" / "+command.args[1],SpriteFor(introJob),false);
                    }
            Marker(Screen(data.entryX,data.entryY),new Color(.1f,.4f,.95f),"캐릭터 진입",null,selected==-2);
            if(!GUI.enabled)return;
            var e=Event.current;
            if(e.type==EventType.MouseDown && e.button==0 && canvas.Contains(e.mousePosition))
            {
                selected=-1;float best=18;
                for(int i=data.units.Length-1;i>=0;i--){var u=data.units[i];if(!showAll && u.wave!=wave)continue;float d=Vector2.Distance(e.mousePosition,Screen(u.x,u.y));if(d<best){best=d;selected=i;}}
                if(Vector2.Distance(e.mousePosition,Screen(data.entryX,data.entryY))<12)selected=-2;
                dragging=selected!=-1;GUI.FocusControl(null);e.Use();Repaint();
            }
            if(e.type==EventType.MouseDrag && dragging && Cell(e.mousePosition,out var cell))
            {
                if(selected==-2){data.entryX=cell.x;data.entryY=cell.y;}else{data.units[selected].x=cell.x;data.units[selected].y=cell.y;}
                dirty=true;e.Use();Repaint();
            }
            if(e.type==EventType.MouseUp)dragging=false;
            if(e.type==EventType.KeyDown && e.keyCode==KeyCode.Delete && selected>=0 && !EditorGUIUtility.editingTextField){Delete();e.Use();}
        }
        private void Delete(){if(selected<0)return;data.units=data.units.Where((u,i)=>i!=selected).ToArray();selected=-1;dirty=true;}
        private void DrawInspector()
        {
            job=EditorGUILayout.IntField("추가할 직업 ID",job);
            if(GUILayout.Button("몬스터 추가"))
            {
                int id=Enumerable.Range(1,short.MaxValue).First(n=>!data.units.Any(u=>u.id==n));
                var u=new Scenario01Unit{id=id,job=job,name="새 몬스터",wave=Math.Max(0,wave),x=data.entryX,y=data.entryY,hp=1};
                data.units=data.units.Concat(new[]{u}).ToArray();selected=data.units.Length-1;dirty=true;
            }
            EditorGUI.BeginChangeCheck();
            if(selected==-2){data.entryX=EditorGUILayout.IntField("진입 X",data.entryX);data.entryY=EditorGUILayout.IntField("진입 Y",data.entryY);}
            else if(selected>=0 && selected<data.units.Length)
            {
                var u=data.units[selected];u.id=EditorGUILayout.IntField("유닛 ID",u.id);u.name=EditorGUILayout.TextField("이름",u.name);u.job=EditorGUILayout.IntField("직업 ID",u.job);
                u.x=EditorGUILayout.IntField("X",u.x);u.y=EditorGUILayout.IntField("Y",u.y);u.wave=EditorGUILayout.IntField("웨이브",u.wave);
                u.hp=EditorGUILayout.IntField("HP",u.hp);u.mp=EditorGUILayout.IntField("MP",u.mp);u.attack=EditorGUILayout.IntField("공격",u.attack);u.defense=EditorGUILayout.IntField("방어",u.defense);
                u.magicAttack=EditorGUILayout.IntField("마법 공격",u.magicAttack);u.magicDefense=EditorGUILayout.IntField("마법 방어",u.magicDefense);
                if(GUILayout.Button("선택 몬스터 삭제"))Delete();
            }
            if(EditorGUI.EndChangeCheck())dirty=true;
            EditorGUILayout.HelpBox("저장 시 배치 해시가 변경됩니다. 기존 진행 저장은 콘텐츠 불일치로 거부될 수 있으므로 새 검증 저장을 사용하세요. 삭제한 ID를 참조하는 이벤트/대사는 자동 변경하지 않습니다.",MessageType.Warning);
            EditorGUILayout.LabelField("몬스터 "+data.units.Length+"개");
            scroll=EditorGUILayout.BeginScrollView(scroll);
            for(int i=0;i<data.units.Length;i++){var u=data.units[i];if(GUILayout.Button(u.id+" "+u.name+" ("+u.x+","+u.y+") W"+u.wave))selected=i;}
            EditorGUILayout.EndScrollView();
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
            if(data==null || !dirty)return;
            try
            {
                bool Inside(int x,int y)=>x>=0 && y>=0 && x<data.width && y<data.height;
                if(!Inside(data.entryX,data.entryY) || data.units.Any(u=>!Inside(u.x,u.y) || u.id<=0 || u.id>short.MaxValue || u.job<0 || u.wave<0) || data.units.Select(u=>u.id).Distinct().Count()!=data.units.Length)
                    throw new InvalidDataException("좌표 범위·중복/잘못된 ID·직업·웨이브를 확인하세요.");
                if(File.ReadAllText(path)!=original)throw new IOException("외부에서 파일이 변경되었습니다. 다시 불러온 후 편집하세요.");
                string wrapper=JsonUtility.ToJson(new UnitList{units=data.units},true);string array=wrapper.Substring(wrapper.IndexOf('['));array=array.Substring(0,array.LastIndexOf(']')+1);
                string updated=ReplaceArray(original,array);
                updated=Regex.Replace(updated,"(\\\"entryX\\\"\\s*:\\s*)-?\\d+",m=>m.Groups[1].Value+data.entryX);
                updated=Regex.Replace(updated,"(\\\"entryY\\\"\\s*:\\s*)-?\\d+",m=>m.Groups[1].Value+data.entryY);
                using(var sha=SHA256.Create())
                {string hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(array+"|"+data.entryX+"|"+data.entryY))).Replace("-","").ToLowerInvariant();updated=Regex.Replace(updated,"(\\\"profileHash\\\"\\s*:\\s*\\\")[^\\\"]*(\\\")",m=>m.Groups[1].Value+hash+m.Groups[2].Value);}
                string temp=path+".placement-tmp";File.WriteAllText(temp,updated,new UTF8Encoding(false));File.Replace(temp,path,null);
                original=updated;dirty=false;AssetDatabase.ImportAsset(path);error=null;
            }
            catch(Exception e){error=e.Message;}
        }
    }
}
