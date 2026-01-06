using System.Collections;
using System.Collections.Generic;
using GlobalEnum;
using UnityEngine;

public class PlayerChar : BaseCharObject, i_PlayerChar
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

        var testLv = (short)1;
        var testJobID = (short)201;

        var lvData = TableManager.I.CharLevel.GetData(testLv);
        var jobData = TableManager.I.CharJob.GetData(testJobID);

        if (lvData != null && jobData != null)
        {
            charStatus.SetStatus(new StatusInfo(lvData, jobData));
        }
    }

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

    public void Dead()
    {

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
