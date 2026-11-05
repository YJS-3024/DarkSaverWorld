using System;
using System.Collections.Generic;
using System.Linq;
using DarkSaver.Original.Tutorial;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class EntryAccountService
    {
        // Compatibility entry points retain the training-only guard; shared battle DTOs keep their existing names.
        private WorldFailure ReadyTraining(bool receiving=false)
        {
            var f=ReadyWorld();if(f!=WorldFailure.None)return f;
            return !world.Map(world.Record.map).training?WorldFailure.InvalidStage:ReadyBattle(receiving);
        }
        public WorldReply ExecuteTrainingAction(TrainingAction action,int target,int x,int y,Func<TrainingActionRequest,TrainingView,TrainingUnitsNotice> authority,int magic=-1,string owned=null,int order=0)
        {var f=ReadyTraining();return f==WorldFailure.None?ExecuteBattleAction(action,target,x,y,authority,magic,owned,order):WorldResult(f);}
        public WorldReply RequestTrainingAction(TrainingAction action,int target,int x,int y,string id,int magic=-1,string owned=null,int order=0)
        {var f=ReadyTraining();return f==WorldFailure.None?RequestBattleAction(action,target,x,y,id,magic,owned,order):WorldResult(f);}
        public WorldReply ReceiveTrainingUnits(TrainingUnitsNotice notice)
        {var f=ReadyTraining(true);return f==WorldFailure.None?ReceiveBattleUnits(notice):WorldResult(f);}
        public WorldReply CancelTrainingAction(string id)
        {var f=ReadyTraining(true);return f==WorldFailure.None?CancelBattleAction(id):WorldResult(f);}
        private WorldFailure ReadyBattle(bool receiving=false)
        {
            var f=ReadyWorld();if(f!=WorldFailure.None)return f;
            if(!world.Map(world.Record.map).training)
            {
                if(world.Definition.campaign?.Map(world.Record.map)?.kind!="scenario")return WorldFailure.InvalidStage;
                if(world.Record.campaignPhase!="explore")return WorldFailure.Busy;
            }
            if(world.Moving&&(!receiving||world.Combat==null)||world.Kind!=FacilityKind.None||world.PendingPortal!=null||(receiving?world.SceneLocked:world.PresentationLocked))return WorldFailure.Busy;
            return WorldFailure.None;
        }
        private HashSet<TutorialTile> BattleOccupied()
        {
            var occupied=new HashSet<TutorialTile>(world.Combat.Units.Where(u=>u.id!=0&&OriginalCommandRange.OccupiesTile(u)).Select(u=>u.Tile));
            foreach(var n in world.Map(world.Record.map).npcs)occupied.Add(new TutorialTile(n.x,n.y));
            foreach(var p in world.Map(world.Record.map).portals)foreach(var c in p.cells)occupied.Add(c.Tile);
            return occupied;
        }
        private List<TutorialTile> BattleRoute(int x,int y)
        {
            return OriginalMemberCommands.Route(world.Map(world.Record.map),world.Definition.combatCatalog,world.Combat.Units,0,x,y);
        }
        public WorldReply ExecuteBattleAction(TrainingAction action,int target,int x,int y,Func<TrainingActionRequest,TrainingView,TrainingUnitsNotice> authority,int magic=-1,string owned=null,int order=0)
        {
            var ready=ReadyBattle();if(ready!=WorldFailure.None)return WorldResult(ready);
            if(authority==null)return WorldResult(WorldFailure.RulesUnavailable);
            string id=Guid.NewGuid().ToString("N");var queued=RequestBattleAction(action,target,x,y,id,magic,owned,order);
            return ResolveBattleRequest(queued,authority);
        }
        private WorldReply ResolveBattleRequest(WorldReply queued,Func<TrainingActionRequest,TrainingView,TrainingUnitsNotice> authority)
        {
            if(!queued.Accepted)return queued;
            string id=queued.View.battle.pending.id;
            try
            {
                var notice=authority(queued.View.battle.pending.Copy(),queued.View.battle);
                if(notice!=null)
                {
                    if(notice.request!=id){if(world.Combat?.Pending?.id==id)ClearPendingBattle(world.Combat);return WorldResult(WorldFailure.StaleRequest);}
                    var reply=ReceiveBattleUnits(notice);
                    // A verified native response can be redelivered verbatim after a failed
                    // durable save. Keep its request just as the asynchronous notice route does.
                    if(!reply.Accepted&&world.Combat?.Pending?.id==id
                        &&!(reply.Failure==WorldFailure.StorageUnavailable&&notice.nativeResponse!=null))ClearPendingBattle(world.Combat);
                    return WorldResult(reply.Failure);
                }
            }
            catch(Exception){/* A failed local authority must not leave input permanently locked. */}
            if(world.Combat?.Pending?.id==id)ClearPendingBattle(world.Combat);
            return WorldResult(WorldFailure.RulesUnavailable);
        }
        public WorldReply RequestBattleAction(TrainingAction action,int target,int x,int y,string id,int magic=-1,string owned=null,int order=0)
        {
            var f=ReadyBattle();if(f!=WorldFailure.None)return WorldResult(f);
            var combat=world.Combat;if(combat==null)return WorldResult(WorldFailure.RulesUnavailable);
            if(combat.Pending!=null)return WorldResult(WorldFailure.Busy);
            if(!Guid.TryParseExact(id,"N",out _)||combat.Requests.Contains(id))return WorldResult(WorldFailure.StaleRequest);
            var actor=combat.Units.First(u=>u.id==0);
            if(!OriginalCommandRange.Living(actor)||world.Record.ghost||!Enum.IsDefined(typeof(TrainingAction),action))return WorldResult(WorldFailure.InvalidInput);
            if(!OriginalBattleTicks.Allows(actor,action))return WorldResult(WorldFailure.RulesUnavailable);
            if(action==TrainingAction.Regroup||action==TrainingAction.SoldierMove)return WorldResult(WorldFailure.InvalidInput); // Requires the source-aware planner/entry point.
            if(action!=TrainingAction.TroopOrder&&order!=0)return WorldResult(WorldFailure.InvalidInput);
            WorldOwnedItem itemSource=null;
            if(action!=TrainingAction.UseItem&&owned!=null)return WorldResult(WorldFailure.InvalidInput);
            long targetLife=0;int mpCost=0,apCost=0;BattleMagicTarget[] magicTargets=null;
            if(action!=TrainingAction.CastMagic&&action!=TrainingAction.UseSkill&&magic!=-1)return WorldResult(WorldFailure.InvalidInput);
            switch(action)
            {
                case TrainingAction.TroopOrder:
                    if(!OriginalTroopOrders.ValidChoice(order)||target!=-1)return WorldResult(WorldFailure.InvalidInput);
                    if(actor.canCommand!=true)return WorldResult(WorldFailure.RulesUnavailable);
                    if(combat.Revision==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
                    x=actor.x;y=actor.y;break;
                case TrainingAction.Move:
                    if(!actor.canMove||!actor.movementBudget.HasValue)return WorldResult(WorldFailure.RulesUnavailable);
                    var path=BattleRoute(x,y);if(path==null||path.Count==0)return WorldResult(WorldFailure.InvalidInput);
                    break;
                case TrainingAction.Attack:
                    if(!actor.canAttack||!actor.attackRange.HasValue)return WorldResult(WorldFailure.RulesUnavailable);
                    var enemy=combat.Units.FirstOrDefault(u=>u.id==target);
                    var basicArea=OriginalBasicAttackArea.Weights(world.Map(world.Record.map).map,world.Definition.combatCatalog,actor);
                    if(enemy==null||enemy.id<400||!OriginalCommandRange.Living(enemy)||actor.attackTargets!=null&&!actor.attackTargets.Contains(enemy.id)
                        ||basicArea==null||basicArea[enemy.y*world.Map(world.Record.map).map.width+enemy.x]<1)return WorldResult(WorldFailure.InvalidInput);
                    targetLife=enemy.life;x=enemy.x;y=enemy.y;break;
                case TrainingAction.UseItem:
                    var itemFailure=PrepareBattleItem(actor,owned,target,ref x,ref y,out itemSource,out magic,out mpCost,out apCost,out magicTargets);if(itemFailure!=WorldFailure.None)return WorldResult(itemFailure);
                    if(combat.Revision==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
                    targetLife=combat.Units.FirstOrDefault(u=>u.id==target)?.life??0;break;
                case TrainingAction.CastMagic:
                case TrainingAction.UseSkill:
                    if(combat.Revision==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
                    var magicFailure=PrepareBattleMagic(actor,magic,target,ref x,ref y,out mpCost,out apCost,out magicTargets,action==TrainingAction.UseSkill);if(magicFailure!=WorldFailure.None)return WorldResult(magicFailure);
                    targetLife=combat.Units.FirstOrDefault(u=>u.id==target)?.life??0;break;
                case TrainingAction.Rest:
                    if(!actor.canRest)return WorldResult(WorldFailure.RulesUnavailable);
                    x=actor.x;y=actor.y;break;
            }
            var spell=(action==TrainingAction.CastMagic||action==TrainingAction.UseSkill||action==TrainingAction.UseItem)?world.Definition.magicMenu.Find(magic):null;
            combat.Pending=new TrainingActionRequest{character=worldCharacter,session=world.CampaignSession,id=id,revision=combat.Revision,sourceLife=actor.life,targetLife=targetLife,action=action,target=target,x=x,y=y,magic=magic,mpCost=mpCost,apCost=apCost,ownedItem=itemSource?.id,item=itemSource?.item??-1,itemSlot=itemSource?.slot??-1,pattern=spell?.pattern??0,magicRange=spell?.range??0,magicDistance=spell?.distance??0,magicUnit=spell?.unit??0,magicTargets=magicTargets,commandRanges=OriginalCommandRange.Evaluate(combat.Units),orderChoice=order,nativeOrder=OriginalTroopOrders.NativeMode(order),commandMembers=action==TrainingAction.TroopOrder?OriginalTroopOrders.KnownMembers(combat.Units,0):null};
            var battleMap=world.Map(world.Record.map).map;
            combat.Pending.sourceTerrain=battleMap.Attribute(actor.Tile);
            combat.Pending.targetTerrain=battleMap.Attribute(new TutorialTile(x,y));
            if(combat.Pending.IsCast)combat.Pending.nativeMagicSignature=OriginalNativeMagic.Signature(world.Definition.magicMenu.Find(magic));
            AttachNativeMoveHint(combat.Pending);
            combat.ItemBaseline=action==TrainingAction.UseItem?world.Record.Copy():null;
            combat.Requests.Add(id);return WorldResult(WorldFailure.None);
        }
        private static void ClearPendingBattle(TrainingCombatSession combat){if(combat!=null){combat.Pending=null;combat.ItemBaseline=null;}}
        public WorldReply CancelBattleAction(string id)
        {
            var f=ReadyWorld();if(f!=WorldFailure.None)return WorldResult(f);
            if(world.Combat?.Pending?.id!=id||id==null)return WorldResult(WorldFailure.StaleRequest);
            ClearPendingBattle(world.Combat);return WorldResult(WorldFailure.None);
        }
        private WorldFailure ValidateBattleUnits(TrainingCombatSession combat,TrainingUnit[] units,int[] revived=null)
        {
            var map=world.Map(world.Record.map);
            if(world.Definition.combatCatalog==null)return WorldFailure.RulesUnavailable;
            if(units==null||units.Length==0||units.Length>1000||units.Any(u=>u==null)||units.Select(u=>u.id).Distinct().Count()!=units.Length
                ||units.Count(u=>u.id==0)!=1)return WorldFailure.InvalidInput;
            var alive=new HashSet<TutorialTile>();
            foreach(var u in units)
            {
                int? movementType=OriginalMemberCommands.MovementType(u,world.Definition.combatCatalog);
                if(u.id<0||u.id>999||u.life<1||u.maxHp<1||u.hp<0||u.hp>u.maxHp||u.maxMp<0||u.mp<0||u.mp>u.maxMp
                    ||!BattleMagicRule.ValidRules(u.magicRules)||!BattleItemRule.ValidRules(u.itemRules)||!OriginalTroopOrders.ValidMode(u.troopOrder)||!OriginalRegroupMovement.ValidMovementType(u.movementType)||!OriginalMemberCommands.ValidTargets(u.attackTargets)||!OriginalBattleTicks.ValidMask(u.nativeMenuMask)||u.ghost&&u.hp>0||u.effectiveStats!=null&&!u.effectiveStats.Valid
                    ||u.maxAp<1||u.ap<0||u.ap>u.maxAp||u.direction<0||u.direction>3||u.movementBudget<0||u.attackRange<1
                    ||u.movementBudget>100000||u.attackRange>map.map.width+map.map.height||!map.map.Contains(u.Tile)
                    ||u.id==0&&(!movementType.HasValue||map.map.Terrain(u.Tile,movementType.Value*3+2)<0||map.map.Terrain(u.Tile,movementType.Value*3+2)==99)||world.Definition.combatCatalog.Job(u.job)==null
                    ||u.nativeAppearanceJob.HasValue&&(u.nativeAppearanceJob<0||u.nativeAppearanceJob>999||world.Definition.combatCatalog.Job(u.nativeAppearanceJob.Value)==null)
                    ||u.nativePalette.HasValue&&(u.nativePalette<0||u.nativePalette>3)||u.nativeInfo!=null&&(!u.nativeInfo.Valid||u.nativeInfo.fields[0]!=u.id||u.nativeInfo.fields[1]!=u.job)
                    ||OriginalCommandRange.OccupiesTile(u)&&!alive.Add(u.Tile)||map.npcs.Any(n=>n.x==u.x&&n.y==u.y)
                    ||map.portals.Any(p=>p.cells.Any(c=>c.Tile==u.Tile))&&!(u.id==0&&u.x==world.Record.x&&u.y==world.Record.y))return WorldFailure.InvalidInput;
                var previous=combat.Units.FirstOrDefault(v=>v.id==u.id);
                if(combat.Lives.TryGetValue(u.id,out long life)&&(u.life<life||previous==null&&u.life<=life
                    ||previous!=null&&u.life==life&&(previous.job!=u.job||previous.hp==0&&u.hp>0&&u.id!=0&&revived?.Contains(u.id)!=true)))return WorldFailure.StaleRequest;
            }
            return WorldFailure.None;
        }
        private WorldFailure RestoreScenarioBattle()
        {
            var saved=world.Record.scenarioBattle;if(saved==null)return WorldFailure.None;
            if(!WorldRecord.Valid(world.Record))return WorldFailure.InvalidStage;
            if(world.Map(world.Record.map).training||world.Definition.campaign?.Map(world.Record.map)?.kind!="scenario"
                ||saved.units.First(u=>u.id==0).job!=world.CommanderJob||world.Record.ghost&&world.Record.hp>0)return WorldFailure.InvalidStage;
            var combat=new TrainingCombatSession();var failure=ValidateBattleUnits(combat,saved.units);
            if(failure!=WorldFailure.None)return failure;
            combat.Revision=saved.revision;combat.Units=saved.units.Select(u=>u.Copy()).ToArray();combat.Notices.Add(saved.lastNotice);
            foreach(var life in saved.lives)combat.Lives[life.id]=life.life;
            world.Combat=combat;return WorldFailure.None;
        }
        private ScenarioBattleRecord SaveScenarioBattle(TrainingCombatSession combat,TrainingUnitsNotice notice,TrainingUnit[] units)
        {
            var lives=combat.Lives.ToDictionary(p=>p.Key,p=>p.Value);
            foreach(var unit in units)lives[unit.id]=unit.life;
            return new ScenarioBattleRecord{map=world.Record.map,revision=combat.Revision+1,lastNotice=notice.id,
                units=units.Select(u=>u.Copy()).ToArray(),lives=lives.OrderBy(p=>p.Key).Select(p=>new BattleLifeRecord{id=p.Key,life=p.Value}).ToArray()};
        }
        public WorldReply ReceiveBattleUnits(TrainingUnitsNotice notice)
            =>ReceiveBattleUnits(notice,false);
        private WorldReply ReceiveBattleUnits(TrainingUnitsNotice notice,bool nativeSnapshot,int[] resetMotions=null,int[] nativeResetUnits=null)
        {
            if(notice==null)return WorldResult(WorldFailure.InvalidInput);
            var f=ReadyBattle(true);if(f!=WorldFailure.None)return WorldResult(f);
            var combat=world.Combat??new TrainingCombatSession();
            if(notice.character!=worldCharacter||notice.session!=world.CampaignSession||!Guid.TryParseExact(notice.id,"N",out _)
                ||notice.expectedRevision!=combat.Revision||combat.Revision==long.MaxValue||combat.Notices.Contains(notice.id))return WorldResult(WorldFailure.StaleRequest);
            var request=combat.Pending;
            if(!string.IsNullOrEmpty(notice.request)&&(request==null||request.id!=notice.request||request.revision!=combat.Revision))return WorldResult(WorldFailure.StaleRequest);
            if(!notice.approved)
            {
                if(request==null||request.id!=notice.request)return WorldResult(WorldFailure.StaleRequest);
                ClearPendingBattle(combat);combat.Notices.Add(notice.id);return WorldResult(WorldFailure.InvalidInput);
            }
            if(notice.magicRestoreMp.HasValue&&(notice.magicRestoreMp<0||string.IsNullOrEmpty(notice.request)||request?.IsCast!=true))return WorldResult(WorldFailure.InvalidInput);
            if(notice.magicTargets!=null&&(string.IsNullOrEmpty(notice.request)||request?.IsCast!=true))return WorldResult(WorldFailure.InvalidInput);
            var units=notice.units;
            f=ValidateNativeBattleResponse(combat,notice,request);if(f!=WorldFailure.None)return WorldResult(f);
            f=ValidateNativeMovement(combat,notice);if(f!=WorldFailure.None)return WorldResult(f);
            f=ValidateNativeMagic(combat,notice);if(f!=WorldFailure.None)return WorldResult(f);
            f=ValidateMagicOutcomes(combat,notice,request);if(f!=WorldFailure.None)return WorldResult(f);
            f=ValidateBattleUnits(combat,units,notice.revivedTargets);if(f!=WorldFailure.None)return WorldResult(f);
            f=ValidateFeedback(combat,notice);if(f!=WorldFailure.None)return WorldResult(f);
            f=ValidateLifecycle(combat,notice);if(f!=WorldFailure.None)return WorldResult(f);
            f=ValidateTroopOrderOutcome(combat,notice,request);if(f!=WorldFailure.None)return WorldResult(f);
            f=ValidateRegroupOutcome(combat,notice,request);if(f!=WorldFailure.None)return WorldResult(f);
            f=ValidateMemberCommandOutcome(combat,notice,request);if(f!=WorldFailure.None)return WorldResult(f);
            f=ValidateSoldierActionOutcome(combat,notice,request);if(f!=WorldFailure.None)return WorldResult(f);
            units=units.Select(u=>u.Copy()).ToArray(); // Never normalize the caller's retryable notice in place.
            var commander=units.First(u=>u.id==0);
            if(commander.job!=world.CommanderJob||world.Record.ghost&&commander.hp>0&&notice.revivedTargets?.Contains(0)!=true)return WorldResult(WorldFailure.InvalidInput);
            List<TutorialTile> route=null;
            if(!string.IsNullOrEmpty(notice.request))
            {
                if(!units.Any(u=>u.id==request.source&&u.life==request.sourceLife)||request.action==TrainingAction.Attack&&!units.Any(u=>u.id==request.target&&u.life==request.targetLife))return WorldResult(WorldFailure.StaleRequest);
                if(request.IsCast)
                {
                    var before=combat.Units.First(u=>u.id==0);
                    var metadata=world.Definition.magicMenu?.Find(request.magic);
                    if((request.action==TrainingAction.UseItem?world.Definition.magicProjectiles?.SupportsEffect(metadata)!=true:request.action==TrainingAction.UseSkill?world.Definition.magicProjectiles?.SupportsSkill(metadata)!=true:world.Definition.magicProjectiles?.Supports(metadata)!=true)||metadata.pattern!=request.pattern||metadata.range!=request.magicRange||metadata.distance!=request.magicDistance||metadata.unit!=request.magicUnit)return WorldResult(WorldFailure.RulesUnavailable);
                    if(request.magicTargets==null||(request.action==TrainingAction.UseItem||request.action==TrainingAction.UseSkill||request.magicRange>0||request.magicUnit==8)&&notice.magicTargets==null)return WorldResult(WorldFailure.InvalidInput);
                    if(notice.magicTargets!=null&&(notice.magicTargets.Distinct().Count()!=notice.magicTargets.Length||notice.magicTargets.Any(id=>!request.magicTargets.Any(t=>t.id==id))))return WorldResult(WorldFailure.InvalidInput);
                    if(request.magicTargets.Any(t=>units.Any(u=>u.id==t.id&&u.life!=t.life&&!(t.wasDead&&notice.revivedTargets?.Contains(t.id)==true&&u.life>t.life)))||(long)commander.mp!=(long)before.mp-request.mpCost+(notice.magicRestoreMp??0)||commander.ap!=before.ap-request.apCost
                        ||commander.maxMp!=before.maxMp||commander.maxAp!=before.maxAp)return WorldResult(WorldFailure.InvalidInput);
                }
                if(request.action==TrainingAction.Move&&request.source==0)
                {
                    if(commander.x!=request.x||commander.y!=request.y)return WorldResult(WorldFailure.InvalidInput);
                    route=BattleRoute(request.x,request.y);
                    if(route==null||route.Count==0||route.Any(p=>units.Any(u=>u.id/50!=0&&OriginalCommandRange.OccupiesTile(u)&&u.Tile==p))
                        ||units.Any(u=>u.id!=0&&OriginalCommandRange.OccupiesTile(u)&&u.x==request.x&&u.y==request.y))return WorldResult(WorldFailure.InvalidInput);
                }
            }
            if(route==null&&notice.nativeMovement==null&&!nativeSnapshot&&(commander.x!=world.Record.x||commander.y!=world.Record.y))return WorldResult(WorldFailure.InvalidInput);
            f=ValidateBattleItemOutcome(combat,notice,request);if(f!=WorldFailure.None)return WorldResult(f);
            var next=world.Record.Copy();next.healthKnown=true;next.hp=commander.hp;next.maxHp=commander.maxHp;next.mp=commander.mp;next.maxMp=commander.maxMp;
            if(notice.revivedTargets?.Contains(0)==true)next.ghost=false;
            next.x=commander.x;next.y=commander.y;next.direction=commander.direction;
            if(notice.experience.HasValue)
            {
                if(next.growth==null)return WorldResult(WorldFailure.RulesUnavailable);
                if(next.growth.revision!=notice.expectedGrowthRevision||next.growth.revision==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
                if(notice.growthState!=null)
                {
                    var state=notice.growthState;var previous=next.growth;
                    if(world.Definition.growthCatalog?.Accepts(state)!=true||state.job!=previous.job||state.job!=commander.job
                        ||state.classLevel<=previous.classLevel||state.totalLevel<=previous.totalLevel||state.revision!=previous.revision+1
                        ||state.lastNotice!=notice.id||state.experience!=notice.experience.Value||state.stats!=null&&!SameGrowthStats(state.stats,commander.effectiveStats))return WorldResult(WorldFailure.InvalidInput);
                    next.growth=state.Copy();next.level=state.totalLevel;
                }
                else {next.growth.experience=notice.experience.Value;next.growth.revision++;next.growth.lastNotice=notice.id;}
            }
            else if(notice.growthState!=null)return WorldResult(WorldFailure.InvalidInput);
            if(next.growth!=null)commander.nativeExperience=next.growth.experience<=uint.MaxValue?(uint?)next.growth.experience:null;
            if(!nativeSnapshot)foreach(var unit in units)SyncNativeUnitState(unit,combat.Units.FirstOrDefault(u=>u.id==unit.id),notice);
            if(notice.experience.HasValue&&commander.nativeInfo!=null)
            {if(notice.experience<=uint.MaxValue)commander.nativeInfo.ExperienceSet((uint)notice.experience.Value);else commander.nativeInfo=null;}
            if(!string.IsNullOrEmpty(notice.request)&&request.action==TrainingAction.UseItem)
            {
                if(notice.itemConsumed.Value)next.inventory=next.inventory.Where(i=>i.id!=request.ownedItem).ToArray();
                else if(notice.itemRemainingUses.HasValue)next.inventory.First(i=>i.id==request.ownedItem).remainingUses=notice.itemRemainingUses;
            }
            if(!world.Map(world.Record.map).training)next.scenarioBattle=SaveScenarioBattle(combat,notice,units);
            f=CommitGrowth(next,request?.action==TrainingAction.UseItem&&!string.IsNullOrEmpty(notice.request)?BattleItemStoredBaseline(combat.ItemBaseline):null);if(f!=WorldFailure.None)return WorldResult(f);
            if(nativeSnapshot&&combat.Units.FirstOrDefault(u=>u.id==0)?.life!=commander.life)combat.Motion=null;
            foreach(int id in resetMotions??Array.Empty<int>()){if(id==0)combat.Motion=null;else combat.MemberMotions.Remove(id);}
            ResetNativeUnitPresentation(combat,units,nativeResetUnits);
            if(route!=null)combat.Motion=new TrainingMotion(new TutorialTile(world.Combat.Units.First(u=>u.id==0).x,world.Combat.Units.First(u=>u.id==0).y),route);
            UpdateRegroupMotions(combat,notice,request);
            ApplyNativeMovement(combat,notice);
            UpdateSoldierAiState(combat,notice,request);
            if(!string.IsNullOrEmpty(notice.request)&&request.IsCast&&!combat.PreviewedCasts.Contains(request.id))
                ApplyMagicPresentation(combat,request.magic,combat.Units.First(u=>u.id==0),request.magicTargets.Where(t=>(notice.magicTargets??new[]{request.target}).Contains(t.id)).ToArray(),request.x,request.y,notice.id,request.action==TrainingAction.UseItem||request.action==TrainingAction.UseSkill||request.magicRange>0||request.magicUnit==8);
            if(notice.nativeMagic!=null)ApplyNativeMagic(combat,notice);
            else if(notice.magicEvents!=null)foreach(var e in notice.magicEvents)
                ApplyMagicPresentation(combat,e.magic,units.Concat(combat.Units).First(u=>u.id==e.source&&u.life==e.sourceLife),e.targets.Select(id=>BattleMagicArea.Capture(units.Concat(combat.Units).First(u=>u.id==id))).ToArray(),e.x,e.y,notice.id+"/event"+Array.IndexOf(notice.magicEvents,e),true);
            if(!string.IsNullOrEmpty(notice.request)&&request.action==TrainingAction.Attack&&(notice.nativeResponse==null||notice.feedbackEvents?.Any(e=>e.kind==BattleFeedbackKind.Attack)==true))
            {combat.Impact=notice.id;combat.ImpactAge=0;combat.ImpactX=request.x;combat.ImpactY=request.y;}
            ApplyFeedback(combat,notice,request);
            ApplyLifecycle(combat,notice);
            // Any accepted world update invalidates an in-flight decision made against an older revision.
            ClearPendingBattle(combat);combat.Units=units.Select(u=>u.Copy()).ToArray();combat.Revision++;combat.Notices.Add(notice.id);
            combat.RegroupAttempts.Clear();
            combat.AutomationAttempts.Clear();
            foreach(var u in units)combat.Lives[u.id]=u.life;
            world.Combat=combat;world.PendingRetreat=null;return WorldResult(WorldFailure.None);
        }
    }
}
