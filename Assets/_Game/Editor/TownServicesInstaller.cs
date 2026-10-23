using System;
using System.Linq;
using DarkSaver.Combat;
using DarkSaver.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Editor
{
    public static class TownServicesInstaller
    {
        [MenuItem("DarkSaver/Scenes/Connect Town Progression Preview")]
        public static void Install()
        {
            const string path = "Assets/_Game/Scenes/Runtime/GameCore.unity";
            JobCatalogConverter.ConvertMercenaries(); MercenaryHireConverter.Convert();
            MercenarySpriteConverter.ConvertInitialSheets();
            var classArtwork = ClassArtworkConverter.Convert();
            var table = AssetDatabase.LoadAssetAtPath<ExperienceProgressionTable>(ExperienceProgressionConverter.Output);
            if (table == null || EditorApplication.isPlaying || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("Editor or progression table not ready.");
            var active = SceneManager.GetActiveScene(); var scene = SceneManager.GetSceneByPath(path); bool opened = !scene.isLoaded;
            if (!opened && scene.isDirty) throw new InvalidOperationException("GameCore has unsaved user changes; not overwritten.");
            try
            {
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                var hud = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<WorldSessionHud>(true)).Single();
                var panel = hud.GetComponent<TownServicesPanel>() ?? hud.gameObject.AddComponent<TownServicesPanel>();
                panel.ConfigureProgression(table); EditorUtility.SetDirty(panel); EditorSceneManager.MarkSceneDirty(scene);
                panel.ConfigureHiring(AssetDatabase.LoadAssetAtPath<DarkSaver.Jobs.MercenaryHireCatalog>(MercenaryHireConverter.Output),
                    AssetDatabase.LoadAssetAtPath<DarkSaver.Jobs.JobCatalog>(JobCatalogConverter.MercenaryOutput));
                var session = new SerializedObject(hud.GetComponent<BattleTransitionController>());
                var catalogs = session.FindProperty("formationCatalogs");
                var mercenaries = AssetDatabase.LoadAssetAtPath<DarkSaver.Jobs.JobCatalog>(JobCatalogConverter.MercenaryOutput);
                bool present = false;
                for (int i = 0; i < catalogs.arraySize; i++) if (catalogs.GetArrayElementAtIndex(i).objectReferenceValue == mercenaries) present = true;
                if (!present) { catalogs.arraySize++; catalogs.GetArrayElementAtIndex(catalogs.arraySize - 1).objectReferenceValue = mercenaries; session.ApplyModifiedPropertiesWithoutUndo(); }
                MercenarySpriteConverter.InstallTemplates(hud.gameObject);
                if (!hud.GetComponent<BattleTransitionController>().ConfigureClassArtwork(classArtwork))
                    throw new InvalidOperationException("Class artwork binding rejected");
                EditorUtility.SetDirty(hud.GetComponent<BattleTransitionController>());
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("GameCore save failed.");
                Debug.Log("Town progression preview connected; no NPC placements or player state changed.");
            }
            finally
            {
                if (opened && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
                if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
            }
        }
    }
}
