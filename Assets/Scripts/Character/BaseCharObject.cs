using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GlobalEnum;
using UnityEngine;
using UnityEngine.Serialization;


public class CharPath
{
    private readonly Queue<PlanePathNode> moveList = new Queue<PlanePathNode>();
    public int MoveListLength => moveList.Count;

    public void ResistNodeList(List<PlanePathNode> nodes)
    {
        for (var index = 0; index < nodes.Count; index++)
        {
            var node = nodes[index];
            if (Contains(node))
                continue;

            moveList.Enqueue(node);
        }
    }

    public PlanePathNode CunNode()
    {
        return moveList.Dequeue();
    }

    public PlanePathNode LastNode()
    {
        return moveList.LastOrDefault();
    }

    public bool Contains(PlanePathNode node)
    {
        foreach (var check in moveList)
        {
            if (check.centerPos.Equals(node.centerPos))
                return true;
        }

        return false;
    }
}

public abstract class BaseCharObject : MonoBehaviour
{
    private CharSpriteRender _charSpriteRender;

    public CharStatus charStatus;
    
    protected Vector3 BeforePos = Vector3.zero;

    private CharPath _charPath;
    public CharPath CharPath
    {
        get
        {
            if (_charPath is null)
                _charPath = new CharPath();

            return _charPath;
        }
    }

    private void Awake()
    {
        if (_charSpriteRender is null)
            _charSpriteRender = GetComponentInChildren<CharSpriteRender>();

        charStatus = Utility.Component.GetComponent<CharStatus>(gameObject, true);
    }


    protected void SetPosition(Vector2 worldPos)
    {
        BeforePos = gameObject.transform.position;
        gameObject.transform.position = worldPos + (Vector2.one * 0.5f);

        var direction = gameObject.transform.position - BeforePos;
        if (direction == Vector3.zero)
        {
            return;
        }
    }

    /// <summary>
    /// 공격 시작
    /// </summary>
    public abstract void Attack(PlanePathNode node);

    /// <summary>
    /// 마법 공격 시작
    /// </summary>
    public abstract void Magic();

    /// <summary>
    /// 이동
    /// </summary>
    public abstract void Move(List<PlanePathNode> nodes = null);
    protected IEnumerator OnStartMove(List<PlanePathNode> nodes, float delayTime = 0.05f)
    {
        if (GetComponent<EnemyChar>())
        {
            yield return new WaitForSeconds(delayTime);
        }

        CharPath.ResistNodeList(nodes);

        while (CharPath.MoveListLength > 0)
        {
            var node = CharPath.CunNode();

            SetPosition(node.centerPos);

            yield return new WaitForSeconds(delayTime);
        }
    }

    /// <summary>
    /// 휴식
    /// </summary>
    public abstract void Recess();

    public abstract void HitDamage(int damage);
}
