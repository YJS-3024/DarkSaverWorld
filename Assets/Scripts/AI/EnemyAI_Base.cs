using System;
using System.Collections;
using System.Collections.Generic;
using BehaviorTree;
using UnityEngine;

public abstract class EnemyAI_Base : MonoBehaviour
{
    protected EnemyChar EnemyChar;
    protected CharStatus CharStatus;

    [SerializeField]
    protected float actPoint = 10;

    protected BTRoot _btRoot;

    protected void InitAI()
    {
        if (TryGetComponent<BaseCharObject>( out var component))
        {
            EnemyChar = component as EnemyChar;
            CharStatus = component.charStatus;
            CharStatus.GetStatus.actPoint = actPoint;
        }

        _btRoot = new BTRoot();
    }

    protected virtual IEnumerator Start()
    {
        yield return null;
    }

    /// <summary>
    /// 플레이어타입을 검색
    /// </summary>
    /// <returns></returns>
    protected abstract bool SearchMove_Player();
    
    /// <summary>
    /// 적타입을 검색
    /// </summary>
    /// <returns></returns>
    protected abstract bool SearchAttack_Enemy();

    /// <summary>
    /// 살아있는지 확인
    /// </summary>
    /// <returns></returns>
    protected abstract bool IsAlive();

    /// <summary>
    /// 이동 실행
    /// </summary>
    protected abstract void Move();
    
    /// <summary>
    /// 공격 실행
    /// </summary>
    protected abstract void Attack();

    /// <summary>
    /// 휴식(턴넘김)을 실행
    /// </summary>
    protected abstract void Rest();

    /// <summary>
    /// 죽었다.
    /// </summary>
    protected abstract void Dead();
}
