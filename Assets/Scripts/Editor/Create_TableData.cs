using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 설명서
/// > 테이블의 1번째 로우를 테이블 데이터로 자동 스크립트 생성
/// > 파일이 많을수록 오래 걸린다.
///
/// 1. 해당 테이블의 오른쪽 클릭(여러개 선택가능)
/// 2. Convert_TableData 클릭
/// 3. 대기하면 선택한 테이블 갯수만큼 스크립트 생긴다.
/// </summary>
public class Create_TableData : Editor
{
    private const string FilePath = "Assets/Scripts/TableData";
    private const string OneTab = "     ";

    [MenuItem("Assets/Table/Convert_TableData")]
    public static void Convert()
    {
        foreach (var obj in Selection.objects)
        {
            if (!(obj is TextAsset textAsset))
                return;

            var values = textAsset.text.Split(new char[]{'\n','\r'}, StringSplitOptions.RemoveEmptyEntries);
            var fieldName = values[0].Split(',').ToList();

            var fileName = textAsset.name
                .Replace("Data_", "")
                .Replace("_","");
            var resultFileName = fileName.Insert(fileName.Length, "Data");

            var fullPath = $"{FilePath}/{resultFileName}.cs";
            ReadFile(fullPath);
            WriteFile_Data(fullPath, resultFileName, fieldName);
            WriteFile_Table(resultFileName, fieldName.FirstOrDefault());
        }
    }

    /// <summary>
    /// 파일 쓰기
    /// </summary>
    /// <param name="path"></param>
    /// <param name="fileName"></param>
    /// <param name="typeList"></param>
    private static void WriteFile_Data(string path, string fileName, List<string> typeList)
    {
        var directoryInfo = new DirectoryInfo(Path.GetDirectoryName(path) ?? string.Empty);
        if (directoryInfo.Exists == false)
        {
            directoryInfo.Create();
        }

        var fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
        var writer = new StreamWriter(fileStream, System.Text.Encoding.Unicode);

        writer.WriteLine(CreateScript(fileName, typeList));

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
    /// <param name="fileName"></param>
    /// <param name="constantList"></param>
    /// <returns></returns>
    private static string CreateScript(string fileName, List<string> constantList)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"public partial class {fileName} : BaseTableData");
        sb.AppendLine("{");

        foreach (var strType in constantList)
        {
            var type = GetType(strType, out var value);
            if (string.IsNullOrEmpty(value))
                continue;

            sb.Append(OneTab);
            sb.AppendLine($"public {type} {value};");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>
    /// 파일 쓰기
    /// </summary>
    /// <param name="path"></param>
    /// <param name="fileName"></param>
    /// <param name="typeList"></param>
    private static void WriteFile_Table(string fileName, string typeFirst)
    {
        var dataName = fileName.Clone().ToString();
        fileName = $"TableManager_{fileName.Replace("Data", "")}";
        var path = $"Assets/Scripts/TableData/TableManagers/{fileName}.cs";

        //  해당 파일까지의 폴더 확인후 없으면 생성
        var directoryInfo = new DirectoryInfo(Path.GetDirectoryName(path) ?? string.Empty);
        if (directoryInfo.Exists == false)
        {
            directoryInfo.Create();
        }

        //  해당 파일이 없다면 생성 있다면 작업종료
        if (File.Exists(path))
            return;

        var fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
        var writer = new StreamWriter(fileStream, System.Text.Encoding.Unicode);

        writer.WriteLine(CreateTableScript(fileName, dataName, typeFirst));

        writer.Close();
    }

    private static string CreateTableScript(string fileName, string dataName, string typeFirst)
    {
        var typeResult = GetType(typeFirst, out var s);

        var sb = new StringBuilder();

        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();

        sb.AppendLine("namespace Table");
        sb.AppendLine("{");

        sb.Append(OneTab);
        sb.AppendLine($"public partial class {fileName} : {nameof(Table.Table_Interface)}");

        sb.Append(OneTab);
        sb.AppendLine($""+"{");

        sb.Append(OneTab);
        sb.Append(OneTab);
        sb.AppendLine($"public Dictionary<int, {dataName}> Dic{dataName}s = new Dictionary<int, {dataName}>();");

        sb.AppendLine(OneTab);

        sb.AppendLine("public bool IsLoadSuccess { get; set; } = false;");

        sb.Append(OneTab);
        sb.AppendLine("}");

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string GetType(string strTable, out string value)
    {
        var strType = strTable;
        if (strType.Contains("f_"))
        {
            value = strType.Replace("f_","");
            return "float";
        }
        else if (strType.Contains("i_"))
        {
            value = strType.Replace("i_","");
            return "int";
        }
        else if (strType.Contains("is_"))
        {
            value = strType.Replace("is_","");
            return "bool";
        }
        else if (strType.Contains("sh_"))
        {
            value = strType.Replace("sh_","");
            return "short";
        }
        else if (strType.Contains("d_"))
        {
            value = strType.Replace("d_","");
            return "double";
        }
        else if (strType.Contains("b_"))
        {
            value = strType.Replace("b_","");
            return "byte";
        }
        else if (strType.Contains("s_"))
        {
            value = strType.Replace("s_","");
            return "string";
        }

        value = string.Empty;
        return "string";
    }
}
