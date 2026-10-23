using System;
using System.Collections.Generic;
using System.IO;
using DarkSaver.Combat;
using UnityEditor;
using UnityEngine;
namespace DarkSaver.Editor
{
    public static class NativeFireballEffectBuilder
    {
        const string Image = "Assets/_Game/Resources/Effects/NativeFireball.png";
        const string Catalog = "Assets/_Game/Resources/Effects/NativeFireballEffects.asset";
        [MenuItem("DarkSaver/Convert/Native Fireball Effects")]
        public static void Build()
        {
            if (File.Exists(Catalog)) return;
            if (File.Exists(Image)) throw new InvalidDataException("Existing fireball image is preserved; verify it before retrying.");
            var patterns = File.ReadAllLines("분석/Saver_1.2/converted/MagicPtn.csv");
            var expected = "\"2\",\"파이어볼타입\",\"0\",\"0\",\"32\",\"32\",\"32\",\"0\",\"5\",\"336\",\"0\",\"32\",\"32\",\"32\",\"0\",\"3\",\"1\",\"0\",\"-1\"";
            if (Array.IndexOf(patterns, expected) < 0) throw new InvalidDataException("Native pattern 2 differs; do not guess geometry.");
            var ids = new List<int>();
            foreach (var entry in MagicCatalogConverter.Parse(File.ReadAllText("분석/Saver_1.2/converted/Magictbl.csv")))
                if (entry.PatternId == 2) ids.Add(entry.Id);
            var data = File.ReadAllBytes("분석/Saver_1.2/decoded/all_arc/gameres/etc/Fx.bmp");
            if (data.Length < 1078 || data[0] != 'B' || data[1] != 'M'
                || BitConverter.ToInt32(data, 14) != 40 || BitConverter.ToInt32(data, 18) != 999
                || BitConverter.ToInt32(data, 22) != 999 || BitConverter.ToInt16(data, 28) != 8
                || BitConverter.ToInt32(data, 30) != 0) throw new InvalidDataException("Expected native indexed Fx bitmap.");
            var offset = BitConverter.ToInt32(data, 10);
            if (offset < 1078 || offset > data.Length - 999 * 1000) throw new InvalidDataException("Invalid native Fx pixels.");
            var texture = new Texture2D(256, 32, TextureFormat.RGBA32, false);
            try
            {
                var pixels = new Color32[256 * 32];
                for (int frame = 0; frame < 8; frame++) for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
                {
                    var sx = frame < 3 ? 336 + frame * 32 : (frame - 3) * 32;
                    var index = data[offset + (998 - y) * 1000 + sx + x]; var p = 54 + index * 4;
                    byte b = data[p], g = data[p + 1], r = data[p + 2];
                    pixels[(31 - y) * 256 + frame * 32 + x] = new Color32(r, g, b, (byte)((r | g | b) == 0 ? 0 : 255));
                }
                texture.SetPixels32(pixels); texture.Apply(); Directory.CreateDirectory(Path.GetDirectoryName(Image));
                File.WriteAllBytes(Image, texture.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
            AssetDatabase.ImportAsset(Image, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(Image);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 32; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true; importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
            var slices = new SpriteMetaData[8];
            for (int i = 0; i < 8; i++) slices[i] = new SpriteMetaData {
                name = i < 3 ? "Projectile_" + i : "Impact_" + (i - 3), rect = new Rect(i * 32, 0, 32, 32),
                alignment = (int)SpriteAlignment.Center, pivot = new Vector2(.5f, .5f) };
#pragma warning disable CS0618
            importer.spritesheet = slices;
#pragma warning restore CS0618
            importer.SaveAndReimport();
            var travel = new Sprite[3]; var hit = new Sprite[5];
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(Image)) if (asset is Sprite sprite)
            {
                for (int i = 0; i < 3; i++) if (sprite.name == "Projectile_" + i) travel[i] = sprite;
                for (int i = 0; i < 5; i++) if (sprite.name == "Impact_" + i) hit[i] = sprite;
            }
            var catalog = ScriptableObject.CreateInstance<NativeFireballEffects>(); catalog.Initialize(ids.ToArray(), travel, hit);
            AssetDatabase.CreateAsset(catalog, Catalog); AssetDatabase.SaveAssetIfDirty(catalog);
        }
    }
}
