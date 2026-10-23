using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using DarkSaver.Combat;
using UnityEditor;
using UnityEngine;
namespace DarkSaver.Editor
{
    public static class NativeProjectileEffectBuilder
    {
        const string Folder = "Assets/_Game/Resources/Effects/ProjectilePatterns";
        public const string Output = "Assets/_Game/Resources/Effects/NativeProjectileEffects.asset";
        [MenuItem("DarkSaver/Convert/Native Projectile Magic Effects")]
        public static void Build()
            => BuildCatalog(1, Output);
        [MenuItem("DarkSaver/Convert/Native Lava Effect")]
        public static void BuildLava()
            => BuildCatalog(10, "Assets/_Game/Resources/Effects/NativeLavaEffects.asset");
        [MenuItem("DarkSaver/Convert/Native Linear Projectile Effect 24")]
        public static void BuildLinear()
            => BuildCatalog(24, "Assets/_Game/Resources/Effects/NativeLinearProjectileEffects.asset");
        [MenuItem("DarkSaver/Convert/Native Curved Directional Effects 11")]
        public static void BuildCurvedDirectional()
            => BuildCatalog(11, "Assets/_Game/Resources/Effects/NativeCurvedDirectionalEffects.asset");
        static void BuildCatalog(int family, string output)
        {
            if (File.Exists(output)) return; // Preserve existing edited catalogs.
            if (family == 1) NativeFireballEffectBuilder.Build();
            var magic = MagicCatalogConverter.Parse(File.ReadAllText("분석/Saver_1.2/converted/Magictbl.csv"));
            var bytes = File.ReadAllBytes("분석/Saver_1.2/decoded/all_arc/gameres/etc/Fx.bmp");
            if (bytes.Length < 1078 || bytes[0] != 'B' || bytes[1] != 'M'
                || BitConverter.ToInt32(bytes, 14) != 40 || BitConverter.ToInt32(bytes, 18) != 999
                || BitConverter.ToInt32(bytes, 22) != 999 || BitConverter.ToInt16(bytes, 28) != 8
                || BitConverter.ToInt32(bytes, 30) != 0) throw new InvalidDataException("Invalid native Fx bitmap.");
            var offset = BitConverter.ToInt32(bytes, 10);
            var travelBytes = family == 24 || family == 11 ? NativeThunderEffectBuilder.ReadBitmap("분석/Saver_1.2/decoded/all_arc/gameres/etc2/Fx2.bmp") : bytes;
            int travelOffset = BitConverter.ToInt32(travelBytes, 10);
            if (offset < 1078 || offset > bytes.Length - 1000 * 999) throw new InvalidDataException("Invalid Fx pixel bounds.");
            Directory.CreateDirectory(Folder); var profiles = new List<NativeFireballEffects>();
            var lines = File.ReadAllLines("분석/Saver_1.2/converted/MagicPtn.csv");
            for (int row = 1; row < lines.Length; row++)
            {
                if (string.IsNullOrWhiteSpace(lines[row])) continue;
                var matches = Regex.Matches(lines[row], "\"([^\"]*)\"");
                if (matches.Count != 19) throw new InvalidDataException("Expected 19 quoted pattern fields.");
                // Unknown families may contain non-numeric placeholders. Do not interpret them.
                if (matches[16].Groups[1].Value.Trim() != family.ToString(CultureInfo.InvariantCulture)) continue;
                var n = new int[19];
                for (int i = 0; i < 19; i++) if (i != 1) n[i] = int.Parse(matches[i].Groups[1].Value, CultureInfo.InvariantCulture);
                if (n[16] != family) continue;
                var ids = new List<int>(); foreach (var entry in magic) if (entry.PatternId == n[0]) ids.Add(entry.Id);
                if (ids.Count == 0) continue;
                if (family == 24 || family == 11) n[15] = 8; // Native renderer uses eight 32px direction cells, not fx2ptn.
                if (n[0] == 2) { profiles.Add(AssetDatabase.LoadAssetAtPath<NativeFireballEffects>("Assets/_Game/Resources/Effects/NativeFireballEffects.asset")); continue; }
                var profilePath = Folder + "/Pattern_" + n[0] + ".asset";
                if (File.Exists(profilePath))
                {
                    var existing = AssetDatabase.LoadAssetAtPath<NativeFireballEffects>(profilePath);
                    if (existing == null) throw new InvalidDataException("Existing incompatible effect profile.");
                    profiles.Add(existing); continue;
                }
                var imagePath = Folder + "/Pattern_" + n[0] + ".png";
                if (File.Exists(imagePath)) throw new InvalidDataException("Existing image is preserved: " + imagePath);
                if (n[8] < 1 || n[8] > 64 || n[15] < 1 || n[15] > 64
                    || n[4] < 1 || n[5] < 1 || n[11] < 1 || n[12] < 1) throw new InvalidDataException("Invalid frame dimensions/counts.");
                int width = checked(n[15] * n[11] + n[8] * n[4]), height = Math.Max(n[5], n[12]);
                if (width > 4096 || height > 1024) throw new InvalidDataException("Effect atlas dimensions are unsupported.");
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                var slices = new SpriteMetaData[n[15] + n[8]];
                try
                {
                    var pixels = new Color32[width * height];
                    for (int frame = 0; frame < slices.Length; frame++)
                    {
                        bool travel = frame < n[15]; int index = travel ? frame : frame - n[15];
                        int w = travel ? n[11] : n[4], h = travel ? n[12] : n[5];
                        int sx = travel ? n[9] + index * w : n[2] + index * n[6];
                        int sy = travel ? n[10] : n[3] + index * n[7];
                        if (sx < 0 || sy < 0 || sx + w > 999 || sy + h > 999) throw new InvalidDataException("Native effect frame exceeds source image.");
                        int dx = travel ? frame * n[11] : n[15] * n[11] + index * n[4];
                        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                        {
                            var source = travel ? travelBytes : bytes; int start = travel ? travelOffset : offset;
                            int color = source[start + (998 - sy - y) * 1000 + sx + x], p = 54 + color * 4;
                            byte b = source[p], g = source[p + 1], r = source[p + 2];
                            pixels[(height - 1 - y) * width + dx + x] = new Color32(r, g, b, (byte)((r | g | b) == 0 ? 0 : 255));
                        }
                        slices[frame] = new SpriteMetaData { name = (travel ? "Projectile_" : "Impact_") + index,
                            rect = new Rect(dx, height - h, w, h), alignment = (int)SpriteAlignment.Center, pivot = new Vector2(.5f, .5f) };
                    }
                    texture.SetPixels32(pixels); texture.Apply(); File.WriteAllBytes(imagePath, texture.EncodeToPNG());
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
                AssetDatabase.ImportAsset(imagePath, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(imagePath);
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit = 32; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true; importer.textureCompression = TextureImporterCompression.Uncompressed;
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
#pragma warning disable CS0618
                importer.spritesheet = slices;
#pragma warning restore CS0618
                importer.SaveAndReimport();
                var projectile = new Sprite[n[15]]; var impact = new Sprite[n[8]];
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(imagePath)) if (asset is Sprite sprite)
                {
                    for (int i = 0; i < projectile.Length; i++) if (sprite.name == "Projectile_" + i) projectile[i] = sprite;
                    for (int i = 0; i < impact.Length; i++) if (sprite.name == "Impact_" + i) impact[i] = sprite;
                }
                var profile = ScriptableObject.CreateInstance<NativeFireballEffects>(); profile.Initialize(ids.ToArray(), projectile, impact, family == 10, family == 24, family == 11);
                AssetDatabase.CreateAsset(profile, profilePath); AssetDatabase.SaveAssetIfDirty(profile); profiles.Add(profile);
            }
            var catalog = ScriptableObject.CreateInstance<NativeProjectileEffectsCatalog>(); catalog.Initialize(profiles.ToArray());
            AssetDatabase.CreateAsset(catalog, output); AssetDatabase.SaveAssetIfDirty(catalog);
            Debug.Log("Native projectile effects: " + catalog.PatternCount + " patterns, " + catalog.MagicCount + " magic definitions.");
        }
    }
}
