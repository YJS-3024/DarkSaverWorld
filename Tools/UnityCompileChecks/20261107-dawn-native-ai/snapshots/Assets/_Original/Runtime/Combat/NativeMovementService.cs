using System;
using System.Linq;
using DarkSaver.Original.World;
using DarkSaver.Original.Tutorial;

namespace DarkSaver.Original.Entry
{
    public sealed partial class EntryAccountService
    {
        // Full presentation backtracking phase1001616C, independent of outgoing hints10016170.
        private int nativeMovementAxis;
        private TrainingMotion Movement(TrainingCombatSession combat,int unit)=>unit==0?combat.Motion:combat.MemberMotions.TryGetValue(unit,out var member)?member.motion:null;
        private NativeMovementPath NativePath(TrainingCombatSession combat,int source,TutorialTile start,TutorialTile end)
        {
            var positions=combat.Units.Select(u=>u.Copy()).ToArray();
            foreach(var unit in positions)
            {var motion=Movement(combat,unit.id);if(motion?.Moving==true){unit.x=motion.CurrentTile.X;unit.y=motion.CurrentTile.Y;}}
            return OriginalNativeMovement.Path(world.Map(world.Record.map).map,world.Definition.combatCatalog,positions,source,start,end,nativeMovementAxis);
        }
        private TrainingUnitsNotice PrepareMovement(TrainingCombatSession combat,NativeBattleNotice proof)
        {
            if(proof==null||!proof.approved||!OriginalNativeMovement.TryDecode(proof.packet,out var packet))return null;
            var actor=combat.Units.FirstOrDefault(u=>u.id==packet.unit);
            if(actor==null||actor.life!=proof.unitLife||!actor.nativeActive.HasValue||!actor.nativeState.HasValue)return null;
            var units=combat.Units.Select(u=>u.Copy()).ToArray();var after=units.First(u=>u.id==actor.id);
            bool ignored=!packet.failed&&(!actor.nativeActive.Value||packet.x<1&&packet.y<1);
            if(!ignored)
            {
                var end=new TutorialTile(packet.x,packet.y);if(!world.Map(world.Record.map).map.Contains(end))return null;
                if(!packet.failed&&(!actor.movementBudget.HasValue||!OriginalMemberCommands.MovementType(actor,world.Definition.combatCatalog).HasValue))return null;
                after.x=end.X;after.y=end.Y;
                if(packet.failed){after.nativeState=(after.nativeState.Value&~1u)|0x20u;if(after.nativeMenuMask.HasValue)after.nativeMenuMask|=16;}
                else
                {
                    var motion=Movement(combat,packet.unit);var start=motion?.Moving==true?(motion.Started?motion.NextTile:motion.CurrentTile):actor.Tile;
                    var path=NativePath(combat,packet.unit,start,end);
                    // Persist final state, not a resumable animation. Completion clears1/20, sets10.
                    if(path!=null)after.nativeState=(after.nativeState.Value&~0x21u)|0x10u;
                    if(path?.points.Length>0){var previous=path.points.Length>1?path.points[path.points.Length-2]:start;after.direction=TutorialGeometry.FacingRow(end.X-previous.X,end.Y-previous.Y);}
                }
            }
            return new TrainingUnitsNotice{id=proof.id,character=proof.character,session=proof.session,expectedRevision=proof.expectedRevision,approved=true,units=units,
                nativeMovement=proof.Copy(),feedbackEvents=Array.Empty<BattleFeedbackEvent>(),lifecycleEvents=Array.Empty<BattleLifecycleEvent>()};
        }
        public WorldReply ReceiveBattleMovement(NativeBattleNotice notice)
        {
            var ready=ReadyBattle(true);if(ready!=WorldFailure.None)return WorldResult(ready);
            if(world.Combat==null)return WorldResult(WorldFailure.RulesUnavailable);
            var prepared=PrepareMovement(world.Combat,notice);
            return prepared==null?WorldResult(WorldFailure.InvalidInput):ReceiveBattleUnits(prepared);
        }
        public WorldReply ReceiveTrainingMovement(NativeBattleNotice notice)
        {var ready=ReadyTraining(true);return ready==WorldFailure.None?ReceiveBattleMovement(notice):WorldResult(ready);}
        private WorldFailure ValidateNativeMovement(TrainingCombatSession combat,TrainingUnitsNotice notice)
        {
            if(notice.nativeMovement==null)return WorldFailure.None;
            if(notice.nativeResponse!=null||notice.nativeMagic!=null||notice.request!=null)return WorldFailure.InvalidInput;
            var expected=PrepareMovement(combat,notice.nativeMovement);
            return OriginalBattleResponses.Matches(notice,expected)?WorldFailure.None:WorldFailure.InvalidInput;
        }
        private void ApplyNativeMovement(TrainingCombatSession combat,TrainingUnitsNotice notice)
        {
            if(notice.nativeMovement==null||!OriginalNativeMovement.TryDecode(notice.nativeMovement.packet,out var packet))return;
            var actor=combat.Units.First(u=>u.id==packet.unit);
            if(!packet.failed&&(actor.nativeActive==false||packet.x<1&&packet.y<1))return;
            if(packet.failed){if(packet.unit==0)combat.Motion=null;else combat.MemberMotions.Remove(packet.unit);return;}
            var target=new TutorialTile(packet.x,packet.y);var motion=Movement(combat,packet.unit)??new TrainingMotion(actor.Tile,Array.Empty<TutorialTile>());
            motion.Redirect(target,start=>
            {
                var path=NativePath(combat,packet.unit,start,target);
                if(path!=null)nativeMovementAxis=path.axisAfter;
                return path?.points;
            });
            if(packet.unit==0)combat.Motion=motion;else if(motion.Moving)combat.MemberMotions[packet.unit]=(actor.life,target,motion);else combat.MemberMotions.Remove(packet.unit);
        }
    }
}
