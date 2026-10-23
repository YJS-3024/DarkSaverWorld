using System;
using System.IO;
using DarkSaver.Player;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class MercenarySpriteConverter
    {
        public static string FramePath(int sheet, string direction, int frame) =>
            $"Assets/_Game/Art/Converted/Mercenaries/Sheet{sheet}_{direction}_{frame}.png";
        private static readonly string[] Directions = {"Up", "Down", "Left", "Right"};
        public static void ConvertInitialSheets()
        {
            foreach (int sheet in new[] {200, 207})
            {
                var source = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                    "분석/Saver_1.2/decoded/all_arc/gameres/char0", sheet + "r.bmp");
                var bytes = File.ReadAllBytes(source);
                if (bytes.Length < 1078 || bytes[0] != 'B' || bytes[1] != 'M' || BitConverter.ToInt32(bytes, 14) != 40
                    || BitConverter.ToInt32(bytes, 18) != 96 || BitConverter.ToInt32(bytes, 22) != 128
                    || BitConverter.ToInt16(bytes, 28) != 8 || BitConverter.ToInt32(bytes, 30) != 0)
                    throw new InvalidDataException("Expected 96x128 indexed character sheet");
                int offset = BitConverter.ToInt32(bytes, 10);
                if (offset < 1078 || offset > bytes.Length - 96 * 128) throw new InvalidDataException("Invalid character pixels");
                Directory.CreateDirectory("Assets/_Game/Art/Converted/Mercenaries");
                for (int row = 0; row < 4; row++) for (int frame = 0; frame < 3; frame++)
                {
                    var pixels = new Color32[1024];
                    for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
                    {
                        int palette = 54 + bytes[offset + (127 - (row * 32 + y)) * 96 + frame * 32 + x] * 4;
                        byte b = bytes[palette], g = bytes[palette + 1], r = bytes[palette + 2];
                        pixels[(31 - y) * 32 + x] = new Color32(r, g, b, (byte)(r == 0 && g == 0 && b == 0 ? 0 : 255));
                    }
                    string path = FramePath(sheet, Directions[row], frame);
                    if (!File.Exists(path))
                    {
                        var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                        try { texture.SetPixels32(pixels); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); }
                        finally { UnityEngine.Object.DestroyImmediate(texture); }
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                        importer.spritePixelsPerUnit = 32; importer.filterMode = FilterMode.Point;
                        importer.textureCompression = TextureImporterCompression.Uncompressed;
                        importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.isReadable = true; importer.SaveAndReimport();
                    }
                    var actual = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (actual == null || actual.width != 32 || actual.height != 32 || !actual.isReadable) throw new InvalidDataException("Invalid existing mercenary frame");
                    var shown = actual.GetPixels32();
                    for (int i = 0; i < pixels.Length; i++)
                        if (shown[i].a != pixels[i].a || pixels[i].a != 0 && (shown[i].r != pixels[i].r || shown[i].g != pixels[i].g || shown[i].b != pixels[i].b))
                            throw new InvalidDataException("Mercenary frame differs from source; not overwritten");
                }
            }
        }
        public static void ConfigureTemplate(int sheet, DirectionalSpriteAnimator target)
        {
            Sprite[] Frames(string direction) {
                var frames = new Sprite[3]; for (int i = 0; i < 3; i++) frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(FramePath(sheet, direction, i)); return frames;
            }
            target.Configure(Frames("Up"), Frames("Down"), Frames("Left"), Frames("Right"));
            if (!target.HasCompleteDirections) throw new InvalidDataException("Incomplete mercenary directions");
        }

        public static void InstallTemplates(GameObject owner)
        {
            var session = owner.GetComponent<DarkSaver.Combat.BattleTransitionController>();
            var catalog = AssetDatabase.LoadAssetAtPath<DarkSaver.Jobs.JobCatalog>(JobCatalogConverter.MercenaryOutput);
            var serialized = new SerializedObject(session); var bindings = serialized.FindProperty("persistedFormationArtwork");
            foreach (int jobId in new[] {200, 201, 202, 228, 229, 230})
            {
                bool exists = false;
                for (int i = 0; i < bindings.arraySize; i++) if (bindings.GetArrayElementAtIndex(i).FindPropertyRelative("jobId").intValue == jobId) exists = true;
                if (exists) continue; // Keep saved user artwork bindings.
                if (catalog == null || !catalog.TryGet(jobId, out var job) || job.SpriteId != 200 && job.SpriteId != 207)
                    throw new InvalidDataException("Unexpected native mercenary artwork mapping");
                int sheet = (int)job.SpriteId; string name = "MercenaryArtwork" + sheet;
                var child = owner.transform.Find(name);
                if (child == null)
                {
                    var go = new GameObject(name, typeof(SpriteRenderer), typeof(DirectionalSpriteAnimator));
                    go.transform.SetParent(owner.transform, false); child = go.transform;
                    ConfigureTemplate(sheet, go.GetComponent<DirectionalSpriteAnimator>());
                    go.SetActive(false);
                }
                int index = bindings.arraySize++; var row = bindings.GetArrayElementAtIndex(index);
                row.FindPropertyRelative("jobId").intValue = jobId;
                row.FindPropertyRelative("template").objectReferenceValue = child.GetComponent<DirectionalSpriteAnimator>();
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
