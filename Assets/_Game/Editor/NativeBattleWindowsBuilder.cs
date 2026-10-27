using System;
using System.Collections.Generic;
using System.IO;
using DarkSaver.UI;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class NativeBattleWindowsBuilder
    {
        const string Folder = "Assets/_Game/Resources/UI/BattleWindows";
        const string Source = "분석/Saver_1.2/";
        [MenuItem("DarkSaver/UI/Build Native Battle Window Shells")]
        public static void Build()
        {
            Directory.CreateDirectory(Folder);
            var header = File.ReadAllBytes(Source + "unpacked/legacy_libraries/Glib_GameLib_lce.header.bin");
            var bitmap = File.ReadAllBytes(Source + "unpacked/legacy_libraries/Glib_GameLib_lce.bmp");
            var buttonHeader = File.ReadAllBytes(Source + "unpacked/legacy_libraries/Glib_GameLib_ltn.header.bin");
            var buttons = File.ReadAllBytes(Source + "unpacked/legacy_libraries/Glib_GameLib_ltn.bmp");
            var list = new List<NativeBattleWindows.Window>();
            int[] ids = { 0, 3, 4, 5, 6, 7, 8, 17, 28 };
            string[] titles = { "맵 정보", "동맹", "정보창 보기", "승리 조건", "지형 정보", "도움말", "결과 보고", "경험치 결과 보고", "도망 확인" };
            var convertedButtons = new Dictionary<int, Texture2D[]>();
            for (int i = 0; i < ids.Length; i++)
            {
                var scene = File.ReadAllBytes(Source + "extracted/Saver_Files/Saver/Glib/gscene" + ids[i] + ".lsc");
                var colors = NativeStatusPanelBuilder.Compose(scene, header, bitmap, out int w, out int h, windowLayout: true);
                var window = new NativeBattleWindows.Window { sceneId = ids[i], title = titles[i], size = new Vector2Int(w, h),
                    background = Save("Window" + ids[i], w, h, colors) };
                int cursor = 28;
                for (int group = 0; group < Read(scene, 24); group++) cursor += 8 + Read(scene, cursor + 4) * 16;
                int count = Read(scene, cursor); cursor += 4;
                window.controls = new NativeBattleWindows.Control[count];
                for (int c = 0; c < count; c++, cursor += 12)
                {
                    int id = Read(scene, cursor), record = 4 + (id - 1) * 24;
                    if (id < 1 || id > Read(buttonHeader, 0) || Read(buttonHeader, record) != id)
                        throw new InvalidDataException("Unknown native button " + id);
                    int x = Read(buttonHeader, record + 4), y = Read(buttonHeader, record + 8);
                    int bw = Read(buttonHeader, record + 12), bh = Read(buttonHeader, record + 16);
                    if (!convertedButtons.TryGetValue(id, out var frames))
                    {
                        frames = new Texture2D[3];
                        for (int state = 0; state < 3; state++)
                        {
                            if (x < 0 || y < 0 || bw < 1 || bh < 1 || x + bw * 3 > 450 || y + bh > 491)
                                throw new InvalidDataException("Invalid native button bounds.");
                            var pixels = new Color32[bw * bh];
                            for (int py = 0; py < bh; py++) for (int px = 0; px < bw; px++)
                            {
                                int p = Read(buttons, 10) + (490 - y - py) * 1352 + (x + state * bw + px) * 3;
                                byte r = buttons[p + 2], g = buttons[p + 1], b = buttons[p];
                                pixels[(bh - 1 - py) * bw + px] = new Color32(r, g, b, (byte)((r | g | b) == 0 ? 0 : 255));
                            }
                            frames[state] = Save("Button" + id + "_" + state, bw, bh, pixels);
                        }
                        convertedButtons.Add(id, frames);
                    }
                    window.controls[c] = new NativeBattleWindows.Control { nativeId = id, frames = frames,
                        rect = new Rect(Read(scene, cursor + 4), Read(scene, cursor + 8), bw, bh) };
                }
                string minimizedPath = Source + "extracted/Saver_Files/Saver/Glib/gscene" + ids[i] + "m.lsc";
                if (File.Exists(minimizedPath))
                {
                    var minimizedPixels = NativeStatusPanelBuilder.Compose(File.ReadAllBytes(minimizedPath), header, bitmap,
                        out int mw, out int mh, windowLayout: true);
                    window.minimizedBackground = Save("Window" + ids[i] + "Minimized", mw, mh, minimizedPixels);
                    window.minimizedSize = new Vector2Int(mw, mh);
                }
                list.Add(window);
            }
            const string path = Folder + "/NativeBattleWindows.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<NativeBattleWindows>(path);
            if (catalog == null) { catalog = ScriptableObject.CreateInstance<NativeBattleWindows>(); AssetDatabase.CreateAsset(catalog, path); }
            catalog.windows = list.ToArray(); EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
        }
        static int Read(byte[] bytes, int offset) => BitConverter.ToInt32(bytes, offset);
        static Texture2D Save(string name, int w, int h, Color32[] colors)
        {
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            string path = Folder + "/" + name + ".png";
            try { texture.SetPixels32(colors); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default; importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point; importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
