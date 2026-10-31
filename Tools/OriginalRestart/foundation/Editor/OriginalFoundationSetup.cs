using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DarkSaver.Original.Editor
{
    // Explicit setup only; no InitializeOnLoad callback or runtime bootstrap.
    public static class OriginalFoundationSetup
    {
        public const string StartScene = "Assets/_Original/Scenes/Start.unity";

        [MenuItem("DarkSaver/새 시작/시작 씬 열기")]
        public static void OpenStart()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(StartScene);
        }

        [MenuItem("DarkSaver/새 시작/기반 설정")]
        public static void Configure()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            root.tag = "MainCamera";
            root.transform.position = new Vector3(0, 0, -10);
            var camera = root.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 7.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            // Empty workspace only; this is not an original camera/timing policy.
            if (!EditorSceneManager.SaveScene(scene, StartScene))
                throw new InvalidOperationException("Could not save the new start scene.");
            EditorSettings.serializationMode = SerializationMode.ForceText;
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(StartScene, true) };
            PlayerSettings.SetPreloadedAssets(Array.Empty<UnityEngine.Object>());
            AssetDatabase.SaveAssets();
            Debug.Log("Original foundation configured: one empty start scene; no previous gameplay.");
        }
    }
}
