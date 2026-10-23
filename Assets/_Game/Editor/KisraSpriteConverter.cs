using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class KisraSpriteConverter
    {
        public const string Output = "Assets/_Game/Art/Converted/Monsters/Kisra701_Down_1.png";

        [MenuItem("DarkSaver/Convert/Kisra 701 Down Frame")]
        public static void Convert()
        {
            ConvertFrame(Output, 1, 1);
        }

        [MenuItem("DarkSaver/Convert/Kisra 701 All Frames")]
        public static void ConvertAll()
        {
            // Source row labels follow the existing animator's Left/Right naming correction.
            var directions = new[] { "Up", "Down", "Left", "Right" };
            for (var row = 0; row < 4; row++)
            for (var frame = 0; frame < 3; frame++)
                ConvertFrame(FramePath(directions[row], frame), row, frame);
        }

        public static string FramePath(string direction, int frame) =>
            $"Assets/_Game/Art/Converted/Monsters/Kisra701_{direction}_{frame}.png";

        private static void ConvertFrame(string path, int frameRow, int frameColumn)
        {
            var exists = File.Exists(path);
            var root = Directory.GetParent(Application.dataPath).FullName;
            var source = Path.Combine(root, "분석/Saver_1.2/decoded/all_arc/gameres/char3/701R.bmp");
            var bytes = File.ReadAllBytes(source);
            if (bytes.Length < 1078 || bytes[0] != 'B' || bytes[1] != 'M' ||
                BitConverter.ToInt32(bytes, 14) != 40 || BitConverter.ToInt32(bytes, 18) != 192 ||
                BitConverter.ToInt32(bytes, 22) != 256 || BitConverter.ToInt16(bytes, 26) != 1 ||
                BitConverter.ToInt16(bytes, 28) != 8 || BitConverter.ToInt32(bytes, 30) != 0)
                throw new InvalidDataException("Expected 192x256 bottom-up uncompressed indexed BMP.");
            var offset = BitConverter.ToInt32(bytes, 10);
            if (offset < 1078 || offset > bytes.Length - 192 * 256)
                throw new InvalidDataException("Invalid BMP pixel offset/length.");
            var pixels = new Color32[64 * 64];
            for (var row = 0; row < 64; row++)
            for (var column = 0; column < 64; column++)
            {
                var index = bytes[offset + (255 - (frameRow * 64 + row)) * 192 + frameColumn * 64 + column];
                var palette = 54 + index * 4;
                var b = bytes[palette]; var g = bytes[palette + 1]; var r = bytes[palette + 2];
                pixels[(63 - row) * 64 + column] = new Color32(r, g, b,
                    (byte)(r == 0 && g == 0 && b == 0 ? 0 : 255));
            }
            if (!exists)
            {
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            try
            {
                texture.SetPixels32(pixels);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.isReadable = true;
            importer.SaveAndReimport();
            }
            var output = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (output == null || output.width != 64 || output.height != 64 || !output.isReadable)
                throw new InvalidDataException("Existing/imported frame is not a readable 64x64 texture; not overwritten.");
            var actual = output.GetPixels32();
            for (var i = 0; i < pixels.Length; i++)
            {
                // Unity may dilate RGB in fully transparent pixels; visible pixels must be exact.
                if (actual[i].a != pixels[i].a || (pixels[i].a != 0 &&
                    (actual[i].r != pixels[i].r || actual[i].g != pixels[i].g || actual[i].b != pixels[i].b)))
                    throw new InvalidDataException("Imported sprite does not match decoded source pixels.");
            }
            Debug.Log($"Verified Kisra frame: {path}, 64x64, black color key and visible pixels match source.");
        }
    }
}
