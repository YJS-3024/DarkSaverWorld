using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class Create_BinaryFile : Editor
{
    private const string TargetPath = "Assets/Resources/Tables/Original/";
    private const string ResultPath = "Assets/Resources/Tables/";
    private const string SaveFileName = "{0}.binary";

    // [MenuItem("Assets/Table/Create_BinaryTable")]
    [MenuItem("Utility/Table/Create_BinaryTable")]
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
