using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainPlayer : BaseCharObject
{
    private Coroutine _moving = null;

    public override void StartMove(List<PlanePathNode> nodes)
    {
        if (_moving != null)
        {
            StopCoroutine(_moving);
            _moving = null;
        }

        _moving = StartCoroutine(OnStartMove(nodes));
    }

    public override void StartAttack()
    {
    }

    public override void StartMagic()
    {
    }

    public override void Move()
    {
    }

    public override void Recess()
    {
    }

    private IEnumerator OnStartMove(List<PlanePathNode> nodes)
    {
        PathManager.I.ResistNodeList(nodes);

        while (PathManager.I.MoveListLength > 0)
        {
            var node = PathManager.I.CunNode();
            PlayerManager.I.MainPlayer.transform.position = node.centerPos + (Vector2.one * 0.5f);

            yield return new WaitForSeconds(0.05f);
        }
    }
}
