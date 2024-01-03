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
            WriteFile_Table(resultFileName, fieldName);
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
            sb.Append(OneTab);
            if (strType.Contains("i_"))
            {
                var value = strType.Replace("i_","");
                sb.AppendLine($"public int {value};");
            }
            else if (strType.Contains("f_"))
            {
                var value = strType.Replace("f_","");
                sb.AppendLine($"public float {value};");
            }
            else if (strType.Contains("s_"))
            {
                var value = strType.Replace("s_","");
                sb.AppendLine($"public string {value};");
            }
            else if (strType.Contains("b_"))
            {
                var value = strType.Replace("b_","");
                sb.AppendLine($"public bool {value};");
            }
            else if (strType.Contains("sh_"))
            {
                var value = strType.Replace("sh_","");
                sb.AppendLine($"public short {value};");
            }
            else if (strType.Contains("d_"))
            {
                var value = strType.Replace("d_","");
                sb.AppendLine($"public double {value};");
            }
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
    private static void WriteFile_Table(string fileName, List<string> typeList)
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

        writer.WriteLine(CreateTableScript(fileName, dataName));

        writer.Close();
    }

    private static string CreateTableScript(string fileName, string dataName)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();

        sb.AppendLine("namespace Table");
        sb.AppendLine("{");

        sb.Append(OneTab);
        sb.AppendLine($"public partial class {fileName}");

        sb.Append(OneTab);
        sb.AppendLine($""+"{");

        sb.Append(OneTab);
        sb.Append(OneTab);
        sb.AppendLine($"public Dictionary<int, {dataName}> Dic{dataName}s = new Dictionary<int, {dataName}>();");

        sb.Append(OneTab);
        sb.AppendLine("}");

        sb.AppendLine("}");

        return sb.ToString();
    }
}
