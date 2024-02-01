using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GlobalEnum;
using UnityEngine;
using UnityEngine.Serialization;

public class CreateActionPlate : MonoBehaviour
{
    [SerializeField] private ActionPlate movePlateGo;

    private readonly List<ActionPlate> _createdList = new List<ActionPlate>();
    private const float PosValue = 1f;

    public bool IsCreatedPlate() => _createdList.Count > 0;
    
    public void ClearPlate()
    {
        foreach (var plate in _createdList)
        {
            Destroy(plate.gameObject);
        }
    
        _createdList.Clear();
    }
    
    public void CreatePlate_Move(Vector2 pos, int rangeCount, Action<Vector3> onClickPlate)
    {
        //1 = 3
        //2 = 5
        //3 = 7
        //4 = 9

        var startNum = -rangeCount;
        var endNum = rangeCount;

        var charNode = TilemapManager.I.GetNode_WorldPos(pos);
        
        for (int y = startNum; y <= endNum; y++)
        {
            for (int x = startNum; x <= endNum; x++)
            {
                if (x == 0 && y == 0)
                    continue;

                var calc = Mathf.Abs(x) + Mathf.Abs(y);
                if (calc <= rangeCount)
                {
                    var xPos = Convert.ToInt32(charNode.centerPos.x + x * PosValue);
                    var yPos = Convert.ToInt32(charNode.centerPos.y + y * PosValue);
                    if (EnemyManager.I.GetIsEnemy(xPos, yPos))
                        continue;

                    if (TilemapManager.I.IsMove(xPos, yPos) == false)
                        continue;

                    var pathNodes = PlayerManager.I.MainPlayer.Path.FindPath_IncludeFindEnemy(charNode.centerPos, new Vector3(xPos, yPos), false);
                    if (pathNodes is null ||
                        pathNodes.Count > rangeCount + 1)
                        continue;

                    var plate = CreatePlate(xPos, yPos);
                    if (plate != null)
                    {
                        plate.SetPlate(eCharCommand.Move, onClickPlate);

                        _createdList.Add(plate);
                    }
                }
            }
        }
    }

    public void CreatePlate_Attack(Vector2 pos, int rangeCount, Action<Vector3> onClickPlate)
    {
        var startNum = -rangeCount;
        var endNum = rangeCount;

        var charNode = TilemapManager.I.GetNode_WorldPos(pos);

        for (int y = startNum; y <= endNum; y++)
        {
            for (int x = startNum; x <= endNum; x++)
            {
                if (x == 0 && y == 0)
                    continue;

                var calc = Mathf.Abs(x) + Mathf.Abs(y);
                if (calc <= rangeCount)
                {
                    var xPos = Convert.ToInt32(charNode.centerPos.x + x * PosValue);
                    var yPos = Convert.ToInt32(charNode.centerPos.y + y * PosValue);

                    if (TilemapManager.I.IsMove(xPos, yPos) == false)
                        continue;

                    var plate = CreatePlate(xPos, yPos);
                    if (plate != null)
                    {
                        plate.SetPlate(eCharCommand.Attack, onClickPlate);

                        _createdList.Add(plate);
                    }
                }
            }
        }
    }

    public void CreatePlate_SkillTargetSingle(Vector2 pos, SkillData skillData, Action<Vector3> onClickPlate)
    {
        if (skillData is null)
            return;

        var commandType = skillData?.SkillType == (int)eSkillType.AttackSkill
            ? eCharCommand.Magic_Attack
            : eCharCommand.Magic_Buff;

        var rangeCount = skillData.SkillRange;

        var startNum = -rangeCount;
        var endNum = rangeCount;

        var charNode = TilemapManager.I.GetNode_WorldPos(pos);

        for (int y = startNum; y <= endNum; y++)
        {
            for (int x = startNum; x <= endNum; x++)
            {
                // if (x == 0 && y == 0)
                //     continue;

                var calc = Mathf.Abs(x) + Mathf.Abs(y);
                if (calc <= rangeCount)
                {
                    var xPos = Convert.ToInt32(charNode.centerPos.x + x * PosValue);
                    var yPos = Convert.ToInt32(charNode.centerPos.y + y * PosValue);

                    // if (TilemapManager.I.IsMove(xPos, yPos) == false)
                    //     continue;

                    if (skillData.SkillType == (int)eSkillType.AttackSkill &&
                        PlayerManager.I.GetIsPlayer(xPos, yPos))
                        continue;

                    if (skillData.SkillType == (int)eSkillType.BuffSkill &&
                        EnemyManager.I.GetIsEnemy(xPos, yPos))
                        continue;

                    var plate = CreatePlate(xPos, yPos);
                    if (plate != null)
                    {
                        plate.SetPlate(commandType, onClickPlate);

                        _createdList.Add(plate);
                    }
                }
            }
        }
    }

    private ActionPlate CreatePlate(int x, int y)
    {
        var go = Instantiate(movePlateGo.gameObject, this.transform);
        if (go != null)
        {
            var plate = go.GetComponent<ActionPlate>();
            plate.transform.localPosition = new Vector3(x + 0.5f, y + 0.5f, 1);
            plate.transform.localScale = Vector3.one;

            plate.gameObject.SetActive(true);

            return plate;
        }

        return null;
    }
}
