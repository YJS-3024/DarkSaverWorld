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
        private readonly List<(RawImage image, int picture,int x,int y)> npcImages = new List<(RawImage, int,int,int)>();
        private RectTransform actions;
        private bool showingActions;
        private WorldFailure previousFailure;
        private string portalInFlight;
        private void Awake()
        {
            originalMusicEnabled=DarkSaver.Original.UI.OriginalClientPreferences.Current.Music;
            server = GetComponent<LocalServerManager>();
            var codec=Resources.Load<TextAsset>("OriginalCodePage949");if(codec!=null)
            {
                worldTextCodec=JsonUtility.FromJson<DarkSaver.Original.UI.NativeCodePage949>(codec.text);server.ConfigureClientText(worldTextCodec);
                var cache=new DarkSaver.Original.UI.NativeCandidateCacheFile(System.IO.Path.Combine(Application.persistentDataPath,"OriginalClient","Candid.Dat"));
                server.ConfigureCandidateCacheWriter(cache.TryWrite);
                byte[] candidate=null;if(cache.Exists)cache.TryRead(out candidate);else candidate=Resources.Load<TextAsset>("OriginalCandidateCache")?.bytes;
                if(candidate!=null)server.ConfigureCandidateCache(candidate,cache.TryWrite);
            }
            tutorial = GetComponent<TutorialSceneRoot>();
            tutorial.WorldEntryReady += Begin;server.Notify_ScenarioEffect+=ReceiveCampaignEffect;server.Notify_MapEntered+=HandleMusicMapEntered;server.Notify_WhisperReceptionChanged+=ReceiveWhisperReception;server.Notify_WorldMessageReceived+=ReceiveWorldMessage;
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
            tutorial.enabled = false; running = true; canvasObject.SetActive(true); view = reply.View;ResetTransientClientWindows();
            if(server.CurrencyAuthority!=null)Handle(server.Request_CurrencyState());
            Paint();
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
            screen.gameObject.AddComponent<DarkSaver.Original.UI.NativeTextSurface>();
            font=Font.CreateDynamicFontFromOSFont(new[]{"굴림","Gulim","Malgun Gothic","Apple SD Gothic Neo","Noto Sans CJK KR"},12);ownsFont=font!=null;
            if(font==null)font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if(EventSystem.current==null)new GameObject("Original World Events",typeof(EventSystem),typeof(StandaloneInputModule));
            else if(!EventSystem.current.GetComponents<BaseInputModule>().Any(m=>m.isActiveAndEnabled))EventSystem.current.gameObject.AddComponent<StandaloneInputModule>();
            worldWindows=Group(screen,"Facility windows",new Rect(0,0,640,480));
            overlay=Group(screen,"Native notices",new Rect(0,0,640,480));
            BuildPersonalWindows();BuildWorldChat();
        }
        private void BuildMap(int displayMap)
        {
            if(mapRoot!=null){mapRoot.gameObject.SetActive(false);Destroy(mapRoot.gameObject);}
            ClearCampaignMap();
            npcImages.Clear();ClearNativeBodies();showingActions=false;mapId=displayMap;builtCampaignGeometry=CampaignGeometryKey(displayMap);var m=DisplayMap(mapId);
            mapRoot=Group(screen,"Native map"+mapId,new Rect(0,0,m.map.width*32,m.map.height*32));mapRoot.SetAsFirstSibling();
            var native = m.ground < 0 ? LoadCampaignMap(m.id) : LoadAnimatedWorldMap(m.id);
            Picture(mapRoot,"Ground without atlas seams",new Rect(0,0,m.map.width*32,m.map.height*32),native == null ? art.grounds[m.ground] : CampaignTexture(native.rgba,native.width,native.height));
            BuildMapAnimation(native);
            var hitPlane=Group(mapRoot,"Native grid input",new Rect(0,0,m.map.width*32,m.map.height*32));hitPlane.gameObject.AddComponent<Image>().color=Color.clear;
            hitPlane.gameObject.AddComponent<NativeMapPointer>().Configure(TrainingGridClick);
            nativeBodyBack=Group(mapRoot,"Native first body pass",new Rect(0,0,0,0));
            nativeBodyAttributes=native?.renderAttributes;
            playerHit=(RectTransform)Hit(mapRoot,"Commander",new Rect(0,0,32,32),()=>{if(BattleMap()&&view.facility==null&&!view.moving){if(CastingSelection){TrainingUnitClick(0);return;}OpenTrainingActions(0);}}).transform;
            var clicks=playerHit.gameObject.AddComponent<EventTrigger>();var doubleClick=new EventTrigger.Entry{eventID=EventTriggerType.PointerClick};
            doubleClick.callback.AddListener(e=>{var p=(PointerEventData)e;if(p.button==PointerEventData.InputButton.Left&&p.clickCount==2&&!BattleMap())TogglePersonal(18);});clicks.triggers.Add(doubleClick);
            player=Picture(playerHit,"Native commander",new Rect(0,0,32,32),CommanderPicture);
            foreach(var npc in m.npcs)
            {
                string id=npc.id;var hit=Hit(mapRoot,npc.name,new Rect(npc.x*32,npc.y*32,32,32),()=>Handle(server.Request_InteractNpc(id)));
                var image=Picture((RectTransform)hit.transform,"Native staff",new Rect(0,0,32,32),art.Picture(npc.picture)??campaign?.Picture(npc.picture));npcImages.Add((image,npc.picture,npc.x,npc.y));
            }
            BuildCampaignActors();BuildTrainingBodies();
            // Native passes: ground/detail -> bodies -> MRC sprites in file order -> labels.
            for(int i=0;i<m.decorations.Length;i++)
            {var d=m.decorations[i];Picture(mapRoot,"MRC sprite"+i,new Rect(d.x,d.y,d.width,d.height),art.decorations[d.picture]);}
            if(native!=null&&m.ground<0)foreach(var d in native.foreground)Picture(mapRoot,"Native foreground",new Rect(d.x,d.y,d.width,d.height),CampaignTexture(d.rgba,d.width,d.height));
            nativeBodyFront=Group(mapRoot,"Native foreground body parts",new Rect(0,0,0,0));
            foreach(var npc in m.npcs){var name=Label(mapRoot,"NPC name",new Rect(npc.x*32-40,npc.y*32-16,112,16),npc.name,TextAnchor.MiddleCenter);name.color=Color.yellow;}
            playerName=Label(mapRoot,"Commander name after native foreground",new Rect(0,0,112,16),character.name,TextAnchor.MiddleCenter);playerName.color=Color.yellow;
            BuildTrainingRanges();BuildActions();BuildBattleMagicUi(); worldWindows.SetAsLastSibling();overlay.SetAsLastSibling();
        }
        private void BuildActions()
        {
            if(actions!=null)Destroy(actions.gameObject);
            trainingButtons.Clear();
            actions=Group(worldWindows,"Training actor menu",new Rect(0,0,96,96));
            var specs=new[]{(0,0,0,4),(32,0,3,7),(64,0,1,5),(0,32,2,6),(64,32,12,9),(0,64,8,8),(32,64,4,10),(64,64,5,11)};
            foreach(var s in specs)
            {
                int icon=s.Item3;
                var hit=Hit(actions,"Action"+icon,new Rect(s.Item1,s.Item2,32,32),()=>
                {
                    TrainingMenuAction(icon);
                });
                bool available=TrainingActionAvailable(icon);
                hit.interactable=available;
                var image=Picture((RectTransform)hit.transform,"Native action",new Rect(0,0,32,32),art.battle.icons,new Rect((available?icon:s.Item4)*32,DarkSaver.Original.UI.NativeBattleMenuIcons.ActionY(available,BattleIconClock),32,32));
                trainingButtons.Add((icon,s.Item4,hit,image));
            }
        }
        private void Update()
        {
            campaignVisuals.Advance(Time.unscaledDeltaTime*1000.0);
            if(!running)return;
            AdvanceCampaignMusic();
            if(HandleClientMessageInput()||HandleCommanderNameInput()||HandleBankAmountInput())return;
            if(WaitingScenarioResult)
            {if(notice!=null&&(Input.GetKeyDown(KeyCode.Escape)||Input.GetMouseButtonDown(1)))CloseNotice();return;}
            if(Input.GetKeyDown(KeyCode.Escape)||Input.GetMouseButtonDown(1))
            {trainingMode=null;CancelBattleMagicSelection();if(notice!=null)CloseNotice();else if(panel!=null)Handle(server.Request_CloseFacility());else showingActions=false;}
            if(BattleMap())
            {
                if(notice==null&&view.presentation?.completed!=false&&!clientMessageState.Visible)
                    for(int key=0;key<12;key++)if(Input.GetKeyDown((KeyCode)((int)KeyCode.F1+key)))
                        DarkSaver.Original.UI.NativeBattleOptions.Key(0x70+key,ActiveBattleCommanders,true,BattleDialogRequest);
            }
            else if(Input.GetKeyDown(KeyCode.F9))ShowQuit();
            if(Input.GetKeyDown(KeyCode.F1)&&!BattleMap())ToggleUtility(33);
            if(Input.GetKeyDown(KeyCode.F8)&&!BattleMap())ToggleUtility(27);
            if(Input.GetKeyDown(KeyCode.F6)&&!BattleMap())ToggleUtility(81);
            if(Input.GetKeyDown(KeyCode.F12)&&!BattleMap()&&PersonalAvailable)server.Request_SpaceMove();
            if(!BattleMap())for(int key=2;key<=5;key++)if(Input.GetKeyDown((KeyCode)((int)KeyCode.F1+key-1)))TogglePersonal(key==2?18:key==3?21:key==4?20:19);
        }
        private void LateUpdate()
        {
            if(!running)return;
            view=server.WorldSnapshot;if(view==null){running=false;canvasObject.SetActive(false);return;}
            if(view.pendingPortal!=null&&view.pendingPortal!=portalInFlight)
            {
                portalInFlight=view.pendingPortal;var gate=DisplayMap(view.record.map).portals.First(g=>g.id==portalInFlight);
                var reply=definition.campaign?.Map(gate.targetMap)!=null?server.Request_EnterMap(gate.targetMap):DisplayMap(gate.targetMap).training?server.Request_EnterTraining(portalInFlight):server.Request_EnterInterior(portalInFlight);
                Handle(reply);view=server.WorldSnapshot;
            }
            if(view.pendingPortal==null)portalInFlight=null;
            MaintainCurrencyState();MaintainRevivalQuote();MaintainWarehouseState();
            if(view.failure!=WorldFailure.None&&view.failure!=previousFailure)ShowFailure(view.failure);
            previousFailure=view.failure;Paint();
        }
        private void Paint()
        {
            if(view==null)return;
            SyncCampaignMapMusic();
            bool presenting=view.presentation!=null&&!view.presentation.completed;
            int displayMap=presenting&&DisplayMap(view.presentation.scene)!=null?view.presentation.scene:view.record.map;
            if(mapId!=displayMap||builtCampaignGeometry!=CampaignGeometryKey(displayMap))BuildMap(displayMap);var m=DisplayMap(mapId);
            float cameraX=presenting&&view.presentation.hasCamera?view.presentation.cameraX:(float)view.renderX;
            float cameraY=presenting&&view.presentation.hasCamera?view.presentation.cameraY:(float)view.renderY;
            float cx=Mathf.Round(Mathf.Clamp(cameraX*32+16-320,0,Mathf.Max(0,m.map.width*32-640)));
            if(!presenting&&BattleMap()&&battleCameraX.HasValue&&battleDialogMap==view.record.map)cx=Mathf.Clamp(battleCameraX.Value*32,0,Mathf.Max(0,m.map.width*32-640));
            float cy=Mathf.Round(Mathf.Clamp(cameraY*32+16-240,0,Mathf.Max(0,m.map.height*32-480)));
            if(!presenting&&BattleMap()&&battleCameraY.HasValue&&battleDialogMap==view.record.map)cy=Mathf.Clamp(battleCameraY.Value*32,0,Mathf.Max(0,m.map.height*32-480));
            Place(mapRoot,new Rect(-cx,-cy,m.map.width*32,m.map.height*32));Place(playerHit,new Rect((float)view.renderX*32,(float)view.renderY*32,32,32));
            PaintMapAnimation(cx,cy);
            playerName.text=CommanderName;
            Place((RectTransform)playerName.transform,new Rect((float)view.renderX*32-40,(float)view.renderY*32-16,112,16));
            nativeBodies.Clear();
            player.texture=view.record.ghost?art.Picture(898):view.battle?.Commander==null?CommanderPicture:CombatBodyPicture(view.battle.Commander);
            var commander=view.battle?.Commander;int appearance=view.record.ghost?898:commander==null?CurrentCommanderJob:OriginalBattleLifecycle.AppearanceKey(commander);
            PaintNativeBody(player,commander==null?view.record.direction:FeedbackDirection(0,commander.life,view.record.direction),view.moving,appearance,commander==null?definition.combatCatalog?.Job(CurrentCommanderJob)?.rawMovementType:OriginalMemberCommands.MovementType(commander,definition.combatCatalog),view.battle?.commanderMotion?.tileX??view.record.x,view.battle?.commanderMotion?.tileY??view.record.y,commander?.nativeState,0);
            foreach(var n in npcImages)PaintNativeBody(n.image,1,false,n.picture,definition.combatCatalog?.Job(n.picture)?.rawMovementType,n.x,n.y);
            MaintainTrainingSelection();
            actions.gameObject.SetActive(BattleMap()&&showingActions&&!presenting&&view.facility==null&&notice==null);
            PositionTrainingActions(cx,cy);
            PaintFacility();
            PaintTrainingStatus();
            PaintTrainingCombat();
            PaintCampaignActors(presenting);
            PaintNativeBodyLayers();
            PaintPersonalWindows();PaintUtilities();PaintWorldChat();
            PaintClassWindow();
            PaintScenarioResult();
        }
        private void Handle(WorldReply reply)
        {if(reply.View!=null)view=reply.View;previousFailure=reply.Failure;if(!reply.Accepted)ShowFailure(reply.Failure);}
        private void HandleMove(WorldReply reply)
        {
            if(reply.Failure==WorldFailure.InvalidInput||reply.Failure==WorldFailure.Moving||reply.Failure==WorldFailure.Busy)
            {if(reply.View!=null)view=reply.View;previousFailure=reply.Failure;return;}
            Handle(reply);
        }
        private static RectTransform Group(RectTransform parent,string name,Rect rect)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);Place(r,rect);return r;}
        private static void Place(RectTransform r,Rect b){r.anchoredPosition=new Vector2(b.x,-b.y);r.sizeDelta=b.size;}
        private static RawImage Picture(RectTransform parent,string name,Rect rect,Texture texture,Rect? source=null)
        {var i=Group(parent,name,rect).gameObject.AddComponent<RawImage>();i.texture=texture;i.raycastTarget=false;if(source.HasValue)i.uvRect=NativeEntryLayout.Uv(texture,source.Value);return i;}
        private Text Label(RectTransform parent,string name,Rect rect,string value,TextAnchor alignment=TextAnchor.UpperLeft)
        {var t=Group(parent,name,rect).gameObject.AddComponent<DarkSaver.Original.UI.NativeBitmapText>();t.font=font;t.fontSize=12;t.text=value;t.supportRichText=false;t.color=Color.black;t.alignment=alignment;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;}
        private Button Hit(RectTransform parent,string name,Rect rect,Action action)
        {var i=Group(parent,name,rect).gameObject.AddComponent<Image>();i.color=Color.clear;var b=i.gameObject.AddComponent<Button>();b.targetGraphic=i;b.transition=Selectable.Transition.None;b.onClick.AddListener(()=>action());return b;}
        private void OnDisable(){ClearClientMessage();FinishCommanderName(false);CancelBankAmount();server?.Request_CancelBankClient(null);server?.Request_CancelWarehouseClient(null);server?.Request_CancelShopClient(null);server?.Request_CancelMercenaryClient(null);server?.Request_CancelClassClient();ResetCampaignMusic();StopMagicAudio();running=false;if(canvasObject!=null)canvasObject.SetActive(false);}
        private void OnDestroy(){ResetBattleDialogs();if(facilitySelectionTexture!=null)Destroy(facilitySelectionTexture);SaveNativeFacilityFrames();SaveClassWindowPosition();if(clientMessageTexture!=null)Destroy(clientMessageTexture);SavePersonalPositions();SaveUtilityPositions();ResetCampaignMusic();if(server!=null){server.Notify_ScenarioEffect-=ReceiveCampaignEffect;server.Notify_MapEntered-=HandleMusicMapEntered;server.Notify_WhisperReceptionChanged-=ReceiveWhisperReception;server.Notify_WorldMessageReceived-=ReceiveWorldMessage;}StopMagicAudio();if(tutorial!=null)tutorial.WorldEntryReady-=Begin;ClearCampaignMap();ClearItemIcons();if(canvasObject!=null)Destroy(canvasObject);if(ownsFont&&font!=null)Destroy(font);}
    }
}
