using System.Collections;
using System.Collections.Generic;
using BehaviorTree;
using GlobalEnum;
using UnityEngine;

public class NormalEnemyAI : MonoBehaviour
{
    private EnemyChar _enemyChar;
    private CharStatus _charStatus;

    public float actPoint = 10;

    public IEnumerator Start()
    {
        if (TryGetComponent<BaseCharObject>( out var component))
        {
            _enemyChar = component as EnemyChar;
            _charStatus = component.charStatus;
            _charStatus.GetStatus.actPoint = actPoint;
        }

        var root = new BTRoot();

        var attackSequence = new BTSequence();
        root.AddChild(attackSequence);
        attackSequence.AddChild(new BTCondition(SearchAttack_Enemy));
        attackSequence.AddChild(new BTAction(Attack));

        var moveSequence = new BTSequence();
        root.AddChild(moveSequence);
        moveSequence.AddChild(new BTCondition(SearchMove_Player));
        moveSequence.AddChild(new BTAction(Move));

        while (true)
        {
            root.Evaluate();
            yield return new WaitForSeconds(0.5f);
        }
    }

    private bool SearchMove_Player()
    {
        if (_charStatus is null)
            return false;

        var searchRange = _charStatus.GetStatus.SearchRange;

        return _charStatus.IsPossibleAction &&
               _enemyChar.OnSearchPlayer(searchRange);
    }

    private void Move()
    {
        Debug.Log("플레이어에게 이동");
        _charStatus.GetStatus.actPoint -= 4;
        _enemyChar.Move();
    }

    private bool SearchAttack_Enemy()
    {
        if (_charStatus is null)
            return false;

        var searchRange = _charStatus.GetStatus.AttackRange;

        return _charStatus.IsPossibleAction &&
               _enemyChar.OnSearchPlayer(searchRange);
    }
    private void Attack()
    {
        Debug.Log("플레이어 공격");
        _charStatus.GetStatus.actPoint -= 2;
        _enemyChar.Attack();
    }
}
