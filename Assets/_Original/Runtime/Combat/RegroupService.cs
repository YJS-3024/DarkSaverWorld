using System;
using System.Collections.Generic;
using System.Linq;
using DarkSaver.Original.Tutorial;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class EntryAccountService
    {
        private BattleRegroupPlan PlanBattleRegroup(int member)
        {
            var map=world.Map(world.Record.map);
            var blocked=new HashSet<TutorialTile>(map.npcs.Select(n=>new TutorialTile(n.x,n.y)));
            foreach(var portal in map.portals)foreach(var cell in portal.cells)blocked.Add(cell.Tile);
            return OriginalRegroupMovement.Plan(map.map,world.Definition.combatCatalog,world.Combat.Units,member,blocked);
        }
        public WorldReply RequestBattleRegroup(int member,string id)
        {
            var ready=ReadyBattle();if(ready!=WorldFailure.None)return WorldResult(ready);
            var combat=world.Combat;if(combat==null)return WorldResult(WorldFailure.RulesUnavailable);
            if(combat.Pending!=null||combat.MemberMotions.ContainsKey(member))return WorldResult(WorldFailure.Busy);
            if(!Guid.TryParseExact(id,"N",out _)||combat.Requests.Contains(id)||combat.Revision==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
            var plan=PlanBattleRegroup(member);if(plan==null)return WorldResult(WorldFailure.RulesUnavailable);
            combat.Pending=new TrainingActionRequest{character=worldCharacter,session=world.CampaignSession,id=id,revision=combat.Revision,
                action=TrainingAction.Regroup,source=member,sourceLife=plan.memberLife,target=plan.commander,targetLife=plan.commanderLife,x=plan.x,y=plan.y,
                nativeOrder=combat.Units.First(u=>u.id==plan.commander).troopOrder.Value,movePath=plan.path.ToArray(),commandRanges=OriginalCommandRange.Evaluate(combat.Units)};
            AttachNativeMoveHint(combat.Pending);combat.Requests.Add(id);return WorldResult(WorldFailure.None);
        }
        public WorldReply ExecuteBattleRegroup(int member,Func<TrainingActionRequest,TrainingView,TrainingUnitsNotice> authority)
        {
            if(authority==null)return WorldResult(WorldFailure.RulesUnavailable);
            return ResolveBattleRequest(RequestBattleRegroup(member,Guid.NewGuid().ToString("N")),authority);
        }
        public WorldReply ExecuteTrainingRegroup(int member,Func<TrainingActionRequest,TrainingView,TrainingUnitsNotice> authority)
        {
            var ready=ReadyTraining();
            return ready==WorldFailure.None?ExecuteBattleRegroup(member,authority):WorldResult(ready);
        }
        // One queued AI decision per pump; retries wait for a new authoritative revision.
        // There is no invented AI period, AP recharge, damage or default authority.
        public WorldReply AdvanceBattleRegroup(Func<TrainingActionRequest,TrainingView,TrainingUnitsNotice> authority)
        {
            if(authority==null||ReadyBattle()!=WorldFailure.None||world.Combat==null||world.Combat.Pending!=null)return null;
            var combat=world.Combat;
            foreach(var actor in combat.Units.Where(u=>u.id>0&&u.id<10).OrderBy(u=>u.id))
            {
                if(combat.MemberMotions.ContainsKey(actor.id)||!combat.RegroupAttempts.Add(actor.id))continue;
                if(PlanBattleRegroup(actor.id)!=null)return ExecuteBattleRegroup(actor.id,authority);
            }
            return null;
        }
        private WorldFailure ValidateRegroupOutcome(TrainingCombatSession combat,TrainingUnitsNotice notice,TrainingActionRequest request)
        {
            if(string.IsNullOrEmpty(notice.request)||request?.action!=TrainingAction.Regroup)return WorldFailure.None;
            var plan=PlanBattleRegroup(request.source);
            if(plan==null||request.target!=plan.commander||request.targetLife!=plan.commanderLife||request.sourceLife!=plan.memberLife
                ||request.x!=plan.x||request.y!=plan.y||request.movePath==null||!request.movePath.SequenceEqual(plan.path)
                ||request.nativeOrder!=combat.Units.First(u=>u.id==plan.commander).troopOrder)return WorldFailure.StaleRequest;
            if(notice.units.Length!=combat.Units.Length||notice.experience.HasValue||notice.magicEvents?.Length>0||notice.revivedTargets?.Length>0)return WorldFailure.InvalidInput;
            foreach(var before in combat.Units)
            {
                var after=notice.units.SingleOrDefault(u=>u.id==before.id);if(after==null||before.troopOrder!=after.troopOrder)return WorldFailure.InvalidInput;
                if(before.id!=request.source)
                {if(!OriginalTroopOrders.SameExceptOrder(before,after))return WorldFailure.InvalidInput;continue;}
                if(after.x!=plan.x||after.y!=plan.y||after.life!=plan.memberLife||!OriginalRegroupMovement.SameExceptMovement(before,after)
                    ||after.nativeState.HasValue!=before.nativeState.HasValue||((after.nativeState.Value^before.nativeState.Value)&~0x31u)!=0)return WorldFailure.InvalidInput;
            }
            return WorldFailure.None;
        }
        private static void UpdateRegroupMotions(TrainingCombatSession combat,TrainingUnitsNotice notice,TrainingActionRequest request)
        {
            foreach(var pair in combat.MemberMotions.ToArray())
            {
                var unit=notice.units.FirstOrDefault(u=>u.id==pair.Key);
                bool redirected=notice.nativeMovement!=null&&OriginalNativeMovement.TryDecode(notice.nativeMovement.packet,out var move)&&move.unit==pair.Key;
                if(unit==null||unit.hp==0||unit.life!=pair.Value.life||unit.Tile!=pair.Value.target&&!redirected)combat.MemberMotions.Remove(pair.Key);
            }
            if(string.IsNullOrEmpty(notice.request)||request==null||request.action!=TrainingAction.Regroup&&request.action!=TrainingAction.SoldierMove&&!(request.action==TrainingAction.Move&&request.source>0))return;
            var before=combat.Units.First(u=>u.id==request.source);
            combat.MemberMotions[request.source]=(before.life,new TutorialTile(request.x,request.y),new TrainingMotion(before.Tile,request.movePath));
        }
    }
}
