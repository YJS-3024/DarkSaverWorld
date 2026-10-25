using System.IO;
using UnityEngine;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace DarkSaver.Editor
{
    // Keeps asynchronous Test Runner results available to editor automation.
    public static class ProjectValidationRunner
    {
        private static TestRunnerApi api;
        public static string RunEditMode(bool runSynchronously = false)
        {
            api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Results());
            return api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode }) { runSynchronously = runSynchronously });
        }

        private sealed class Results : ICallbacks
        {
            private string fixturePath;
            public void RunStarted(ITestAdaptor tests)
            {
                var scene = SceneManager.GetActiveScene();
                // Test Runner opens an untitled scene; Unity 6 refuses additive fixtures
                // until that runner-owned scene has a path. User scenes are untouched.
                if (string.IsNullOrEmpty(scene.path) || !File.Exists(scene.path))
                {
                    fixturePath = "Assets/_Game/Tests/EditMode/ValidationFixture-" + System.Guid.NewGuid().ToString("N") + ".unity";
                    if (!EditorSceneManager.SaveScene(scene, fixturePath))
                        throw new System.IO.IOException("Could not prepare named Test Runner scene.");
                }
            }
            public void TestStarted(ITestAdaptor test) { SessionState.SetString("DS.ValidationCurrentTest",test.FullName); }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                string path = Path.GetFullPath(Path.Combine(Application.dataPath,
                    "../Tools/UnityCompileChecks/current-editmode.xml"));
                TestRunnerApi.SaveResultToFile(result, path);
                SessionState.SetString("DS.ValidationCurrentTest", "Finished");
                Debug.Log($"DS_EDITMODE_RESULT passed={result.PassCount} failed={result.FailCount} skipped={result.SkipCount} inconclusive={result.InconclusiveCount}");
                api.UnregisterCallbacks(this);
                if (fixturePath != null) AssetDatabase.DeleteAsset(fixturePath);
            }
        }
    }
}
