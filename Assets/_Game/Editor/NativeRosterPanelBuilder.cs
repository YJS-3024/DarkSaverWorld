using System.IO;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class NativeRosterPanelBuilder
    {
        [MenuItem("DarkSaver/UI/Build Native Roster Panels")]
        public static void Build()
        {
            var header = File.ReadAllBytes("분석/Saver_1.2/unpacked/legacy_libraries/Glib_GameLib_lce.header.bin");
            var bitmap = File.ReadAllBytes("분석/Saver_1.2/unpacked/legacy_libraries/Glib_GameLib_lce.bmp");
            for (int rows = 0; rows <= 4; rows++)
            {
                string source = rows == 0 ? "gscene24m" : "gscene" + (23 + rows);
                var scene = File.ReadAllBytes($"분석/Saver_1.2/extracted/Saver_Files/Saver/Glib/{source}.lsc");
                var pixels = NativeStatusPanelBuilder.Compose(scene, header, bitmap, out int width, out int height, rows == 0 ? -1 : rows);
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                string path = $"Assets/_Game/Resources/UI/NativeRoster{(rows == 0 ? "Collapsed" : rows.ToString())}.png";
                try
                {
                    texture.SetPixels32(pixels); texture.Apply();
                    File.WriteAllBytes(path, texture.EncodeToPNG());
                }
                finally { Object.DestroyImmediate(texture); }
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = false; importer.filterMode = FilterMode.Point;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }
    }
}
