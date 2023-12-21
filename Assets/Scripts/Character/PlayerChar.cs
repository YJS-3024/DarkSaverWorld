using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GlobalEnum;
using UnityEngine;

public class PlayerChar : BaseCharObject
{
    [SerializeField] public Transform CameraFollowPos;

    private Coroutine _moving = null;

    public bool IsMainPlayer { get; } = true;

    public Vector3 GetNodePos => TilemapManager.I.GetNode_WorldPos(transform.position)?.centerPos ?? Vector3.zero;

    public eCharAction CharAction {
        get => CharStatus.CharAction;
        set => CharStatus.CharAction = value;
    }

    public int MoveRange
    {
        get => CharStatus.GetStatus.MoveRange;
        set => CharStatus.GetStatus.MoveRange = value;
    }

    public int AttackRange
    {
        get => CharStatus.GetStatus.AttackRange;
        set => CharStatus.GetStatus.AttackRange = value;
    }

    public override void Move(List<PlanePathNode> nodes = null)
    {
        if (nodes == null)
            return;

        if (_moving != null)
        {
            StopCoroutine(_moving);
            _moving = null;
        }

        _moving = StartCoroutine(OnStartMove(nodes));

        CharStatus.GetStatus.actPoint -= 5;
        CharAction = eCharAction.None;
    }

    public override void Attack(PlanePathNode node)
    {
        var enemy = EnemyManager.I.GetEnemy(node.centerPos);
        if (enemy != null)
        {
            enemy.HitDamage(CharStatus.GetStatus.AttackValue);
            Debug.Log($"{enemy.name} 공~격~!");
        }

        CharStatus.GetStatus.actPoint -= 2;
        CharAction = eCharAction.None;
    }

    public override void Magic()
    {
    }

    public override void Recess()
    {
    }

    public override void HitDamage(int damage)
    {

    }
}
