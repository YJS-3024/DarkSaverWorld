using System;
using System.IO;
using System.Linq;
using DarkSaver.Combat;
using UnityEditor;
using UnityEngine;
namespace DarkSaver.Editor
{
    public static class NativeTornadoEffectBuilder
    {
        public const string Output = "Assets/_Game/Resources/Effects/NativeTornadoEffects.asset";
        const string Image = "Assets/_Game/Resources/Effects/NativeTornado.png";
        [MenuItem("DarkSaver/Convert/Native Tornado Effects")]
        public static void Build()
        {
            if (File.Exists(Output)) return;
            if (File.Exists(Image)) throw new InvalidDataException("Existing tornado image is preserved.");
            var line = File.ReadAllLines("분석/Saver_1.2/converted/MagicPtn.csv").Single(x => x.StartsWith("\"8\","));
            var fields = System.Text.RegularExpressions.Regex.Matches(line, "\"([^\"]*)\"");
            string[] expected = { "250", "240", "32", "32", "32", "0", "4", "512", "822", "64", "64", "64", "0", "4", "3", "8", "-1" };
            if (fields.Count != 19) throw new InvalidDataException("Invalid tornado pattern.");
            for (int i = 0; i < expected.Length; i++) if (fields[i + 2].Groups[1].Value != expected[i]) throw new InvalidDataException("Tornado pattern changed.");
            var bytes = File.ReadAllBytes("분석/Saver_1.2/decoded/all_arc/gameres/etc/Fx.bmp");
            if (bytes.Length < 1078 || bytes[0] != 'B' || bytes[1] != 'M' || BitConverter.ToInt32(bytes, 14) != 40
                || BitConverter.ToInt32(bytes, 18) != 999 || BitConverter.ToInt32(bytes, 22) != 999
                || BitConverter.ToInt16(bytes, 28) != 8 || BitConverter.ToInt32(bytes, 30) != 0)
                throw new InvalidDataException("Invalid Fx bitmap.");
            int offset = BitConverter.ToInt32(bytes, 10);
            if (offset < 1078 || offset > bytes.Length - 999000) throw new InvalidDataException("Invalid Fx bounds.");
            var texture = new Texture2D(512, 64, TextureFormat.RGBA32, false); var slices = new SpriteMetaData[8];
            try
            {
                var pixels = new Color32[512 * 64];
                for (int i = 0; i < 8; i++)
                {
                    int w = i < 4 ? 64 : 32, sx = i < 4 ? 512 + i * 64 : 250 + (i - 4) * 32, sy = i < 4 ? 822 : 240;
                    for (int y = 0; y < w; y++) for (int x = 0; x < w; x++)
                    {
                        int p = 54 + bytes[offset + (998 - sy - y) * 1000 + sx + x] * 4;
                        byte b = bytes[p], g = bytes[p + 1], r = bytes[p + 2];
                        pixels[(63 - y) * 512 + i * 64 + x] = new Color32(r, g, b, (byte)((r | g | b) == 0 ? 0 : 255));
                    }
                    slices[i] = new SpriteMetaData { name = "Tornado_" + i, rect = new Rect(i * 64, 64 - w, w, w), alignment = (int)SpriteAlignment.Center, pivot = new Vector2(.5f, .5f) };
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
            importer.SaveAndReimport(); var travel = new Sprite[4]; var impact = new Sprite[4];
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(Image)) if (asset is Sprite sprite)
            { int i = int.Parse(sprite.name.Substring(8)); if (i < 4) travel[i] = sprite; else impact[i - 4] = sprite; }
            var ids = MagicCatalogConverter.Parse(File.ReadAllText("분석/Saver_1.2/converted/Magictbl.csv")).Where(x => x.PatternId == 8).Select(x => x.Id).ToArray();
            var profile = ScriptableObject.CreateInstance<NativeFireballEffects>(); profile.Initialize(ids, travel, impact);
            AssetDatabase.CreateAsset(profile, Output); AssetDatabase.SaveAssetIfDirty(profile);
            Debug.Log("Native tornado: " + ids.Length + " magic definitions.");
        }
    }
}
