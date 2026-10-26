using System;
using System.IO;
using DarkSaver.Items;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class ItemIconSheetBuilder
    {
        public const string Source = "분석/Saver_Late/decoded_art/gameres/item.arc/item.bmp";
        public const string Output = "Assets/_Game/Resources/UI/ItemIcons.png";
        public const int Width = ItemIcons.Columns * ItemIcons.CellSize;
        public const int Height = ItemIcons.Rows * ItemIcons.CellSize;

        public static Color32[] Decode(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 58 || bytes[0] != 'B' || bytes[1] != 'M'
                || BitConverter.ToInt32(bytes, 14) != 40 || BitConverter.ToInt32(bytes, 18) != Width
                || BitConverter.ToInt32(bytes, 22) != Height || BitConverter.ToInt16(bytes, 28) != 8
                || BitConverter.ToInt32(bytes, 30) != 0)
                throw new InvalidDataException("Expected original 3200x320 uncompressed 8-bit item sheet.");
            int offset = BitConverter.ToInt32(bytes, 10);
            if (offset < 58 || offset > 1078 || (offset-54)%4!=0 || offset > bytes.Length - Width * Height)
                throw new InvalidDataException("Truncated item pixels.");
            int paletteCount=(offset-54)/4; // Later source has 255 palette entries, not 256.
            var pixels = new Color32[Width * Height];
            // Original is bottom-up. Unity pixel rows are also bottom-up; do not flip twice.
            for (int i = 0; i < pixels.Length; i++) {
                if(bytes[offset+i]>=paletteCount)throw new InvalidDataException("Item color index exceeds native palette.");
                int palette = 54 + bytes[offset + i] * 4;
                byte b = bytes[palette], g = bytes[palette + 1], r = bytes[palette + 2];
                pixels[i] = new Color32(r, g, b, (byte)((r | g | b) == 0 ? 0 : 255));
            }
            return pixels;
        }

        public static Rect CellRect(int index)
        {
            if (index < 0 || index >= ItemIcons.Columns * ItemIcons.Rows) throw new ArgumentOutOfRangeException(nameof(index));
            return new Rect(index % ItemIcons.Columns * 32, Height - (index / ItemIcons.Columns + 1) * 32, 32, 32);
        }

        [MenuItem("DarkSaver/Convert/Item icon sheet (create only)")]
        public static void Build()
        {
            string blocked = DarkSaver.EditorTools.WorkBatchMemoryMaintenance.BlockingReason();
            if (!string.IsNullOrEmpty(blocked)) throw new InvalidOperationException(blocked);
            if (File.Exists(Output) || File.Exists(Output + ".meta")) throw new IOException("Existing item artwork not overwritten.");
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            try {
                texture.SetPixels32(Decode(File.ReadAllBytes(Source))); texture.Apply();
                File.WriteAllBytes(Output, texture.EncodeToPNG());
            } finally { UnityEngine.Object.DestroyImmediate(texture); }
            AssetDatabase.ImportAsset(Output, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(Output);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 32; importer.maxTextureSize = 4096;
            importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = false;
            var frames = new SpriteMetaData[ItemIcons.Columns * ItemIcons.Rows];
            for (int i = 0; i < frames.Length; i++) frames[i] = new SpriteMetaData {
                name = "ItemIcon_" + i.ToString("D4"), rect = CellRect(i), alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(.5f, .5f)
            };
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
#pragma warning disable CS0618
            importer.spritesheet = frames;
#pragma warning restore CS0618
            importer.SaveAndReimport();
        }
    }
}
