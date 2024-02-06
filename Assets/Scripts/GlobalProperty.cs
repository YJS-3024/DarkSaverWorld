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
        MainPlayer = 3,
        Player = 6,
        Field_Board,
        Field_Block,
        Field_Upper,

        Max
    }

    public enum SceneType
    {
        Scene_Title,
        Scene_Village,
        Scene_Field,
        Scene_Battle,
    }

    public enum eCharDirectionType
    {
        Back,
        Forward,
        Left,
        Right,
    }

    public enum eCommandType
    {
        None,
        Move,
        UseItem,
        Attack,
        MagicSkill,
        JobSkill,
        Rest,
        Operation,
        Option,
    }

    public enum eCharCommand
    {
        None,
        Move,           // 이동
        UseItem,        // 아이템 사용
        Attack,         // 일반 공격
        Magic_Buff,     // 마법
        Magic_Attack,   // 마법
        Magic_JobSkill, // 특수기
        Recess,         // 휴식
        Management,     // 용병관리
        System_Option,  // 시스템 설정
        
        MoveField       // 이동 (마을)
        
    }

    public enum eCharType
    {
        NONE,
        PLAYER = 1,
        NPC,

        Monster_Scarecrow = 10,
        Monster_Normal,
        Monster_Elete,
        Monster_Boss,
    }

    public enum eSkillType
    {
        None,
        AttackSkill,
        BuffSkill,
    }

    public enum ePanelType
    {
        
    }
}
