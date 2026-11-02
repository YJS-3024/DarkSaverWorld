using System;
using System.Linq;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class EntryAccountService
    {
        public WorldReply RequestBattleMemberAction(int member,TrainingAction action,int target,int x,int y,string id)
            =>PrepareBattleMemberAction(member,action,target,x,y,id,false);
        private WorldReply PrepareBattleMemberAction(int member,TrainingAction action,int target,int x,int y,string id,bool automatic)
        {
            var ready=ReadyBattle();if(ready!=WorldFailure.None)return WorldResult(ready);
            var combat=world.Combat;if(combat==null)return WorldResult(WorldFailure.RulesUnavailable);
            if(combat.Pending!=null||combat.MemberMotions.ContainsKey(member))return WorldResult(WorldFailure.Busy);
            if(!Guid.TryParseExact(id,"N",out _)||combat.Requests.Contains(id)||combat.Revision==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
            if(action!=TrainingAction.Move&&action!=TrainingAction.Attack&&action!=TrainingAction.Rest||!OriginalMemberCommands.Controlled(combat.Units,member))return WorldResult(WorldFailure.InvalidInput);
            var actor=combat.Units.First(u=>u.id==member);var map=world.Map(world.Record.map);
            if(!automatic&&!OriginalBattleTicks.Allows(actor,action))return WorldResult(WorldFailure.RulesUnavailable);
            var request=new TrainingActionRequest{character=worldCharacter,session=world.CampaignSession,id=id,revision=combat.Revision,source=member,sourceLife=actor.life,action=action,target=target};
            if(action==TrainingAction.Move)
            {
                if(target!=-1)return WorldResult(WorldFailure.InvalidInput);
                var path=OriginalMemberCommands.Route(map,world.Definition.combatCatalog,combat.Units,member,x,y);
                if(path==null||path.Count==0)return WorldResult(actor.canMove&&actor.movementBudget.HasValue?WorldFailure.InvalidInput:WorldFailure.RulesUnavailable);
                request.movePath=path.ToArray();
            }
            else if(action==TrainingAction.Attack)
            {
                if(!actor.canAttack||!actor.attackRange.HasValue||actor.attackTargets==null)return WorldResult(WorldFailure.RulesUnavailable);
                if(!OriginalMemberCommands.CanAttack(map,world.Definition.combatCatalog,combat.Units,member,target))return WorldResult(WorldFailure.InvalidInput);
                var enemy=combat.Units.First(u=>u.id==target);x=enemy.x;y=enemy.y;request.targetLife=enemy.life;
            }
            else
            {
                if(target!=-1)return WorldResult(WorldFailure.InvalidInput);
                if(!actor.canRest)return WorldResult(WorldFailure.RulesUnavailable);x=actor.x;y=actor.y;
            }
            request.x=x;request.y=y;request.sourceTerrain=map.map.Attribute(actor.Tile);request.targetTerrain=map.map.Attribute(new DarkSaver.Original.Tutorial.TutorialTile(x,y));
            request.commandRanges=OriginalCommandRange.Evaluate(combat.Units);AttachNativeMoveHint(request);combat.Pending=request;combat.Requests.Add(id);return WorldResult(WorldFailure.None);
        }
        public WorldReply ExecuteBattleMemberAction(int member,TrainingAction action,int target,int x,int y,Func<TrainingActionRequest,TrainingView,TrainingUnitsNotice> authority)
        {
            if(authority==null)return WorldResult(WorldFailure.RulesUnavailable);
            return ResolveBattleRequest(RequestBattleMemberAction(member,action,target,x,y,Guid.NewGuid().ToString("N")),authority);
        }
        public WorldReply ExecuteTrainingMemberAction(int member,TrainingAction action,int target,int x,int y,Func<TrainingActionRequest,TrainingView,TrainingUnitsNotice> authority)
        {
            var ready=ReadyTraining();return ready==WorldFailure.None?ExecuteBattleMemberAction(member,action,target,x,y,authority):WorldResult(ready);
        }
        private WorldFailure ValidateMemberCommandOutcome(TrainingCombatSession combat,TrainingUnitsNotice notice,TrainingActionRequest request)
        {
            if(string.IsNullOrEmpty(notice.request)||request==null||request.source==0||request.action==TrainingAction.Regroup||request.action==TrainingAction.SoldierMove)return WorldFailure.None;
            if(!OriginalMemberCommands.Controlled(combat.Units,request.source)||request.action!=TrainingAction.Move&&request.action!=TrainingAction.Attack&&request.action!=TrainingAction.Rest)return WorldFailure.InvalidInput;
            var source=combat.Units.First(u=>u.id==request.source);var map=world.Map(world.Record.map);
            if(request.sourceLife!=source.life||request.sourceTerrain!=map.map.Attribute(source.Tile)||request.targetTerrain!=map.map.Attribute(new DarkSaver.Original.Tutorial.TutorialTile(request.x,request.y)))return WorldFailure.StaleRequest;
            if(request.action==TrainingAction.Move)
            {
                var path=OriginalMemberCommands.Route(map,world.Definition.combatCatalog,combat.Units,request.source,request.x,request.y);
                if(path==null||request.movePath==null||!path.SequenceEqual(request.movePath))return WorldFailure.StaleRequest;
            }
            if(request.action==TrainingAction.Attack&&(!OriginalMemberCommands.CanAttack(map,world.Definition.combatCatalog,combat.Units,request.source,request.target)||!combat.Units.Any(u=>u.id==request.target&&u.life==request.targetLife)))return WorldFailure.StaleRequest;
            if(notice.units.Length!=combat.Units.Length||notice.magicEvents?.Length>0||notice.revivedTargets?.Length>0||request.action!=TrainingAction.Attack&&notice.experience.HasValue)return WorldFailure.InvalidInput;
            foreach(var before in combat.Units)
            {
                var after=notice.units.SingleOrDefault(u=>u.id==before.id);if(after==null)return WorldFailure.InvalidInput;
                var copy=after.Copy();
                if(before.id==request.source)
                {
                    if(!request.automatic&&(after.nativeState.HasValue!=before.nativeState.HasValue||after.nativeState.HasValue&&((after.nativeState.Value^before.nativeState.Value)&~0x31u)!=0))return WorldFailure.InvalidInput;
                    copy.direction=before.direction;copy.ap=before.ap;copy.nativeState=before.nativeState;
                    if(notice.nativeResponse!=null)copy.nativeMenuMask=before.nativeMenuMask;
                    if(request.action==TrainingAction.Move){if(after.x!=request.x||after.y!=request.y)return WorldFailure.InvalidInput;copy.x=before.x;copy.y=before.y;}
                    if(request.action==TrainingAction.Rest){copy.hp=before.hp;copy.mp=before.mp;}
                }
                else if(request.action==TrainingAction.Attack&&before.id==request.target)
                {copy.hp=before.hp;copy.direction=before.direction;copy.nativeState=before.nativeState;}
                if(!OriginalMemberCommands.Same(before,copy))return WorldFailure.InvalidInput;
            }
            return WorldFailure.None;
        }
    }
}
