using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class CommandPanel : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private BaseScrollPool scrollPool;

    private readonly List<CommandButton> _activeButtonList = new List<CommandButton>();

    private void OnEnable()
    {
        var list = new[] { "Move", "Attack", "Magic", "Rest"};
        foreach (var command in list)
        {
            var item = scrollPool.PopItem(scrollRect.content);
            if (item is CommandButton btn)
            {
                btn.SetCommand($"Action_{command}", null);
                btn.SetActiveButton(true);

                _activeButtonList.Add(btn);
            }
        }
    }

    private void OnDisable()
    {
        foreach (var btn in _activeButtonList)
        {
            scrollPool.PushItem(btn);
            _activeButtonList.Remove(btn);
        }
    }
}
