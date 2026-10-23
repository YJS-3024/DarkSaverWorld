using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DarkSaver.Combat;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class NativeTargetEffectBuilder
    {
        public const string Output = "Assets/_Game/Resources/Effects/NativeTargetEffects.asset";
        const string Image = "Assets/_Game/Resources/Effects/NativeTargetEffects.png";
        [MenuItem("DarkSaver/Convert/Native Target Effects 5 7 9")]
        public static void Build()
            => BuildCatalog(Output, Image, false);
        [MenuItem("DarkSaver/Convert/Native Target Effects 8")]
        public static void BuildShrinking()
            => BuildCatalog("Assets/_Game/Resources/Effects/NativeShrinkingTargetEffects.asset",
                "Assets/_Game/Resources/Effects/NativeShrinkingTargetEffects.png", true);
        [MenuItem("DarkSaver/Convert/Native Target Effects 22")]
        public static void BuildSlow()
            => BuildCatalog("Assets/_Game/Resources/Effects/NativeSlowTargetEffects.asset",
                "Assets/_Game/Resources/Effects/NativeSlowTargetEffects.png", false, true);
        [MenuItem("DarkSaver/Convert/Native Target Effects 21")]
        public static void BuildHeldShrink()
            => BuildCatalog("Assets/_Game/Resources/Effects/NativeHeldShrinkTargetEffects.asset",
                "Assets/_Game/Resources/Effects/NativeHeldShrinkTargetEffects.png", false, false, true);
        [MenuItem("DarkSaver/Convert/Native Delayed Target Effects")]
        public static void BuildDelayed()
            => BuildCatalog("Assets/_Game/Resources/Effects/NativeDelayedTargetEffects.asset",
                "Assets/_Game/Resources/Effects/NativeDelayedTargetEffects.png", false, false, false, true);
        [MenuItem("DarkSaver/Convert/Native Ground Explosion Effects 12")]
        public static void BuildGroundExplosion()
            => BuildCatalog("Assets/_Game/Resources/Effects/NativeGroundExplosionEffects.asset",
                "Assets/_Game/Resources/Effects/NativeGroundExplosionEffects.png", false, false, false, false, true);
        public static void BuildCoordinateTargets()=>BuildCatalog("Assets/_Game/Resources/Effects/NativeCoordinateTargetEffects.asset","Assets/_Game/Resources/Effects/NativeCoordinateTargetEffects.png",false,coordinateTargets:true);
        public static void BuildCoordinateGround()=>BuildCatalog("Assets/_Game/Resources/Effects/NativeCoordinateGroundEffects.asset","Assets/_Game/Resources/Effects/NativeCoordinateGroundEffects.png",false,coordinateGround:true);
        public static void BuildMeteorTargets()=>BuildCatalog("Assets/_Game/Resources/Effects/NativeMeteorTargetEffects.asset","Assets/_Game/Resources/Effects/NativeMeteorTargetEffects.png",false,randomGround:true,meteorTargets:true);
        static void BuildCatalog(string output, string image, bool shrinking, bool doubleCycle = false, bool heldShrink = false, bool delayedImpact = false, bool randomGround = false,bool coordinateTargets=false,bool coordinateGround=false,bool meteorTargets=false)
        {
            if (File.Exists(output)) return;
            if (File.Exists(image)) throw new InvalidDataException("Existing target image is preserved.");
            var magic = MagicCatalogConverter.Parse(File.ReadAllText("분석/Saver_1.2/converted/Magictbl.csv"));
            var rows = new List<(int[] N, int[] Ids, int Y)>(); int width = 0, height = 0;
            foreach (var line in File.ReadAllLines("분석/Saver_1.2/converted/MagicPtn.csv").Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var fields = Regex.Matches(line, "\"([^\"]*)\"");
                if (fields.Count != 19) throw new InvalidDataException("Invalid pattern record.");
                if (!int.TryParse(fields[16].Groups[1].Value, out var family)
                    || (meteorTargets?family!=4:coordinateTargets?family!=13:coordinateGround?family!=4 && family!=13:randomGround ? family != 12 : delayedImpact ? family != 6 && family != 14 && family != 15 && family != 18 && family != 19 && family != 20
                        : heldShrink ? family != 21 : doubleCycle ? family != 22 : shrinking ? family != 8 : family != 5 && family != 7 && family != 9)) continue;
                var n = new int[9]; n[0] = int.Parse(fields[0].Groups[1].Value, CultureInfo.InvariantCulture);
                for (int i = 2; i <= 8; i++) n[i] = int.Parse(fields[i+(coordinateGround?7:0)].Groups[1].Value, CultureInfo.InvariantCulture);
                var ids = magic.Where(x => x.PatternId == n[0]).Select(x => x.Id).ToArray();
                if (ids.Length == 0) continue;
                if (n[4] <= 0 || n[5] <= 0 || n[8] <= 0 || n[8] > 64) throw new InvalidDataException("Invalid target dimensions/count.");
                for (int frame = 0; frame < n[8]; frame++)
                {
                    int x = n[2] + frame * n[6], y = n[3] + frame * n[7];
                    if (x < 0 || y < 0 || x + n[4] > 999 || y + n[5] > 999) throw new InvalidDataException("Target source bounds exceeded.");
                }
                rows.Add((n, ids, height)); width = Math.Max(width, checked(n[4] * n[8])); height = checked(height + n[5]);
            }
            if (rows.Count == 0 || width > 4096 || height > 8192) throw new InvalidDataException("Unsupported target atlas bounds.");
            var source = NativeThunderEffectBuilder.ReadBitmap("분석/Saver_1.2/decoded/all_arc/gameres/" + (doubleCycle || heldShrink || randomGround ? "etc2/Fx2.bmp" : "etc/Fx.bmp"));
            int offset = BitConverter.ToInt32(source, 10); var pixels = new Color32[width * height];
            var slices = new List<SpriteMetaData>();
            foreach (var row in rows)
            {
                var n = row.N;
                for (int frame = 0; frame < n[8]; frame++)
                {
                    for (int y = 0; y < n[5]; y++) for (int x = 0; x < n[4]; x++)
                    {
                        int index = source[offset + (998 - n[3] - frame * n[7] - y) * 1000 + n[2] + frame * n[6] + x];
                        int p = 54 + index * 4; byte b = source[p], g = source[p + 1], r = source[p + 2];
                        pixels[(height - 1 - row.Y - y) * width + frame * n[4] + x] = new Color32(r, g, b, (byte)((r | g | b) == 0 ? 0 : 255));
                    }
                    slices.Add(new SpriteMetaData { name = $"Target_{n[0]}_{frame}", rect = new Rect(frame * n[4], height - row.Y - n[5], n[4], n[5]),
                        alignment = (int)SpriteAlignment.Center, pivot = new Vector2(.5f, .5f) });
                }
            }
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try { texture.SetPixels32(pixels); texture.Apply(); File.WriteAllBytes(image, texture.EncodeToPNG()); }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
            AssetDatabase.ImportAsset(image, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(image);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 32; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 8192;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
#pragma warning disable CS0618
            importer.spritesheet = slices.ToArray();
#pragma warning restore CS0618
            importer.SaveAndReimport();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(image).OfType<Sprite>().ToDictionary(x => x.name);
            var patterns = rows.Select(row => new NativeTargetEffects.Pattern(row.N[0], row.Ids,
                Enumerable.Range(0, row.N[8]).Select(i => sprites[$"Target_{row.N[0]}_{i}"]).ToArray(), shrinking, doubleCycle, heldShrink, delayedImpact, randomGround,coordinateTargets || coordinateGround)).ToArray();
            var catalog = ScriptableObject.CreateInstance<NativeTargetEffects>(); catalog.Initialize(patterns);
            AssetDatabase.CreateAsset(catalog, output); AssetDatabase.SaveAssetIfDirty(catalog);
            Debug.Log($"Native target effects: {catalog.PatternCount} patterns, {catalog.MagicCount} magic definitions.");
        }
    }
}
