using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class NativeStatusFontBuilder
    {
        public const string Output = "Assets/_Game/Resources/UI/NativeStatusFont.png";
        public const string WorldOutput = "Assets/_Game/Resources/UI/NativeWorldNameFont.png";
        [MenuItem("DarkSaver/UI/Build Native Status Font")]
        public static void Build()
        {
            BuildAtlas(Output, new Color32(8,8,8,255));
        }

        [MenuItem("DarkSaver/UI/Build Native World Name Font")]
        public static void BuildWorldNames()
        {
            // Same analyzed dark14 glyph masks. White allows the UI to tint names;
            // world-name color/placement remain Unity presentation choices.
            BuildAtlas(WorldOutput, new Color32(255,255,255,255));
        }

        private static void BuildAtlas(string output, Color32 foreground)
        {
            var bytes = File.ReadAllBytes("분석/Saver_1.2/extracted/Saver_Files/Saver/dark14.mxf");
            const int count = 2592, columns = 64, size = 14, width = columns * size, height = 41 * size;
            if (bytes.Length != count * 32) throw new InvalidDataException("Unexpected dark14 font size.");
            var encoding = Encoding.GetEncoding(949, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var frames = new SpriteMetaData[count];
            try
            {
                var pixels = new Color32[width * height];
                for (int glyph = 0; glyph < count; glyph++)
                {
                    int left = glyph % columns * size, top = glyph / columns * size;
                    for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
                    {
                        // 0x4403F0: MSB first, ZERO bits paint foreground; rows consume two bytes.
                        bool ink = (bytes[glyph * 32 + y * 2 + x / 8] & (128 >> (x % 8))) == 0;
                        pixels[(height - 1 - top - y) * width + left + x] = new Color32(foreground.r,foreground.g,foreground.b,(byte)(ink ? 255 : 0));
                    }
                    string name = "Unused_" + glyph;
                    if (glyph < 96) name = "U_" + (glyph + 32).ToString("X4");
                    else
                    {
                        int high = glyph < 192 ? 0xA4 : 0xB0 + (glyph - 192) / 96;
                        int low = 0xA0 + (glyph < 192 ? glyph - 96 : (glyph - 192) % 96);
                        // Native rows have 96 positions; A0/FF holes are not Unicode characters.
                        if (low >= 0xA1 && low <= 0xFE)
                        {
                            var text = encoding.GetString(new[] { (byte)high, (byte)low });
                            if (text.Length == 1) name = "U_" + ((int)text[0]).ToString("X4");
                        }
                    }
                    frames[glyph] = new SpriteMetaData { name = name,
                        rect = new Rect(left, height - top - size, size, size), alignment = 0, pivot = new Vector2(.5f,.5f) };
                }
                texture.SetPixels32(pixels); texture.Apply(); File.WriteAllBytes(output, texture.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
            AssetDatabase.ImportAsset(output, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(output);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 32; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true; importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
#pragma warning disable CS0618
            importer.spritesheet = frames;
#pragma warning restore CS0618
            importer.SaveAndReimport();
        }
    }
}
