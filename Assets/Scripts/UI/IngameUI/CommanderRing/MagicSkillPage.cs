using System.Collections;
using System.Collections.Generic;
using UI.Extension;
using UnityEngine;
using UnityEngine.Serialization;

public class MagicSkillPage : MonoBehaviour
{
    [FormerlySerializedAs("arrSkills")]
    [SerializeField] private ButtonEx[] arrBtnSkills;

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
            var isActive = skills.Count > i;
            arrBtnSkills[i].gameObject.SetActive(true);
            arrBtnSkills[i].ButtonString = TableManager.I.String.GetString(isActive
                ? skills[i].SkillID
                : 0);
        }
    }
}
