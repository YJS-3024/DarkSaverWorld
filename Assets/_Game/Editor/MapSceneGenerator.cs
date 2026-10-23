using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DarkSaver.Core;
using DarkSaver.Maps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Editor
{
    public static class MapSceneGenerator
    {
        private const string SceneFolder = "Assets/_Game/Scenes/Maps";

        [MenuItem("DarkSaver/Legacy/Generate Map Placeholders")]
        public static void GenerateAll()
        {
            var root = Directory.GetParent(Application.dataPath)?.FullName
                ?? throw new InvalidOperationException("프로젝트 루트를 확인할 수 없습니다.");
            var catalogPath = Path.Combine(root, "분석", "Saver_1.2", "catalogs", "maps.csv");
            var entries = ReadCatalog(catalogPath);

            Directory.CreateDirectory(Path.Combine(root, SceneFolder));
            var generatedCount = 0;

            foreach (var entry in entries)
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                CreateSceneContents(entry);
                var scenePath = $"{SceneFolder}/Map_{entry.Key}_{SafeFileName(entry.Title)}.unity";
                EditorSceneManager.SaveScene(scene, scenePath);
                generatedCount++;
            }

            AssetDatabase.Refresh();
            Debug.Log($"Legacy map placeholder scenes generated: {generatedCount}; build scenes unchanged.");
        }

        private static void CreateSceneContents(MapEntry entry)
        {
            var root = new GameObject($"Map {entry.Key} - {entry.Title}");
            root.AddComponent<MapSceneDefinition>().Configure(entry.Key, entry.Title, entry.Summary);

            var game = new GameObject("DarkSaver Game");
            game.AddComponent<DarkSaverGame>();

            var cameraObject = new GameObject("Main Camera");
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.07f, .08f, .07f);
            camera.orthographic = true;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static List<MapEntry> ReadCatalog(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("맵 카탈로그가 없습니다.", path);
            var lines = File.ReadAllLines(path, Encoding.UTF8);
            var result = new List<MapEntry>(Math.Max(0, lines.Length - 1));
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var columns = ParseCsvLine(line);
                if (columns.Count < 6) throw new FormatException($"잘못된 maps.csv 행: {line}");
                var title = string.IsNullOrWhiteSpace(columns[1]) ? "Unnamed" : columns[1].Trim();
                result.Add(new MapEntry(columns[0].Trim(), title, columns[5].Trim()));
            }
            return result;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var values = new List<string>();
            var current = new StringBuilder();
            var quoted = false;
            for (var i = 0; i < line.Length; i++)
            {
                var character = line[i];
                if (character == '"')
                {
                    if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else quoted = !quoted;
                }
                else if (character == ',' && !quoted)
                {
                    values.Add(current.ToString());
                    current.Clear();
                }
                else current.Append(character);
            }
            values.Add(current.ToString());
            return values;
        }

        private static string SafeFileName(string value)
        {
            foreach (var invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
            return string.IsNullOrWhiteSpace(value) ? "Unnamed" : value.Trim();
        }

        private readonly struct MapEntry
        {
            public readonly string Key;
            public readonly string Title;
            public readonly string Summary;

            public MapEntry(string key, string title, string summary)
            {
                Key = key;
                Title = title;
                Summary = summary;
            }
        }
    }
}
