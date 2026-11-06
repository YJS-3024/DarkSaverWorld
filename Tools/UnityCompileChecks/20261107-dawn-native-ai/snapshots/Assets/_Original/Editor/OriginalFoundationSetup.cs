using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DarkSaver.Original.Entry;
using DarkSaver.Original.Tutorial;
using DarkSaver.Original.World;
using DarkSaver.Original.Campaign;

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
            var entry = new GameObject("Original Entry", typeof(LocalServerManager), typeof(EntrySceneRoot), typeof(TutorialSceneRoot), typeof(WorldSceneRoot));
            var art = AssetDatabase.LoadAssetAtPath<NativeEntryArt>("Assets/_Original/Data/Entry/NativeEntryArt.asset");
            if (art == null) throw new InvalidOperationException("Native entry art has not been generated.");
            var serialized = new SerializedObject(entry.GetComponent<EntrySceneRoot>());
            serialized.FindProperty("art").objectReferenceValue = art; serialized.ApplyModifiedPropertiesWithoutUndo();
            serialized.FindProperty("useLoginAndServerSelection").boolValue = false; serialized.ApplyModifiedPropertiesWithoutUndo();
            var tutorialArt = AssetDatabase.LoadAssetAtPath<NativeTutorialArt>("Assets/_Original/Data/Tutorial/NativeTutorialArt.asset");
            if (tutorialArt == null) throw new InvalidOperationException("Native tutorial art has not been generated.");
            var tutorial = new SerializedObject(entry.GetComponent<TutorialSceneRoot>());
            tutorial.FindProperty("art").objectReferenceValue = tutorialArt; tutorial.ApplyModifiedPropertiesWithoutUndo();
            var worldArt = AssetDatabase.LoadAssetAtPath<NativeWorldArt>("Assets/_Original/Data/World/NativeWorldArt.asset");
            if(worldArt==null)throw new InvalidOperationException("Native world art has not been generated.");
            var world = new SerializedObject(entry.GetComponent<WorldSceneRoot>());
            world.FindProperty("art").objectReferenceValue = worldArt; world.ApplyModifiedPropertiesWithoutUndo();
            world.FindProperty("campaign").objectReferenceValue = AssetDatabase.LoadAssetAtPath<NativeCampaignArt>("Assets/_Original/Data/Campaign/NativeCampaignArt.asset"); world.ApplyModifiedPropertiesWithoutUndo();
            if (!EditorSceneManager.SaveScene(scene, StartScene))
                throw new InvalidOperationException("Could not save the new start scene.");
            EditorSettings.serializationMode = SerializationMode.ForceText;
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(StartScene, true) };
            PlayerSettings.SetPreloadedAssets(Array.Empty<UnityEngine.Object>());
            AssetDatabase.SaveAssets();
            Debug.Log("Entry configured: local profile -> commander creation -> waiting1500 -> battle301 -> Kaosia. Login/server screens are preserved and disabled by default; unknown world policies remain held.");
        }
    }
}
