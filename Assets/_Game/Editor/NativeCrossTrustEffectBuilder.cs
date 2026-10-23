using System;
using System.IO;
using System.Linq;
using DarkSaver.Combat;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class NativeCrossTrustEffectBuilder
    {
        const string Asset = "Assets/_Game/Resources/Effects/NativeCrossTrustEffects.asset";
        const string Image = "Assets/_Game/Resources/Effects/NativeCrossTrustEffects.png";
        [MenuItem("DarkSaver/Convert/Native Cross Trust Caster")]
        public static void Build()
        {
            if (File.Exists(Asset)) return;
            if (File.Exists(Image)) throw new InvalidDataException("Existing cross-trust image is preserved.");
            var entries = MagicCatalogConverter.Parse(File.ReadAllText("분석/Saver_1.2/converted/Magictbl.csv"))
                .Where(x => x.PatternId == 42).ToArray();
            if (entries.Length == 0 || entries.Any(x => x.Area <= 0 || x.Area > 200)) throw new InvalidDataException("Invalid cross-trust visual lifetime.");
            var bytes = NativeThunderEffectBuilder.ReadBitmap("분석/Saver_1.2/decoded/all_arc/gameres/etc/Fx.bmp");
            int offset = BitConverter.ToInt32(bytes, 10);
            // Explicit source rectangles constructed on the native stack at 10007FF0.
            var rects = new[] { new RectInt(371,311,27,39), new RectInt(251,315,28,44), new RectInt(280,314,27,60) };
            var pixels = new Color32[82 * 60]; var slices = new SpriteMetaData[3]; int dx = 0;
            for (int i = 0; i < rects.Length; i++)
            {
                var r = rects[i];
                for (int y = 0; y < r.height; y++) for (int x = 0; x < r.width; x++)
                {
                    int color = bytes[offset + (998 - r.y - y) * 1000 + r.x + x], p = 54 + color * 4;
                    byte b = bytes[p], g = bytes[p+1], red = bytes[p+2];
                    pixels[(59-y)*82+dx+x] = new Color32(red,g,b,(byte)((red|g|b)==0 ? 0 : 255));
                }
                slices[i] = new SpriteMetaData { name = "CrossTrust_"+i, rect = new Rect(dx,60-r.height,r.width,r.height), alignment = (int)SpriteAlignment.Center, pivot = new Vector2(.5f,.5f) };
                dx += r.width;
            }
            var texture = new Texture2D(82,60,TextureFormat.RGBA32,false);
            try { texture.SetPixels32(pixels); texture.Apply(); File.WriteAllBytes(Image,texture.EncodeToPNG()); }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
            AssetDatabase.ImportAsset(Image,ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(Image);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 32; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true; importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
#pragma warning disable CS0618
            importer.spritesheet = slices;
#pragma warning restore CS0618
            importer.SaveAndReimport();
            var frames = AssetDatabase.LoadAllAssetsAtPath(Image).OfType<Sprite>().OrderBy(x => x.name).ToArray();
            var asset = ScriptableObject.CreateInstance<NativeCrossTrustEffects>();
            asset.Initialize(entries.Select(x => x.Id).ToArray(),entries.Select(x => checked((int)x.Area*5)).ToArray(),frames);
            AssetDatabase.CreateAsset(asset,Asset); AssetDatabase.SaveAssetIfDirty(asset);
        }
    }
}
