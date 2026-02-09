using System;
using System.Collections;
using System.Collections.Generic;
using GlobalEnum;
using UnityEngine;

public class PlayerUnit : BaseCharObject, i_PlayerChar
{
    [SerializeField] public Transform CameraFollowPos;

    private Coroutine _moving = null;
    public bool IsMoving => _moving != null;

    public bool IsMainPlayer { get; } = true;

    public Vector3 GetNodePos => SceneController.I.CurScene.Map.GetNode_WorldPos(transform.position)?.centerPos ?? Vector3.zero;

    public eCharAction CharAction {
        get => charStatus.CharAction;
        set => charStatus.CharAction = value;
    }

    public int MoveRange
    {
        get => charStatus.GetStatus.moveRange;
        set => charStatus.GetStatus.moveRange = value;
    }

    public int AttackRange
    {
        get => charStatus.GetStatus.attackRange;
        set => charStatus.GetStatus.attackRange = value;
    }

    private IEnumerator Start()
    {
        yield return null;

        //  테스트 스텟 셋팅
        var testLv = (short)1;
        var testJobID = (short)201;
        
        var lvData = TableManager.I.CharLevel.GetData(testLv);
        var jobData = TableManager.I.CharJob.GetData(testJobID);

        PlayerInit(lvData, jobData);
    }

    private void OnDestroy()
    {
        PlayerManager.I.RemovePlayer(CharIdx);
    }

    /// <summary>
    /// 플레이어의 스테이터스 정보 셋팅
    /// </summary>
    /// <param name="levelData"></param>
    /// <param name="jobData"></param>
    public void PlayerInit(CharLevelData levelData, CharJobData jobData)
    {
        if (levelData != null && jobData != null)
        {
            charStatus.SetStatus(new StatusInfo(levelData, jobData));
        }
    }

    /// <summary>
    /// 이동
    /// </summary>
    /// <param name="nodes"></param>
    public void Move(List<PlanePathNode> nodes = null)
    {
        if (nodes == null)
            return;

        if (_moving != null)
        {
            StopCoroutine(_moving);
            _moving = null;
        }

        _moving = StartCoroutine(OnStartMove(nodes, EndMove));

        charStatus.GetStatus.actPoint -= 5;
        CharAction = eCharAction.None;
    }

    /// <summary>
    /// 죽었다.
    /// </summary>
    public void Dead()
    {
        Debug.Log($"{charStatus.GetStatus.unitID}의 hp가 모두 소진되어 마을로 이동");
        
        SceneController.I.ChangeScene(SceneType.Scene_Village);
    }

    public  void Attack(PlanePathNode node)
    {
        var enemy = EnemyManager.I.GetEnemy(node.centerPos);
        if (enemy != null)
        {
            Debug.Log($"{enemy.name} 공~격~!");
            enemy.HitDamage(charStatus.GetStatus.attackValue);
        }

        charStatus.GetStatus.actPoint -= 2;
        CharAction = eCharAction.None;
    }

    public void MagicSkill(int magicId) { }

    public void MagicSkill(int magicId, List<PlanePathNode> node) { }

    public void Rest()
    {
    }

    public void HitDamage(int damage)
    {
        charStatus.GetStatus.curHp -= damage;
        Debug.Log($"{charStatus.GetStatus.unitID}가 {damage} 피격");
        
        if (charStatus.GetStatus.curHp <= 0)
        {
            Dead();
        }
    }

    private void EndMove()
    {
        if (_moving != null)
        {
            StopCoroutine(_moving);
            _moving = null;
        }
    }
}
