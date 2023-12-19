using System.Collections;
using System.Collections.Generic;
using GlobalEnum;
using UnityEngine;

public abstract class BaseCharObject : MonoBehaviour
{
    private CharSpriteRender _charSpriteRender;
    
    public CharStatus CharStatus;
    
    protected Vector3 BeforePos = Vector3.zero;

    protected Coroutine MoveCoroutine = null;
    
    private void Awake()
    {
        if (_charSpriteRender is null)
            _charSpriteRender = GetComponentInChildren<CharSpriteRender>();

        CharStatus = Utility.Component.GetComponent<CharStatus>(gameObject, true);
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

    /// <summary>
    /// 휴식
    /// </summary>
    public abstract void Recess();

    public abstract void HitDamage(int damage);

    public virtual bool OnSearchEnemy(int range)
    {
        return false;
    }

    protected IEnumerator OnStartMove(List<PlanePathNode> nodes, float delayTime = 0.05f)
    {
        if (GetComponent<EnemyChar>())
        {
            yield return new WaitForSeconds(delayTime);
        }

        TilemapManager.I.Path.ResistNodeList(nodes);

        while (TilemapManager.I.Path.MoveListLength > 0)
        {
            var node = TilemapManager.I.Path.CunNode();

            SetPosition(node.centerPos);

            yield return new WaitForSeconds(delayTime);
        }
    }
}
