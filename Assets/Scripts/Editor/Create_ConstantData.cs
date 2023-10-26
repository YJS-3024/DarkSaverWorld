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
    private const string LoadFileName = "Data_Constant";

    [MenuItem("Assets/Table/Convert_Constant")]
    public static void Convert()
    {
        if (Selection.objects.Length <= 0 ||
            Selection.objects.Length > 1)
        {
            Debug.LogWarning("하나의 테이블만 선택해 주세요.");
            return;
        }

        var textAsset = LoadTable();
        if (textAsset is null)
        {
            Debug.LogWarning("테이블 파일만 선택해 주세요.");
            return;
        }

        if (textAsset.name.Equals(LoadFileName) == false)
        {
            Debug.LogWarning("Constant 테이블을 선택해 주세요.");
            return;
        }

        var values = textAsset.text.Split(new char[]{'\n','\r'}, StringSplitOptions.RemoveEmptyEntries);
        var fieldName = values[0].Split(',').ToList();
        var checkIndex = fieldName.FindIndex(x => x == CheckFieldName);

        List<KeyValuePair<string, string>> typeList = new List<KeyValuePair<string, string>>();
        for (int i = 1; i < values.Length; i++)
        {
            var fieldValue = values[i].Split(',');
            if (fieldValue.Length >= checkIndex && string.IsNullOrEmpty(fieldValue[checkIndex]) == false)
            {
                var key = fieldValue[checkIndex];
                var value = fieldValue[checkIndex + 1];
                typeList.Add(new KeyValuePair<string, string>(key, value));
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
        if (Selection.objects.FirstOrDefault() is TextAsset asset)
        {
            return asset;
        }

        return null;
    }

    /// <summary>
    /// 파일 쓰기
    /// </summary>
    /// <param name="path"></param>
    /// <param name="typeList"></param>
    private static void WriteFile(string path, List<KeyValuePair<string, string>> typeList)
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
    private static string CreateScript(List<KeyValuePair<string, string>> constantList)
    {
        var sb = new StringBuilder();

        sb.AppendLine("public class ConstantData");
        sb.AppendLine("{");

        foreach (var constant in constantList)
        {
            var valueType = GetCheckValueType(constant.Value);
            var floatCode = valueType.Equals("float") ? "f" : "";
            sb.AppendLine($"   public const {valueType} {constant.Key} = {constant.Value}{floatCode};");
            sb.AppendLine("");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>
    /// 형태에 맞는 스트링
    /// </summary>
    /// <param name="strValue"></param>
    /// <returns></returns>
    private static string GetCheckValueType(string strValue)
    {
        if (strValue.Contains("."))
        {
            return "float";
        }

        var value = System.Convert.ToInt64(strValue);
        if (value > 0)
        {
            if (int.MaxValue < value)
            {
                return "long";
            }
            else if (short.MaxValue >= value)
            {
                return "short";
            }
        }

        return "int";
    }
}
