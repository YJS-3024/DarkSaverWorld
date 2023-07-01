using System.Collections;
using System.Collections.Generic;
using GlobalEnum;
using UnityEngine;

public abstract class BaseCharObject : MonoBehaviour
{
    private CharSpriteRender _charSpriteRender;
    
    protected Vector3 BeforePos = Vector3.zero;
    
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
    
    
    private void Awake()
    {
        if (_charSpriteRender is null)
        {
            _charSpriteRender = GetComponentInChildren<CharSpriteRender>();
        }
    }


    protected void SetPosition(Vector2 worldPos)
    {
        BeforePos = PlayerManager.I.MainPlayer.transform.position;
        PlayerManager.I.MainPlayer.transform.position = worldPos + (Vector2.one * 0.5f);

        var direction = PlayerManager.I.MainPlayer.transform.position - BeforePos;
        SetDirection(direction);
    }

    protected void SetDirection(Vector2 directPos)
    {
        if (directPos == Vector2.zero)
        {
            return;
        }
        
        if (directPos.y > 0)
        {
            _charSpriteRender.SetSpriteDirection(eCharDirectionType.Back);
            if (directPos.x > 0)
            {
                _charSpriteRender.SetSpriteDirection(eCharDirectionType.Right);
            }
            else if (directPos.x < 0)
            {
                _charSpriteRender.SetSpriteDirection(eCharDirectionType.Left);
            }
        }
        else
        {
            _charSpriteRender.SetSpriteDirection(eCharDirectionType.Forward);
            if (directPos.x > 0)
            {
                _charSpriteRender.SetSpriteDirection(eCharDirectionType.Right);
            }
            else if (directPos.x < 0)
            {
                _charSpriteRender.SetSpriteDirection(eCharDirectionType.Left);
            }
        }
    }
}
