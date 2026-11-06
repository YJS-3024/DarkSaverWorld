using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using DarkSaver.Original.Entry;
using DarkSaver.Original.Maps;
using DarkSaver.Original.Campaign;

namespace DarkSaver.Original.World
{
    public sealed partial class WorldSceneRoot
    {
        private bool CampaignCutscene=>OriginalCampaignInput.Cutscene(view?.presentation);
        private int nativeMessagePointerFrame=-1;
        private void AcknowledgeDirectedPointer()
        {
            if(!OriginalCampaignInput.DirectedMessage(view?.presentation,view?.nativeMessage)||nativeMessagePointerFrame==Time.frameCount)return;
            nativeMessagePointerFrame=Time.frameCount;Handle(server.Request_ContinueScenario());
        }
        private AudioSource campaignMusic;
        private readonly NativeScoreRepeat campaignMusicRepeat=new NativeScoreRepeat();
        private NativeMapMusicState nativeMapMusic;
        private bool originalMusicEnabled=true;
        private string musicWorldSession;
        private int musicWorldMap=-1;
        private string musicCleanupResult;
        private void StopCampaignMusic(){if(campaignMusic!=null)campaignMusic.Stop();campaignMusicRepeat.Stop();}
        private void ResetCampaignMusic(){StopCampaignMusic();nativeMapMusic=null;musicWorldSession=null;musicWorldMap=-1;}
        private void EnsureCampaignMusic()
        {if(nativeMapMusic==null&&campaign?.mapMusicTable!=null){nativeMapMusic=new NativeMapMusicState(campaign.MapMusic.Lookup,PlayCampaignClip,StopCampaignMusic);nativeMapMusic.SetEnabled(originalMusicEnabled);}}
        private void HandleMusicMapEntered(WorldReply reply){if(reply.Accepted)SyncCampaignMapMusic(true);}
        private void SyncCampaignMapMusic(bool entered=false)
        {
            var current=server.WorldSnapshot;if(current==null)return;EnsureCampaignMusic();
            if(nativeMapMusic==null||!entered&&musicWorldSession==current.campaignSession&&musicWorldMap==current.record.map)return;
            var ended=current.record.lastScenarioResult;
            if(musicWorldMap!=current.record.map&&ended?.native!=null&&ended.id!=musicCleanupResult)
            {
                // Original D0B0/ENDGAME posts590 before4CD restores the map.
                // D150 itself leaves the score playing through notice/result.
                nativeMapMusic.Stop();musicCleanupResult=ended.id;
            }
            musicWorldSession=current.campaignSession;musicWorldMap=current.record.map;nativeMapMusic.EnterMap(musicWorldMap);
        }
        public void SetOriginalMusicEnabled(bool enabled){originalMusicEnabled=enabled;EnsureCampaignMusic();nativeMapMusic?.SetEnabled(enabled);}
        private void AdvanceCampaignMusic()
        {
            if(campaignMusic==null||campaignMusic.clip==null)return;
            if(campaignMusic.clip.loadState==AudioDataLoadState.Failed){StopCampaignMusic();return;}
            if(campaignMusic.clip.loadState==AudioDataLoadState.Loading)return;
            if(campaignMusicRepeat.Observe(campaignMusic.isPlaying))campaignMusic.Play();
        }
        private bool PlayCampaignClip(string path)
        {
            // DirectMusic.Play stops its old segment before trying to open the new file.
            StopCampaignMusic();var clip=campaign?.Music(path);if(clip==null||clip.loadState==AudioDataLoadState.Failed)return false;
            if(campaignMusic==null){campaignMusic=gameObject.AddComponent<AudioSource>();campaignMusic.playOnAwake=false;campaignMusic.loop=false;campaignMusic.spatialBlend=0;}
            campaignMusic.clip=clip;campaignMusic.time=0;campaignMusic.Play();campaignMusicRepeat.Begin();return true;
        }
        private void PlayCampaignMusic(CampaignCommand command)
        {SyncCampaignMapMusic();if(nativeMapMusic!=null)nativeMapMusic.Play(command.args.FirstOrDefault());else PlayCampaignClip(command.args.FirstOrDefault());}
        private RectTransform campaignCombineLayer;
        private readonly List<RawImage> campaignCombineBodies=new List<RawImage>();
        private Image campaignCombineFlash;
        private readonly List<RawImage> campaignCombineMasks=new List<RawImage>();
        private void PaintCampaignCombine(bool presenting)
        {
            var combine=presenting?view.presentation.combine:null;
            if(campaignCombineLayer==null)
            {
                campaignCombineLayer=Group(worldWindows,"Native COMBINE_CLASS screen positions",new Rect(0,0,640,480));
                var flash=Group(campaignCombineLayer,"Native white transition",new Rect(0,0,640,480));
                campaignCombineFlash=flash.gameObject.AddComponent<Image>();campaignCombineFlash.raycastTarget=false;
            }
            campaignCombineLayer.gameObject.SetActive(combine!=null&&combine.phase<3);
            if(combine==null)return;
            foreach(var image in campaignCombineBodies)image.gameObject.SetActive(false);
            foreach(var image in campaignCombineMasks)image.gameObject.SetActive(false);
            int i=0;
            foreach(var body in combine.bodies)
            {
                if(i==campaignCombineBodies.Count)campaignCombineBodies.Add(Picture(campaignCombineLayer,"Native combine body",new Rect(0,0,32,32),null));
                var image=campaignCombineBodies[i++];image.gameObject.SetActive(body.job!=0);image.texture=campaign.Picture(body.job);
                if(image.texture!=null)image.uvRect=NativeEntryLayout.Uv(image.texture,new Rect(32,32,32,32));
                Place((RectTransform)image.transform,new Rect((float)body.renderX,(float)body.renderY,32,32));
            }
            var points=combine.phase==0?combine.bodies.Select(b=>(x:(float)b.renderX+16,y:(float)b.renderY+16)).ToArray():new[]{(x:(float)combine.centerX,y:(float)(combine.centerY+(combine.phase==2?combine.flash*6:0)))};
            int m=0;var mask=campaign.CombineMask(combine.maskArchive,combine.maskIndex);
            foreach(var point in points)
            {
                if(mask==null)continue;
                if(m==campaignCombineMasks.Count)campaignCombineMasks.Add(Picture(campaignCombineLayer,"Native RBM white mask",new Rect(0,0,0,0),mask));
                var image=campaignCombineMasks[m++];image.gameObject.SetActive(true);image.texture=mask;image.color=new Color(1,1,1,255f/256f);image.transform.SetAsLastSibling();
                Place((RectTransform)image.transform,new Rect(point.x-mask.width/2,point.y-mask.height/2,mask.width,mask.height));
            }
            campaignCombineFlash.color=new Color(1,1,1,combine.alpha/256f);campaignCombineFlash.transform.SetAsLastSibling();
            campaignCombineLayer.SetAsLastSibling();
        }
        private readonly CampaignVisualPlayback campaignVisuals=new CampaignVisualPlayback();
        private string campaignVisualSession;
        private void ReceiveCampaignEffect(CampaignCommand command)
        {
            if(!running||view==null)return;
            if(command.name=="PLAYMIDI"){PlayCampaignMusic(command);return;}
            if(command.name=="STOPMIDI"){SyncCampaignMapMusic();if(nativeMapMusic!=null)nativeMapMusic.Stop();else StopCampaignMusic();return;}
            if(campaignVisualSession!=view.campaignSession){campaignVisuals.Clear();campaignVisualSession=view.campaignSession;}
            var map=definition.Map(command.scene)?.map;if(map==null)return;
            campaignVisuals.Accept(command,definition.magicMenu,definition.magicProjectiles,map.width,map.height);
        }
        private BattleAttackEffect CampaignAttack(int id)=>view.presentation?.completed==false&&campaignVisuals.scene==mapId&&campaignVisuals.context==view.presentation.context&&campaignVisuals.attacks.TryGetValue(id,out var effect)?effect:null;
        private BattleMagicEffect[] CampaignMagic()=>view.presentation?.completed==false&&campaignVisuals.scene==mapId&&campaignVisuals.context==view.presentation.context?campaignVisuals.magic.ToArray():System.Array.Empty<BattleMagicEffect>();
        private readonly Dictionary<byte[],Texture2D> campaignTextures = new Dictionary<byte[],Texture2D>();
        private readonly Dictionary<int,RawImage> campaignBodies = new Dictionary<int,RawImage>();
        private readonly Dictionary<int,Text> campaignNames = new Dictionary<int,Text>();
        private RectTransform campaignActorLayer,campaignDialogue;
        private readonly List<Text> campaignMessageLines = new List<Text>();
        private RectTransform campaignPortraitMessage, campaignPlainMessage;
        private Text campaignPlainText;
        private RawImage campaignFace;
        private string campaignHeld,builtCampaignGeometry;
        private string CampaignGeometryKey(int id)=>string.Join("|",(view?.record.campaignProgress?.steps??System.Array.Empty<CampaignScriptStep>())
            .Where(s=>s.scene==id&&(s.command.StartsWith("CHANGESPRITE ",System.StringComparison.Ordinal)||s.command.StartsWith("CHANGETILE2 ",System.StringComparison.Ordinal))).Select(s=>s.key));
        private readonly Dictionary<int,(string key,WorldMapDefinition map)> campaignMaps=new Dictionary<int,(string,WorldMapDefinition)>();
        private WorldMapDefinition DisplayMap(int id)
        {
            var original=definition.Map(id);string key=CampaignGeometryKey(id);
            if(key.Length==0||campaign?.Definition.HasNativeMaps!=true)return original;
            if(campaignMaps.TryGetValue(id,out var cached)&&cached.key==key)return cached.map;
            var changed=campaign.Definition.ApplyChanges(id,view.record.campaignProgress.steps);var map=original.map;var copy=original.CopyForPlacement();
            copy.map=new Tutorial.TutorialMap{width=map.width,height=map.height,attributes=changed.attributes,renderAttributes=changed.renderAttributes,terrain=map.terrain,abilities=map.abilities};
            campaignMaps[id]=(key,copy);return copy;
        }
        private bool BattleMap() => DisplayMap(view.record.map).training || definition.campaign?.Map(view.record.map)?.kind == "scenario";
        private NativeMapPixels LoadCampaignMap(int id)
        {
            var steps=view?.record.campaignProgress?.steps;
            var bytes=CampaignGeometryKey(id).Length==0?campaign.MapBytes(id):campaign.Definition.ApplyChanges(id,steps).bytes;
            return NativeMapDecoder.RenderAnimatedLayers(bytes,graphic=>campaign.Graphic(graphic,id),MapRandomSeed());
        }
        private readonly Dictionary<string,uint> campaignMapSeeds=new Dictionary<string,uint>();
        private uint MapRandomSeed()
        {string key=(view?.campaignSession??"")+":"+mapId;if(!campaignMapSeeds.TryGetValue(key,out uint seed)){seed=unchecked((uint)System.DateTimeOffset.UtcNow.ToUnixTimeSeconds());campaignMapSeeds[key]=seed;}return seed;}
        private Texture2D CampaignTexture(byte[] bytes,int width,int height)
        {
            if(campaignTextures.TryGetValue(bytes,out var texture))return texture;
            texture=new Texture2D(width,height,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            var flipped=new byte[bytes.Length];int stride=width*4;
            for(int y=0;y<height;y++)System.Buffer.BlockCopy(bytes,y*stride,flipped,(height-1-y)*stride,stride);
            texture.LoadRawTextureData(flipped);texture.Apply(false,true);campaignTextures.Add(bytes,texture);return texture;
        }
        private void ClearCampaignMap()
        {
            animatedMap=null;mapAnimationTiles.Clear();mapAnimationImages.Clear();mapAnimationLayer=null;
            foreach(var texture in campaignTextures.Values)if(texture!=null)Destroy(texture);campaignTextures.Clear();campaignBodies.Clear();campaignNames.Clear();
            if(campaignDialogue!=null){campaignDialogue.gameObject.SetActive(false);Destroy(campaignDialogue.gameObject);}campaignDialogue=null;
            campaignHeld=null;
        }
        private void BuildCampaignActors()
        {
            campaignActorLayer=Group(mapRoot,"Native scenario bodies",new Rect(0,0,DisplayMap(mapId).map.width*32,DisplayMap(mapId).map.height*32));
        }
        private void PaintCampaignActors(bool presenting)
        {
            PaintCampaignCombine(presenting);
            if(presenting&&!string.IsNullOrEmpty(view.presentation.heldCommand)&&campaignHeld!=view.presentation.heldCommand)
            {campaignHeld=view.presentation.heldCommand;ShowFailure(WorldFailure.RulesUnavailable);}
            bool scriptedActors=presenting&&view.presentation.nativeEvent!=true;
            bool showPlayer=!scriptedActors&&LiveBattleBodyVisible(view.battle?.Commander);
            playerHit.gameObject.SetActive(showPlayer);playerName.gameObject.SetActive(showPlayer);
            foreach(var image in campaignBodies.Values)image.transform.parent.gameObject.SetActive(false);
            foreach(var name in campaignNames.Values)name.gameObject.SetActive(false);
            if(scriptedActors)
                foreach(var actor in view.presentation.actors)DrawCampaignActor(actor.unit,actor.job,actor.renderX,actor.renderY,actor.direction,actor.dead,actor.sourceJob=="COM0"?character.name:campaign.Definition.jobs.FirstOrDefault(j=>j.job==actor.job)?.name??actor.sourceJob,actor.moving,actor.x,actor.y);
            else if(view.battle==null&&view.record.campaignPhase!="outro"&&view.record.campaignPhase!="complete")
                foreach(var unit in view.units??System.Array.Empty<DarkSaver.Original.Campaign.CampaignUnit>())DrawCampaignActor(unit.unit,unit.job,unit.x,unit.y,unit.direction,false,unit.name);
            if(campaignDialogue==null)
            {
                campaignDialogue=Group(overlay,"Original scenario MESSAGE",new Rect(40,50,562,104));
                var hit=campaignDialogue.gameObject.AddComponent<Image>();hit.color=Color.clear;
                campaignDialogue.gameObject.AddComponent<Button>().onClick.AddListener(()=>{if(view?.presentation?.nativeEvent!=true)Handle(server.Request_ContinueScenario());});
                campaignDialogue.gameObject.AddComponent<CanvasGroup>();
                campaignPortraitMessage=Group(campaignDialogue,"Native portrait message",new Rect(0,0,562,104));
                Picture(campaignPortraitMessage,"Native dialogue frame",new Rect(0,0,562,104),art.battle.dialogue);
                campaignFace=Picture(campaignPortraitMessage,"Native speaker face",new Rect(20,20,64,64),null);
                campaignMessageLines.Clear();
                for(int line=0;line<3;line++)
                {
                    var text=Label(campaignPortraitMessage,"Native message line "+line,new Rect(110,23+line*20,437,18),"",TextAnchor.UpperLeft);
                    text.fontSize=12;text.color=Color.white;text.horizontalOverflow=HorizontalWrapMode.Overflow;campaignMessageLines.Add(text);
                }
                campaignPlainMessage=Group(campaignDialogue,"Native plain notice",new Rect(0,0,562,66));
                BuildCampaignNoticeFrame(campaignPlainMessage);
                campaignPlainText=Label(campaignPlainMessage,"Native centered notice",new Rect(0,23,562,18),"");
                campaignPlainText.color=Color.white;campaignPlainText.horizontalOverflow=HorizontalWrapMode.Overflow;
            }
            var dialogue=presenting?view.presentation:null;
            if(view.nativeMessage?.visible==true)
                dialogue=new CampaignPresentationView{message=view.nativeMessage.text,messageMode=view.nativeMessage.mode,face=view.nativeMessage.face,speaker=view.nativeMessage.speaker,
                    messageFirstLine=view.nativeMessage.firstLine,nativeEvent=true,nativeMessageVisible=true,messageResolved=true};
            bool visible=dialogue!=null&&(dialogue.nativeEvent?dialogue.nativeMessageVisible:!string.IsNullOrWhiteSpace(dialogue.message));campaignDialogue.gameObject.SetActive(visible);
            if(visible)
            {
                var input=campaignDialogue.GetComponent<CanvasGroup>();input.blocksRaycasts=!dialogue.nativeEvent;input.interactable=!dialogue.nativeEvent;
                campaignDialogue.SetAsLastSibling();
                bool plain=dialogue.messageMode==0;
                Place(campaignDialogue,new Rect(40,plain?130:dialogue.messageMode==1?50:300,562,plain?66:104));
                campaignPlainMessage.gameObject.SetActive(plain);campaignPortraitMessage.gameObject.SetActive(!plain);
                string message=dialogue.messageResolved?dialogue.message:dialogue.message.Replace("%S",character.name).Replace("%s",character.name);
                campaignPlainText.text=message;
                AnchorPersonalLine(campaignPlainText,280,23,6);
                var lines=CampaignPresentation.MessageLines(message);
                for(int i=0;i<campaignMessageLines.Count;i++)
                {int row=dialogue.messageFirstLine+i;campaignMessageLines[i].text=row<lines.Length?lines[row]:"";}
                campaignFace.texture=campaign.Face(dialogue.face);campaignFace.enabled=campaignFace.texture!=null;
            }
        }
        private void BuildCampaignNoticeFrame(RectTransform parent)
        {
            //08E10 rounds the odd right/bottom endpoints outward to even pixels.
            // Preserve its20px board tiles rather than stretching a portrait recess.
            void Tile(int sx,int sy,int x,int y)=>Picture(parent,"Native notice board tile",new Rect(x,y,20,20),art.battle.board,new Rect(sx,sy,20,20));
            for(int x=0;x<542;x+=20)for(int y=0;y<46;y+=20)Tile(20,320,x,y);
            for(int x=20;x<542;x+=20){Tile(20,300,x,0);Tile(20,340,x,46);}
            for(int y=20;y<46;y+=20){Tile(0,320,0,y);Tile(40,320,542,y);}
            Tile(0,300,0,0);Tile(0,340,0,46);Tile(40,300,542,0);Tile(40,340,542,46);
        }
        private void DrawCampaignActor(int id,int job,double x,double y,int direction,bool dead,string name,bool moving=false,int tileX=-1,int tileY=-1)
        {
            if(!campaignBodies.TryGetValue(id,out var image))
            {
                var hit=Hit(campaignActorLayer,"Native unit "+id,new Rect(0,0,32,32),()=>Handle(server.Request_CampaignUnitAction(id)));
                image=Picture((RectTransform)hit.transform,"Body",new Rect(0,0,32,32),null);campaignBodies[id]=image;
                var label=Label(mapRoot,"Native unit name",new Rect(0,0,112,16),name,TextAnchor.MiddleCenter);label.color=Color.yellow;campaignNames[id]=label;
            }
            image.transform.parent.gameObject.SetActive(!dead);image.texture=campaign.Picture(job);image.enabled=image.texture!=null;
            Place((RectTransform)image.transform.parent,new Rect((float)x*32,(float)y*32,32,32));
            PaintNativeBody(image,CampaignAttack(id)?.direction??direction,moving,job,definition.combatCatalog?.Job(job)?.rawMovementType,tileX<0?(int)x:tileX,tileY<0?(int)y:tileY,null,id);
            var text=campaignNames[id];text.text=name;text.gameObject.SetActive(!dead);Place((RectTransform)text.transform,new Rect((float)x*32-40,(float)y*32-16,112,16));
        }
    }
}
