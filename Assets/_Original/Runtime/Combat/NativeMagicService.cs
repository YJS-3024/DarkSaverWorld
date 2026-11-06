using System;
using System.Linq;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class EntryAccountService
    {
        // The original GameCtl target click sends, then calls Magic immediately; only
        // its visual slots are local. No damage, costs or inventory are predicted here.
        public WorldReply ExecuteNativeBattleCast(TrainingAction action,int target,int x,int y,Func<TrainingActionRequest,TrainingView,TrainingUnitsNotice> authority,int magic=-1,string owned=null)
        {
            if(action!=TrainingAction.CastMagic&&action!=TrainingAction.UseSkill&&action!=TrainingAction.UseItem)return WorldResult(WorldFailure.InvalidInput);
            var ready=ReadyBattle();if(ready!=WorldFailure.None)return WorldResult(ready);
            if(authority==null)return WorldResult(WorldFailure.RulesUnavailable);
            string id=Guid.NewGuid().ToString("N");var queued=RequestBattleAction(action,target,x,y,id,magic,owned);if(!queued.Accepted)return queued;
            var combat=world.Combat;var request=combat.Pending;
            ApplyMagicPresentation(combat,request.magic,combat.Units.First(u=>u.id==0),request.magicTargets,request.x,request.y,id,
                request.action==TrainingAction.UseItem||request.action==TrainingAction.UseSkill||request.magicRange>0||request.magicUnit==8);
            combat.PreviewedCasts.Add(id);
            try{return ResolveBattleRequest(WorldResult(WorldFailure.None),authority);}
            finally{combat.PreviewedCasts.Remove(id);}
        }
        public WorldReply ExecuteNativeTrainingCast(TrainingAction action,int target,int x,int y,Func<TrainingActionRequest,TrainingView,TrainingUnitsNotice> authority,int magic=-1,string owned=null)
        {var ready=ReadyTraining();return ready==WorldFailure.None?ExecuteNativeBattleCast(action,target,x,y,authority,magic,owned):WorldResult(ready);}
        private TrainingUnitsNotice PrepareNativeMagic(TrainingCombatSession combat,NativeBattleNotice proof)
        {
            if(proof==null||!proof.approved||!OriginalNativeMagic.TryNotice(proof.packet,out var packet))return null;
            var expected=new TrainingUnitsNotice{id=proof.id,character=proof.character,session=proof.session,expectedRevision=proof.expectedRevision,approved=true,
                units=combat.Units.Select(u=>u.Copy()).ToArray(),nativeMagic=proof.Copy(),feedbackEvents=Array.Empty<BattleFeedbackEvent>(),lifecycleEvents=Array.Empty<BattleLifecycleEvent>()};
            // GameCtl does not call Magic for zero targets/negative magic or a same-team echo.
            if(packet.targets.Length==0||packet.magic<0)return expected;
            var source=combat.Units.FirstOrDefault(u=>u.id==packet.source);
            if(packet.source>=0)
            {
                if(source==null||source.life!=proof.unitLife||proof.packet[1]==0x89&&(!proof.localTeam.HasValue||!proof.sourceTeam.HasValue))return null;
                if(OriginalNativeMagic.Dispatches(packet,proof.localTeam,proof.sourceTeam,proof.packet[1])==false)return expected;
                if(!source.nativeActive.HasValue||!source.nativeEnabled.HasValue)return null;
            }
            else if(proof.unitLife!=0)return null;
            var metadata=world.Definition.magicMenu?.Find(packet.magic);
            if(world.Definition.magicProjectiles?.SupportsEffect(metadata)!=true)return null;
            var targets=packet.targets.Distinct().ToArray();
            if(proof.targetLives==null||proof.targetLives.Length!=targets.Length||proof.targetLives.Any(t=>t==null)||proof.targetLives.Select(t=>t.id).Distinct().Count()!=targets.Length
                ||targets.Any(id=>!proof.targetLives.Any(t=>t.id==id&&combat.Units.Any(u=>u.id==id&&u.life==t.life))))return null;
            // This is a server display notification, not a new local target click.
            // DA10 passes signed coordinates directly to Magic; do not apply the outgoing
            // selection range/Unit8/negative-direction rules again to incoming visuals.
            expected.magicEvents=new[]{new BattleMagicEvent{magic=packet.magic,source=packet.source,sourceLife=source?.life??0,x=packet.x,y=packet.y,targets=targets}};
            return expected;
        }
        public WorldReply ReceiveBattleMagicPacket(NativeBattleNotice notice)
        {
            var ready=ReadyBattle(true);if(ready!=WorldFailure.None)return WorldResult(ready);
            if(world.Combat==null)return WorldResult(WorldFailure.RulesUnavailable);
            var prepared=PrepareNativeMagic(world.Combat,notice);
            return prepared==null?WorldResult(WorldFailure.InvalidInput):ReceiveBattleUnits(prepared);
        }
        private WorldReply ReceiveBattleLevelEffectPacket(NativeBattleNotice notice,out byte[] request)
        {
            request=null;var ready=ReadyBattle(true);if(ready!=WorldFailure.None)return WorldResult(ready);
            var combat=world.Combat;
            if(combat==null||notice?.approved!=true||!OriginalNativeMagic.TryLevelEffectNotice(notice.packet,out int unit))return WorldResult(WorldFailure.InvalidInput);
            if(notice.character!=worldCharacter||notice.session!=world.CampaignSession||!Guid.TryParseExact(notice.id,"N",out _)
                ||notice.expectedRevision!=combat.Revision||combat.Notices.Contains(notice.id))return WorldResult(WorldFailure.StaleRequest);
            NativeMagicPacket cast=null;
            if(unit>=0)
            {
                var positions=combat.Units.Select(u=>u.Copy()).ToArray();
                foreach(var moving in positions)
                {var motion=Movement(combat,moving.id);if(motion?.Moving==true){moving.x=motion.CurrentTile.X;moving.y=motion.CurrentTile.Y;}}
                var actor=positions.FirstOrDefault(u=>u.id==unit);
                if(actor==null||actor.life!=notice.unitLife)return WorldResult(WorldFailure.StaleRequest);
                var metadata=world.Definition.magicMenu?.Find(4001);
                if(world.Definition.magicProjectiles?.SupportsEffect(metadata)!=true||metadata.distance!=0||metadata.range!=8||metadata.unit!=3)return WorldResult(WorldFailure.RulesUnavailable);
                var eligibility=NativeMagicEligibility(actor,metadata,positions,notice.localTeam,actor.x,actor.y,out var permitted);
                if(eligibility!=WorldFailure.None)return WorldResult(eligibility);
                // An absent native enable/active flag is unknown, not a dead unit.
                if(combat.Units.Any(u=>permitted.Contains(u.id)&&(!u.nativeActive.HasValue||!u.nativeEnabled.HasValue)))return WorldResult(WorldFailure.RulesUnavailable);
                cast=OriginalNativeMagic.LevelEffectRequest(unit,metadata,positions,permitted);
                if(positions.Count(u=>permitted.Contains(u.id)&&u.nativeActive==true&&u.nativeEnabled==true
                    &&BattleMagicArea.Weight(actor.x,actor.y,u.x,u.y,metadata.range)>0)>255)return WorldResult(WorldFailure.InvalidInput);
                if(cast!=null&&(notice.targetLives==null||notice.targetLives.Length!=cast.targets.Length
                    ||notice.targetLives.Any(t=>t==null)||notice.targetLives.Select(t=>t.id).Distinct().Count()!=cast.targets.Length
                    ||cast.targets.Any(id=>!notice.targetLives.Any(t=>t.id==id&&combat.Units.Any(u=>u.id==id&&u.life==t.life)))))return WorldResult(WorldFailure.StaleRequest);
            }
            else if(notice.unitLife!=0)return WorldResult(WorldFailure.InvalidInput);
            // This is an original client control command, not a changed unit
            // snapshot. It must not save resources, advance the battle revision,
            // or cancel a pending action merely to emit its followup bytes.
            combat.Notices.Add(notice.id);if(cast!=null)request=OriginalNativeMagic.Request(cast);
            return WorldResult(WorldFailure.None);
        }
        public WorldReply ReceiveTrainingMagicPacket(NativeBattleNotice notice)
        {var ready=ReadyTraining(true);return ready==WorldFailure.None?ReceiveBattleMagicPacket(notice):WorldResult(ready);}
        private WorldReply ReceiveBattleMagicRedirect(NativeBattleNotice notice,out byte[] request)
        {
            request=null;var ready=ReadyBattle(true);if(ready!=WorldFailure.None)return WorldResult(ready);
            var input=notice?.Copy();var combat=world.Combat;
            if(input?.approved!=true||!OriginalMagicRedirect.TryDecode(input.packet,out int magic,out int unit,out int x,out int y))return WorldResult(WorldFailure.InvalidInput);
            if(combat==null)return WorldResult(WorldFailure.RulesUnavailable);
            if(input.character!=worldCharacter||input.session!=world.CampaignSession||!Guid.TryParseExact(input.id,"N",out _)
                ||input.expectedRevision!=combat.Revision||combat.Notices.Contains(input.id))return WorldResult(WorldFailure.StaleRequest);
            var failure=SelectNativeMagic(magic,unit,x,y,input.localTeam,2,out var cast);
            if(failure!=WorldFailure.None)return WorldResult(failure);
            if(unit>=0&&combat.Units.FirstOrDefault(u=>u.id==unit)?.life!=input.unitLife)return WorldResult(WorldFailure.StaleRequest);
            if(unit<0&&input.unitLife!=0)return WorldResult(WorldFailure.InvalidInput);
            if(cast!=null)
            {
                if(input.targetLives==null||input.targetLives.Length<cast.targets.Length||input.targetLives.Any(t=>t==null||!combat.Units.Any(u=>u.id==t.id&&u.life==t.life))
                    ||input.targetLives.Select(t=>t.id).Distinct().Count()!=input.targetLives.Length
                    ||cast.targets.Any(id=>!input.targetLives.Any(t=>t.id==id&&combat.Units.Any(u=>u.id==id&&u.life==t.life))))return WorldResult(WorldFailure.StaleRequest);
                request=OriginalNativeMagic.Request(cast);ApplySelectedMagic(combat,cast,input.id);
            }
            // Control-only receipt: retain resources, storage, revision and pending action.
            combat.Notices.Add(input.id);return WorldResult(WorldFailure.None);
        }
        private WorldFailure SelectNativeMagic(int magic,int unit,int x,int y,int? localTeam,int mode,out NativeMagicPacket cast)
        {
            cast=null;var combat=world.Combat;if(combat==null)return WorldFailure.RulesUnavailable;
            var metadata=world.Definition.magicMenu?.Find(magic);
            if(world.Definition.magicProjectiles?.SupportsEffect(metadata)!=true)return WorldFailure.RulesUnavailable;
            var positions=combat.Units.Select(u=>u.Copy()).ToArray();
            foreach(var actor in positions){var motion=Movement(combat,actor.id);if(motion?.Moving==true){actor.x=motion.CurrentTile.X;actor.y=motion.CurrentTile.Y;}}
            // An overlapping received grid has no established last-writer occupant.
            if(positions.Where(OriginalCommandRange.OccupiesTile).GroupBy(u=>u.Tile).Any(g=>g.Count()>1))return WorldFailure.RulesUnavailable;
            var caster=positions.FirstOrDefault(u=>u.id==unit);
            if(unit>=0&&caster==null)return WorldFailure.StaleRequest;
            if(unit<0&&metadata.unit!=8&&metadata.distance<=0)return WorldFailure.RulesUnavailable;
            int source=unit;int[] eligible=null;
            if(metadata.unit!=8)
            {
                if(unit==-1)eligible=positions.Select(u=>u.id).ToArray();
                else
                {
                    if(unit==-2){if(!localTeam.HasValue||localTeam<0||localTeam>=20)return WorldFailure.RulesUnavailable;source=localTeam.Value*50;}
                    else if(unit==-3)source=700;
                    else if(unit<0)return WorldFailure.RulesUnavailable;
                    var owner=positions.FirstOrDefault(u=>u.id==source);
                    var failure=NativeMagicEligibility(owner,metadata,positions,localTeam,x,y,out eligible);if(failure!=WorldFailure.None)return failure;
                }
            }
            bool Unknown(TrainingUnit u)=>!u.nativeActive.HasValue||u.nativeActive==true&&!u.nativeEnabled.HasValue;
            int? gridUnit=null;
            if(metadata.unit==8)
            {
                var point=positions.Where(u=>u.x==x&&u.y==y).ToArray();if(point.Any(Unknown))return WorldFailure.RulesUnavailable;
                gridUnit=point.FirstOrDefault(OriginalCommandRange.OccupiesTile)?.id??-1;
            }
            var relevant=metadata.unit==8?positions.Where(u=>gridUnit>=0&&u.id/10==gridUnit/10):positions.Where(u=>eligible.Contains(u.id));
            var visualSource=positions.FirstOrDefault(u=>u.id==source);
            if(relevant.Any(Unknown)||visualSource!=null&&Unknown(visualSource))return WorldFailure.RulesUnavailable;
            cast=OriginalMagicRedirect.Select(metadata,source,x,y,caster,positions,eligible,gridUnit,mode);
            return cast?.targets.Length>255?WorldFailure.InvalidInput:WorldFailure.None;
        }
        private static WorldFailure NativeMagicEligibility(TrainingUnit owner,MagicMenuEntry metadata,TrainingUnit[] positions,int? localTeam,int x,int y,out int[] eligible)
        {
            eligible=null;if(owner==null||metadata==null)return WorldFailure.RulesUnavailable;
            // Server-directed client controls do not require a fabricated
            // per-spell target list. Read actual50/51 team fields and the
            // original02500/025C0 relation rules when those are sufficient.
            int? Team(int id)
            {var info=positions.FirstOrDefault(u=>u.id==id)?.nativeInfo;return info?.Valid==true?(int?)info.fields[24]:null;}
            var selected=new System.Collections.Generic.List<int>();bool known=true;
            foreach(var target in positions)
            {
                if(target.nativeActive==false||target.nativeEnabled==false||BattleSkillArea.Weight(metadata,owner.Tile,x,y,target.x,target.y)<=0)continue;
                bool? allowed=OriginalNativeRelations.MagicEligible(metadata.team,owner.id,target.id,Team,localTeam);
                if(!allowed.HasValue){known=false;break;}
                if(allowed==true)selected.Add(target.id);
            }
            if(known){eligible=selected.ToArray();return WorldFailure.None;}
            // Retain explicit typed/legacy eligibility when original team
            // metadata is unavailable. It cannot override a known native result.
            var rule=owner.magicRules?.FirstOrDefault(r=>r.magic==metadata.id);
            if(rule?.enabled!=true||rule.targets==null)return WorldFailure.RulesUnavailable;
            eligible=rule.targets.ToArray();return WorldFailure.None;
        }
        private void ApplySelectedMagic(TrainingCombatSession combat,NativeMagicPacket cast,string id)
        {
            ApplyNativeMagic(combat,new TrainingUnitsNotice{id=id,magicEvents=new[]{new BattleMagicEvent{magic=cast.magic,source=cast.source,
                sourceLife=combat.Units.FirstOrDefault(u=>u.id==cast.source)?.life??0,x=cast.x,y=cast.y,targets=cast.targets}}});
        }
        private WorldFailure ValidateNativeMagic(TrainingCombatSession combat,TrainingUnitsNotice notice)
        {
            if(notice.nativeMagic==null)return WorldFailure.None;
            if(notice.nativeResponse!=null||notice.nativeMovement!=null||notice.request!=null)return WorldFailure.InvalidInput;
            return OriginalBattleResponses.Matches(notice,PrepareNativeMagic(combat,notice.nativeMagic),true)?WorldFailure.None:WorldFailure.InvalidInput;
        }
        private void ApplyNativeMagic(TrainingCombatSession combat,TrainingUnitsNotice notice)
        {
            foreach(var e in notice.magicEvents??Array.Empty<BattleMagicEvent>())
            {
                var source=combat.Units.FirstOrDefault(u=>u.id==e.source&&u.nativeActive==true&&u.nativeEnabled==true);
                if(source!=null)ApplyMagicPresentation(combat,e.magic,source,e.targets.Select(id=>BattleMagicArea.Capture(combat.Units.First(u=>u.id==id))).ToArray(),e.x,e.y,notice.id,true);
                else
                {
                    var pattern=world.Definition.magicProjectiles.Find(world.Definition.magicMenu.Find(e.magic).pattern);
                    combat.MagicEffects.RemoveAll(f=>e.targets.Contains(f.targetUnit)&&f.role!=BattleMagicEffectRole.ChargeOnly);if(pattern.type==0)return;
                    var metadata=world.Definition.magicMenu.Find(e.magic);
                    foreach(int id in e.targets)
                    {
                        var target=combat.Units.First(u=>u.id==id);combat.MagicEffects.RemoveAll(f=>f.targetUnit==id&&f.role!=BattleMagicEffectRole.ChargeOnly);
                        combat.MagicEffects.Add(new BattleMagicEffect{id=notice.id+"/"+id,magic=e.magic,pattern=pattern.Copy(),sourceX=-1,sourceY=-1,sourceUnit=-1,sourceJob=-1,
                            targetUnit=id,targetX=target.x,targetY=target.y,areaX=e.x,areaY=e.y,radius=metadata.range,mapWidth=world.Map(world.Record.map).map.width,mapHeight=world.Map(world.Record.map).map.height,role=BattleMagicEffectRole.TargetOnly});
                    }
                }
            }
        }
    }
}
