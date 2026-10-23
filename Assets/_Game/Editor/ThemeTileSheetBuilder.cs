using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    /// <summary>Lossless sheets grouped by original resource bundle and image role.</summary>
    public static class ThemeTileSheetBuilder
    {
        private const string Source = "Assets/_Game/Art/Converted/RestoredMaps/Sprites";
        private const string Destination = "Assets/_Game/Art/Converted/RestoredMaps/ThemeSheets";
        private const int Width = 2048;
        private const int Padding = 2;

        [MenuItem("DarkSaver/Maps/Build Theme Tile Sheets")]
        public static void Build()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stop play mode and wait for compilation first.");
            Directory.CreateDirectory(Destination);
            var groups = Directory.GetFiles(Source, "*.png")
                .OrderBy(path => path, StringComparer.Ordinal)
                .GroupBy(path => string.Join("_", Path.GetFileNameWithoutExtension(path).Split('_').Take(2)));
            var sheets = new Dictionary<string, SpriteMetaData[]>();
            foreach (var group in groups)
            {
                var textures = new List<Texture2D>();
                try
                {
                    var entries = new List<SpriteMetaData>();
                    int x = Padding, y = Padding, rowHeight = 0;
                    foreach (var path in group)
                    {
                        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        textures.Add(texture);
                        if (!texture.LoadImage(File.ReadAllBytes(path), false))
                            throw new InvalidDataException(path);
                        if (texture.width + Padding * 2 > Width)
                            throw new InvalidDataException("Image exceeds sheet width: " + path);
                        if (x + texture.width + Padding > Width)
                        {
                            x = Padding;
                            y += rowHeight + Padding;
                            rowHeight = 0;
                        }
                        bool secondary = group.Key.EndsWith("_secondary", StringComparison.Ordinal);
                        entries.Add(new SpriteMetaData
                        {
                            name = Path.GetFileNameWithoutExtension(path),
                            rect = new Rect(x, y, texture.width, texture.height),
                            alignment = secondary ? 9 : 0,
                            pivot = secondary ? new Vector2(0, 1) : new Vector2(.5f, .5f)
                        });
                        x += texture.width + Padding;
                        rowHeight = Math.Max(rowHeight, texture.height);
                    }
                    int height = Mathf.NextPowerOfTwo(y + rowHeight + Padding);
                    if (height > 8192)
                        throw new InvalidDataException("Sheet exceeds maximum height: " + group.Key);
                    var sheet = new Texture2D(Width, height, TextureFormat.RGBA32, false);
                    try
                    {
                        sheet.SetPixels32(new Color32[Width * height]);
                        for (int i = 0; i < textures.Count; i++)
                        {
                            var rect = entries[i].rect;
                            sheet.SetPixels32((int)rect.x, (int)rect.y, (int)rect.width,
                                (int)rect.height, textures[i].GetPixels32());
                        }
                        sheet.Apply();
                        string output = Destination + "/" + group.Key + ".png";
                        File.WriteAllBytes(output, sheet.EncodeToPNG());
                        sheets.Add(output, entries.ToArray());
                    }
                    finally { UnityEngine.Object.DestroyImmediate(sheet); }
                }
                finally
                {
                    foreach (var texture in textures) UnityEngine.Object.DestroyImmediate(texture);
                }
            }
            AssetDatabase.Refresh();
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var entry in sheets)
                {
                    var importer = (TextureImporter)AssetImporter.GetAtPath(entry.Key);
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Multiple;
                    importer.spritePixelsPerUnit = 32;
                    importer.filterMode = FilterMode.Point;
                    importer.mipmapEnabled = false;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.maxTextureSize = 8192;
                    importer.alphaIsTransparency = false;
                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    importer.SetTextureSettings(settings);
#pragma warning disable CS0618 // Compatibility with the project's existing TextureImporter pipeline.
                    importer.spritesheet = entry.Value;
#pragma warning restore CS0618
                    importer.SaveAndReimport();
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
            Debug.Log($"Theme sheets created: {sheets.Count}, sprites: {sheets.Values.Sum(value => value.Length)}. Individual source images retained.");
        }
    }
}
