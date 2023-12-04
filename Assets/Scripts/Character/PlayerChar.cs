using System;
using System.Collections;
using System.Collections.Generic;
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

    public int CharMoveCount
    {
        get => CharStatus.GetStatus.MoveCount;
        set => CharStatus.GetStatus.MoveCount = value;
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

    private IEnumerator OnStartMove(List<PlanePathNode> nodes)
    {
        TilemapManager.I.Path.ResistNodeList(nodes);

        while (TilemapManager.I.Path.MoveListLength > 0)
        {
            var node = TilemapManager.I.Path.CunNode();

            SetPosition(node.centerPos);
            
            yield return new WaitForSeconds(0.05f);
        }
    }
}
