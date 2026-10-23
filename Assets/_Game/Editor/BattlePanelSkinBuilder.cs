using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class BattlePanelSkinBuilder
    {
        [MenuItem("DarkSaver/UI/Build Battle Panel Skin")]
        public static void Build()
        {
            var data = File.ReadAllBytes("분석/Saver_1.2/unpacked/legacy_libraries/Glib_GameLib_lce.bmp");
            if (BitConverter.ToInt32(data, 18) != 200 || BitConverter.ToInt32(data, 22) != 330
                || BitConverter.ToInt16(data, 28) != 8 || BitConverter.ToInt32(data, 30) != 0)
                throw new InvalidDataException("Unexpected GameLib.lce bitmap layout.");
            Export(data, "BattlePaper", 0, 0, 40, 32, 240, 256, false);
            // Cell 3 includes the complete border; a 32px crop discards its right/bottom edges.
            var frame = ReadNativeCell(3, 200, 330);
            Export(data, "BattlePortraitFrame", frame.x, frame.y, frame.width, frame.height,
                frame.width, frame.height, true);
        }

        public static RectInt ReadNativeCell(int id, int imageWidth, int imageHeight)
        {
            var header = File.ReadAllBytes("분석/Saver_1.2/unpacked/legacy_libraries/Glib_GameLib_lce.header.bin");
            if (header.Length < 8) throw new InvalidDataException("Truncated GameLib.lce header.");
            int count = BitConverter.ToInt32(header, 0);
            // Count followed by (id, x, y, width, height) int32 records and a 4-byte trailer.
            // Trailer semantics are not needed for cropping and must not be guessed.
            if (count <= 0 || (long)count * 20 + 8 != header.Length)
                throw new InvalidDataException("Unexpected GameLib.lce cell table.");
            RectInt result = default;
            bool found = false;
            var ids = new System.Collections.Generic.HashSet<int>();
            for (int i = 0; i < count; i++)
            {
                int offset = 4 + i * 20;
                int cellId = BitConverter.ToInt32(header, offset);
                var cell = new RectInt(BitConverter.ToInt32(header, offset + 4),
                    BitConverter.ToInt32(header, offset + 8), BitConverter.ToInt32(header, offset + 12),
                    BitConverter.ToInt32(header, offset + 16));
                if (!ids.Add(cellId) || cell.x < 0 || cell.y < 0 || cell.width <= 0 || cell.height <= 0
                    || (long)cell.x + cell.width > imageWidth || (long)cell.y + cell.height > imageHeight)
                    throw new InvalidDataException("Invalid GameLib.lce cell " + cellId);
                if (cellId == id) { result = cell; found = true; }
            }
            if (!found) throw new InvalidDataException("Missing GameLib.lce cell " + id);
            return result;
        }
        private static void Export(byte[] data, string name, int left, int top, int tileWidth, int tileHeight,
            int width, int height, bool sprite)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                int offset = BitConverter.ToInt32(data, 10), paletteStart = 14 + BitConverter.ToInt32(data, 14);
                var colors = new Color32[width * height];
                for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int palette = paletteStart + data[offset + (329 - top - y % tileHeight) * 200 + left + x % tileWidth] * 4;
                    colors[(height - 1 - y) * width + x] = new Color32(data[palette + 2], data[palette + 1], data[palette], 255);
                }
                texture.SetPixels32(colors); texture.Apply();
                string path = "Assets/_Game/Resources/UI/" + name + ".png";
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, texture.EncodeToPNG());
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 32;
                importer.mipmapEnabled = false; importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }
    }
}
