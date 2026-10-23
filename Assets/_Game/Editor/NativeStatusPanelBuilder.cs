using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class NativeStatusPanelBuilder
    {
        public const string Output = "Assets/_Game/Resources/UI/NativeStatusPanel.png";
        [MenuItem("DarkSaver/UI/Build Native Status Panel")]
        public static void Build()
        {
            var scene = File.ReadAllBytes("분석/Saver_1.2/extracted/Saver_Files/Saver/Glib/gscene1.lsc");
            var header = File.ReadAllBytes("분석/Saver_1.2/unpacked/legacy_libraries/Glib_GameLib_lce.header.bin");
            var bitmap = File.ReadAllBytes("분석/Saver_1.2/unpacked/legacy_libraries/Glib_GameLib_lce.bmp");
            var pixels = Compose(scene, header, bitmap, out int width, out int height);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                texture.SetPixels32(pixels); texture.Apply();
                File.WriteAllBytes(Output, texture.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
            AssetDatabase.ImportAsset(Output, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(Output);
            importer.textureType = TextureImporterType.Default; importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point; importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        }

        // Saver.exe 0x433880 reads id, placement count, then count*(x,y,repeatX,repeatY).
        // 0x433BC0 composites groups in file order; DManage 0x5D10 clips each blit to its surface.
        public static Color32[] Compose(byte[] scene, byte[] header, byte[] bitmap, out int width, out int height, int rosterRows = 0, bool resultLayout = false, bool windowLayout = false)
        {
            int Read(byte[] bytes, int offset)
            {
                if (offset < 0 || offset > bytes.Length - 4) throw new InvalidDataException("Truncated native UI data.");
                return BitConverter.ToInt32(bytes, offset);
            }
            width = Read(scene, 0); height = Read(scene, 4);
            int groups = resultLayout || windowLayout ? Read(scene, 24) : rosterRows == -1 ? 1 : rosterRows == 0 ? 35 : 23;
            if (windowLayout && (width < 1 || width > 640 || height < 1 || height > 480 || groups < 0 || groups > 256))
                throw new InvalidDataException("Unexpected native window layout.");
            if (resultLayout && (width != 434 || height != 344 || groups != 27))
                throw new InvalidDataException("Unexpected native result layout.");
            if (!resultLayout && !windowLayout && (rosterRows < -1 || rosterRows > 4 || width != (rosterRows == -1 ? 33 : 120)
                || height != (rosterRows == -1 ? 22 : rosterRows == 0 ? 213 : 44 + 41 * rosterRows) || Read(scene, 24) != groups)
                )
                throw new InvalidDataException("Unexpected gscene1 layout.");
            if (Read(bitmap, 18) != 200 || Read(bitmap, 22) != 330 || Read(bitmap, 14) != 40
                || BitConverter.ToInt16(bitmap, 28) != 8 || Read(bitmap, 30) != 0)
                throw new InvalidDataException("Unexpected GameLib.lce BMP.");
            int pixelOffset = Read(bitmap, 10);
            if (pixelOffset < 1078 || pixelOffset > bitmap.Length - 200 * 330)
                throw new InvalidDataException("Truncated GameLib pixels.");
            int cellCount = Read(header, 0);
            if (cellCount != 116 || header.Length != 8 + cellCount * 20)
                throw new InvalidDataException("Unexpected cell table.");
            var cells = new Dictionary<int, RectInt>();
            for (int i = 0; i < cellCount; i++)
            {
                int offset = 4 + i * 20;
                var cell = new RectInt(Read(header, offset + 4), Read(header, offset + 8), Read(header, offset + 12), Read(header, offset + 16));
                if (cell.x < 0 || cell.y < 0 || cell.width <= 0 || cell.height <= 0
                    || (long)cell.x + cell.width > 200 || (long)cell.y + cell.height > 330)
                    throw new InvalidDataException("Invalid native cell.");
                cells.Add(Read(header, offset), cell);
            }
            var result = new Color32[width * height];
            int cursor = 28;
            for (int group = 0; group < groups; group++)
            {
                int id = Read(scene, cursor), count = Read(scene, cursor + 4); cursor += 8;
                if (!cells.TryGetValue(id, out var cell) || count < 0 || count > 1000)
                    throw new InvalidDataException("Invalid native image group.");
                for (int placement = 0; placement < count; placement++)
                {
                    int left = Read(scene, cursor), top = Read(scene, cursor + 4);
                    int repeatX = Read(scene, cursor + 8), repeatY = Read(scene, cursor + 12); cursor += 16;
                    if (repeatX < 0 || repeatY < 0 || (long)repeatX * repeatY > 10000)
                        throw new InvalidDataException("Invalid native repetition.");
                    for (int rx = 0; rx < repeatX; rx++) for (int ry = 0; ry < repeatY; ry++)
                    for (int y = 0; y < cell.height; y++) for (int x = 0; x < cell.width; x++)
                    {
                        long dx = (long)left + rx * cell.width + x, dy = (long)top + ry * cell.height + y;
                        if (dx < 0 || dx >= width || dy < 0 || dy >= height) continue;
                        int paletteIndex = bitmap[pixelOffset + (329 - cell.y - y) * 200 + cell.x + x];
                        int p = 54 + paletteIndex * 4;
                        byte r = bitmap[p + 2], g = bitmap[p + 1], b = bitmap[p];
                        // Native background blits are opaque; black becomes the final surface's color key.
                        result[(height - 1 - (int)dy) * width + (int)dx] = new Color32(r, g, b, (byte)((r | g | b) == 0 ? 0 : 255));
                    }
                }
            }
            // Two independent button descriptors follow the static image groups, then an empty final list.
            int buttons = Read(scene, cursor); cursor += 4;
            if (windowLayout ? buttons < 0 || buttons > 128 : buttons != (resultLayout ? 1 : rosterRows == -1 ? 0 : 2)) throw new InvalidDataException("Unexpected native button table.");
            for (int i = 0; i < buttons; i++) { Read(scene, cursor); Read(scene, cursor + 4); Read(scene, cursor + 8); cursor += 12; }
            if (Read(scene, cursor) != 0 || cursor + 4 != scene.Length)
                throw new InvalidDataException("Unexpected trailing gscene1 data.");
            return result;
        }
    }
}
