using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class EntryAccountService
    {
        // Native10016170 is DLL-local, not a server seed or an AP/position value.
        private int nativeMoveHintAxis;
        private void AttachNativeMoveHint(TrainingActionRequest request)
        {
            if(request==null||request.action!=TrainingAction.Move&&request.action!=TrainingAction.Regroup&&request.action!=TrainingAction.SoldierMove)return;
            var hint=OriginalNativeMoveHints.Calculate(world.Map(world.Record.map).map,world.Definition.combatCatalog,world.Combat.Units,request.source,request.x,request.y,nativeMoveHintAxis);
            if(hint==null)return; // Preserve the existing local movement contract, no fabricated wire hint.
            request.nativeMovePoints=hint.points;request.nativeMoveAxisBefore=hint.axisBefore;request.nativeMoveAxisAfter=hint.axisAfter;nativeMoveHintAxis=hint.axisAfter;
        }
    }
}
