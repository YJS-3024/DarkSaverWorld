using System;
using System.IO;
using DarkSaver.UI;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class NativeTownShopBuilder
    {
        public const string Output = "Assets/_Game/Resources/UI/TownShop/NativeTownShop.asset";
        public const string ConfirmationOutput = "Assets/_Game/Resources/UI/TownTradeConfirmation/NativeTradeConfirmation.asset";
        public const string InventoryOutput = "Assets/_Game/Resources/UI/TownInventory/NativeTownInventory.asset";
        public const string InventoryMinimizedOutput = "Assets/_Game/Resources/UI/TownInventoryMinimized/NativeTownInventoryMinimized.asset";
        public const string CharacterInfoOutput = "Assets/_Game/Resources/UI/TownCharacterInfo/NativeTownCharacterInfo.asset";
        public const string CharacterInfoMinimizedOutput = "Assets/_Game/Resources/UI/TownCharacterInfoMinimized/NativeTownCharacterInfoMinimized.asset";
        public const string CharacterAdjustmentOutput = "Assets/_Game/Resources/UI/TownAbilityAdjustment/NativeTownAbilityAdjustment.asset";
        const string Source = "분석/Saver_1.2/";
        static int Read(byte[] b, int o) => BitConverter.ToInt32(b, o);

        [MenuItem("DarkSaver/UI/Build Native Town Shop")]
        public static void Build()
            => BuildLayout(10, 419, 303, new[] {40, 39, 19}, Output);

        [MenuItem("DarkSaver/UI/Build Native Trade Confirmation")]
        public static void BuildConfirmation()
            => BuildLayout(29, 528, 75, new[] {27, 28}, ConfirmationOutput);

        [MenuItem("DarkSaver/UI/Build Native Town Inventory")]
        public static void BuildInventory()
            => BuildLayout(4, 409, 227, new[] {37, 38, 6}, InventoryOutput);

        [MenuItem("DarkSaver/UI/Build Native Town Inventory Minimized")]
        public static void BuildInventoryMinimized()
            => BuildLayout(4, 29, 30, Array.Empty<int>(), InventoryMinimizedOutput, "scene4m.lsc");

        [MenuItem("DarkSaver/UI/Build Native Town Character Info")]
        public static void BuildCharacterInfo()
            => BuildLayout(6, 335, 274, new[] {61, 62, 82}, CharacterInfoOutput);

        [MenuItem("DarkSaver/UI/Build Native Town Character Info Minimized")]
        public static void BuildCharacterInfoMinimized()
            => BuildLayout(6, 34, 22, Array.Empty<int>(), CharacterInfoMinimizedOutput, "scene6m.lsc");

        [MenuItem("DarkSaver/UI/Build Native Town Character Adjustment")]
        public static void BuildCharacterAdjustment()
            => BuildLayout(23, 540, 435, new[] {37,38,77,79,78,78,78,78,78,78,80,80,80,80,80,80,56,57,81,19,54}, CharacterAdjustmentOutput);

        static void BuildLayout(int sceneId, int width, int height, int[] controlIds, string output, string sourceFile = null, int[] expectedTail = null)
        {
            // Create only: never overwrite an edited converted catalog.
            if (File.Exists(output)) throw new InvalidOperationException("Converted town window already exists.");
            var scene = File.ReadAllBytes(Source + "extracted/Saver_Files/Saver/Wlib/" + (sourceFile ?? "scene" + sceneId + ".lsc"));
            var header = File.ReadAllBytes(Source + "unpacked/legacy_libraries/Wlib_waitlib_lce.header.bin");
            var bmp = File.ReadAllBytes(Source + "unpacked/legacy_libraries/Wlib_waitlib_lce.bmp");
            if (Read(scene, 0) != width || Read(scene, 4) != height || Read(header, 0) != 278
                || Read(bmp, 18) != 500 || Read(bmp, 22) != 800 || BitConverter.ToInt16(bmp, 28) != 8 || Read(bmp, 30) != 0)
                throw new InvalidDataException("Unexpected native town shop format.");
            var colors = new Color32[width * height]; int cursor = 28;
            for (int group = 0; group < Read(scene, 24); group++) {
                int id = Read(scene, cursor), count = Read(scene, cursor + 4); cursor += 8;
                int record = 4 + (id - 1) * 20;
                if (id < 1 || id > 278 || Read(header, record) != id) throw new InvalidDataException("Unknown town cell.");
                int x = Read(header, record + 4), y = Read(header, record + 8), w = Read(header, record + 12), h = Read(header, record + 16);
                if (x < 0 || y < 0 || w < 1 || h < 1 || x + w > 500 || y + h > 800 || count < 0 || count > 1000)
                    throw new InvalidDataException("Invalid town cell bounds.");
                for (int placement = 0; placement < count; placement++, cursor += 16) {
                    int left = Read(scene, cursor), top = Read(scene, cursor + 4), nx = Read(scene, cursor + 8), ny = Read(scene, cursor + 12);
                    if (nx < 0 || ny < 0 || (long)nx * ny > 10000) throw new InvalidDataException("Invalid town repetition.");
                    for (int rx = 0; rx < nx; rx++) for (int ry = 0; ry < ny; ry++)
                    for (int py = 0; py < h; py++) for (int px = 0; px < w; px++) {
                        int dx = left + rx * w + px, dy = top + ry * h + py;
                        if (dx < 0 || dx >= width || dy < 0 || dy >= height) continue;
                        int palette = 54 + bmp[Read(bmp, 10) + (799 - y - py) * 500 + x + px] * 4;
                        byte r = bmp[palette + 2], g = bmp[palette + 1], b = bmp[palette];
                        colors[(height - 1 - dy) * width + dx] = new Color32(r, g, b, (byte)((r | g | b) == 0 ? 0 : 255));
                    }
                }
            }
            var buttonHeader = File.ReadAllBytes(Source + "unpacked/legacy_libraries/Wlib_waitlib_ltn.header.bin");
            var buttons = File.ReadAllBytes(Source + "unpacked/legacy_libraries/Wlib_waitlib_ltn.bmp");
            if (Read(buttons, 18) != 450 || Read(buttons, 22) != 600 || BitConverter.ToInt16(buttons, 28) != 24 || Read(buttons, 30) != 0)
                throw new InvalidDataException("Unexpected town buttons.");
            int controls = Read(scene, cursor); cursor += 4;
            if (controls != controlIds.Length) throw new InvalidDataException("Unexpected shop controls.");
            expectedTail ??= new[]{0};
            int tailOffset=cursor+controls*12;
            if(tailOffset+expectedTail.Length*4!=scene.Length) throw new InvalidDataException("Unexpected town layout tail size.");
            for(int i=0;i<expectedTail.Length;i++) if(Read(scene,tailOffset+i*4)!=expectedTail[i]) throw new InvalidDataException("Unexpected town layout tail.");
            var window = new NativeBattleWindows.Window { sceneId = sceneId, title = "마을 창 " + sceneId, size = new Vector2Int(width, height), controls = new NativeBattleWindows.Control[controls] };
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            window.background = Save("Window" + sceneId, width, height, colors, output);
            for (int i = 0; i < controls; i++, cursor += 12) {
                int id = Read(scene, cursor), record = 4 + (id - 1) * 24;
                if (id != controlIds[i] || Read(buttonHeader, record) != id)
                    throw new InvalidDataException("Unexpected shop button ID.");
                int x = Read(buttonHeader, record + 4), y = Read(buttonHeader, record + 8), w = Read(buttonHeader, record + 12), h = Read(buttonHeader, record + 16);
                if (x < 0 || y < 0 || w < 1 || h < 1 || x + w * 3 > 450 || y + h > 600) throw new InvalidDataException("Invalid shop button bounds.");
                var frames = new Texture2D[3];
                for (int state = 0; state < 3; state++) {
                    var pixels = new Color32[w * h];
                    for (int py = 0; py < h; py++) for (int px = 0; px < w; px++) {
                        int p = Read(buttons, 10) + (599 - y - py) * 1352 + (x + state * w + px) * 3;
                        byte r = buttons[p + 2], g = buttons[p + 1], b = buttons[p];
                        pixels[(h - 1 - py) * w + px] = new Color32(r, g, b, (byte)((r | g | b) == 0 ? 0 : 255));
                    }
                    frames[state] = Save("Button" + id + "_" + state, w, h, pixels, output);
                }
                window.controls[i] = new NativeBattleWindows.Control {nativeId = id, frames = frames, rect = new Rect(Read(scene, cursor + 4), Read(scene, cursor + 8), w, h)};
            }
            // scene40 additionally declares field1 at (279,112). Its editable behavior is not restored.
            var catalog = ScriptableObject.CreateInstance<NativeBattleWindows>(); catalog.windows = new[] {window};
            AssetDatabase.CreateAsset(catalog, output); AssetDatabase.SaveAssets();
        }
        static Texture2D Save(string name, int w, int h, Color32[] colors, string output)
        {
            string path = Path.GetDirectoryName(output) + "/" + name + ".png";
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            try {
                texture.SetPixels32(colors); texture.Apply(); var encoded=texture.EncodeToPNG();
                // Recover only identical images from an interrupted create; never overwrite edited assets.
                if(File.Exists(path)) {
                    if(!File.ReadAllBytes(path).AsSpan().SequenceEqual(encoded)) throw new InvalidOperationException("Converted shop image differs: "+name);
                } else File.WriteAllBytes(path,encoded);
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.mipmapEnabled = false; importer.filterMode = FilterMode.Point; importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.isReadable = false; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
