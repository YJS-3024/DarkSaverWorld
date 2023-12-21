using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyChar : BaseCharObject
{
    public override void Attack(PlanePathNode node = null)
    {
        var mainPlayer = PlayerManager.I.MainPlayer;
        var attackRange = CharStatus.GetStatus.AttackRange;

        var myPos = TilemapManager.I.GetNode_WorldPos(transform.position).centerPos;
        var targetPos = TilemapManager.I.GetNode_WorldPos(mainPlayer.transform.position).centerPos;

        var nodes = TilemapManager.I.Path.FindPath(myPos, targetPos, false);
        if (nodes.Count <= attackRange)
        {
            mainPlayer.HitDamage(CharStatus.GetStatus.AttackValue);
            Debug.Log($"{mainPlayer.name} 공~격~!");
        }
    }

    public override void Magic()
    {
    }

    public override void Move(List<PlanePathNode> nodes = null)
    {
        var searchRange = CharStatus.GetStatus.SearchRange;
        var mainPlayer = PlayerManager.I.MainPlayer;

        var myPos = TilemapManager.I.GetNode_WorldPos(transform.position).centerPos;
        var targetPos = TilemapManager.I.Path.MoveListLength > 0
            ? TilemapManager.I.Path.LastNode().centerPos
            : TilemapManager.I.GetNode_WorldPos(mainPlayer.transform.position).centerPos;

        nodes = TilemapManager.I.Path.FindPath(myPos, targetPos, false);
        if (nodes.Count <= searchRange + 1)
        {
            nodes.RemoveAt(nodes.Count - 1);
            MoveCoroutine = StartCoroutine(OnStartMove(nodes));
        }
    }

    public override void Recess()
    {
    }

    public override void HitDamage(int damage)
    {

    }

    public override bool OnSearchEnemy(int range)
    {
        var mainPlayer = PlayerManager.I.MainPlayer;
        if (mainPlayer is null)
            return false;

        var myPos = TilemapManager.I.GetNode_WorldPos(transform.position).centerPos;
        var targetPos = TilemapManager.I.Path.MoveListLength > 0
            ? TilemapManager.I.Path.LastNode().centerPos
            : TilemapManager.I.GetNode_WorldPos(mainPlayer.transform.position).centerPos;
        var nodes = TilemapManager.I.Path.FindPath(myPos, targetPos, false);
        if (nodes is null)
            return false;

        if (nodes.Count <= range + 1)
        {
            // Debug.Log("이동하겠소");
            return true;
        }

        // Debug.Log("이동하지 않겠소");
        return false;
    }
}
