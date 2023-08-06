using System.Collections;
using System.Collections.Generic;
using GlobalEnum;
using UnityEngine;

public class TableLoader : MonoBehaviour
{
    private List<string> GetTableNames()
    {
        var tableNames = new List<string>();
        // tableNames.Add("");


        return tableNames;
    }

    private void LoadTable()
    {
        foreach (var name in GetTableNames())
        {
            
            
            
            TextAsset csvFile = ResourceManager.I.Load<TextAsset>(eResourceType.TextAsset, name);
            if (csvFile is null)
                continue;
            
            
        }
 
    }
    
}
