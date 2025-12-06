using System.Collections;
using System.Collections.Generic;
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

    private TileMap TileMap => SceneController.I.CurTileMap;

    // Start is called before the first frame update
    private IEnumerator Start()
    {
        yield return new WaitUntil(() => IsInit);
        
        _curPos = TileMap.GetNode_WorldPos(transform.position).centerPos;

        while (true)
        {
            yield return new WaitForSeconds(1f);
            
            if (TileMap.IsStandChar(_curPos) &&
                TileMap.IsStandEnemy(_curPos))
                continue;

            if(_enemyList.Count >= createCountMax)
                continue;
            
            _enemyList.Add(EnemyManager.I.CreateEnemy(createMonsterId, transform.position, IsScarecrow));

            yield return new WaitForSeconds(createInterval);
        }
    }
}
