using System;
using System.Collections;
using System.Linq;
using GlobalEnum;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utility;

public partial class BattleScene : SceneData
{
    public CreateActionPlate ActionPlate;

    private void Awake()
    {
        InitPlate();
    }

    // Start is called before the first frame update
    private IEnumerator Start()
    {
        if (GameSystem.I is null)
        {
            yield return new WaitUntil(()=>GameSystem.I.Initialize());
        }

        yield return new WaitUntil(()=>TilemapManager.I.Initialize());
        yield return new WaitUntil(()=>CameraManager.I.Initialize());

        PlayerManager.I.CreatePlayer(true);

        SceneController.I.CompleteSceneLoad();
    }

    private void InitPlate()
    {

        if (ActionPlate is null)
        {
            var prefab = ResourceManager.I.Load<GameObject>(eResourceType.Prefabs, "ActionPlates");
            if (prefab != null)
            {
                var go = Instantiate(prefab, transform);
                go.transform.localPosition = new Vector3(0, 0, 0);
                ActionPlate = go.GetComponent<CreateActionPlate>();
            }
        }
    }

    public void CreatePlates(Vector2 centerPos, eCharCommand commandType, short range, Action<Vector3> onClickPlate)
    {
        PlayerManager.I.MainPlayer.CharCommand = commandType;
        switch (commandType)
        {
            case eCharCommand.Move:
            {
                ActionPlate.CreatePlate_Move(centerPos, range, onClickPlate);
                break;
            }
            case eCharCommand.Attack:
            {
                ActionPlate.CreatePlate_Attack(centerPos, range, onClickPlate);
                break;
            }
        }
    }

    public void CreatePlates(Vector2 centerPos, int skillId, Action<Vector3> onClickPlate)
    {
        var skillData = TableManager.I.Skill.GetSkill(skillId);
        if (skillData is null)
            return;

        var actionType = skillData.SkillType == 1
            ? eCharCommand.Magic_Attack
            : eCharCommand.Magic_Buff;

        PlayerManager.I.MainPlayer.CharCommand = actionType;
        switch (actionType)
        {
            case eCharCommand.Magic_Attack:
            case eCharCommand.Magic_Buff:
            case eCharCommand.Magic_JobSkill:
            {
                PlayerManager.I.SelectSkillId = skillId;
                ActionPlate.CreatePlate_SkillTargetSingle(centerPos, skillData, onClickPlate);
                break;
            }
        }
    }

    public void ClearPlates()
    {
        ActionPlate.ClearPlate();
    }

    public override SceneType SceneType() => GlobalEnum.SceneType.Scene_Battle;
}