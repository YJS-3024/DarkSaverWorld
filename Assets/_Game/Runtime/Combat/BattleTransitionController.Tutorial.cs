using DarkSaver.Core;
using DarkSaver.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace DarkSaver.Combat
{
    public sealed partial class BattleTransitionController
    {
        public bool HasPendingTutorial => character?.tutorial != null && !character.tutorial.IsComplete;
        // Save the candidate before publishing it, including the new-character checkpoint.
        public bool TrySaveTutorial(int expectedStep, bool moved, bool skip, bool checkpoint, out string reason)
        {
            if(!processingSessionRequest)return SubmitSession(new DarkSaver.Network.TutorialProgressRequest(expectedStep,moved,skip,checkpoint),out reason);
            reason = "튜토리얼 진행을 저장할 수 없습니다.";
            var player = FindAnyObjectByType<TopDownPlayerController>();
            if (!HasPendingTutorial || !character.tutorial.IsValid || transitioning || player == null
                || !player.isActiveAndEnabled || !SceneManager.GetSceneByName("Kaosia_Map").isLoaded
                || !SceneManager.GetSceneByName("Town_Runtime").isLoaded || SceneManager.GetSceneByName("Battle").isLoaded
                || FindAnyObjectByType<DarkSaver.World.WorldSceneCoordinator>()?.IsTravelling == true) return false;
            var candidate = character.Clone();
            if (checkpoint)
            { if (candidate.tutorial.step != expectedStep) return false; }
            else
            {
                if (!candidate.tutorial.TryAdvance(expectedStep, moved, skip, out var next)) return false;
                candidate.tutorial = next;
            }
            var destination = player.transform.position;
            bool returningToCenter = !checkpoint && candidate.tutorial.IsComplete;
            if (returningToCenter)
            {
                Tilemap ground = null, terrain = null;
                foreach (var root in SceneManager.GetSceneByName("Kaosia_Map").GetRootGameObjects())
                    foreach (var map in root.GetComponentsInChildren<Tilemap>(true))
                    {
                        if (map.name == "Ground") ground = map;
                        else if (map.name == "TerrainAttributes") terrain = map;
                    }
                // Local town policy: exact world position requested for tutorial completion.
                var cell = new Vector3Int(21, 21, 0);
                if (ground == null || terrain == null || !ground.HasTile(cell)
                    || terrain.GetColliderType(cell) != Tile.ColliderType.None)
                { reason = "마을 중앙의 스폰 위치를 확인할 수 없습니다."; return false; }
                destination = new Vector3(21f, 21f, player.transform.position.z);
            }
            var data = CaptureSaveState("Kaosia_Map", "Town_Runtime", destination);
            data.character = candidate;
            if (saveStore == null) saveStore = new NativeSaveStore(NativeSaveStore.DefaultPath);
            if (!saveStore.TrySave(data, out reason)) return false;
            character = candidate;
            if (returningToCenter)
            {
                player.SetTutorialInputBlocked(true); // Cancel held movement/click paths before relocation.
                player.transform.position = destination;
                var body = player.GetComponent<Rigidbody2D>();
                if (body != null) { body.position = destination; body.linearVelocity = Vector2.zero; }
                var camera = FindAnyObjectByType<DarkSaver.CameraSystem.CameraFollow2D>();
                if (camera != null) camera.transform.position = new Vector3(destination.x, destination.y, camera.transform.position.z);
                player.SetTutorialInputBlocked(false);
            }
            return true;
        }
    }
}
