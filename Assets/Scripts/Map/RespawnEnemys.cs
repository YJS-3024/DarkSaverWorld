using System;
using System.Collections;
using System.Collections.Generic;
using GlobalEnum;
using UnityEngine;

public class RespawnEnemys : MonoBehaviour
{
    [SerializeField] private int createCountMax = 3;
    [SerializeField] private int createMonsterId = 1;

    private float createInterval = 10f;
    private readonly List<BaseCharObject> _enemyList = new List<BaseCharObject>();

    private Vector3 _curPos = Vector3.zero;

    public bool IsInit = false;
    public bool IsScarecrow = false;

    // Start is called before the first frame update
    private IEnumerator Start()
    {
        yield return new WaitUntil(() => IsInit);
        
        _curPos = TilemapManager.I.GetNode_WorldPos(transform.position).centerPos;

        while (true)
        {
            yield return new WaitForSeconds(1f);
            
            if (TilemapManager.I.IsStandChar(_curPos) &&
                TilemapManager.I.IsStandEnemy(_curPos))
                continue;

            if(_enemyList.Count >= createCountMax)
                continue;
            
            _enemyList.Add(EnemyManager.I.CreateEnemy(createMonsterId, transform.position, IsScarecrow));

            yield return new WaitForSeconds(createInterval);
        }
    }
}
