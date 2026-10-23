using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace DarkSaver.EditorTools
{
    // General combat/UI tests use training maps. Scenario verification stays explicit.
    public static class TrainingBattleTestMenu
    {
        const string Key = "DarkSaver.CombatTest.TrainingMap";
        public static int SelectedMap
        {
            get
            {
                int id = SessionState.GetInt(Key, 101);
                return id >= 101 && id <= 107 ? id : 101;
            }
        }
        public static void Select(int id)
        {
            if (id < 101 || id > 107) throw new ArgumentOutOfRangeException(nameof(id));
            SessionState.SetInt(Key, id);
        }
        [MenuItem("DarkSaver/Testing/Training map/101")] static void Select101() => Select(101);
        [MenuItem("DarkSaver/Testing/Training map/102")] static void Select102() => Select(102);
        [MenuItem("DarkSaver/Testing/Training map/103")] static void Select103() => Select(103);
        [MenuItem("DarkSaver/Testing/Training map/104")] static void Select104() => Select(104);
        [MenuItem("DarkSaver/Testing/Training map/105")] static void Select105() => Select(105);
        [MenuItem("DarkSaver/Testing/Training map/106")] static void Select106() => Select(106);
        [MenuItem("DarkSaver/Testing/Training map/107")] static void Select107() => Select(107);
        [MenuItem("DarkSaver/Testing/Training map/101", true)]
        [MenuItem("DarkSaver/Testing/Training map/102", true)]
        [MenuItem("DarkSaver/Testing/Training map/103", true)]
        [MenuItem("DarkSaver/Testing/Training map/104", true)]
        [MenuItem("DarkSaver/Testing/Training map/105", true)]
        [MenuItem("DarkSaver/Testing/Training map/106", true)]
        [MenuItem("DarkSaver/Testing/Training map/107", true)]
        static bool ValidateSelection()
        {
            for (int id = 101; id <= 107; id++)
                Menu.SetChecked("DarkSaver/Testing/Training map/" + id, id == SelectedMap);
            return !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling;
        }
        [MenuItem("DarkSaver/Testing/Open selected training scene")]
        public static void OpenSelected()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Open training scenes only in idle Edit mode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save your modified scenes before switching training maps.");
            EditorSceneManager.OpenScene(TrainingSceneGenerator.EntryPath(SelectedMap), OpenSceneMode.Single);
        }
        [MenuItem("DarkSaver/Testing/Run selected training combat test")]
        public static void RunSelected() => BattlePlayVerification.BeginTraining(SelectedMap);
    }
}
