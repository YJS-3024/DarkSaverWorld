using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace DarkSaver.Editor
{
    public static class BattleFeedbackIconBuilder
    {
        public const string Output = "Assets/_Game/Resources/UI/BattleFeedbackIcons.png";
        [MenuItem("DarkSaver/UI/Build Native Damage And Miss Icons")]
        public static void Build()
        {
            if (File.Exists(Output)) return;
            var data = File.ReadAllBytes("분석/Saver_1.2/decoded/all_arc/gameres/etc2/board.bmp");
            if (data.Length < 1078 || data[0] != 'B' || data[1] != 'M'
                || BitConverter.ToInt32(data, 14) != 40 || BitConverter.ToInt32(data, 18) != 600
                || BitConverter.ToInt32(data, 22) != 544 || BitConverter.ToInt16(data, 28) != 8
                || BitConverter.ToInt32(data, 30) != 0) throw new InvalidDataException("Expected 600x544 indexed board BMP.");
            int offset = BitConverter.ToInt32(data, 10);
            if (offset < 1078 || offset > data.Length - 600 * 544) throw new InvalidDataException("Invalid board pixels.");
            var texture = new Texture2D(132, 56, TextureFormat.RGBA32, false);
            try
            {
                var pixels = new Color32[132 * 56];
                void Copy(int sx, int sy, int width, int height, int dx, int dy)
                {
                    for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                    {
                        int index = data[offset + (543 - sy - y) * 600 + sx + x], p = 54 + index * 4;
                        byte b = data[p], g = data[p + 1], r = data[p + 2];
                        pixels[(55 - dy - y) * 132 + dx + x] = new Color32(r, g, b, (byte)((r | g | b) == 0 ? 0 : 255));
                    }
                }
                for (int row = 0; row < 4; row++) Copy(235, 418 + row * 18, 96, 14, 0, row * 14);
                Copy(1, 447, 36, 11, 96, 0);
                texture.SetPixels32(pixels); texture.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(Output)); File.WriteAllBytes(Output, texture.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
            AssetDatabase.ImportAsset(Output, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(Output);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 32; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true; importer.textureCompression = TextureImporterCompression.Uncompressed;
            var frames = new SpriteMetaData[49];
            for (int row = 0; row < 4; row++) for (int glyph = 0; glyph < 12; glyph++)
                frames[row * 12 + glyph] = new SpriteMetaData {
                    name = "Number_" + row + "_" + (glyph + 1), rect = new Rect(glyph * 8, 42 - row * 14, 8, 14),
                    alignment = (int)SpriteAlignment.Center, pivot = new Vector2(.5f, .5f)
                };
            frames[48] = new SpriteMetaData { name = "MISS", rect = new Rect(96, 45, 36, 11),
                alignment = (int)SpriteAlignment.Center, pivot = new Vector2(.5f, .5f) };
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
#pragma warning disable CS0618
            importer.spritesheet = frames;
#pragma warning restore CS0618
            importer.SaveAndReimport();
        }
    }
}
