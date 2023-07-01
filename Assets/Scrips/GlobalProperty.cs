using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace GlobalEnum
{
    public enum eResourceType
    {
        None,
        Prefabs,
        Sprite,
        Texture,
        
    }
    
    public enum eLayer
    {
        Player = 6,
        Field_Board,
        Field_Block,
        Field_Upper,

        Max
    }

    public enum eCharDirectionType
    {
        Back,
        Forward,
        Left,
        Right,
    }
}
