using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class BattleIconSheetBuilder
    {
        public const string Output = "Assets/_Game/Resources/UI/BattleIcons.png";

        [MenuItem("DarkSaver/UI/Build Battle Icon Frames")]
        public static void Build()
        {
            // Editor conversion only: no original BMP or analysis path is used by the player.
            var data = File.ReadAllBytes("분석/Saver_1.2/decoded/all_arc/gameres/etc/Micon.bmp");
            int offset = BitConverter.ToInt32(data, 10), width = BitConverter.ToInt32(data, 18);
            int height = BitConverter.ToInt32(data, 22), header = BitConverter.ToInt32(data, 14);
            if (data[0] != 'B' || data[1] != 'M' || width != 640 || height != 640
                || BitConverter.ToInt16(data, 28) != 8 || BitConverter.ToInt32(data, 30) != 0)
                throw new InvalidDataException("Expected the original uncompressed 640x640 indexed Micon sheet.");
            var texture = new Texture2D(640, 320, TextureFormat.RGBA32, false);
            try
            {
                var pixels = new Color32[640 * 320];
                for (int y = 0; y < 320; y++)
                for (int x = 0; x < 640; x++)
                {
                    int index = data[offset + (height - 1 - y) * width + x];
                    int palette = 14 + header + index * 4;
                    byte b = data[palette], g = data[palette + 1], r = data[palette + 2];
                    pixels[(319 - y) * width + x] = new Color32(r, g, b,
                        (byte)((r | g | b) == 0 ? 0 : 255));
                }
                texture.SetPixels32(pixels); texture.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(Output));
                File.WriteAllBytes(Output, texture.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
            AssetDatabase.ImportAsset(Output, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(Output);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var frames = new SpriteMetaData[200];
            for (int frame = 0; frame < 2; frame++)
            for (int row = 0; row < 5; row++)
            for (int column = 0; column < 20; column++)
                frames[frame * 100 + row * 20 + column] = new SpriteMetaData {
                    name = (row == 0 ? "Action_" + column.ToString("00") : "Magic_" + (row - 1) + "_" + column.ToString("00"))
                        + "_Frame_" + frame,
                    rect = new Rect(column * 32, 320 - (frame * 160 + row * 32) - 32, 32, 32),
                    alignment = (int)SpriteAlignment.Center, pivot = new Vector2(.5f, .5f)
                };
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
#pragma warning disable CS0618
            importer.spritesheet = frames;
#pragma warning restore CS0618
            importer.SaveAndReimport();
        }
    }
}
