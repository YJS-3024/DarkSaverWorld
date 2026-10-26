using System;
using System.IO;
using System.Linq;
using DarkSaver.Combat;
using DarkSaver.Core;
using DarkSaver.Player;
using DarkSaver.UI;
using DarkSaver.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using DarkSaver.EditorTools.CanvasTesting;

namespace DarkSaver.EditorTools
{
    public static partial class BattlePlayVerification
    {
        static BattleTransitionController townTrainingSession;
        [MenuItem("DarkSaver/Verification/Run Kaosia training portal round trip")]
        public static void BeginTownTraining()
        {
            SessionState.SetBool(Key + "townTraining", true);
            try { BeginTraining(101); }
            catch { SessionState.SetBool(Key + "townTraining", false); throw; }
        }

        static void TickTownTraining(int stage)
        {
            if (EditorApplication.timeSinceStartup < nextAction) return;
            nextAction = EditorApplication.timeSinceStartup + .2;
            var coordinator = UnityEngine.Object.FindAnyObjectByType<WorldSceneCoordinator>();
            var player = UnityEngine.Object.FindAnyObjectByType<TopDownPlayerController>();
            var hud = UnityEngine.Object.FindAnyObjectByType<WorldSessionHud>();
            var session = UnityEngine.Object.FindAnyObjectByType<BattleTransitionController>();
            if (coordinator == null || player == null || hud == null || session == null || coordinator.IsTravelling
                || !player.isActiveAndEnabled || !session.HasCreatedCharacter) return;
            bool Town() => SceneManager.GetSceneByName("Town_Runtime").isLoaded;
            void SessionKept() {
                if (session != townTrainingSession || session.PlayerName != "전투검증")
                    throw new InvalidOperationException("Character session changed during training travel.");
            }
            void CheckSave(int id) {
                SessionKept();
                if (Town() || !SceneManager.GetSceneByName("Map_" + id + "_;").isLoaded || !session.SaveWorld())
                    throw new InvalidOperationException("Training world/save not ready: " + session.LastSaveMessage);
                var store = new NativeSaveStore(Path.Combine(output, "verification-save.json"));
                if (!store.TryLoad(out var saved, out _) || saved.mapScene != "Map_" + id + "_;"
                    || saved.runtimeScene != "Training" + id + "_Runtime" || saved.character.name != "전투검증")
                    throw new InvalidOperationException("Training save round trip mismatch.");
                Note("PASS: Training" + id + " map/runtime, preserved GameCore session, native save/readback.");
            }
            if (stage == 20)
            {
                if (!Town()) return;
                session.ConfigureSaveFile(Path.Combine(output, "verification-save.json"));
                townTrainingSession = session;
                if (SessionState.GetBool(Key + "characterInfo", false)) { PrepareCharacterInfo(session, player); return; }
                if (SessionState.GetBool(Key + "townItems", false)) { PrepareTownItems(session, player); return; }
                if (SessionState.GetBool(Key + "townHiring", false)) { PrepareTownHiring(session, player); return; }
                if (SessionState.GetBool(Key + "classChange", false)) { PrepareClassChange(session, player); return; }
                if (SessionState.GetBool(Key + "mercenaryDeployment", false)) HireVerificationMercenaries(session, player);
                var roots = SceneManager.GetSceneByName("Town_Runtime").GetRootGameObjects();
                var entrance = roots.Single(go => go.name == "Training Entrance");
                var returned = roots.Single(go => go.name == "Spawn FromTraining");
                player.transform.position = returned.transform.position;
                player.GetComponent<Rigidbody2D>().position = returned.transform.position;
                Physics2D.SyncTransforms();
                if (!player.TryMoveToWorld(entrance.transform.position)) throw new InvalidOperationException("Cannot walk to training entrance.");
                SessionState.SetInt(Key + "stage", 21); Note("Walking to the western training entrance via normal movement."); return;
            }
            if (stage == 21)
            {
                if (!SceneManager.GetSceneByName("Training101_Runtime").isLoaded) return;
                CheckSave(101); ScreenCapture.CaptureScreenshot(Path.Combine(output, "training101-world.png"));
                if (SessionState.GetBool(Key + "mercenaryDeployment", false))
                { PrepareMercenaryDeployment(session, player); return; }
                foreach (var doc in hud.GetComponentsInChildren<InGameUi>().Select(CanvasProbe.For))
                    if (doc.rootVisualElement.Q<DarkSaver.EditorTools.CanvasTesting.VisualElement>("training-panel") != null)
                        throw new InvalidOperationException("Obsolete training selection menu exists.");
                var portal = SceneManager.GetSceneByName("Training101_Runtime").GetRootGameObjects().Single(go => go.name == "To Kaosia From Training");
                if (!player.TryMoveToWorld(portal.transform.position)) throw new InvalidOperationException("Cannot walk to training return portal.");
                SessionState.SetInt(Key + "stage", 22);
                Note("PASS: job100 entered Training101 automatically through the physical entrance; no selection menu."); return;
            }
            if (stage == 22)
            {
                if (!Town()) return;
                SessionKept();
                if (SceneManager.GetSceneByName("Map_101_;").isLoaded || SceneManager.GetSceneByName("Training101_Runtime").isLoaded)
                    throw new InvalidOperationException("Old training scenes were retained.");
                var entrance = SceneManager.GetSceneByName("Town_Runtime").GetRootGameObjects().Single(go => go.name == "Training Entrance");
                if (!player.TryMoveToWorld(entrance.transform.position)) throw new InvalidOperationException("Second automatic entry path rejected.");
                SessionState.SetInt(Key + "stage", 23); Note("PASS: physical return restored Kaosia; walking to auto-enter again."); return;
            }
            if (stage == 23)
            {
                if (!SceneManager.GetSceneByName("Training101_Runtime").isLoaded) return;
                CheckSave(101);
                if (!hud.ReturnFromTraining()) throw new InvalidOperationException("Training HUD return rejected.");
                SessionState.SetInt(Key + "stage", 24); return;
            }
            if (stage == 24 && Town())
            {
                SessionKept();
                if (SceneManager.GetSceneByName("Map_101_;").isLoaded || SceneManager.GetSceneByName("Training101_Runtime").isLoaded || player.UiInputBlocked)
                    throw new InvalidOperationException("Training scenes/input lock leaked after return.");
                Finish("PASS: job100 auto-entry -> Training101 -> physical return -> auto-entry -> Training101 -> HUD return; session/save preserved");
            }
        }
    }
}
