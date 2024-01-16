using System.Collections;
using System.Collections.Generic;
using UI.Extension;
using UnityEngine;
using UnityEngine.Serialization;

public class MagicSkillPage : MonoBehaviour
{
    [FormerlySerializedAs("arrSkills")]
    [SerializeField] private CommandButton[] arrBtnSkills;

    private void Awake()
    {
        gameObject.SetActive(false);

        foreach (var btnSkill in arrBtnSkills)
            btnSkill.gameObject.SetActive(false);
    }

    public void SetPage()
    {
        var skills = TableManager.I.Skill.GetSkillList();
        if (skills.Count == 0)
            return;

        for (int i = 0; i < arrBtnSkills.Length; i++)
        {
            arrBtnSkills[i].gameObject.SetActive(true);

            var isActive = skills.Count > i;
            if (isActive)
            {
                arrBtnSkills[i].SetCommand_Skill(skills[i].SkillID);
            }

            arrBtnSkills[i].SetActiveButton(isActive);
        }
    }
}
