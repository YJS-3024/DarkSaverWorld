using System;
using System.Collections.Generic;
using System.Linq;
using DarkSaver.Original.Entry;
using DarkSaver.Original.Tutorial;
using UnityEngine;
using DarkSaver.Original.UI;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace DarkSaver.Original.World
{
    public sealed partial class WorldSceneRoot
    {
        private RectTransform battleSpellMenu,battleSpellEffects;
        private bool showingBattleSpells,skillMode,itemMode;
        private string selectedBattleItem;
        private bool CastingSelection=>trainingMode==TrainingAction.CastMagic||trainingMode==TrainingAction.UseSkill||trainingMode==TrainingAction.UseItem;
        private int[] BattleSpellChoices=>skillMode?view?.record?.growth?.skills?.Where(id=>definition.magicProjectiles?.SupportsSkill(definition.magicMenu?.Find(id))==true).ToArray():view?.battleMagic;
        private int selectedBattleMagic=-1;
        private readonly List<(Button hit,RawImage image,int slot)> battleSpellButtons=new List<(Button,RawImage,int)>();
        private readonly Dictionary<string,RawImage> battleSpellImages=new Dictionary<string,RawImage>();
        private readonly List<RawImage> battleAreaCells=new List<RawImage>();
        private readonly List<RaycastResult> battleAreaHits=new List<RaycastResult>();
        private int SelectedMagicDistance=>definition.magicMenu?.Find(selectedBattleMagic)?.distance??0;
        private int SelectedMagicArea=>definition.magicMenu?.Find(selectedBattleMagic)?.range??0;
        private bool SelectedMagicGroup=>definition.magicMenu?.Find(selectedBattleMagic)?.unit==8;
        private BattleMagicRule SelectedMagicRule=>itemMode?view?.battle?.Commander?.itemRules?.FirstOrDefault(r=>r.item==view.record.inventory.FirstOrDefault(i=>i.id==selectedBattleItem)?.item)?.ForMagic(selectedBattleMagic):view?.battle?.Commander?.magicRules?.FirstOrDefault(r=>r.magic==selectedBattleMagic);
        private bool SelectedAllowsSelf=>SelectedMagicDistance==0||SelectedMagicRule?.targets?.Contains(0)==true;
        private bool CanSelectMagicUnit(TrainingUnit unit)=>(unit.hp>0||SelectedMagicRule?.allowDeadTargets==true)&&SelectedMagicRule?.targets?.Contains(unit.id)==true;
        private bool AnyBattleMagicAvailable()=>view?.battleMagic?.Any(m=>BattleSpellAvailable(m,false))==true;
        private bool AnyBattleSkillAvailable()=>view?.record?.growth?.skills?.Any(m=>BattleSpellAvailable(m,true))==true;
        private bool BattleMagicAvailable(int magic)=>itemMode?BattleItemAvailable(view?.record?.inventory?.FirstOrDefault(i=>i.id==selectedBattleItem)):BattleSpellAvailable(magic,skillMode);
        private bool BattleSpellAvailable(int magic,bool skill)
        {
            var actor=view?.battle?.Commander;var rule=actor?.magicRules?.FirstOrDefault(r=>r.magic==magic);var metadata=definition.magicMenu?.Find(magic);
            return OriginalCommandRange.Living(actor)&&OriginalBattleTicks.Allows(actor,skill?TrainingAction.UseSkill:TrainingAction.CastMagic)&&!view.record.ghost&&view.battle.pending==null&&!view.moving
                &&(skill?view.record.growth?.skills?.Contains(magic)==true&&definition.magicProjectiles?.SupportsSkill(metadata)==true:view.record.growth?.magic?.Contains(magic)==true&&definition.magicProjectiles?.Supports(metadata)==true)&&rule?.enabled==true&&rule.mpCost.HasValue&&rule.apCost.HasValue&&rule.targets!=null
                &&actor.mp>=rule.mpCost.Value&&actor.ap>=rule.apCost.Value&&server.HasBattleAuthority(DisplayMap(view.record.map).training);
        }
        private void CancelBattleMagicSelection(){showingBattleSpells=false;showingBattleOrders=false;}
        private void BuildBattleMagicUi()
        {
            StopMagicAudio();
            if(battleSpellMenu!=null)Destroy(battleSpellMenu.gameObject);battleSpellImages.Clear();battleSpellButtons.Clear();battleAreaCells.Clear();showingBattleSpells=false;selectedBattleMagic=-1;skillMode=false;itemMode=false;selectedBattleItem=null;
            battleSpellEffects=Group(mapRoot,"Approved projectile effects",new Rect(0,0,0,0));
            battleSpellMenu=Group(worldWindows,"Original surrounding magic menu",new Rect(0,0,96,96));
            for(int slot=0;slot<9;slot++)
            {
                int index=slot;
                var hit=Hit(battleSpellMenu,"Confirmed spell slot"+slot,new Rect(32+OriginalBattleMagicMenu.OffsetX(slot),32+OriginalBattleMagicMenu.OffsetY(slot),32,32),()=>
                {
                    if(itemMode)
                    {
                        var item=BattleItemAt(index);if(!BattleItemAvailable(item))return;
                        selectedBattleItem=item.id;selectedBattleMagic=definition.battleItems.Find(item.item).magic;showingBattleSpells=false;trainingMode=TrainingAction.UseItem;Paint();return;
                    }
                    var list=BattleSpellChoices;if(list==null||index>=list.Length||!BattleMagicAvailable(list[index]))return;
                    selectedBattleMagic=list[index];showingBattleSpells=false;trainingMode=skillMode?TrainingAction.UseSkill:TrainingAction.CastMagic;Paint();
                });
                var image=Picture((RectTransform)hit.transform,"Native magic icon",new Rect(0,0,32,32),art.battle.icons);battleSpellButtons.Add((hit,image,slot));
            }
            battleSpellMenu.gameObject.SetActive(false);BuildBattleOrders();
        }
        private void PaintBattleMagic()
        {
            PaintBattleOrders();PaintBattleMagicArea();
            bool visible=BattleMap()&&showingBattleSpells&&(itemMode?AnyBattleItemAvailable():skillMode?AnyBattleSkillAvailable():AnyBattleMagicAvailable())&&notice==null&&view.facility==null;
            battleSpellMenu.gameObject.SetActive(visible);
            if(visible)
            {
                Place(battleSpellMenu,new Rect((float)view.renderX*32+mapRoot.anchoredPosition.x-32,(float)view.renderY*32-mapRoot.anchoredPosition.y-32,96,96));
                foreach(var button in battleSpellButtons)
                {
                    if(itemMode)
                    {
                        var item=BattleItemAt(button.slot);button.hit.gameObject.SetActive(item!=null);button.hit.interactable=BattleItemAvailable(item);button.image.enabled=item!=null;
                        if(item!=null){button.image.texture=ItemIcon(definition.Item(item.item)?.picture??-1);button.image.uvRect=new Rect(0,0,1,1);}continue;
                    }
                    button.image.texture=art.battle.icons;
                    var list=BattleSpellChoices;int magic=list!=null&&button.slot<list.Length?list[button.slot]:-1;
                    button.hit.gameObject.SetActive(magic>=0);button.hit.interactable=magic>=0&&BattleMagicAvailable(magic);var entry=definition.magicMenu?.Find(magic);
                    button.image.enabled=entry!=null&&entry.iconX>=0&&entry.iconX<640&&entry.iconY>=32&&entry.iconY<160;
                    if(button.image.enabled)button.image.uvRect=NativeEntryLayout.Uv(art.battle.icons,new Rect(entry.iconX,NativeBattleMenuIcons.NormalY(entry.iconY,BattleIconClock),32,32));
                }
            }
            var effects=view.presentation?.completed==false?CampaignMagic():view.battle?.magicEffects??Array.Empty<BattleMagicEffect>();var frames=BattleMagicPresentation.Frames(effects);var current=new HashSet<string>(frames.Select(f=>f.key));
            foreach(var id in battleSpellImages.Keys.Where(id=>!current.Contains(id)).ToArray()){Destroy(battleSpellImages[id].gameObject);battleSpellImages.Remove(id);}
            foreach(var entry in frames)
            {
                var frame=entry.frame;
                if(!battleSpellImages.TryGetValue(entry.key,out var image))
                {image=Picture(battleSpellEffects,"Native approved magic part",new Rect(0,0,32,32),art.battle.effects);battleSpellImages.Add(entry.key,image);}
                image.transform.SetAsLastSibling();
                image.texture=frame.surface==1?art.battle.attackEffects:frame.surface==2?Texture2D.whiteTexture:art.battle.effects;
                if(frame.surface==2){Place((RectTransform)image.transform,new Rect(-mapRoot.anchoredPosition.x,mapRoot.anchoredPosition.y,640,480));image.uvRect=new Rect(0,0,1,1);continue;}
                if(frame.surface==3)
                {
                    var actor=view.battle?.Unit(frame.unit);var job=definition.combatCatalog.Job(actor?.job??character.job);
                    image.texture=frame.unit==0?CommanderPicture:art.Picture(job?.picture??-1)??campaign?.Picture(actor?.job??-1);
                    if(image.texture!=null){int cell=image.texture.width/3;image.uvRect=NativeEntryLayout.Uv(image.texture,new Rect(0,(actor?.direction??1)*cell,cell,cell));}
                }
                else image.uvRect=NativeEntryLayout.Uv(image.texture,new Rect(frame.atlasX,frame.atlasY,frame.sourceWidth>0?frame.sourceWidth:frame.width,frame.sourceHeight>0?frame.sourceHeight:frame.height));
                Place((RectTransform)image.transform,new Rect((float)frame.x,(float)frame.y,frame.width,frame.height));
            }
            PaintMagicAudio(effects);
        }
        private void CastBattleMagicAt(int x,int y)
        {
            if(skillMode&&SelectedMagicDistance<0&&!SkillPoint(x,y,out x,out y))return;
            var result=itemMode?(DisplayMap(view.record.map).training?server.Request_TrainingItemAt(selectedBattleItem,x,y):server.Request_BattleItemAt(selectedBattleItem,x,y)):skillMode?(DisplayMap(view.record.map).training?server.Request_TrainingSkillAt(selectedBattleMagic,x,y):server.Request_BattleSkillAt(selectedBattleMagic,x,y)):(DisplayMap(view.record.map).training?server.Request_TrainingMagicAt(selectedBattleMagic,x,y):server.Request_BattleMagicAt(selectedBattleMagic,x,y));
            HandleMove(result);if(result.Accepted)trainingMode=null;
        }
        private void PaintBattleMagicArea()
        {
            foreach(var cell in battleAreaCells)cell.gameObject.SetActive(false);
            if(!CastingSelection||SelectedMagicDistance>=0&&SelectedMagicArea<=0&&!SelectedMagicGroup||!BattleMagicAvailable(selectedBattleMagic)||notice!=null||view.facility!=null||showingActions||showingBattleSpells)return;
            var events=EventSystem.current;if(events==null)return;
            battleAreaHits.Clear();events.RaycastAll(new PointerEventData(events){position=Input.mousePosition},battleAreaHits);
            if(battleAreaHits.Count==0||battleAreaHits[0].gameObject==null||!battleAreaHits[0].gameObject.transform.IsChildOf(mapRoot))return;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(mapRoot,Input.mousePosition,null,out var point))return;
            int centerX=Mathf.FloorToInt(point.x/32),centerY=Mathf.FloorToInt(-point.y/32);var map=DisplayMap(mapId).map;
            if(skillMode&&SelectedMagicDistance<0)
            {
                if(!SkillPoint(centerX,centerY,out int px,out int py))return;int index=0;var m=definition.magicMenu.Find(selectedBattleMagic);
                for(int x=0;x<map.width;x++)for(int y=0;y<map.height;y++)
                    if(BattleSkillArea.Weight(m,view.battle.Commander.Tile,px,py,x,y)>0)
                        PaintMagicAreaCell(ref index,x,y,view.battle.units.Any(u=>u.x==x&&u.y==y&&CanSelectMagicUnit(u)));
                return;
            }
            if(SelectedMagicDistance==0){centerX=view.record.x;centerY=view.record.y;}
            if(!map.Contains(new TutorialTile(centerX,centerY))||TutorialGeometry.Distance(view.battle.Commander.Tile,new TutorialTile(centerX,centerY))>SelectedMagicDistance)return;
            var rule=SelectedMagicRule;
            if(SelectedMagicGroup)
            {
                var selected=view.battle.units.FirstOrDefault(u=>CanSelectMagicUnit(u)&&u.x==centerX&&u.y==centerY);
                if(selected==null)return;int index=0;
                foreach(var target in BattleMagicArea.GroupTargets(view.battle.units,rule.targets,selected.id,rule.allowDeadTargets==true))PaintMagicAreaCell(ref index,target.x,target.y,true);
                return;
            }
            var targets=new HashSet<TutorialTile>(BattleMagicArea.Targets(view.battle.units,rule.targets,centerX,centerY,SelectedMagicArea,rule.allowDeadTargets==true).Select(t=>new TutorialTile(t.x,t.y)));
            int i=0;
            for(int x=0;x<map.width;x++)for(int y=0;y<map.height;y++)
            {
                if(BattleMagicArea.Weight(centerX,centerY,x,y,SelectedMagicArea)<=0)continue;
                PaintMagicAreaCell(ref i,x,y,targets.Contains(new TutorialTile(x,y)));
            }
        }
        private WorldOwnedItem BattleItemAt(int slot)
        {
            var rule=view?.battle?.Commander?.itemRules?.FirstOrDefault(r=>r.menuSlot==slot);
            return rule==null?null:view.record.inventory.Where(i=>i.item==rule.item&&i.remainingUses!=0).OrderBy(i=>i.slot).FirstOrDefault();
        }
        private bool AnyBattleItemAvailable()=>Enumerable.Range(0,9).Any(slot=>BattleItemAvailable(BattleItemAt(slot)));
        private bool BattleItemAvailable(WorldOwnedItem item)
        {
            if(item==null||item.remainingUses==0)return false;var actor=view?.battle?.Commander;var rule=actor?.itemRules?.FirstOrDefault(r=>r.item==item.item);var link=definition.battleItems?.Find(item.item);
            return OriginalCommandRange.Living(actor)&&OriginalBattleTicks.Allows(actor,TrainingAction.UseItem)&&!view.record.ghost&&view.battle.pending==null&&!view.moving&&definition.Item(item.item)?.equipType==7
                &&link!=null&&definition.magicProjectiles?.SupportsEffect(definition.magicMenu?.Find(link.magic))==true
                &&rule?.enabled==true&&rule.mpCost.HasValue&&rule.apCost.HasValue&&rule.targets!=null&&actor.mp>=rule.mpCost&&actor.ap>=rule.apCost
                &&server.HasBattleAuthority(DisplayMap(view.record.map).training);
        }
        private bool SkillPoint(int x,int y,out int px,out int py)
        {
            px=x;py=y;int d=SelectedMagicDistance;var actor=view.battle.Commander;
            if(BattleSkillArea.Directional(d))
            {
                int dx=x-actor.x,dy=y-actor.y;if(dx==0&&dy==0)return false;
                px=System.Math.Abs(dx)>=System.Math.Abs(dy)?dx>0?-1:-2:dy<0?-3:-4;py=-1;
            }
            else if(d==-2||d==-5){px=actor.x;py=actor.y;}
            return BattleSkillArea.ValidPoint(definition.magicMenu.Find(selectedBattleMagic),actor.Tile,px,py,DisplayMap(mapId).map);
        }
        private void PaintMagicAreaCell(ref int index,int x,int y,bool target)
        {
            if(index==battleAreaCells.Count)battleAreaCells.Add(Picture(trainingRanges,"Native magic target cell",new Rect(0,0,31,31),art.battle.ranges[2]));
            var image=battleAreaCells[index++];image.texture=art.battle.ranges[target?3:2];image.gameObject.SetActive(true);Place((RectTransform)image.transform,new Rect(x*32,y*32,31,31));
        }
    }
}
