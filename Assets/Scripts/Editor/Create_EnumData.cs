using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 설명서
/// > Enum을 모아둔 테이블 데이터를 스크립트를 생성
///
/// 1. 해당 테이블의 오른쪽 클릭
/// 2. Convert_Constant 클릭
/// 3. 대기하면 스크립트 생긴다.
/// </summary>
public class Create_EnumData : Editor
{
    private const string CheckEnumName = "Group";       //  EnumName으로 셋팅할 칼럼
    private const string CheckTypeName = "Index";       //  Enum에 추가할 타입의 칼럼
    private const string SaveFilePath = "Assets/Scripts/TableData";
    private const string SaveFileName = "EnumData";

    [MenuItem("Assets/Table/Convert_EnumList")]
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
        var checkIndex = fieldName.FindIndex(x => x == CheckEnumName);

        var enumList = new Dictionary<string, List<string>>();


        for (var index = 1; index < values.Length; index++)
        {
            var value = values[index];
            if (string.IsNullOrEmpty(value))
                continue;

            var checkValue = value.Split(',').ToList();
            if (enumList.TryGetValue(checkValue[checkIndex], out var list) == false)
            {
                enumList.Add(checkValue[checkIndex], new List<string>());
                list = enumList[checkValue[checkIndex]];
            }

            list.Add(checkValue.FirstOrDefault());
        }


        var fullPath = $"{SaveFilePath}/{SaveFileName}.cs";
        ReadFile(fullPath);
        WriteFile(fullPath, enumList);
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
    private static void WriteFile(string path, Dictionary<string, List<string>> typeList)
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
    /// <param name="enumList"></param>
    /// <returns></returns>
    private static string CreateScript(Dictionary<string, List<string>> enumList)
    {
        var sb = new StringBuilder();

        var list = enumList.Keys.ToList();
        foreach (var enumName in list)
        {
            sb.AppendLine($"public enum {enumName}");
            sb.AppendLine("{");

            var typeList = enumList[enumName].ToList();
            foreach (var strType in typeList)
            {
                sb.AppendLine($"    {strType},");
            }

            sb.AppendLine("}");
            sb.AppendLine("\n");
        }

        return sb.ToString();
    }
}
