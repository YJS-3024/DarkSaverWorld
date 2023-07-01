using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GlobalEnum;
using UnityEngine;

public class CharSpriteRender : MonoBehaviour
{
    private SpriteRenderer _charRenderer;

    private List<Sprite> _charBoard;

    private string jobName = "Fighter";
    public eCharDirectionType DirectionType { set; private get; } = eCharDirectionType.Forward;
    private int animStep = 1;
    
    private bool animStepDir = true;
    private float animTime = 0;
    
    private const float AnimTimeMax = 0.2f;
    
    private void Awake()
    {
        if (_charRenderer is null)
        {
            _charRenderer = GetComponent<SpriteRenderer>();
            if (_charRenderer is null)
            {
                _charRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
        }

        if (_charBoard is null)
        {
            var path = "Character/Job/1.BoByeong";
            var sprites = ResourceManager.I.RoadSpritesAll(path);
            _charBoard = sprites.Where(x => x.name.Contains(jobName)).ToList();

            _charRenderer.sprite = GetSprite();
        }
    }

    private void Update()
    {
        if (animTime > AnimTimeMax)
        {
            animTime = 0;
            
            //  이미지 변경 방향
            if (animStep == 2 || animStep == 0)
                animStepDir = !animStepDir;
            
            //  이미지 스텝
            animStep += animStepDir ? 1 : -1;

            if (_charBoard.Count > 0)
            {
                _charRenderer.sprite = GetSprite();
            }
        }

        animTime += Time.deltaTime;
    }

    private Sprite GetSprite()
    {
        if (_charBoard.Count > 0)
        {
            return _charBoard.Find(x => x.name.Equals(GetSpriteName()));
        }

        return null;
    }
    
    private string GetSpriteName() => $"{jobName}_{DirectionType.ToString()}_0{animStep}";
    
    public void SetSpriteDirection(eCharDirectionType directionType)
    {
        DirectionType = directionType;
        
        _charRenderer.sprite = GetSprite();
    }
}
