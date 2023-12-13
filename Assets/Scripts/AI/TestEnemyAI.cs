using System.Collections;
using System.Collections.Generic;
using BehaviorTree;
using UnityEngine;

public class TestEnemyAI : MonoBehaviour
{

    public IEnumerator Start()
    {
        var root = new BTRoot();
        var sequence = new BTSequence();
        var condition = new BTCondition(TestCheck);
        var action = new BTAction(TestLog);

        root.AddChild(sequence);
        sequence.AddChild(condition);
        sequence.AddChild(action);

        while (true)
        {
            root.Evaluate();
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void TestLog()
    {
        Debug.Log("test");
    }

    private bool TestCheck()
    {
        Debug.Log("checked");
        return false;
    }
}
