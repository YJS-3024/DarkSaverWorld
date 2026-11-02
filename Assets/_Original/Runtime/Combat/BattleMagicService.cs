using System.Linq;
using DarkSaver.Original.Tutorial;

namespace DarkSaver.Original.Entry
{
    using DarkSaver.Original.World;
    public sealed partial class EntryAccountService
    {
        private WorldFailure PrepareBattleMagic(TrainingUnit actor,int magic,int target,ref int x,ref int y,out int mpCost,out int apCost,out BattleMagicTarget[] targets,bool skill=false,BattleMagicRule itemRule=null)
        {
            mpCost=apCost=0;targets=null;
            var metadata=world.Definition.magicMenu?.Find(magic);
            // Team/target eligibility is explicitly supplied; never infer native relationships from IDs.
            if(itemRule!=null?world.Definition.magicProjectiles?.SupportsEffect(metadata)!=true:skill?world.Definition.magicProjectiles?.SupportsSkill(metadata)!=true:world.Definition.magicProjectiles?.Supports(metadata)!=true)return WorldFailure.RulesUnavailable;
            if(itemRule==null)
            {
                if(skill){if(world.Record.growth?.skills?.Contains(magic)!=true)return WorldFailure.NotOwned;}
                else
                {
                    if(world.Record.magicLoadout?.Matches(world.Record.growth)!=true)return WorldFailure.RulesUnavailable;
                    if(!world.Record.magicLoadout.selected.Contains(magic))return WorldFailure.NotOwned;
                }
            }
            var rule=itemRule??actor.magicRules?.FirstOrDefault(r=>r.magic==magic);
            if(rule?.enabled!=true||!rule.mpCost.HasValue||!rule.apCost.HasValue||rule.targets==null)return WorldFailure.RulesUnavailable;
            if(skill&&metadata.distance<0)
            {
                if(target!=-1||!BattleSkillArea.ValidPoint(metadata,actor.Tile,x,y,world.Map(world.Record.map).map))return WorldFailure.InvalidInput;
                targets=BattleSkillArea.Targets(metadata,actor,world.Combat.Units,rule.targets,x,y,rule.allowDeadTargets==true);
                if(targets.Length>255)return WorldFailure.InvalidInput;
                mpCost=rule.mpCost.Value;apCost=rule.apCost.Value;
                return actor.mp<mpCost||actor.ap<apCost?WorldFailure.InsufficientResource:WorldFailure.None;
            }
            var enemy=world.Combat.Units.FirstOrDefault(u=>u.id==target);
            if(target>=0)
            {if(enemy==null||enemy.hp==0&&rule.allowDeadTargets!=true)return WorldFailure.InvalidInput;x=enemy.x;y=enemy.y;}
            var map=world.Map(world.Record.map).map;
            if(metadata.distance==0){x=actor.x;y=actor.y;}
            if(!map.Contains(new TutorialTile(x,y))||TutorialGeometry.Distance(actor.Tile,new TutorialTile(x,y))>metadata.distance)return WorldFailure.InvalidInput;
            if(metadata.unit==8)
            {
                if(enemy==null||!rule.targets.Contains(target))return WorldFailure.InvalidInput;
                targets=BattleMagicArea.GroupTargets(world.Combat.Units,rule.targets,target,rule.allowDeadTargets==true);
                // Native Unit8 succeeds even with zero valid slots and encodes no coordinate center.
                x=y=-1;
            }
            else if(metadata.range==0)
            {
                if(enemy==null||!rule.targets.Contains(target))return WorldFailure.InvalidInput;
                targets=new[]{BattleMagicArea.Capture(enemy)};
            }
            else
            {
                if(target<-1)return WorldFailure.InvalidInput;
                targets=BattleMagicArea.Targets(world.Combat.Units,rule.targets,x,y,metadata.range,rule.allowDeadTargets==true);
                if(!skill&&targets.Length==0||targets.Length>255)return WorldFailure.InvalidInput;
            }
            mpCost=rule.mpCost.Value;apCost=rule.apCost.Value;
            return actor.mp<mpCost||actor.ap<apCost?WorldFailure.InsufficientResource:WorldFailure.None;
        }
        public WorldReply RequestBattleMagic(int magic,int target,string request)=>RequestBattleAction(TrainingAction.CastMagic,target,0,0,request,magic);
        public WorldReply RequestBattleMagicAt(int magic,int x,int y,string request)=>RequestBattleAction(TrainingAction.CastMagic,-1,x,y,request,magic);
        public WorldReply RequestTrainingMagicAt(int magic,int x,int y,string request)
        {var f=ReadyTraining();return f==WorldFailure.None?RequestBattleMagicAt(magic,x,y,request):WorldResult(f);}
        public WorldReply RequestTrainingMagic(int magic,int target,string request)
        {var f=ReadyTraining();return f==WorldFailure.None?RequestBattleMagic(magic,target,request):WorldResult(f);}
    }
}
