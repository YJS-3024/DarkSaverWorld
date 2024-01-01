using System;
using System.Collections;
using System.Collections.Generic;
using GlobalEnum;
using UnityEngine;

public class RespawnEnemys : MonoBehaviour
{
    [SerializeField] private int createCountMax = 3;

    private float createInterval = 10f;
    private readonly List<BaseCharObject> _enemyList = new List<BaseCharObject>();

    private Vector3 _curPos = Vector3.zero;

    // Start is called before the first frame update
    private IEnumerator Start()
    {
        _curPos = TilemapManager.I.GetNode_WorldPos(transform.position).centerPos;

        while (true)
        {
            yield return new WaitForSeconds(1f);
            
            if (TilemapManager.I.IsStandChar(_curPos) &&
                TilemapManager.I.IsStandEnemy(_curPos))
                continue;

            if(_enemyList.Count >= createCountMax)
                continue;
            
            _enemyList.Add(EnemyManager.I.CreateEnemy(eCharType.ENEMY, transform.position));

            yield return new WaitForSeconds(createInterval);
        }
    }
}
