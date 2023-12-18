using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyManager : MonoSingleton<EnemyManager>
{
    private List<EnemyChar> _activeEnemyList;

    protected override void Destroy()
    {
        
    }

    public override bool Initialize()
    {
        if (_activeEnemyList is null)
        {
            _activeEnemyList = FindObjectsOfType<EnemyChar>().ToList();
        }
        
        return true;
    }

    public bool GetIsEnemy(Vector2 posVec)
    {
        foreach (var enemy in _activeEnemyList)
        {
            var pos = TilemapManager.I.GetNode_WorldPos(enemy.transform.position).centerPos;
            if (pos.Equals(posVec))
            {
                return true;
            }
        }

        return false;
    }

    public EnemyChar GetEnemy(Vector2 posVec)
    {
        foreach (var enemy in _activeEnemyList)
        {
            var pos = TilemapManager.I.GetNode_WorldPos(enemy.transform.position).centerPos;
            if (pos.Equals(posVec))
            {
                return enemy;
            }
        }

        return null;
    }

    public bool GetIsEnemy(int posX, int posY)
    {
        var posVec = new Vector2(posX, posY);
        foreach (var enemy in _activeEnemyList)
        {
            var pos = TilemapManager.I.GetNode_WorldPos(enemy.transform.position).centerPos;
            if (pos.Equals(posVec))
            {
                return true;
            }
        }

        return false;
    }
}
