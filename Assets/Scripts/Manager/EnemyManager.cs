using System.Collections.Generic;
using System.Linq;
using GlobalEnum;
using Unity.Mathematics;
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

    public EnemyChar CreateEnemy(eCharType charType, Vector3 createPos)
    {
        var path = "Character/EnemyChar";
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
            _activeEnemyList.Add(comp);
            return comp;
        }

        return null;
    }

    public bool GetIsEnemy(Vector2 posVec)
    {
        var pos = TilemapManager.I.GetNode_WorldPos(posVec).centerPos;
        foreach (var enemy in _activeEnemyList)
        {
            var targetPos = TilemapManager.I.GetNode_WorldPos(enemy.transform.position).centerPos;
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
