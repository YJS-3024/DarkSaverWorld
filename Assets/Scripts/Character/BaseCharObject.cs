using System.Collections;
using System.Collections.Generic;
using GlobalEnum;
using UnityEngine;

public abstract class BaseCharObject : MonoBehaviour
{
    private CharSpriteRender _charSpriteRender;
    
    public CharStatus CharStatus;
    
    protected Vector3 BeforePos = Vector3.zero;
    
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
        
        if (direction.y > 0)
        {
            // _charSpriteRender.SetSpriteDirection(eCharDirectionType.Back);
            // if (direction.x == 0)
            //     return;
            //
            // _charSpriteRender.SetSpriteDirection(direction.x > 0
            //     ? eCharDirectionType.Right
            //     : eCharDirectionType.Left);
        }
        else
        {
            // _charSpriteRender.SetSpriteDirection(eCharDirectionType.Forward);
            // if (direction.x == 0)
            //     return;
            //
            // _charSpriteRender.SetSpriteDirection(direction.x > 0
            //     ? eCharDirectionType.Right
            //     : eCharDirectionType.Left);
        }
    }

    /// <summary>
    /// 이동 시작
    /// </summary>
    public abstract void StartMove(List<PlanePathNode> nodes);

    /// <summary>
    /// 공격 시작
    /// </summary>
    public abstract void StartAttack();

    /// <summary>
    /// 마법 공격 시작
    /// </summary>
    public abstract void StartMagic();

    /// <summary>
    /// 이동
    /// </summary>
    public abstract void Move();

    /// <summary>
    /// 휴식
    /// </summary>
    public abstract void Recess();

}
