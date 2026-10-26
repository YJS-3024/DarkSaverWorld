using System;
using System.Collections.Generic;
using System.IO;
using DarkSaver.Jobs;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class ClassArtworkConverter
    {
        public const string Output = "Assets/_Game/Runtime/Jobs/ClassArtworkCatalog.asset";
        public const string Folder = "Assets/_Game/Art/Converted/Classes";
        public static string SheetPath(int sheet) => Folder + "/Class" + sheet + ".png";
        public static ClassArtworkCatalog Convert() => Convert(0);
        public static ClassArtworkCatalog Convert(int colorMode)
        {
            if (colorMode != 0 && colorMode != 2 && colorMode != 3) throw new ArgumentOutOfRangeException(nameof(colorMode));
            string output = colorMode == 0 ? Output : Output.Replace(".asset", "_Color" + colorMode + ".asset");
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("Editor must be idle");
            var existing = AssetDatabase.LoadAssetAtPath<ClassArtworkCatalog>(output);
            if (existing != null) return existing; // Never overwrite an authored catalog.
            var jobs = AssetDatabase.LoadAssetAtPath<JobCatalog>(JobCatalogConverter.Output);
            if (jobs == null) throw new InvalidOperationException("Native player jobs missing");
            Directory.CreateDirectory(Folder);
            var sheets = new Dictionary<int, Texture2D>();
            var entries = new List<ClassArtworkCatalog.Entry>();
            var pending = new List<Sprite>();
            var catalog = ScriptableObject.CreateInstance<ClassArtworkCatalog>();
            try {
                for (int index = 0; index < jobs.Count; index++)
                {
                    var job = jobs.GetAt(index); int sheet = checked((int)job.SpriteId);
                    string source = null;
                    for (int archive = 0; archive <= 6; archive++) {
                        var candidate = "분석/Saver_Late/decoded_art/gameres/char" + archive + ".arc/" + sheet + "r.bmp";
                        if (File.Exists(candidate)) { if (source != null) throw new InvalidDataException("Ambiguous class sheet " + sheet); source = candidate; }
                    }
                    if (source == null) { Debug.Log("Class artwork unavailable; no fallback for job " + job.Id); continue; }
                    if (!sheets.TryGetValue(sheet, out var texture))
                    {
                        var bytes = File.ReadAllBytes(source);
                        if (bytes.Length < 1078 || bytes[0] != 'B' || bytes[1] != 'M' || BitConverter.ToInt32(bytes, 14) != 40
                            || BitConverter.ToInt32(bytes, 18) != 96 || (BitConverter.ToInt32(bytes, 22) != 192 && BitConverter.ToInt32(bytes, 22) != 128)
                            || BitConverter.ToInt16(bytes, 28) != 8 || BitConverter.ToInt32(bytes, 30) != 0)
                            throw new InvalidDataException("Expected native 96x192/128 indexed class sheet: " + source);
                        int height = BitConverter.ToInt32(bytes, 22);
                        int offset = BitConverter.ToInt32(bytes, 10);
                        if (offset < 58 || offset > 1078 || (offset - 54) % 4 != 0 || offset > bytes.Length - 96 * height)
                            throw new InvalidDataException("Invalid class pixels: " + source);
                        int paletteCount = (offset - 54) / 4;
                        if(sheet==170 && colorMode==3 && paletteCount<224)
                        { Debug.LogWarning("Native class170 allied-color palette is incomplete; no guessed colors generated."); continue; }
                        var pixels = new Color32[96 * height];
                        for (int y = 0; y < height; y++) for (int x = 0; x < 96; x++) {
                            int colorIndex = bytes[offset + y * 96 + x];
                            colorIndex = RemapColorIndex(colorIndex, colorMode);
                            if (colorIndex >= paletteCount) throw new InvalidDataException("Class color index exceeds palette: " + source);
                            int palette = 54 + colorIndex * 4;
                            byte b = bytes[palette], g = bytes[palette + 1], r = bytes[palette + 2];
                            pixels[y * 96 + x] = new Color32(r, g, b, (byte)(r == 0 && g == 0 && b == 0 ? 0 : 255));
                        }
                        string path = colorMode == 0 ? SheetPath(sheet) : SheetPath(sheet).Replace(".png", "_Color" + colorMode + ".png");
                        if (!File.Exists(path)) {
                            var temporary = new Texture2D(96, height, TextureFormat.RGBA32, false);
                            try { temporary.SetPixels32(pixels); temporary.Apply(); File.WriteAllBytes(path, temporary.EncodeToPNG()); }
                            finally { UnityEngine.Object.DestroyImmediate(temporary); }
                            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                            importer.textureType = TextureImporterType.Default; importer.filterMode = FilterMode.Point;
                            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.mipmapEnabled = false;
                            importer.alphaIsTransparency = true; importer.isReadable = true; importer.SaveAndReimport();
                        }
                        texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                        if (texture == null || texture.width != 96 || texture.height != height || !texture.isReadable)
                            throw new InvalidDataException("Invalid existing class texture: " + path);
                        var actual = texture.GetPixels32();
                        for (int i = 0; i < pixels.Length; i++)
                            if (actual[i].a != pixels[i].a || pixels[i].a != 0 && (actual[i].r != pixels[i].r || actual[i].g != pixels[i].g || actual[i].b != pixels[i].b))
                                throw new InvalidDataException("Existing class pixels differ; not overwritten: " + path);
                        sheets.Add(sheet, texture);
                    }
                    var frames = new Sprite[12];
                    for (int row = 0; row < 4; row++) for (int frame = 0; frame < 3; frame++) {
                        var sprite = Sprite.Create(texture, new Rect(frame * 32, texture.height - (row + 1) * 32, 32, 32), new Vector2(.5f, .1f), 32, 0, SpriteMeshType.FullRect);
                        sprite.name = "Class" + job.Id + "_" + new[] {"Up", "Down", "Left", "Right"}[row] + "_" + frame;
                        frames[row * 3 + frame] = sprite; pending.Add(sprite);
                    }
                    entries.Add(new ClassArtworkCatalog.Entry(job.Id, frames));
                }
                catalog.Initialize(entries.ToArray()); AssetDatabase.CreateAsset(catalog, output);
                foreach (var sprite in pending) AssetDatabase.AddObjectToAsset(sprite, catalog);
                EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssetIfDirty(catalog);
                Debug.Log("Native class artwork converted: " + entries.Count + " classes; source files are editor-only");
                return catalog;
            } catch {
                if (!AssetDatabase.Contains(catalog)) { foreach (var sprite in pending) UnityEngine.Object.DestroyImmediate(sprite); UnityEngine.Object.DestroyImmediate(catalog); }
                throw;
            }
        }
        public static int RemapColorIndex(int index, int colorMode)
        {
            if (index < 0 || index > 255 || colorMode != 0 && colorMode != 2 && colorMode != 3)
                throw new ArgumentOutOfRangeException();
            return index >= 0xA0 && index <= 0xAF ? index + colorMode * 16 : index;
        }

        [MenuItem("DarkSaver/Convert/Class Relation Colors")]
        public static void ConvertRelationColors()
        {
            var normal = Convert(0);
            var other = Convert(2);
            var ally = Convert(3);
            normal.ConfigureRelationColors(other, ally);
            EditorUtility.SetDirty(normal);
            AssetDatabase.SaveAssetIfDirty(normal);
        }
    }
}
