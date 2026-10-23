using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class NativeBattleResultBuilder
    {
        public const string Output = "Assets/_Game/Resources/UI/NativeBattleResult.png";

        [MenuItem("DarkSaver/UI/Build Native Battle Result")]
        public static void Build()
        {
            const string source = "분석/Saver_1.2/";
            var pixels = NativeStatusPanelBuilder.Compose(
                File.ReadAllBytes(source + "extracted/Saver_Files/Saver/Glib/gscene17.lsc"),
                File.ReadAllBytes(source + "unpacked/legacy_libraries/Glib_GameLib_lce.header.bin"),
                File.ReadAllBytes(source + "unpacked/legacy_libraries/Glib_GameLib_lce.bmp"),
                out int width, out int height, resultLayout: true);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                texture.SetPixels32(pixels); texture.Apply();
                File.WriteAllBytes(Output, texture.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
            AssetDatabase.ImportAsset(Output, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(Output);
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false; importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true; importer.SaveAndReimport();
            // gscene17 button descriptor: GameLib.ltn id19 at (372,311).
            var header = File.ReadAllBytes(source + "unpacked/legacy_libraries/Glib_GameLib_ltn.header.bin");
            var bmp = File.ReadAllBytes(source + "unpacked/legacy_libraries/Glib_GameLib_ltn.bmp");
            int record = 4 + 18 * 24, offset = BitConverter.ToInt32(bmp, 10);
            if (BitConverter.ToInt32(header, record) != 19
                || BitConverter.ToInt32(header, record + 4) != 0
                || BitConverter.ToInt32(header, record + 8) != 342
                || BitConverter.ToInt32(header, record + 12) != 46
                || BitConverter.ToInt32(header, record + 16) != 19)
                throw new InvalidDataException("Unexpected native result button.");
            for (int state = 0; state < 3; state++)
            {
                texture = new Texture2D(46, 19, TextureFormat.RGBA32, false);
                try
                {
                    var colors = new Color32[46 * 19];
                    for (int y = 0; y < 19; y++) for (int x = 0; x < 46; x++)
                    {
                        int p = offset + (490 - 342 - y) * 1352 + (state * 46 + x) * 3;
                        colors[(18 - y) * 46 + x] = new Color32(bmp[p + 2], bmp[p + 1], bmp[p], 255);
                    }
                    texture.SetPixels32(colors); texture.Apply();
                    string path = "Assets/_Game/Resources/UI/NativeResultConfirm" + state + ".png";
                    File.WriteAllBytes(path, texture.EncodeToPNG());
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                    var buttonImporter = (TextureImporter)AssetImporter.GetAtPath(path);
                    buttonImporter.textureType = TextureImporterType.Default;
                    buttonImporter.mipmapEnabled = false; buttonImporter.filterMode = FilterMode.Point;
                    buttonImporter.textureCompression = TextureImporterCompression.Uncompressed;
                    buttonImporter.SaveAndReimport();
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
            }
        }
    }
}
