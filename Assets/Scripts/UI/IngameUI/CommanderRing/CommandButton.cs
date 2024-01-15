using System.Collections;
using System.Collections.Generic;
using GlobalEnum;
using UnityEngine;
using UnityEngine.UI;

public class CommandButton : MonoBehaviour
{
    [SerializeField] private Button btnCommand;

    public void SetCommand(eCommandType commandType, int id = 0)
    {


        if (id.Equals(0))
        {
            return;
        }


    }
}
