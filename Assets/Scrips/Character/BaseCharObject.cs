using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseCharObject : MonoBehaviour
{
    /// <summary>
    /// 이동 시작
    /// </summary>
    public abstract void StartMove(List<PlanePathNode> nodes);

    /// <summary>
    /// 공격 시작
    /// </summary>
    public abstract void StartAttack();

    /// <summary>
    /// 마법 공격 시작
    /// </summary>
    public abstract void StartMagic();

    /// <summary>
    /// 이동
    /// </summary>
    public abstract void Move();

    /// <summary>
    /// 휴식
    /// </summary>
    public abstract void Recess();
}
