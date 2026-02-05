using System.Collections.Generic;
using UnityEngine;

public class EnemyChar : BaseCharObject, i_Enemy_Battle
{
    public bool isDead = false;

    private TileMap TileMap => SceneController.I.CurScene.Map;
    private PathUtility Path => SceneController.I.CurScene.Path;
    
    public long EnemyIdx
    {
        get => CharIdx;
        set => CharIdx = value;
    }

    public void Attack(PlanePathNode node = null)
    {
        // var playerChar = PlayerManager.I.MainPlayer;
        // var attackRange = charStatus.GetStatus.attackRange;
        //
        // var myPos = TileMap.GetNode_WorldPos(transform.position).centerPos;
        // var targetPos = TileMap.GetNode_WorldPos(playerChar.transform.position).centerPos;
        //
        // var nodes = Path.FindPath_IncludeFindEnemy(myPos, targetPos, false);
        // if (nodes.Count <= attackRange)
        // {
        //     playerChar.HitDamage(charStatus.GetStatus.attackValue);
        //     Debug.Log($"{playerChar.name} 공~격~!");
        // }
        
        var attackRange = charStatus.GetStatus.attackRange;
        var myPos = TileMap.GetNode_WorldPos(transform.position).centerPos;

        var nodes = Path.FindUnitNodeList(myPos, attackRange);
        if (nodes.Count == 0)
            return;

        foreach (var n in nodes)
        {
            var playerUnit = Path.CheckNodeInPlayerUnitList(n);
            if (playerUnit != null)
            {
                playerUnit.HitDamage(charStatus.GetStatus.attackValue);
                Debug.Log($"{playerUnit.name} 공~격~!");
            }
        }
    }

    public void Move(List<PlanePathNode> nodes = null)
    {
        var searchRange = charStatus.GetStatus.searchRange;
        var mainPlayer = PlayerManager.I.MainPlayer;

        var myPos = TileMap.GetNode_WorldPos(transform.position).centerPos;
        var targetPos = CharPath.MoveListLength > 0
            ? CharPath.LastNode().centerPos
            : TileMap.GetNode_WorldPos(mainPlayer.transform.position).centerPos;

        nodes = Path.FindPath_IncludeFindEnemy(myPos, targetPos, false);
        if (nodes is null)
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

    public void Dead()
    {
        if(charStatus.GetStatus.curHp > 0)
            return;

        isDead = true;

        EnemyManager.I.RemoveEnemy(EnemyIdx);

        DestroyImmediate(this.gameObject);
    }

    public void Rest()
    {
    }

    public void HitDamage(int damage)
    {
        var status = charStatus.GetStatus;

        var beforeHP = status.curHp;
        var DemagedHP = status.curHp - damage;

        if (DemagedHP <= 0)
        {
            Dead();
        }

    }

    public virtual bool OnSearchPlayer(int range)
    {
        var mainPlayer = PlayerManager.I.MainPlayer;
        if (mainPlayer is null)
            return false;

        var myPos = TileMap.GetNode_WorldPos(transform.position).centerPos;
        var targetPos = CharPath.MoveListLength > 0
            ? CharPath.LastNode().centerPos
            : TileMap.GetNode_WorldPos(mainPlayer.transform.position).centerPos;

        //  적 발견
        var searchNodes = Path.FindPath(myPos, targetPos, false);
        if (searchNodes is null)
            return false;

        //  적에게 가는 경로 검색(못가는 경우를 위한 체크)
        var possibleMoveNodes = Path.FindPath_IncludeFindEnemy(myPos, targetPos, false);
        if (possibleMoveNodes is null)
            return false;

        //  이동거리보다 검색 거리가 높으면 하위 검색거리 삭제
        if (searchNodes.Count > charStatus.GetStatus.moveRange)
        {
            var tempValue = searchNodes.Count - charStatus.GetStatus.moveRange;
            searchNodes.RemoveRange(charStatus.GetStatus.moveRange,tempValue);
        }

        if (searchNodes.Count <= range + 1)
        {
            // Debug.Log("이동하겠소");
            return true;
        }

        // Debug.Log("이동하지 않겠소");
        return false;
    }
}
