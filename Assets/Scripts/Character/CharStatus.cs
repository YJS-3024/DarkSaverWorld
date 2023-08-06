using UnityEngine;
using System.Collections;
using GlobalEnum;

[System.Serializable]
public class StatusInfo
{
    public string UserName; //유저 이름

    public string charClass; //캐릭터 직업이름
    public int charLavel; //캐릭터 레벨

    //게이지 관련
    public float actPoint = 10.0f; //행동치
    public float CurHP; //현재체력
    public float MaxHP; //맥스체력
    public float CurMP; //현재마법력
    public float MaxMP; //맥스마법력
    public float RealExp; //경험치
    public float MaxExp; //목표경험치

    //전투관련
    public int AttackMotion; //어떤공격을 받는가
    public int AttackPoint; //공격력
    public int DefensePoint; //방어력

    public int MagicMotion; //어떤공격을 받는가
    public int MagicAtkPoint; //마법공격력
    public int MagicDefPoint; //마법방어력

    public int MoveCount = 3;
    
    public int HoldMoney; //수중의 돈
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
        MyCharacter.CurHP = MyCharacter.MaxHP;
        MyCharacter.CurMP = MyCharacter.MaxMP;
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

    public void ResetAction()
    {
        
    }
}