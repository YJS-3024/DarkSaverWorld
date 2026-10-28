using System.Collections.Generic;
using UnityEngine;
namespace DarkSaver.Player
{
    public static class WorldMovementStep
    {
        // Preserve the distance budget across short waypoint tails at the legacy cadence.
        public static bool TryAdvance(IReadOnlyList<Vector3> path,int index,Vector2 origin,float budget,out Vector2 position,out int next)
        {
            position=origin;next=index;
            bool Finite(float value)=>!float.IsNaN(value) && !float.IsInfinity(value);
            if(path==null || index<0 || index>=path.Count || !Finite(origin.x) || !Finite(origin.y) || !Finite(budget) || budget<=0)return false;
            var candidate=origin;int cursor=index;float remaining=budget;
            while(cursor<path.Count && remaining>0)
            {
                var target=(Vector2)path[cursor];if(!Finite(target.x) || !Finite(target.y))return false;
                var delta=target-candidate;float distance=delta.magnitude;
                if(distance<=remaining){candidate=target;remaining-=distance;cursor++;}
                else{candidate+=delta/distance*remaining;remaining=0;}
            }
            position=candidate;next=cursor;return true;
        }
    }
}
