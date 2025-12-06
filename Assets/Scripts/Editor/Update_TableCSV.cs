using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Update_TableCSV : Editor
{
    private const string TargetPath = "Assets/Resources/Tables/Original/";
    private const string CsvListUrl =
        "https://docs.google.com/spreadsheets/d/1aNj3J5-tACKmgrxI0aKa9wHM_hIBuwEQCZ1Etg85thY/edit#gid=1306533330";

    private static Coroutine _coroutine;

    [MenuItem("Utility/Table/Update_TableCSV")]
    public static void UpdateCSV()
    {
    }

    private static IEnumerator UpdateCsvList()
    {
        yield return null;
    }
}
