using System;
using System.Collections.Generic;
using System.IO;
using DarkSaver.Combat;
using UnityEditor;
using UnityEngine;
namespace DarkSaver.Editor
{
    public static class NativeThunderEffectBuilder
    {
        public const string Output = "Assets/_Game/Resources/Effects/NativeThunderEffectsV2.asset";
        const string Image = "Assets/_Game/Resources/Effects/NativeThunderV2.png";
        [MenuItem("DarkSaver/Convert/Native Thunder Effects")]
        public static void Build()
        {
            if (File.Exists(Output)) return;
            if (File.Exists(Image)) throw new InvalidDataException("Existing thunder image is preserved.");
            var patterns = File.ReadAllText("분석/Saver_1.2/converted/MagicPtn.csv");
            foreach (int id in new[] { 5, 6, 29 })
            {
                var match = System.Text.RegularExpressions.Regex.Match(patterns, "(?m)^\\\"" + id + "\\\",[^\\r\\n]*");
                var values = System.Text.RegularExpressions.Regex.Matches(match.Value, "\"([^\"]*)\"");
                string[] expected = { "250", "208", "32", "32", "32", "0", "4", "0", "208", "50", "350", "50", "0", "5", "2", "5", "-1" };
                if (values.Count != 19) throw new InvalidDataException("Thunder pattern is missing.");
                for (int i = 0; i < expected.Length; i++) if (values[i + 2].Groups[1].Value != expected[i]) throw new InvalidDataException("Thunder pattern changed.");
            }
            var fx = ReadBitmap("분석/Saver_1.2/decoded/all_arc/gameres/etc/Fx.bmp");
            var texture = new Texture2D(14 * 64, 350, TextureFormat.RGBA32, false);
            var slices = new SpriteMetaData[14]; var pixels = new Color32[14 * 64 * 350];
            try
            {
                for (int i = 0; i < 14; i++)
                {
                    int w = i < 5 ? 50 : i < 9 ? 32 : 40, h = i < 5 ? 350 : w;
                    int sx = i < 5 ? i * 50 : i < 9 ? 250 + (i - 5) * 32 : (i - 9) * 40;
                    int sy = i < 9 ? 208 : 128;
                    var bytes = fx; int offset = BitConverter.ToInt32(bytes, 10);
                    for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                    {
                        int color = bytes[offset + (998 - sy - y) * 1000 + sx + x], p = 54 + color * 4;
                        byte b = bytes[p], g = bytes[p + 1], r = bytes[p + 2];
                        pixels[(349 - y) * 896 + i * 64 + x] = new Color32(r, g, b, (byte)((r | g | b) == 0 ? 0 : 255));
                    }
                    slices[i] = new SpriteMetaData { name = "Thunder_" + i, rect = new Rect(i * 64, 350 - h, w, h), alignment = (int)SpriteAlignment.Center, pivot = new Vector2(.5f, .5f) };
                }
                texture.SetPixels32(pixels); texture.Apply(); File.WriteAllBytes(Image, texture.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
            AssetDatabase.ImportAsset(Image, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(Image);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 32; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true; importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
#pragma warning disable CS0618
            importer.spritesheet = slices;
#pragma warning restore CS0618
            importer.SaveAndReimport(); var sprites = new Sprite[14];
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(Image)) if (asset is Sprite sprite) sprites[int.Parse(sprite.name.Substring(8))] = sprite;
            var ids = new List<int>(); foreach (var entry in MagicCatalogConverter.Parse(File.ReadAllText("분석/Saver_1.2/converted/Magictbl.csv")))
                if (entry.PatternId == 5 || entry.PatternId == 6 || entry.PatternId == 29) ids.Add(entry.Id);
            var profile = ScriptableObject.CreateInstance<NativeThunderEffects>(); profile.Initialize(ids.ToArray(), sprites);
            AssetDatabase.CreateAsset(profile, Output); AssetDatabase.SaveAssetIfDirty(profile);
            Debug.Log("Native thunder: " + ids.Count + " magic definitions.");
        }
        internal static byte[] ReadBitmap(string path)
        {
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length < 1078 || bytes[0] != 'B' || bytes[1] != 'M' || BitConverter.ToInt32(bytes, 14) != 40
                || BitConverter.ToInt32(bytes, 18) != 999 || BitConverter.ToInt32(bytes, 22) != 999
                || BitConverter.ToInt16(bytes, 28) != 8 || BitConverter.ToInt32(bytes, 30) != 0
                || BitConverter.ToInt32(bytes, 10) < 1078 || BitConverter.ToInt32(bytes, 10) > bytes.Length - 999000)
                throw new InvalidDataException("Invalid thunder source bitmap.");
            return bytes;
        }
    }
}
