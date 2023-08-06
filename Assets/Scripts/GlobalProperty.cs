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
        TextAsset,
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

    public enum eCharAction
    {
        None,
        Move,           // 이동
        UseItem,        // 아이템 사용
        Attack,         // 일반 공격
        Magic,          // 마법
        Attack_Special, // 특수기
        Recess,         // 휴식
        Management,     // 용병관리
        System_Option,  // 시스템 설정
        
    }

    public enum eCharacter
    {
        NONE,
        PLAYER = 1,
        NPC,
        
        ENEMY = 11,
        BOSS,
    }

    public enum ePanelType
    {
        
    }
}
