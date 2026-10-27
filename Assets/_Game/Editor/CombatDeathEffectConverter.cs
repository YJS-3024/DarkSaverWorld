using System;
using System.IO;
using DarkSaver.Combat;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class CombatDeathEffectConverter
    {
        public const string Folder = "Assets/_Game/Art/Converted/Effects/Death";
        public const string CatalogPath = "Assets/_Game/Resources/Effects/CombatDeathEffectCatalog.asset";
        public static string FramePath(int column) => $"{Folder}/DeathEffect_{column}.png";

        [MenuItem("DarkSaver/Convert/Death Effect Frames")]
        public static void ConvertAll()
        {
            var source = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                "분석/Saver_1.2/decoded/all_arc/gameres/etc/Fx.bmp");
            var bytes = File.ReadAllBytes(source);
            if (bytes.Length < 1078 || bytes[0] != 'B' || bytes[1] != 'M'
                || BitConverter.ToInt32(bytes, 14) != 40 || BitConverter.ToInt32(bytes, 18) != 999
                || BitConverter.ToInt32(bytes, 22) != 999 || BitConverter.ToInt16(bytes, 26) != 1
                || BitConverter.ToInt16(bytes, 28) != 8 || BitConverter.ToInt32(bytes, 30) != 0)
                throw new InvalidDataException("Expected 999x999 bottom-up uncompressed indexed Fx BMP.");
            var offset = BitConverter.ToInt32(bytes, 10);
            const int stride = 1000;
            if (offset < 1078 || offset > bytes.Length - stride * 999)
                throw new InvalidDataException("Invalid Fx BMP pixel offset/length.");
            Directory.CreateDirectory(Folder);
            var sprites = new Sprite[7];
            for (var column = 0; column < sprites.Length; column++)
            {
                var pixels = new Color32[31 * 31];
                for (var y = 0; y < 31; y++)
                for (var x = 0; x < 31; x++)
                {
                    var index = bytes[offset + (998 - y) * stride + column * 32 + x];
                    var palette = 54 + index * 4;
                    var b = bytes[palette]; var g = bytes[palette + 1]; var r = bytes[palette + 2];
                    pixels[(30 - y) * 31 + x] = new Color32(r, g, b, (byte)(r == 0 && g == 0 && b == 0 ? 0 : 255));
                }
                var path = FramePath(column);
                if (!File.Exists(path))
                {
                    var texture = new Texture2D(31, 31, TextureFormat.RGBA32, false);
                    try { texture.SetPixels32(pixels); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); }
                    finally { UnityEngine.Object.DestroyImmediate(texture); }
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 32;
                    var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                    settings.spriteAlignment = (int)SpriteAlignment.Custom; settings.spritePivot = new Vector2(0, 1);
                    importer.SetTextureSettings(settings); importer.filterMode = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.isReadable = true;
                    importer.SaveAndReimport();
                }
                var actualTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (actualTexture == null || actualTexture.width != 31 || actualTexture.height != 31 || !actualTexture.isReadable)
                    throw new InvalidDataException("Existing/imported death effect texture is incompatible; not overwritten.");
                var actual = actualTexture.GetPixels32();
                for (var i = 0; i < pixels.Length; i++)
                    if (actual[i].a != pixels[i].a || (pixels[i].a != 0 &&
                        (actual[i].r != pixels[i].r || actual[i].g != pixels[i].g || actual[i].b != pixels[i].b)))
                        throw new InvalidDataException("Death effect pixels differ from source; not overwritten.");
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null || sprite.pixelsPerUnit != 32 || sprite.pivot != new Vector2(0, 31))
                    throw new InvalidDataException("Death effect Sprite import settings differ; not overwritten.");
                sprites[column] = sprite;
            }
            var existing = AssetDatabase.LoadAssetAtPath<CombatDeathEffectCatalog>(CatalogPath);
            if (File.Exists(CatalogPath))
            {
                if (existing == null) throw new InvalidDataException("Existing catalog has an incompatible type; not overwritten.");
                for (var i = 0; i < 7; i++)
                    if (!existing.TryGetEffect(i * 3, out var sprite) || sprite != sprites[i])
                        throw new InvalidDataException("Existing catalog references differ; not overwritten.");
            }
            else
            {
                var catalog = ScriptableObject.CreateInstance<CombatDeathEffectCatalog>();
                if (!catalog.Configure(sprites)) { UnityEngine.Object.DestroyImmediate(catalog); throw new InvalidDataException("Invalid frames."); }
                AssetDatabase.CreateAsset(catalog, CatalogPath); AssetDatabase.SaveAssetIfDirty(catalog);
            }
            Debug.Log("Verified 7 native death effect frames and catalog; source pixels match.");
        }
    }
}
