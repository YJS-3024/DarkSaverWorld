using System.Collections.Generic;
using System.Linq;
using GlobalEnum;
using Unity.Mathematics;
using UnityEngine;

public class EnemyManager : MonoSingleton<EnemyManager>
{
    private Dictionary<long, EnemyChar> _activeEnemyList;

    private long _createIndex = 0;

    protected override void Destroy()
    {
        
    }

    public override bool Initialize()
    {
        if (_activeEnemyList is null)
        {
            _activeEnemyList = new Dictionary<long, EnemyChar>();
        }
        
        return true;
    }

    public EnemyChar CreateEnemy(int monId, Vector3 createPos)
    {
        var enemyType = eCharType.Monster_Normal;
        var path = $"Character/Enemy{enemyType.ToString()}";
        var o = ResourceManager.I.Load<GameObject>(eResourceType.Prefabs, path);
        if (o is null)
            return null;

        var go = Instantiate(o);
        if (go != null)
        {
            go.transform.position = createPos;
            go.transform.rotation = quaternion.identity;
            go.transform.localScale = Vector3.one;

            var comp = go.GetComponent<EnemyChar>();
            comp.EnemyID = ++_createIndex;

            _activeEnemyList.Add(comp.EnemyID, comp);
            return comp;
        }

        return null;
    }

    public bool GetIsEnemy(Vector2 posVec)
    {
        var pos = TilemapManager.I.GetNode_WorldPos(posVec).centerPos;
        foreach (var enemy in _activeEnemyList)
        {
            var targetPos = TilemapManager.I.GetNode_WorldPos(enemy.Value.transform.position).centerPos;
            if (pos.Equals(targetPos))
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
            var pos = TilemapManager.I.GetNode_WorldPos(enemy.Value.transform.position).centerPos;
            if (pos.Equals(posVec))
            {
                return enemy.Value;
            }
        }

        return null;
    }

    public bool GetIsEnemy(int posX, int posY)
    {
        var posVec = new Vector2(posX, posY);
        foreach (var enemy in _activeEnemyList)
        {
            var pos = TilemapManager.I.GetNode_WorldPos(enemy.Value.transform.position).centerPos;
            if (pos.Equals(posVec))
            {
                return true;
            }
        }

        return false;
    }

    public void RemoveEnemy(long enemyId)
    {
        if (_activeEnemyList.ContainsKey(enemyId))
        {
            _activeEnemyList.Remove(enemyId);
        }
    }
}
