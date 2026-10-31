using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using DarkSaver.Original.Entry;
using DarkSaver.Original.Maps;

namespace DarkSaver.Original.World
{
    public sealed partial class WorldSceneRoot
    {
        private readonly Dictionary<byte[],Texture2D> campaignTextures = new Dictionary<byte[],Texture2D>();
        private readonly Dictionary<int,RawImage> campaignBodies = new Dictionary<int,RawImage>();
        private readonly Dictionary<int,Text> campaignNames = new Dictionary<int,Text>();
        private RectTransform campaignActorLayer,campaignDialogue;
        private Text campaignMessage;
        private RawImage campaignFace;
        private string campaignHeld;
        private bool BattleMap() => definition.Map(view.record.map).training || definition.campaign?.Map(view.record.map)?.kind == "scenario";
        private NativeMapPixels LoadCampaignMap(int id) => NativeMapDecoder.RenderLayers(campaign.MapBytes(id),campaign.Graphic);
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
            foreach(var texture in campaignTextures.Values)if(texture!=null)Destroy(texture);campaignTextures.Clear();campaignBodies.Clear();campaignNames.Clear();
            if(campaignDialogue!=null){campaignDialogue.gameObject.SetActive(false);Destroy(campaignDialogue.gameObject);}campaignDialogue=null;
            campaignHeld=null;
        }
        private void BuildCampaignActors()
        {
            campaignActorLayer=Group(mapRoot,"Native scenario bodies",new Rect(0,0,definition.Map(mapId).map.width*32,definition.Map(mapId).map.height*32));
        }
        private void PaintCampaignActors(bool presenting)
        {
            if(presenting&&!string.IsNullOrEmpty(view.presentation.heldCommand)&&campaignHeld!=view.presentation.heldCommand)
            {campaignHeld=view.presentation.heldCommand;ShowFailure(WorldFailure.RulesUnavailable);}
            playerHit.gameObject.SetActive(!presenting);playerName.gameObject.SetActive(!presenting);
            foreach(var image in campaignBodies.Values)image.transform.parent.gameObject.SetActive(false);
            foreach(var name in campaignNames.Values)name.gameObject.SetActive(false);
            if(presenting)
                foreach(var actor in view.presentation.actors)DrawCampaignActor(actor.unit,actor.job,actor.renderX,actor.renderY,actor.direction,actor.dead,actor.sourceJob=="COM0"?character.name:campaign.Definition.jobs.FirstOrDefault(j=>j.job==actor.job)?.name??actor.sourceJob);
            else
                foreach(var unit in view.units??System.Array.Empty<DarkSaver.Original.Campaign.CampaignUnit>())DrawCampaignActor(unit.unit,unit.job,unit.x,unit.y,unit.direction,false,unit.name);
            if(campaignDialogue==null)
            {
                campaignDialogue=Group(overlay,"Original scenario MESSAGE",new Rect(40,50,562,104));
                var hit=campaignDialogue.gameObject.AddComponent<Image>();hit.color=Color.clear;
                campaignDialogue.gameObject.AddComponent<Button>().onClick.AddListener(()=>Handle(server.Request_ContinueScenario()));
                Picture(campaignDialogue,"Native dialogue frame",new Rect(0,0,562,104),art.battle.dialogue);
                campaignFace=Picture(campaignDialogue,"Native speaker face",new Rect(20,20,64,64),null);
                campaignMessage=Label(campaignDialogue,"Native message",new Rect(110,20,437,66),"");campaignMessage.color=Color.white;
            }
            bool visible=presenting&&!string.IsNullOrWhiteSpace(view.presentation.message);campaignDialogue.gameObject.SetActive(visible);
            if(visible)
            {
                campaignDialogue.SetAsLastSibling();campaignMessage.text=view.presentation.message.Replace('|','\n').Replace("%S",character.name).Replace("%s",character.name);
                campaignFace.texture=campaign.Face(view.presentation.face);campaignFace.enabled=campaignFace.texture!=null;
            }
        }
        private void DrawCampaignActor(int id,int job,double x,double y,int direction,bool dead,string name)
        {
            if(!campaignBodies.TryGetValue(id,out var image))
            {
                var hit=Hit(campaignActorLayer,"Native unit "+id,new Rect(0,0,32,32),()=>Handle(server.Request_CampaignUnitAction(id)));
                image=Picture((RectTransform)hit.transform,"Body",new Rect(0,0,32,32),null);campaignBodies[id]=image;
                var label=Label(mapRoot,"Native unit name",new Rect(0,0,112,16),name,TextAnchor.MiddleCenter);label.color=Color.yellow;campaignNames[id]=label;
            }
            image.transform.parent.gameObject.SetActive(!dead);image.texture=campaign.Picture(job);image.enabled=image.texture!=null;
            if(image.texture!=null)
            {int cell=image.texture.width/3,frame=new[]{0,1,2,1}[(int)(view.clock/125)%4];image.uvRect=NativeEntryLayout.Uv(image.texture,new Rect(frame*cell,direction*cell,cell,cell));}
            Place((RectTransform)image.transform.parent,new Rect((float)x*32,(float)y*32,32,32));
            var text=campaignNames[id];text.text=name;text.gameObject.SetActive(!dead);Place((RectTransform)text.transform,new Rect((float)x*32-40,(float)y*32-16,112,16));
        }
    }
}
