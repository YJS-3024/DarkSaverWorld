using System.Collections;
using System.Collections.Generic;
using BehaviorTree;
using GlobalEnum;
using UnityEngine;

public class NormalEnemyAI : MonoBehaviour
{
    private EnemyChar _enemyChar;
    private CharStatus _charStatus;
    private void Awake()
    {
        if (TryGetComponent<BaseCharObject>( out var component))
        {
            _enemyChar = component as EnemyChar;
            _charStatus = component.CharStatus;
        }
    }


    public IEnumerator Start()
    {
        var root = new BTRoot();
        var sequence = new BTSequence();
        var condition = new BTCondition(SearchMove_PlayerTeams);
        var action = new BTAction(Move);

        root.AddChild(sequence);
        sequence.AddChild(condition);
        sequence.AddChild(action);

        while (true)
        {
            root.Evaluate();
            yield return new WaitForSeconds(0.5f);
        }
    }

    private bool SearchMove_PlayerTeams()
    {
        var searchRange = _charStatus.GetStatus.SearchRange;

        return _charStatus.IsPossibleAction &&
               _enemyChar.OnSearch_PlayerTeams(searchRange);
    }

    private void Move()
    {
        _charStatus.GetStatus.actPoint -= 10;
        _enemyChar.Move();
    }
}
