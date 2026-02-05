using UnityEngine;
using System.Collections;
using GlobalEnum;
using UnityEngine.Serialization;

[System.Serializable]
public class StatusInfo
{
    public long unitID; //  유닛 ID
    public string userName; //유저 이름

    public string charClass; //캐릭터 직업이름
    public int charLevel; //캐릭터 레벨

    //게이지 관련
    public float actPoint = 10.0f; //행동치
    public float curHp; //현재체력
    public float maxHp; //맥스체력
    public float curMp; //현재마법력
    public float maxMp; //맥스마법력
    public float realExp; //경험치
    public float maxExp; //목표경험치

    //전투관련
    public int attackMotion; //어떤공격을 받는가
    public int attackValue; //공격력
    public int defenseValue; //방어력

    public int magicMotion; //어떤공격을 받는가
    public int magicAtkValue; //마법공격력
    public int magicDefValue; //마법방어력

    public int searchRange = 4;     //  탐색 범위
    public int moveRange = 3;       //  이동 범위
    public int attackRange = 1;     //  공격 범위

    public int holdMoney; //수중의 돈

    public StatusInfo()
    {

    }

    public StatusInfo(MonsterData data)
    {
        unitID = data.ID;
        userName = data.ID.ToString();
        charLevel = 1;
        realExp = maxExp = 0;

        curHp = maxHp = data.MaxHp;
        curMp = maxMp = 0;

        attackValue = data.Atk;
        magicAtkValue = data.Atk;

        defenseValue = data.Def;
        magicDefValue = data.Def;

        searchRange = data.SearchRange;
        moveRange = data.MoveRange;
        attackRange = data.AtkRange;
    }

    public StatusInfo(CharLevelData lvData, CharJobData jobData)
    {
        curHp = maxHp = jobData.MaxHp;
        curMp = maxMp = jobData.MaxMp;
        realExp = 0;
        maxExp = lvData.ClassMaxExp;

        attackValue = jobData.MinAttack;
        defenseValue = jobData.MinDef;
        magicAtkValue = jobData.MinMagic;
        magicDefValue = jobData.MinResist;

        searchRange = 0;
        moveRange = jobData.MoveRange;
        attackRange = jobData.AttackRange;
    }
}

public class CharStatus : MonoBehaviour
{
    protected StatusInfo MyCharacter;
    public StatusInfo GetStatus => MyCharacter;

    public eCharAction CharAction = eCharAction.None;
    public bool IsPossibleAction => CharAction == eCharAction.None && MyCharacter.actPoint >= 10;
    
    
    private void Awake()
    {
        MyCharacter = new StatusInfo();
        MyCharacter.curHp = MyCharacter.maxHp;
        MyCharacter.curMp = MyCharacter.maxMp;
    }

    // Update is called once per frame
    private void Update()
    {
        //행동치가 쌓인다
        if (MyCharacter.actPoint < 10)
        {
            float deltaPoint = 0.5f * Time.deltaTime;
            MyCharacter.actPoint += deltaPoint;
            
            if (MyCharacter.actPoint >= 10 && CharAction != eCharAction.None)
            {
                CharAction = eCharAction.None;
            }
        }
    }

    public void SetStatus(StatusInfo status, bool isScarecrow = false)
    {
        MyCharacter = status;

        if (isScarecrow)
        {
            var ai = GetComponent<EnemyAI_Base>();
            if (ai != null)
            {
                Destroy(ai);
            }
        }
    }

    public void ResetAction()
    {
        
    }
}