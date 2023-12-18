using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyChar : BaseCharObject
{
    public override void StartMove(List<PlanePathNode> nodes)
    {
    }

    public override void StartAttack()
    {
    }

    public override void StartMagic()
    {
    }

    public override void Move()
    {
        var searchRange = CharStatus.GetStatus.SearchRange;
        var mainPlayer = PlayerManager.I.MainPlayer;

        var enemyPos = TilemapManager.I.GetNode_WorldPos(transform.position).centerPos;
        var charPos = TilemapManager.I.Path.MoveListLength > 0
            ? TilemapManager.I.Path.LastNode().centerPos
            : TilemapManager.I.GetNode_WorldPos(mainPlayer.transform.position).centerPos;
        var nodes = TilemapManager.I.Path.FindPath(enemyPos, charPos, false);
        if (nodes.Count <= searchRange)
        {
            nodes.RemoveAt(nodes.Count - 1);
            MoveCoroutine = StartCoroutine(OnStartMove(nodes));
        }
    }

    public override void Recess()
    {
    }

    public override bool OnSearch_PlayerTeams(int range)
    {
        var mainPlayer = PlayerManager.I.MainPlayer;
        if (mainPlayer is null)
            return false;

        var enemyPos = TilemapManager.I.GetNode_WorldPos(transform.position).centerPos;
        var charPos = TilemapManager.I.Path.MoveListLength > 0
            ? TilemapManager.I.Path.LastNode().centerPos
            : TilemapManager.I.GetNode_WorldPos(mainPlayer.transform.position).centerPos;
        var nodes = TilemapManager.I.Path.FindPath(enemyPos, charPos, false);
        if (nodes.Count <= range)
        {
            Debug.Log("이동하겠소");
            return true;
        }

        Debug.Log("이동하지 않겠소");
        return false;
    }
}
