using System;
using System.Linq;
using DarkSaver.Original.World;

namespace DarkSaver.Original.World
{
    // Local authoritative event contract, not a reconstructed native wire opcode.
    public sealed class BattleMagicEvent
    {public int magic,source,x,y;public long sourceLife;public int[] targets;}
}
namespace DarkSaver.Original.Entry
{
    public sealed partial class EntryAccountService
    {
        private WorldFailure ValidateMagicOutcomes(TrainingCombatSession combat,TrainingUnitsNotice notice,TrainingActionRequest request)
        {
            if(notice.nativeMagic!=null)return WorldFailure.None; // Replayed raw proof was validated first.
            if(notice.units==null||notice.units.Any(u=>u==null))return WorldFailure.InvalidInput;
            if(notice.magicEvents!=null)
            {
                if(!string.IsNullOrEmpty(notice.request)||notice.magicEvents.Length>255)return WorldFailure.InvalidInput;
                foreach(var e in notice.magicEvents)
                {
                    if(e==null||world.Definition.magicProjectiles?.SupportsEffect(world.Definition.magicMenu?.Find(e.magic))!=true||e.targets==null||e.targets.Length>255||e.targets.Distinct().Count()!=e.targets.Length
                        ||!combat.Units.Concat(notice.units).Any(u=>u.id==e.source&&u.life==e.sourceLife)||e.targets.Any(id=>!combat.Units.Concat(notice.units).Any(u=>u.id==id)))return WorldFailure.InvalidInput;
                    var metadata=world.Definition.magicMenu.Find(e.magic);
                    if(metadata.distance<0?!BattleSkillArea.ValidPoint(metadata,combat.Units.Concat(notice.units).First(u=>u.id==e.source&&u.life==e.sourceLife).Tile,e.x,e.y,world.Map(world.Record.map).map):metadata.unit==8?(e.x!=-1||e.y!=-1):!world.Map(world.Record.map).map.Contains(new Tutorial.TutorialTile(e.x,e.y)))return WorldFailure.InvalidInput;
                }
            }
            if(notice.revivedTargets!=null)
            {
                if(notice.revivedTargets.Distinct().Count()!=notice.revivedTargets.Length)return WorldFailure.InvalidInput;
                foreach(int id in notice.revivedTargets)
                {
                    if(!combat.Units.Any(u=>u.id==id&&u.hp==0)||!notice.units.Any(u=>u.id==id&&u.hp>0&&!u.ghost))return WorldFailure.InvalidInput;
                    if(!string.IsNullOrEmpty(notice.request))
                    {if(request?.IsCast!=true||request.magicTargets?.Any(t=>t.id==id&&t.wasDead)!=true||(notice.magicTargets??new[]{request.target}).Contains(id)!=true)return WorldFailure.InvalidInput;}
                    else if(notice.magicEvents?.Any(e=>e.targets.Contains(id))!=true)return WorldFailure.InvalidInput;
                }
            }
            return WorldFailure.None;
        }
        private void ApplyMagicPresentation(TrainingCombatSession combat,int magic,TrainingUnit source,BattleMagicTarget[] targets,int x,int y,string id,bool multiple)
        {
            var pattern=world.Definition.magicProjectiles.Find(world.Definition.magicMenu.Find(magic).pattern);
            combat.MagicEffects.RemoveAll(e=>e.sourceUnit==source.id&&e.role==BattleMagicEffectRole.ChargeOnly||targets.Any(t=>e.targetUnit==t.id));
            foreach(var e in combat.MagicEffects.Where(e=>e.sourceUnit==source.id&&e.IsThunder))e.role=BattleMagicEffectRole.TargetOnly;
            if(pattern.type==0)return;
            var metadata=world.Definition.magicMenu.Find(magic);
            bool skillCaster=BattleSkillFrames.HasCaster(pattern.type,metadata.range);
            if(pattern.HasCharge&&(multiple||targets.Length==0||pattern.type!=2)||pattern.type==18||skillCaster)
                combat.MagicEffects.Add(new BattleMagicEffect{id=id+"/caster",magic=magic,pattern=pattern.Copy(),sourceX=source.x,sourceY=source.y,sourceUnit=source.id,sourceJob=source.job,areaX=x,areaY=y,radius=metadata.range,mapWidth=world.Map(world.Record.map).map.width,mapHeight=world.Map(world.Record.map).map.height,role=BattleMagicEffectRole.ChargeOnly});
            if(pattern.type==16)return; // Native target callback clears its slot; only caster trails draw.
            foreach(var t in targets)
                combat.MagicEffects.Add(new BattleMagicEffect{id=multiple?id+"/"+t.id:id,magic=magic,pattern=pattern.Copy(),sourceX=source.x,sourceY=source.y,sourceUnit=source.id,sourceJob=source.job,targetUnit=t.id,targetX=t.x,targetY=t.y,areaX=x,areaY=y,radius=metadata.range,mapWidth=world.Map(world.Record.map).map.width,mapHeight=world.Map(world.Record.map).map.height,role=multiple?BattleMagicEffectRole.TargetOnly:BattleMagicEffectRole.TargetWithCharge});
        }
    }
}
