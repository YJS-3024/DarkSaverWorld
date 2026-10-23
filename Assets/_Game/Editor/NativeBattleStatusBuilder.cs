using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class NativeBattleStatusBuilder
    {
        public const string Output = "Assets/_Game/Resources/UI/NativeStatusGauges.png";
        [MenuItem("DarkSaver/UI/Build Native Status Gauges")]
        public static void Build()
        {
            var data = File.ReadAllBytes("분석/Saver_1.2/decoded/all_arc/gameres/etc2/board.bmp");
            if (data.Length < 1078 || BitConverter.ToInt32(data, 18) != 600 || BitConverter.ToInt32(data, 22) != 544
                || BitConverter.ToInt16(data, 28) != 8 || BitConverter.ToInt32(data, 30) != 0)
                throw new InvalidDataException("Expected indexed 600x544 board BMP.");
            int offset = BitConverter.ToInt32(data, 10);
            if (offset < 1078 || offset > data.Length - 600 * 544) throw new InvalidDataException("Truncated board BMP.");
            var texture = new Texture2D(80, 34, TextureFormat.RGBA32, false);
            try
            {
                var pixels = new Color32[80 * 34];
                void Copy(int sx, int sy, int w, int h, int dx, int dy)
                {
                    for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                    {
                        int index = data[offset + (543 - sy - y) * 600 + sx + x], p = 54 + index * 4;
                        byte b = data[p], g = data[p + 1], r = data[p + 2];
                        pixels[(33 - dy - y) * 80 + dx + x] = new Color32(r, g, b, (byte)((r | g | b) == 0 ? 0 : 255));
                    }
                }
                for (int row = 0; row < 3; row++) Copy(2, 370 + row * 12, 80, 9, 0, row * 9);
                // Native status digits: slash at x375, then 0..9 at x380; row 2 at y342.
                Copy(375, 342, 55, 7, 0, 27);
                texture.SetPixels32(pixels); texture.Apply();
                File.WriteAllBytes(Output, texture.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
            AssetDatabase.ImportAsset(Output, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(Output);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 32; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true; importer.textureCompression = TextureImporterCompression.Uncompressed;
            var sprites = new SpriteMetaData[14];
            for (int row = 0; row < 3; row++) sprites[row] = new SpriteMetaData {
                name = "Gauge_" + row, rect = new Rect(0, 25 - row * 9, 80, 9), alignment = 0, pivot = new Vector2(.5f,.5f) };
            for (int i = 0; i < 11; i++) sprites[3+i] = new SpriteMetaData {
                name = "Digit_" + (i-1), rect = new Rect(i * 5, 0, 5, 7), alignment = 0, pivot = new Vector2(.5f,.5f) };
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
#pragma warning disable CS0618
            importer.spritesheet = sprites;
#pragma warning restore CS0618
            importer.SaveAndReimport();
        }
    }
}
