using System;
using UI.Extension;
using UnityEngine;
using UnityEngine.UI;

public class CommandButton : BaseScrollItem
{
    [SerializeField] private ButtonEx btnCommand;
    [SerializeField] private Image btnIcon;

    private Action _onCallback;

    private void Awake()
    {
        btnCommand.AddListener(_onCallback);
    }

    public void SetCommand_Skill(int skillId, Action<int> onCallback)
    {
        int id = skillId;
        if (skillId.Equals(0))
            return;

        _onCallback = () => { onCallback?.Invoke(id);};
    }

    public void SetCommand(string text, Action onCallback)
    {
        btnCommand.ButtonString = text;
        _onCallback = onCallback;
    }

    public void SetActiveButton(bool isActive)
    {
        btnCommand.SetActive(isActive);
    }
}
