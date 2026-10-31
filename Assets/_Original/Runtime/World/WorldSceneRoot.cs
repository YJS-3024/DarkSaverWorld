using System;
using System.Collections.Generic;
using System.Linq;
using DarkSaver.Original.Entry;
using DarkSaver.Original.Tutorial;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DarkSaver.Original.Campaign;

namespace DarkSaver.Original.World
{
    [DisallowMultipleComponent, RequireComponent(typeof(TutorialSceneRoot), typeof(LocalServerManager))]
    public sealed partial class WorldSceneRoot : MonoBehaviour
    {
        [SerializeField] private NativeWorldArt art = null;
        [SerializeField] private NativeCampaignArt campaign = null;
        private WorldDefinition definition;
        private LocalServerManager server;
        private TutorialSceneRoot tutorial;
        private EntryCharacter character;
        private GameObject canvasObject;
        private RectTransform screen, mapRoot, worldWindows, overlay;
        private Font font;
        private bool ownsFont, running;
        private int mapId = -1;
        private WorldView view;
        private RawImage player;
        private RectTransform playerHit;
        private Text playerName;
        private readonly List<(RawImage image, int picture)> npcImages = new List<(RawImage, int)>();
        private RectTransform actions;
        private bool showingActions;
        private WorldFailure previousFailure;
        private string portalInFlight;
        private void Awake()
        {
            server = GetComponent<LocalServerManager>(); tutorial = GetComponent<TutorialSceneRoot>();
            tutorial.WorldEntryReady += Begin;
        }
        private void Begin(EntryCharacter value)
        {
            if (value == null || !value.tutorialCompleted) return;
            if(art?.IsConfigured!=true){Debug.LogError("Original world assets are missing.",this);return;}
            character = value.Copy(); definition = art.Definition;
            if (canvasObject == null) Build();
            try { if(campaign?.IsConfigured==true)definition=campaign.Merge(definition); }
            catch(System.Exception e){Debug.LogWarning("NPC/map configuration rejected: "+e.Message,this);ShowFailure(WorldFailure.InvalidInput);return;}
            server.ConfigureCampaign(campaign);
            var reply = server.Request_BeginWorld(art);
            if (!reply.Accepted) { ShowFailure(reply.Failure); return; }
            tutorial.enabled = false; running = true; canvasObject.SetActive(true); view = reply.View; Paint();
        }
        private void Build()
        {
            canvasObject = new GameObject("Original World Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 210; canvas.pixelPerfect = true;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640,480); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var black = new GameObject("Letterbox",typeof(RectTransform),typeof(Image));black.transform.SetParent(canvas.transform,false);
            var br=(RectTransform)black.transform;br.anchorMin=Vector2.zero;br.anchorMax=Vector2.one;br.offsetMin=br.offsetMax=Vector2.zero;
            black.GetComponent<Image>().color=Color.black;black.GetComponent<Image>().raycastTarget=false;
            screen=Group((RectTransform)canvas.transform,"Native640x480",new Rect(0,0,640,480));screen.anchorMin=screen.anchorMax=new Vector2(.5f,.5f);screen.anchoredPosition=new Vector2(-320,240);
            screen.gameObject.AddComponent<RectMask2D>();
            font=Font.CreateDynamicFontFromOSFont(new[]{"굴림","Gulim","Malgun Gothic","Apple SD Gothic Neo","Noto Sans CJK KR"},12);ownsFont=font!=null;
            if(font==null)font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if(EventSystem.current==null)new GameObject("Original World Events",typeof(EventSystem),typeof(StandaloneInputModule));
            else if(!EventSystem.current.GetComponents<BaseInputModule>().Any(m=>m.isActiveAndEnabled))EventSystem.current.gameObject.AddComponent<StandaloneInputModule>();
            worldWindows=Group(screen,"Facility windows",new Rect(0,0,640,480));
            overlay=Group(screen,"Native notices",new Rect(0,0,640,480));
        }
        private void BuildMap(int displayMap)
        {
            if(mapRoot!=null){mapRoot.gameObject.SetActive(false);Destroy(mapRoot.gameObject);}
            ClearCampaignMap();
            npcImages.Clear();showingActions=false;mapId=displayMap;var m=definition.Map(mapId);
            mapRoot=Group(screen,"Native map"+mapId,new Rect(0,0,m.map.width*32,m.map.height*32));mapRoot.SetAsFirstSibling();
            var native = m.ground < 0 ? LoadCampaignMap(m.id) : null;
            Picture(mapRoot,"Ground without atlas seams",new Rect(0,0,m.map.width*32,m.map.height*32),native == null ? art.grounds[m.ground] : CampaignTexture(native.rgba,native.width,native.height));
            var hitPlane=Group(mapRoot,"Native grid input",new Rect(0,0,m.map.width*32,m.map.height*32));hitPlane.gameObject.AddComponent<Image>().color=Color.clear;
            hitPlane.gameObject.AddComponent<NativeMapPointer>().Configure((x,y)=>{showingActions=false;Handle(server.Request_MoveWorld(x,y));});
            playerHit=(RectTransform)Hit(mapRoot,"Commander",new Rect(0,0,32,32),()=>{if(BattleMap()&&view.facility==null&&!view.moving){showingActions=!showingActions;Paint();}}).transform;
            player=Picture(playerHit,"Native commander",new Rect(0,0,32,32),art.Picture(character.job));
            foreach(var npc in m.npcs)
            {
                string id=npc.id;var hit=Hit(mapRoot,npc.name,new Rect(npc.x*32,npc.y*32,32,32),()=>Handle(server.Request_InteractNpc(id)));
                var image=Picture((RectTransform)hit.transform,"Native staff",new Rect(0,0,32,32),art.Picture(npc.picture)??campaign?.Picture(npc.picture));npcImages.Add((image,npc.picture));
            }
            BuildCampaignActors();
            // Native passes: ground/detail -> bodies -> MRC sprites in file order -> labels.
            for(int i=0;i<m.decorations.Length;i++)
            {var d=m.decorations[i];Picture(mapRoot,"MRC sprite"+i,new Rect(d.x,d.y,d.width,d.height),art.decorations[d.picture]);}
            if(native!=null)foreach(var d in native.foreground)Picture(mapRoot,"Native foreground",new Rect(d.x,d.y,d.width,d.height),CampaignTexture(d.rgba,d.width,d.height));
            foreach(var npc in m.npcs){var name=Label(mapRoot,"NPC name",new Rect(npc.x*32-40,npc.y*32-16,112,16),npc.name,TextAnchor.MiddleCenter);name.color=Color.yellow;}
            playerName=Label(mapRoot,"Commander name after native foreground",new Rect(0,0,112,16),character.name,TextAnchor.MiddleCenter);playerName.color=Color.yellow;
            BuildActions(); worldWindows.SetAsLastSibling();overlay.SetAsLastSibling();
        }
        private void BuildActions()
        {
            if(actions!=null)Destroy(actions.gameObject);
            actions=Group(worldWindows,"Training actor menu",new Rect(0,0,96,96));
            var specs=new[]{(0,0,0,4),(32,0,3,7),(64,0,1,5),(0,32,2,6),(64,32,12,9),(0,64,8,8),(32,64,4,10),(64,64,5,11)};
            foreach(var s in specs)
            {
                int icon=s.Item3;
                var hit=Hit(actions,"Action"+icon,new Rect(s.Item1,s.Item2,32,32),()=>
                {
                    showingActions=false;
                    if(icon==5)ShowTrainingOptions();
                    else if(icon!=0)ShowFailure(WorldFailure.RulesUnavailable);
                });
                bool available=icon==0||icon==5;
                hit.interactable=available;
                Picture((RectTransform)hit.transform,"Native action",new Rect(0,0,32,32),art.battle.icons,new Rect((available?icon:s.Item4)*32,available?0:128,32,32));
            }
        }
        private void Update()
        {
            if(!running)return;
            if(Input.GetKeyDown(KeyCode.Escape)||Input.GetMouseButtonDown(1))
            {if(notice!=null)CloseNotice();else if(panel!=null)Handle(server.Request_CloseFacility());else showingActions=false;}
            if(Input.GetKeyDown(KeyCode.F9))
            {if(definition.Map(view.record.map).training)ShowTrainingEscape();else ShowQuit();}
            if(Input.GetKeyDown(KeyCode.F2)&&BattleMap())ToggleTrainingStatus();
        }
        private void LateUpdate()
        {
            if(!running)return;
            view=server.WorldSnapshot;if(view==null){running=false;canvasObject.SetActive(false);return;}
            if(view.pendingPortal!=null&&view.pendingPortal!=portalInFlight)
            {
                portalInFlight=view.pendingPortal;var gate=definition.Map(view.record.map).portals.First(g=>g.id==portalInFlight);
                var reply=definition.campaign?.Map(gate.targetMap)!=null?server.Request_EnterMap(gate.targetMap):definition.Map(gate.targetMap).training?server.Request_EnterTraining(portalInFlight):server.Request_EnterInterior(portalInFlight);
                Handle(reply);view=server.WorldSnapshot;
            }
            if(view.pendingPortal==null)portalInFlight=null;
            if(view.failure!=WorldFailure.None&&view.failure!=previousFailure)ShowFailure(view.failure);
            previousFailure=view.failure;Paint();
        }
        private void Paint()
        {
            if(view==null)return;
            bool presenting=view.presentation!=null&&!view.presentation.completed;
            int displayMap=presenting&&definition.Map(view.presentation.scene)!=null?view.presentation.scene:view.record.map;
            if(mapId!=displayMap)BuildMap(displayMap);var m=definition.Map(mapId);
            float cameraX=presenting&&view.presentation.hasCamera?view.presentation.cameraX:(float)view.renderX;
            float cameraY=presenting&&view.presentation.hasCamera?view.presentation.cameraY:(float)view.renderY;
            float cx=Mathf.Round(Mathf.Clamp(cameraX*32+16-320,0,Mathf.Max(0,m.map.width*32-640)));
            float cy=Mathf.Round(Mathf.Clamp(cameraY*32+16-240,0,Mathf.Max(0,m.map.height*32-480)));
            Place(mapRoot,new Rect(-cx,-cy,m.map.width*32,m.map.height*32));Place(playerHit,new Rect((float)view.renderX*32,(float)view.renderY*32,32,32));
            Place((RectTransform)playerName.transform,new Rect((float)view.renderX*32-40,(float)view.renderY*32-16,112,16));
            int frame=new[]{0,1,2,1}[(int)(view.clock/125)%4];player.texture=art.Picture(view.record.ghost?898:character.job);
            player.uvRect=NativeEntryLayout.Uv(player.texture,new Rect(frame*32,view.record.direction*32,32,32));
            foreach(var n in npcImages)n.image.uvRect=NativeEntryLayout.Uv(n.image.texture,new Rect(frame*32,32,32,32));
            actions.gameObject.SetActive(BattleMap()&&showingActions&&!presenting&&view.facility==null&&notice==null);
            Place(actions,new Rect((float)view.renderX*32-cx-32,(float)view.renderY*32-cy-32,96,96));
            PaintFacility();
            PaintTrainingStatus();
            PaintCampaignActors(presenting);
        }
        private void Handle(WorldReply reply)
        {if(reply.View!=null)view=reply.View;if(!reply.Accepted)ShowFailure(reply.Failure);else if(view?.failure==WorldFailure.None)previousFailure=WorldFailure.None;}
        private static RectTransform Group(RectTransform parent,string name,Rect rect)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);Place(r,rect);return r;}
        private static void Place(RectTransform r,Rect b){r.anchoredPosition=new Vector2(b.x,-b.y);r.sizeDelta=b.size;}
        private static RawImage Picture(RectTransform parent,string name,Rect rect,Texture texture,Rect? source=null)
        {var i=Group(parent,name,rect).gameObject.AddComponent<RawImage>();i.texture=texture;i.raycastTarget=false;if(source.HasValue)i.uvRect=NativeEntryLayout.Uv(texture,source.Value);return i;}
        private Text Label(RectTransform parent,string name,Rect rect,string value,TextAnchor alignment=TextAnchor.UpperLeft)
        {var t=Group(parent,name,rect).gameObject.AddComponent<Text>();t.font=font;t.fontSize=12;t.text=value;t.supportRichText=false;t.color=Color.black;t.alignment=alignment;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;}
        private Button Hit(RectTransform parent,string name,Rect rect,Action action)
        {var i=Group(parent,name,rect).gameObject.AddComponent<Image>();i.color=Color.clear;var b=i.gameObject.AddComponent<Button>();b.targetGraphic=i;b.transition=Selectable.Transition.None;b.onClick.AddListener(()=>action());return b;}
        private void OnDisable(){running=false;if(canvasObject!=null)canvasObject.SetActive(false);}
        private void OnDestroy(){if(tutorial!=null)tutorial.WorldEntryReady-=Begin;ClearCampaignMap();if(canvasObject!=null)Destroy(canvasObject);if(ownsFont&&font!=null)Destroy(font);}
    }
}
