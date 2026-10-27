using System;
using System.Collections.Generic;
using DarkSaver.Combat;
namespace DarkSaver.Events
{
    // SetArea2 -> native score backtracking -> adjacent cardinal-pair compression.
    // MoveChar 0x1000DAEA..DAFA adds7 to the temporary movement field.
    // Pure route query: no AP cost, movement result, HP or server decision is manufactured.
    public static class OriginalTutorialMovement
    {
        public static bool TryPlan(int width,int height,int actor,CombatGridCell origin,CombatGridCell destination,
            Func<CombatGridCell,int?> movement,Func<CombatGridCell,int?> occupant,bool verticalFirst,
            out IReadOnlyList<CombatGridCell> path,out bool nextVerticalFirst)
        {
            path=Array.Empty<CombatGridCell>();nextVerticalFirst=verticalFirst;
            if(!CombatTerrainTraversal.TryBuildMovementArea(width,height,origin,OriginalTutorialRanges.MovementBudget+7,
                actor,movement,occupant,out var area)
                || !CombatMovementPath.TryBacktrackMovementArea(width,height,origin,destination,area,verticalFirst,out var cardinal,out var next))return false;
            bool CanStop(CombatGridCell cell)
            {var owner=occupant(cell);return !owner.HasValue || owner.Value==actor;}
            if(!CanStop(destination))return false;
            // Native compressor joins cardinal pairs without an additional invented corner rule.
            if(!CombatMovementPath.TryCompressCardinalPath(origin,cardinal,(_,_)=>true,out var compressed))return false;
            foreach(var cell in compressed)if(!CanStop(cell))return false;
            path=compressed;nextVerticalFirst=next;return true;
        }
    }
}
