using System;
using System.Linq;
using DarkSaver.Combat;
using DarkSaver.World;
using DarkSaver.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Editor
{
    public static class BrugosScenarioSceneGenerator
    {
        private const string Map = "Assets/_Game/Scenes/Maps/Brugos01_Map.unity";
        private const string Core = "Assets/_Game/Scenes/Runtime/GameCore.unity";
        private const string Battle = "Assets/_Game/Scenes/Battle/Battle.unity";
        private const string Runtime = "Assets/_Game/Scenes/Runtime/Brugos01_Runtime.unity";
        private const string Entry = "Assets/_Game/Scenes/Entry/Brugos01_Play.unity";

        [MenuItem("DarkSaver/Scenes/Create Brugos 01 Demo")]
        public static void Generate()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (string.IsNullOrEmpty(scene.path))
                    throw new InvalidOperationException("Save or close untitled scenes before creating the 01 demo. Modified named scenes are preserved without saving.");
            }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Runtime) != null ||
                AssetDatabase.LoadAssetAtPath<SceneAsset>(Entry) != null)
                throw new InvalidOperationException("01 demo scenes already exist; refusing to overwrite them.");
            foreach (var path in new[] { Map, Core, Battle })
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                    throw new InvalidOperationException($"Required scene is missing: {path}");
            var enemy = AssetDatabase.LoadAssetAtPath<BattleEnemyDefinition>(
                "Assets/_Game/Runtime/Combat/Brugos01Kisra.asset");
            var symbol = AssetDatabase.LoadAssetAtPath<Sprite>(
                KisraSpriteConverter.Output);
            if (enemy == null || symbol == null)
                throw new InvalidOperationException("Converted enemy definition or demo symbol is missing.");
            var up = LoadFrames("Up"); var down = LoadFrames("Down");
            var left = LoadFrames("Left"); var right = LoadFrames("Right");

            var previous = SceneManager.GetActiveScene();
            var generated = default(Scene);
            try
            {
                generated = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(generated);
                var spawn = new GameObject("Spawn DemoEntry", typeof(SceneSpawnPoint));
                spawn.transform.position = new Vector3(14.5f, 7.5f, 0f);
                spawn.GetComponent<SceneSpawnPoint>().Configure("DemoEntry");
                var encounter = new GameObject("Kisra 700 Demo Encounter", typeof(SpriteRenderer),
                    typeof(BoxCollider2D), typeof(BattleEncounter2D), typeof(DirectionalSpriteAnimator));
                // 40-row MRC: source tile (19,10) -> world cell center (19.5,29.5).
                encounter.transform.position = new Vector3(19.5f, 29.5f, 0f);
                encounter.GetComponent<SpriteRenderer>().sprite = symbol;
                encounter.GetComponent<SpriteRenderer>().sortingOrder = 20000 - 295;
                encounter.GetComponent<BoxCollider2D>().isTrigger = true;
                encounter.GetComponent<BoxCollider2D>().size = Vector2.one;
                encounter.GetComponent<BattleEncounter2D>().ConfigureEnemy(enemy);
                encounter.GetComponent<DirectionalSpriteAnimator>().Configure(up, down, left, right);
                if (!EditorSceneManager.SaveScene(generated, Runtime))
                    throw new InvalidOperationException("Could not save 01 gameplay scene.");
                EditorSceneManager.CloseScene(generated, true);

                generated = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(generated);
                var bootstrap = new GameObject("Brugos 01 Demo Bootstrap", typeof(AdditiveSceneBootstrap))
                    .GetComponent<AdditiveSceneBootstrap>();
                bootstrap.Configure("GameCore", "Brugos01_Map", "Brugos01_Runtime");
                // Demo entry uses the DEO cutscene's COM0 appearance, not a recovered player spawn rule.
                bootstrap.ConfigureEntryPosition(new Vector2(14.5f, 7.5f));
                if (!EditorSceneManager.SaveScene(generated, Entry))
                    throw new InvalidOperationException("Could not save 01 entry scene.");
                EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(
                    new[] { Entry, Runtime, Map, Core, Battle }
                        .Where(path => !EditorBuildSettings.scenes.Any(scene => scene.path == path))
                        .Select(path => new EditorBuildSettingsScene(path, true))).ToArray();
                Debug.Log("Created 01 demo entry and gameplay scenes. Map-only scene was not modified. Full DEO/event playback is not implemented.");
            }
            finally
            {
                if (generated.IsValid() && generated.isLoaded) EditorSceneManager.CloseScene(generated, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        private static Sprite[] LoadFrames(string direction)
        {
            var frames = Enumerable.Range(0, 3).Select(frame =>
                AssetDatabase.LoadAssetAtPath<Sprite>(KisraSpriteConverter.FramePath(direction, frame))).ToArray();
            if (frames.Any(frame => frame == null))
                throw new InvalidOperationException("Run Kisra 701 All Frames conversion before creating the demo.");
            return frames;
        }
    }
}
