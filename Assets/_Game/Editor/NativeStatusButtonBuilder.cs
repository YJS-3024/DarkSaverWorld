using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class NativeStatusButtonBuilder
    {
        public const string Output = "Assets/_Game/Resources/UI/NativeStatusButtons.png";
        [MenuItem("DarkSaver/UI/Build Native Status Buttons")]
        public static void Build()
        {
            var header = File.ReadAllBytes("분석/Saver_1.2/unpacked/legacy_libraries/Glib_GameLib_ltn.header.bin");
            var data = File.ReadAllBytes("분석/Saver_1.2/unpacked/legacy_libraries/Glib_GameLib_ltn.bmp");
            if (header.Length != 1616 || BitConverter.ToInt32(header, 0) != 67
                || data.Length < 54 || BitConverter.ToInt32(data, 18) != 450 || BitConverter.ToInt32(data, 22) != 491
                || BitConverter.ToInt16(data, 28) != 24 || BitConverter.ToInt32(data, 30) != 0)
                throw new InvalidDataException("Unexpected GameLib.ltn format.");
            int offset = BitConverter.ToInt32(data, 10), stride = (450 * 3 + 3) & ~3;
            if (offset < 54 || offset > data.Length - stride * 491) throw new InvalidDataException("Truncated button BMP.");
            var texture = new Texture2D(45, 30, TextureFormat.RGBA32, false);
            try
            {
                var pixels = new Color32[45 * 30];
                for (int row = 0; row < 2; row++)
                {
                    int id = 37 + row, record = 4 + (id - 1) * 24;
                    int sx = BitConverter.ToInt32(header, record + 4), sy = BitConverter.ToInt32(header, record + 8);
                    if (BitConverter.ToInt32(header, record) != id || sx != 256 || sy != row * 15
                        || BitConverter.ToInt32(header, record + 12) != 15 || BitConverter.ToInt32(header, record + 16) != 15
                        || BitConverter.ToInt32(header, record + 20) != 0)
                        throw new InvalidDataException("Unexpected status button record.");
                    for (int y = 0; y < 15; y++) for (int x = 0; x < 45; x++)
                    {
                        int p = offset + (490 - sy - y) * stride + (sx + x) * 3;
                        byte r = data[p+2], g = data[p+1], b = data[p];
                        pixels[(29 - row * 15 - y) * 45 + x] = new Color32(r,g,b,(byte)((r|g|b) == 0 ? 0 : 255));
                    }
                }
                texture.SetPixels32(pixels); texture.Apply(); File.WriteAllBytes(Output, texture.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
            AssetDatabase.ImportAsset(Output, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(Output);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 32; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true; importer.textureCompression = TextureImporterCompression.Uncompressed;
            var sprites = new SpriteMetaData[6];
            for (int row = 0; row < 2; row++) for (int state = 0; state < 3; state++) sprites[row*3+state] = new SpriteMetaData {
                name = "Status_" + (37+row) + "_" + state, rect = new Rect(state*15,15-row*15,15,15),
                alignment = 0, pivot = new Vector2(.5f,.5f) };
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
#pragma warning disable CS0618
            importer.spritesheet = sprites;
#pragma warning restore CS0618
            importer.SaveAndReimport();
        }
    }
}
