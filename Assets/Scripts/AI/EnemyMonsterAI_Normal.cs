using System.Collections;
using System.Collections.Generic;
using BehaviorTree;
using GlobalEnum;
using UnityEngine;

public class EnemyMonsterAI_Normal : EnemyAI_Base
{


    protected override IEnumerator Start()
    {
        InitAI();

        var aliveSequence = new BTSequence();
        _btRoot.AddChild(aliveSequence);
        aliveSequence.AddChild(new BTCondition(IsAlive));

        var attackSequence = new BTSequence();
        _btRoot.AddChild(attackSequence);
        attackSequence.AddChild(new BTCondition(SearchAttack_Enemy));
        attackSequence.AddChild(new BTAction(Attack));

        var moveSequence = new BTSequence();
        _btRoot.AddChild(moveSequence);
        moveSequence.AddChild(new BTCondition(SearchMove_Player));
        moveSequence.AddChild(new BTAction(Move));

        while (true)
        {
            if (IsAlive() == false &&   //  HP 가 0
                EnemyChar.isDead)       //  죽었다 판단
            {
                yield break;            //  AI 종료
            }

            _btRoot.Evaluate();
            yield return new WaitForSeconds(0.5f);
        }
    }

    protected override bool SearchMove_Player()
    {
        if (CharStatus is null)
            return false;

        var searchRange = CharStatus.GetStatus.searchRange;

        return CharStatus.IsPossibleAction &&
               EnemyChar.OnSearchPlayer(searchRange);
    }

    protected override bool IsAlive()
    {
        return CharStatus.GetStatus.curHp > 0;
    }

    protected override void Move()
    {
        Debug.Log("플레이어에게 이동");
        CharStatus.GetStatus.actPoint -= 4;
        EnemyChar.Move();
    }

    protected override bool SearchAttack_Enemy()
    {
        if (CharStatus is null)
            return false;

        var searchRange = CharStatus.GetStatus.attackRange;

        return CharStatus.IsPossibleAction &&
               EnemyChar.OnSearchPlayer(searchRange);
    }

    protected override void Attack()
    {
        Debug.Log("플레이어 공격");
        CharStatus.GetStatus.actPoint -= 2;
        EnemyChar.Attack();
    }

    protected override void Rest()
    {

    }

    protected override void Dead()
    {

    }
}
