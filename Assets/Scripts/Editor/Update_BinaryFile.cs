using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class Update_BinaryFile : Editor
{
    private const string TargetPath = "Assets/Resources/Tables/Original/";
    private const string ResultPath = "Assets/Resources/Tables/";
    private const string SaveFileName = "{0}.binary";

    [MenuItem("Utility/Table/Update_BinaryFile")]
    public static void Create()
    {
        //  저장할 폴더 체크
        var directoryInfo = new DirectoryInfo(Path.GetDirectoryName(ResultPath) ?? string.Empty);
        if (directoryInfo.Exists == false)
        {
            directoryInfo.Create();
        }

        //  원본 파일 폴더의 파일 확인
        var assets = AssetDatabase.FindAssets("", new[] { TargetPath });

        foreach (var guid in assets)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var obj = AssetDatabase.LoadAssetAtPath(assetPath, typeof(TextAsset));

            var textAsset = obj as TextAsset;
            if (textAsset is null)
                return;

            var path = $"{ResultPath}{string.Format(SaveFileName, textAsset.name)}";
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Create))
                {
                    using (BinaryWriter bw = new BinaryWriter(fs))
                    {
                        bw.Write(textAsset.text);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
                throw;
            }
        }
    }
}
