using System;
using skfksky1004.DevKit.UI;
using UnityEngine;
using UnityEngine.UI;

public class CommandButton : MonoBehaviour
{
    [SerializeField] private ButtonEx btnCommand;
    [SerializeField] private Image btnIcon;

    private Action _onCallback;

    private void Awake()
    {
        btnCommand?.AddListener(_onCallback);
    }

    public void SetCommand_Skill(int skillId = 0)
    {
        if (skillId.Equals(0))
            return;

        _onCallback = () =>
        {
            var skillData = TableManager.I.Skill.GetSkill(skillId);
            if (skillData is null)
                return;

            var mainPlayer = PlayerManager.I.MainPlayer;
            mainPlayer.MagicSkill(skillId);
        };
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
