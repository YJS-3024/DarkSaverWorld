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

    public Vector2 GetNodePos
    {
        get
        {
            return TilemapManager.I.GetNode_WorldPos(transform.position)?.centerPos ?? Vector2.zero;
        }
    }

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

    public override void StartMove(List<PlanePathNode> nodes)
    {
        if (_moving != null)
        {
            StopCoroutine(_moving);
            _moving = null;
        }

        _moving = StartCoroutine(OnStartMove(nodes));

        CharStatus.GetStatus.actPoint -= 5;
        CharAction = eCharAction.None;
    }

    public override void StartAttack()
    {
        var searchRange = CharStatus.GetStatus.AttackRange;
        var mainPlayer = PlayerManager.I.MainPlayer;

        var pos = TilemapManager.I.GetNode_WorldPos(transform.position).centerPos;
        var targetPos = TilemapManager.I.GetNode_WorldPos(mainPlayer.transform.position).centerPos;
        var nodes = TilemapManager.I.Path.FindPath(pos, targetPos, false);
        if (nodes.Count <= searchRange)
        {
            var enemy = EnemyManager.I.GetEnemy(nodes.FirstOrDefault().centerPos);
            Debug.Log($"{enemy.name} 공~격~!");
        }
    }

    public override void StartMagic()
    {
    }

    public override void Move()
    {
    }

    public override void Recess()
    {
    }
}
