using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Prototype.Editor
{
    [InitializeOnLoad]
    internal static class DarkSaverPrototypeSceneInstaller
    {
        internal const string ScenePath =
            "Assets/_Game/Scene/DarkSaverPrototypeScene.unity";

        static DarkSaverPrototypeSceneInstaller()
        {
            EditorApplication.delayCall += EnsureSceneExists;
        }

        [MenuItem("Dark Saver/Open Prototype Scene")]
        private static void OpenPrototypeScene()
        {
            EnsureSceneExists();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static void EnsureSceneExists()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath))
                CreateScene();

            AddToBuildSettings();
        }

        private static void CreateScene()
        {
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            scene.name = "DarkSaverPrototypeScene";

            var root = new GameObject("DarkSaver Prototype");
            root.AddComponent<global::DarkSaver.Prototype.DarkSaverPrototype>();
            SceneManager.MoveGameObjectToScene(root, scene);

            var cameraObject = new GameObject("Main Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.04f, .05f, .045f);
            cameraObject.tag = "MainCamera";
            SceneManager.MoveGameObjectToScene(cameraObject, scene);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorSceneManager.CloseScene(scene, true);

            if (previous.IsValid() && previous.isLoaded)
                SceneManager.SetActiveScene(previous);

            AssetDatabase.SaveAssets();
        }

        private static void AddToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(entry => entry.path == ScenePath))
                return;

            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
