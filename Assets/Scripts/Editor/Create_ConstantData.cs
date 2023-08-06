using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

/// <summary>
/// 설명서
/// > ConstantDataTable의 타입을 enum으로 자동 스크립트 생성
/// > 파일은 하나밖에 만들어 지지 않는다
///
/// 1. 해당 테이블의 오른쪽 클릭
/// 2. Convert_Constant 클릭
/// 3. 대기하면 스크립트 생긴다.
/// </summary>
public class Create_ConstantData : Editor
{
    private const string CheckFieldName = "Index";
    private const string SaveFilePath = "Assets/Scripts/TableData";
    private const string SaveFileName = "ConstantData";

    [MenuItem("Assets/Table/Convert_Constant")]
    public static void Convert()
    {
        if (Selection.objects.Length <= 0 ||
            Selection.objects.Length > 1)
        {
            return;
        }

        var textAsset = LoadTable();
        if (textAsset is null)
            return;

        var values = textAsset.text.Split(new char[]{'\n','\r'}, StringSplitOptions.RemoveEmptyEntries);
        var fieldName = values[0].Split(',').ToList();
        var checkIndex = fieldName.FindIndex(x => x == CheckFieldName);

        List<string> typeList = new List<string>();
        for (int i = 1; i < values.Length; i++)
        {
            var fieldValue = values[i].Split(',');
            if (fieldValue.Length >= checkIndex && string.IsNullOrEmpty(fieldValue[checkIndex]) == false)
            {
                typeList.Add(fieldValue[checkIndex]);
            }
        }

        var fullPath = $"{SaveFilePath}/{SaveFileName}.cs";
        ReadFile(fullPath);
        WriteFile(fullPath, typeList);
    }

    /// <summary>
    /// CSV 내용
    /// </summary>
    private static TextAsset LoadTable()
    {
        if (Selection.objects.FirstOrDefault() is TextAsset textAsset)
            return textAsset;

        return null;
    }

    /// <summary>
    /// 파일 쓰기
    /// </summary>
    /// <param name="path"></param>
    /// <param name="typeList"></param>
    private static void WriteFile(string path, List<string> typeList)
    {
        var directoryInfo = new DirectoryInfo(Path.GetDirectoryName(path) ?? string.Empty);
        if (directoryInfo.Exists == false)
        {
            directoryInfo.Create();
        }

        var fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
        var writer = new StreamWriter(fileStream, System.Text.Encoding.Unicode);

        writer.WriteLine(CreateScript(typeList));

        writer.Close();
    }

    /// <summary>
    /// 파일 읽기
    /// </summary>
    /// <param name="path"></param>
    private static void ReadFile(string path)
    {
        var fileInfo = new FileInfo(path);
        if (fileInfo.Exists == false)
            return;

        fileInfo.Delete();
    }

    /// <summary>
    /// C# 스크립트 생성
    /// </summary>
    /// <param name="constantList"></param>
    /// <returns></returns>
    private static string CreateScript(List<string> constantList)
    {
        var sb = new StringBuilder();

        sb.AppendLine("public enum Enum_DataConstant");
        sb.AppendLine("{");

        foreach (var strType in constantList)
        {
            sb.AppendLine($"    {strType},");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }
}
