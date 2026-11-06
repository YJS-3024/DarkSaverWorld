using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DarkSaver.Original.UI;
using UnityEngine.UI;
using DarkSaver.Original.Tutorial;
using DarkSaver.Original.Entry;

namespace DarkSaver.Original.World
{
    public sealed partial class WorldSceneRoot
    {
        private RectTransform trainingBodies,trainingRanges;
        private readonly Dictionary<int,(RectTransform hit,RawImage image,Text name)> trainingImages=new Dictionary<int,(RectTransform,RawImage,Text)>();
        private readonly List<RawImage> trainingCells=new List<RawImage>();
        private readonly List<(int icon,int gray,Button hit,RawImage image)> trainingButtons=new List<(int,int,Button,RawImage)>();
        private TrainingAction? trainingMode;
        private long trainingRangeRevision=-1;
        private int previousRangeMagic=-1;
        private TrainingAction? previousTrainingMode;
        private void BuildTrainingBodies()
        {
            trainingImages.Clear();trainingCells.Clear();trainingActionUnit=0;previousRangeSource=-1;trainingMode=null;trainingRangeRevision=-1;previousTrainingMode=null;
            trainingBodies=Group(mapRoot,"Received battle units",new Rect(0,0,0,0));
        }
        private void BuildTrainingRanges()
        {
            trainingRanges=Group(mapRoot,"Native battle ranges above bodies",new Rect(0,0,0,0));
            BuildCommandRange();
            BuildBattleFeedback();
        }
        private bool TrainingActionAvailable(int icon)
        {
            if(trainingActionUnit!=0)return MemberActionAvailable(icon);
            if(icon==5)return view?.battle?.Commander==null||OriginalBattleTicks.AllowsIcon(view.battle.Commander,icon);
            if(view?.battle==null)return false; // Wait for the received combat roster; no town movement fallback.
            var unit=view.battle.Commander;
            if(!OriginalBattleTicks.AllowsIcon(unit,icon))return false;
            bool training=DisplayMap(view.record.map).training;
            bool authority=icon==4||((icon==0||icon==1||icon==8)?server.HasBattleAuthorityFor(training,icon==0?TrainingAction.Move:icon==1?TrainingAction.Attack:TrainingAction.Rest):server.HasBattleAuthority(training));
            if(!OriginalCommandRange.Living(unit)||view.moving||view.battle.pending!=null||!authority)return false;
            if(icon==2)return AnyBattleMagicAvailable();
            if(icon==12)return AnyBattleSkillAvailable();
            if(icon==3)return AnyBattleItemAvailable();
            if(icon==4)return BattleOrderAvailable();
            return icon==0?unit.canMove&&unit.movementBudget.HasValue&&OriginalMemberCommands.MovementType(unit,definition.combatCatalog).HasValue:icon==1?unit.canAttack&&unit.attackRange.HasValue:icon==8&&unit.canRest;
        }
        private void TrainingMenuAction(int icon)
        {
            if(trainingActionUnit!=0){MemberMenuAction(icon);return;}
            showingActions=false;trainingMode=null;CancelBattleMagicSelection();
            skillMode=icon==12;itemMode=icon==3;
            if(icon==2)showingBattleSpells=AnyBattleMagicAvailable();
            else if(icon==12)showingBattleSpells=AnyBattleSkillAvailable();
            else if(icon==3)showingBattleSpells=AnyBattleItemAvailable();
            else if(icon==4)showingBattleOrders=BattleOrderAvailable();
            else if(icon==5)ShowTrainingOptions();
            else if(icon==0&&view.battle!=null)trainingMode=TrainingAction.Move;
            else if(icon==1&&view.battle!=null)trainingMode=TrainingAction.Attack;
            else if(icon==8)Handle(DisplayMap(view.record.map).training?server.Request_TrainingRest():server.Request_BattleRest());
            Paint();
        }
        private void TrainingGridClick(int x,int y)
        {
            AcknowledgeDirectedPointer();
            battleStatusUnit=DarkSaver.Original.UI.NativeBattleStatusSelection.Click(view.battle?.units.FirstOrDefault(u=>u.x==x&&u.y==y&&u.hp>0)?.id??-1);
            showingActions=false;showingBattleSpells=false;showingBattleOrders=false;
            if(CastingSelection){if(SelectedMagicDistance<0||SelectedMagicArea>0&&!SelectedMagicGroup)CastBattleMagicAt(x,y);else{var target=view.battle?.units.FirstOrDefault(u=>CanSelectMagicUnit(u)&&u.x==x&&u.y==y);if(target!=null)TrainingUnitClick(target.id);}return;}
            if(view.battle==null){if(!BattleMap())HandleMove(server.Request_MoveWorld(x,y));return;}
            if(trainingMode==TrainingAction.Move)
            {bool training=DisplayMap(view.record.map).training;var reply=trainingActionUnit==0?(training?server.Request_TrainingMove(x,y):server.Request_BattleMove(x,y)):(training?server.Request_TrainingMemberMove(trainingActionUnit,x,y):server.Request_BattleMemberMove(trainingActionUnit,x,y));HandleMove(reply);if(reply.Accepted)trainingMode=null;}
        }
        private void TrainingUnitClick(int id)
        {
            AcknowledgeDirectedPointer();
            battleStatusUnit=DarkSaver.Original.UI.NativeBattleStatusSelection.Click(id);
            if(CastingSelection)
            {
                if(SelectedMagicDistance<0||SelectedMagicArea>0&&!SelectedMagicGroup){var target=view.battle?.Unit(id);if(target!=null)CastBattleMagicAt(target.x,target.y);return;}
                var result=itemMode?(DisplayMap(view.record.map).training?server.Request_TrainingItem(selectedBattleItem,id):server.Request_BattleItem(selectedBattleItem,id)):skillMode?(DisplayMap(view.record.map).training?server.Request_TrainingSkill(selectedBattleMagic,id):server.Request_BattleSkill(selectedBattleMagic,id)):(DisplayMap(view.record.map).training?server.Request_TrainingMagic(selectedBattleMagic,id):server.Request_BattleMagic(selectedBattleMagic,id));HandleMove(result);if(result.Accepted)trainingMode=null;return;
            }
            if(id>=0&&id<10){OpenTrainingActions(id);return;}
            if(trainingMode!=TrainingAction.Attack)return;
            bool training=DisplayMap(view.record.map).training;var reply=trainingActionUnit==0?(training?server.Request_TrainingAttack(id):server.Request_BattleAttack(id)):(training?server.Request_TrainingMemberAttack(trainingActionUnit,id):server.Request_BattleMemberAttack(trainingActionUnit,id));HandleMove(reply);if(reply.Accepted)trainingMode=null;
        }
        private void PaintTrainingCombat()
        {
            MaintainTrainingSelection();
            foreach(var button in trainingButtons)
            {
                bool enabled=TrainingActionAvailable(button.icon);button.hit.interactable=enabled;
                button.image.uvRect=NativeEntryLayout.Uv(art.battle.icons,new Rect((enabled?button.icon:button.gray)*32,NativeBattleMenuIcons.ActionY(enabled,BattleIconClock),32,32));
            }
            var combat=view.battle;
            foreach(var pair in trainingImages)pair.Value.hit.gameObject.SetActive(false);
            if(combat!=null&&(view.presentation?.completed!=false||view.presentation.nativeEvent))
            {
                foreach(var u in combat.units.Where(u=>u.id!=0))
                {
                    if(!trainingImages.TryGetValue(u.id,out var sprite))
                    {
                        int id=u.id;var hit=(RectTransform)Hit(trainingBodies,"Battle unit"+id,new Rect(0,0,32,32),()=>TrainingUnitClick(id)).transform;
                        sprite=(hit,Picture(hit,"Native directional body",new Rect(0,0,32,32),null),Label(hit,"Name",new Rect(-40,-16,112,16),"",TextAnchor.MiddleCenter));
                        sprite.name.color=Color.yellow;trainingImages.Add(id,sprite);
                    }
                    var motion=combat.motions?.FirstOrDefault(m=>m.unit==u.id&&m.life==u.life);
                    sprite.hit.gameObject.SetActive(LiveBattleBodyVisible(u));Place(sprite.hit,new Rect((float)(motion?.x??u.x)*32,(float)(motion?.y??u.y)*32,32,32));
                    var job=definition.combatCatalog?.Job(u.job);sprite.image.texture=u.ghost?art.Picture(898):CombatBodyPicture(u);
                    sprite.image.enabled=sprite.image.texture!=null;
                    PaintNativeBody(sprite.image,motion?.direction??FeedbackDirection(u.id,u.life,u.direction),motion!=null,u.ghost?898:OriginalBattleLifecycle.AppearanceKey(u),OriginalMemberCommands.MovementType(u,definition.combatCatalog),motion?.tileX??u.x,motion?.tileY??u.y,u.nativeState,u.id);
                    sprite.name.text=job?.name??"";
                }
            }
            if(view.moving||combat?.pending!=null||notice!=null||view.facility!=null||view.record.campaignPhase=="outro"||view.record.campaignPhase=="complete")trainingMode=null;
            if(CastingSelection&&!BattleMagicAvailable(selectedBattleMagic))trainingMode=null;
            if(trainingMode==TrainingAction.Move&&!TrainingActionAvailable(0)||trainingMode==TrainingAction.Attack&&!TrainingActionAvailable(1))trainingMode=null;
            if(combat==null||trainingMode!=previousTrainingMode||combat.revision!=trainingRangeRevision||selectedBattleMagic!=previousRangeMagic||trainingActionUnit!=previousRangeSource)
            {
                previousRangeSource=trainingActionUnit;previousRangeMagic=selectedBattleMagic;previousTrainingMode=trainingMode;trainingRangeRevision=combat?.revision??-1;
                foreach(var cell in trainingCells)cell.gameObject.SetActive(false);
                if(combat!=null&&trainingMode.HasValue)
                {
                    var unit=combat.Unit(trainingActionUnit);var map=DisplayMap(mapId).map;
                    var basicArea=trainingMode==TrainingAction.Attack?OriginalBasicAttackArea.Weights(map,definition.combatCatalog,unit):null;
                    var moveArea=trainingMode==TrainingAction.Move?OriginalMemberCommands.Reachable(DisplayMap(mapId),definition.combatCatalog,combat.units,trainingActionUnit):null;
                    var occupied=new HashSet<TutorialTile>(combat.units.Where(u=>u.id!=trainingActionUnit&&OriginalCommandRange.OccupiesTile(u)).Select(u=>u.Tile));
                    foreach(var p in DisplayMap(mapId).portals)foreach(var c in p.cells)occupied.Add(c.Tile);
                    foreach(var n in DisplayMap(mapId).npcs)occupied.Add(new TutorialTile(n.x,n.y));
                    int i=0;
                    for(int y=0;y<map.height;y++)for(int x=0;x<map.width;x++)
                    {
                        var tile=new TutorialTile(x,y);if(tile==unit.Tile&&!(CastingSelection&&SelectedAllowsSelf))continue;
                        bool move=trainingMode==TrainingAction.Move;
                        bool reachable=move?moveArea!=null&&moveArea[y*map.width+x]>0&&!occupied.Contains(tile):CastingSelection?TutorialGeometry.Distance(unit.Tile,tile)<=SelectedMagicDistance:basicArea!=null&&basicArea[y*map.width+x]>0;
                        if(!reachable)continue;
                        if(i==trainingCells.Count)trainingCells.Add(Picture(trainingRanges,"Native reachable tile",new Rect(0,0,31,31),art.battle.ranges[0]));
                        var image=trainingCells[i++];image.gameObject.SetActive(true);image.texture=art.battle.ranges[move?0:CastingSelection?2:1];Place((RectTransform)image.transform,new Rect(x*32,y*32,31,31));
                    }
                }
            }
            // Native death sound calls occur in the unit update before magic render calls.
            PaintDeathAudio(BattleMap()&&!CampaignCutscene?combat?.deaths:null);
            PaintBattleMagic();
            PaintCommandRange();
        }
    }
}
