using System;
using System.Linq;
using DarkSaver.Original.Tutorial;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class EntryAccountService
    {
        private int SoldierRestHint(TrainingCombatSession combat,int member)
        {
            var unit=combat.Units.FirstOrDefault(u=>u.id==member);
            // resetcharacter zeroes100 dwords at14C..2D8, including the client-local1FC hint.
            return unit!=null&&combat.AiHints.TryGetValue(member,out var value)&&value.life==unit.life?value.hint:0;
        }
        private SoldierActionPlan PlanSoldierAction(int member,Func<int,int> choose,double? clockMs)
        {
            var combat=world.Combat;int hint=SoldierRestHint(combat,member);
            Func<int,int> selector=choose??(count=>combat.AiRandom.Next(32768)%count);
            return OriginalSoldierActions.Plan(world.Map(world.Record.map),world.Definition.combatCatalog,combat.Units,member,hint,selector,clockMs??world.Clock);
        }
        private void ApplyNoPacketAiStep(TrainingCombatSession combat,TrainingUnit actor,SoldierActionPlan plan)
        {
            if(plan.unavailable)return;
            // These are verified client AI control flags/hints. No authoritative health, AP,
            // position, growth, inventory or disk state changes on a no-packet tick.
            bool changed=actor.nativeState!=plan.stateAfter||SoldierRestHint(combat,actor.id)!=plan.hintAfter;
            actor.nativeState=plan.stateAfter;combat.AiHints[actor.id]=(actor.life,plan.hintAfter);
            if(changed)combat.AutomationAttempts.Remove(actor.id);
        }
        private WorldReply QueueSoldierPlan(SoldierActionPlan plan,string id)
        {
            var combat=world.Combat;
            if(plan.action==TrainingAction.SoldierMove)
            {
                var move=plan.movement;var map=world.Map(world.Record.map).map;var actor=combat.Units.First(u=>u.id==plan.member);
                combat.Pending=new TrainingActionRequest{character=worldCharacter,session=world.CampaignSession,id=id,revision=combat.Revision,
                    source=plan.member,sourceLife=move.memberLife,action=TrainingAction.SoldierMove,target=0,targetLife=move.commanderLife,
                    x=move.x,y=move.y,movePath=move.path.ToArray(),sourceTerrain=map.Attribute(actor.Tile),targetTerrain=map.Attribute(new TutorialTile(move.x,move.y)),commandRanges=OriginalCommandRange.Evaluate(combat.Units)};
                AttachNativeMoveHint(combat.Pending);combat.Requests.Add(id);
            }
            else
            {
                var queued=PrepareBattleMemberAction(plan.member,plan.action.Value,plan.target,0,0,id,true);if(!queued.Accepted)return queued;
            }
            var r=combat.Pending;r.automatic=true;r.nativeOrder=combat.Units.First(u=>u.id==0).troopOrder.Value;
            r.aiHintBefore=plan.hintBefore;r.aiHintAfter=plan.hintAfter;r.aiStateAfter=plan.stateAfter;r.attackCandidates=plan.candidates.Select(c=>c.Copy()).ToArray();
            return WorldResult(WorldFailure.None);
        }
        public WorldReply RequestBattleSoldierAction(int member,string id,Func<int,int> choose=null,double? clockMs=null)
        {
            var ready=ReadyBattle();if(ready!=WorldFailure.None)return WorldResult(ready);
            var combat=world.Combat;if(combat==null)return WorldResult(WorldFailure.RulesUnavailable);
            if(combat.Pending!=null||combat.MemberMotions.ContainsKey(member))return WorldResult(WorldFailure.Busy);
            if(!Guid.TryParseExact(id,"N",out _)||combat.Requests.Contains(id)||combat.Revision==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
            SoldierActionPlan plan;try{plan=PlanSoldierAction(member,choose,clockMs);}catch(Exception){return WorldResult(WorldFailure.RulesUnavailable);}
            if(!plan.action.HasValue)return WorldResult(WorldFailure.RulesUnavailable);
            return QueueSoldierPlan(plan,id);
        }
        public WorldReply ExecuteBattleSoldierAction(int member,Func<TrainingActionRequest,TrainingView,TrainingUnitsNotice> authority,Func<int,int> choose=null,double? clockMs=null)
        {
            if(authority==null)return WorldResult(WorldFailure.RulesUnavailable);
            return ResolveBattleRequest(RequestBattleSoldierAction(member,Guid.NewGuid().ToString("N"),choose,clockMs),authority);
        }
        public WorldReply ExecuteTrainingSoldierAction(int member,Func<TrainingActionRequest,TrainingView,TrainingUnitsNotice> authority,Func<int,int> choose=null,double? clockMs=null)
        {
            var ready=ReadyTraining();return ready==WorldFailure.None?ExecuteBattleSoldierAction(member,authority,choose,clockMs):WorldResult(ready);
        }
        public WorldReply AdvanceBattleAutomation(Func<TrainingActionRequest,TrainingView,TrainingUnitsNotice> authority,Func<int,int> choose=null,double? clockMs=null)
        {
            if(authority==null||ReadyBattle()!=WorldFailure.None||world.Combat==null||world.Combat.Pending!=null)return null;
            var combat=world.Combat;
            foreach(var actor in combat.Units.Where(u=>u.id>0&&u.id<10).OrderBy(u=>u.id))
            {
                if(combat.MemberMotions.ContainsKey(actor.id)||!combat.AutomationAttempts.Add(actor.id))continue;
                SoldierActionPlan plan;try{plan=PlanSoldierAction(actor.id,choose,clockMs);}catch(Exception){continue;}
                if(!plan.action.HasValue){ApplyNoPacketAiStep(combat,actor,plan);continue;}
                if(combat.Revision==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
                return ResolveBattleRequest(QueueSoldierPlan(plan,Guid.NewGuid().ToString("N")),authority);
            }
            return null;
        }
        private WorldFailure ValidateSoldierActionOutcome(TrainingCombatSession combat,TrainingUnitsNotice notice,TrainingActionRequest request)
        {
            if(string.IsNullOrEmpty(notice.request)||request?.automatic!=true)return request?.action==TrainingAction.SoldierMove&&!string.IsNullOrEmpty(notice.request)?WorldFailure.InvalidInput:WorldFailure.None;
            if(request.source<1||request.source>9||!OriginalSoldierActions.Controlled(combat.Units,request.source)
                ||request.nativeOrder!=combat.Units.First(u=>u.id==0).troopOrder||request.aiHintBefore!=SoldierRestHint(combat,request.source))return WorldFailure.StaleRequest;
            var actor=notice.units.Single(u=>u.id==request.source);
            if(actor.nativeState!=request.aiStateAfter)return WorldFailure.InvalidInput;
            if(request.action==TrainingAction.SoldierMove)
            {
                var before=combat.Units.First(u=>u.id==request.source);var map=world.Map(world.Record.map);
                var leader=combat.Units.First(u=>u.id==0);int? kind=OriginalMemberCommands.MovementType(before,world.Definition.combatCatalog);
                if(request.target!=0||request.targetLife!=leader.life||request.sourceLife!=before.life||actor.x!=request.x||actor.y!=request.y
                    ||request.movePath==null||!before.movementBudget.HasValue||!kind.HasValue||notice.units.Length!=combat.Units.Length
                    ||notice.experience.HasValue||notice.magicEvents?.Length>0||notice.revivedTargets?.Length>0
                    ||request.sourceTerrain!=map.map.Attribute(before.Tile)||request.targetTerrain!=map.map.Attribute(new TutorialTile(request.x,request.y)))return WorldFailure.InvalidInput;
                var blocked=OriginalMemberCommands.StaticBlocked(map);foreach(var unit in combat.Units)if(unit.id/50!=request.source/50&&OriginalCommandRange.OccupiesTile(unit))blocked.Add(unit.Tile);
                var route=map.map.Path(before.Tile,new TutorialTile(request.x,request.y),before.movementBudget.Value+7,blocked,kind.Value);
                if(route==null||!route.SequenceEqual(request.movePath))return WorldFailure.StaleRequest;
                foreach(var old in combat.Units)
                {
                    var current=notice.units.SingleOrDefault(u=>u.id==old.id);
                    if(current==null||(old.id==request.source?!OriginalRegroupMovement.SameExceptMovement(old,current):!OriginalMemberCommands.Same(old,current)))return WorldFailure.InvalidInput;
                }
            }
            else
            {
                if(actor.ap!=0||(actor.nativeState.Value&0x10)!=0)return WorldFailure.InvalidInput;
                if(request.action==TrainingAction.Attack)
                {
                    var candidates=OriginalSoldierActions.Candidates(world.Map(world.Record.map),world.Definition.combatCatalog,combat.Units,combat.Units.First(u=>u.id==request.source),!OriginalSoldierActions.NativeContext(combat.Units,request.source));
                    if(candidates==null||request.attackCandidates==null||!candidates.Select(c=>(c.id,c.life)).SequenceEqual(request.attackCandidates.Select(c=>(c.id,c.life)))||!candidates.Any(c=>c.id==request.target&&c.life==request.targetLife))return WorldFailure.StaleRequest;
                }
            }
            return WorldFailure.None;
        }
        private static void UpdateSoldierAiState(TrainingCombatSession combat,TrainingUnitsNotice notice,TrainingActionRequest request)
        {
            foreach(var id in combat.AiHints.Keys.ToArray())if(!notice.units.Any(u=>u.id==id&&u.life==combat.AiHints[id].life))combat.AiHints.Remove(id);
            if(!string.IsNullOrEmpty(notice.request)&&request?.automatic==true)combat.AiHints[request.source]=(request.sourceLife,request.aiHintAfter);
        }
    }
}
