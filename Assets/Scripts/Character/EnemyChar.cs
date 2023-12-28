using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyChar : BaseCharObject
{
    public override void Attack(PlanePathNode node = null)
    {
        var mainPlayer = PlayerManager.I.MainPlayer;
        var attackRange = charStatus.GetStatus.AttackRange;

        var myPos = TilemapManager.I.GetNode_WorldPos(transform.position).centerPos;
        var targetPos = TilemapManager.I.GetNode_WorldPos(mainPlayer.transform.position).centerPos;

        var nodes = TilemapManager.I.Path.FindPath_IncludeFindEnemy(myPos, targetPos, false);
        if (nodes.Count <= attackRange)
        {
            mainPlayer.HitDamage(charStatus.GetStatus.AttackValue);
            Debug.Log($"{mainPlayer.name} 공~격~!");
        }
    }

    public override void Magic()
    {
    }

    public override void Move(List<PlanePathNode> nodes = null)
    {
        var searchRange = charStatus.GetStatus.SearchRange;
        var mainPlayer = PlayerManager.I.MainPlayer;

        var myPos = TilemapManager.I.GetNode_WorldPos(transform.position).centerPos;
        var targetPos = CharPath.MoveListLength > 0
            ? CharPath.LastNode().centerPos
            : TilemapManager.I.GetNode_WorldPos(mainPlayer.transform.position).centerPos;

        nodes = TilemapManager.I.Path.FindPath_IncludeFindEnemy(myPos, targetPos, false);
        if (nodes.IsUnityNull())
            return;

        if (nodes.Count <= searchRange + 1)
        {
            nodes.RemoveAt(nodes.Count - 1);
            StartCoroutine(OnStartMove(nodes));
        }
        else
        {
            var moveNode = nodes.GetRange(0, searchRange);
            StartCoroutine(OnStartMove(moveNode));
        }
    }

    public override void Recess()
    {
    }

    public override void HitDamage(int damage)
    {

    }

    public virtual bool OnSearchPlayer(int range)
    {
        var mainPlayer = PlayerManager.I.MainPlayer;
        if (mainPlayer is null)
            return false;

        var myPos = TilemapManager.I.GetNode_WorldPos(transform.position).centerPos;
        var targetPos = CharPath.MoveListLength > 0
            ? CharPath.LastNode().centerPos
            : TilemapManager.I.GetNode_WorldPos(mainPlayer.transform.position).centerPos;

        //  적 발견
        var searchNodes = TilemapManager.I.Path.FindPath(myPos, targetPos, false);
        if (searchNodes is null)
            return false;

        //  적에게 가는 경로 검색(못가는 경우를 위한 체크)
        var possibleMoveNodes = TilemapManager.I.Path.FindPath_IncludeFindEnemy(myPos, targetPos, false);
        if (possibleMoveNodes is null)
            return false;

        if (searchNodes.Count <= range + 1)
        {
            // Debug.Log("이동하겠소");
            return true;
        }

        // Debug.Log("이동하지 않겠소");
        return false;
    }
}
