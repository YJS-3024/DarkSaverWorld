using System;
using UI.Extension;
using UnityEngine;
using UnityEngine.UI;

public class CommandButton : BaseScrollItem
{
    [SerializeField] private ButtonEx btnCommand;
    [SerializeField] private Image btnIcon;

    private Action _onCallback_None;
    private Action<int> _onCallback_Value;
    private int _value = -1;

    private void Awake()
    {
        btnCommand.AddListener(OnClick_Command);
    }

    public void SetCommand_Skill(int skillId, Action<int> onCallback)
    {
        _value = skillId;

        var skillData = TableManager.I.Skill.GetSkill(skillId);
        if (skillData is null)
            return;

        btnCommand.ButtonString = TableManager.I.String.GetString(skillData.SkillName);

        _onCallback_None = null;
        _onCallback_Value = onCallback;
    }

    public void SetCommand(string text, Action onCallback)
    {
        _value = -1;
        btnCommand.ButtonString = text;

        _onCallback_None = onCallback;
        _onCallback_Value = null;
    }

    public void SetActiveButton(bool isActive)
    {
        btnCommand.SetActive(isActive);
    }

    private void OnClick_Command()
    {
        if (_value == -1)
        {
            _onCallback_None?.Invoke();
        }
        else
        {
            _onCallback_Value?.Invoke(_value);
        }
    }
}
