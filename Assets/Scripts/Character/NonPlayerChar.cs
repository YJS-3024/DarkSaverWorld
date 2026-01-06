using System.Collections.Generic;
using GlobalEnum;

public class NonPlayerChar : BaseCharObject, i_Npc_NoBattle
{
    public eCharType CharType => eCharType.NPC;
    
    
    public void Move(List<PlanePathNode> nodes = null)
    {
        
    }
}
