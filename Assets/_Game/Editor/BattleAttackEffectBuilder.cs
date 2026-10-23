using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class BattleAttackEffectBuilder
    {
        public const string Output = "Assets/_Game/Resources/Effects/BattleAttackFx2.png";
        [MenuItem("DarkSaver/Convert/Native Attack Fx2 Frames")]
        public static void Build()
        {
            BuildFrames(false);
        }

        // Explicit repair only; normal conversion still preserves existing artwork.
        public static void Rebuild() => BuildFrames(true);

        private static void BuildFrames(bool overwrite)
        {
            if (!overwrite && File.Exists(Output)) return;
            var bytes = File.ReadAllBytes("분석/Saver_1.2/decoded/all_arc/gameres/etc2/Fx2.bmp");
            if (bytes.Length < 1078 || bytes[0] != 'B' || bytes[1] != 'M'
                || BitConverter.ToInt32(bytes, 14) != 40 || BitConverter.ToInt32(bytes, 18) != 999
                || BitConverter.ToInt32(bytes, 22) != 999 || BitConverter.ToInt16(bytes, 28) != 8
                || BitConverter.ToInt32(bytes, 30) != 0) throw new InvalidDataException("Expected indexed bottom-up Fx2 BMP.");
            var offset = BitConverter.ToInt32(bytes, 10);
            if (offset < 1078 || offset > bytes.Length - 999 * 1000) throw new InvalidDataException("Invalid Fx2 pixel data.");
            var texture = new Texture2D(128, 32, TextureFormat.RGBA32, false);
            try
            {
                var pixels = new Color32[128 * 32];
                for (var frame = 0; frame < 4; frame++)
                for (var y = 0; y < 32; y++)
                for (var x = 0; x < 32; x++)
                {
                    // SetRect(left=frame*32, top=32, right=(frame+1)*32, bottom=64).
                    // x86 arguments are pushed right-to-left: these are horizontal frames.
                    var index = bytes[offset + (998 - (32 + y)) * 1000 + frame * 32 + x];
                    var palette = 54 + index * 4;
                    byte b = bytes[palette], g = bytes[palette + 1], r = bytes[palette + 2];
                    pixels[(31 - y) * 128 + frame * 32 + x] = new Color32(r, g, b, (byte)((r | g | b) == 0 ? 0 : 255));
                }
                texture.SetPixels32(pixels); texture.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(Output)); File.WriteAllBytes(Output, texture.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
            AssetDatabase.ImportAsset(Output, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(Output);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 32; importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var frames = new SpriteMetaData[4];
            for (int i = 0; i < 4; i++) frames[i] = new SpriteMetaData {
                name = "NativeAttack_" + i, rect = new Rect(i * 32, 0, 32, 32),
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
