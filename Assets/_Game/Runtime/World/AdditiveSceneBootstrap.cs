using System.Collections;
using DarkSaver.CameraSystem;
using DarkSaver.Player;
using DarkSaver.Core;
using DarkSaver.Combat;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace DarkSaver.World
{
    public sealed class AdditiveSceneBootstrap : MonoBehaviour
    {
        [SerializeField] private string coreScene;
        [SerializeField] private string mapScene;
        [SerializeField] private string runtimeScene;
        [SerializeField] private bool useEntryPosition;
        [SerializeField] private Vector2 entryPosition;

        public void ConfigureEntryPosition(Vector2 position)
        {
            useEntryPosition = true;
            entryPosition = position;
        }

        public void Configure(string core, string map, string runtime)
        {
            coreScene = core;
            mapScene = map;
            runtimeScene = runtime;
        }

        private IEnumerator Start()
        {
            var resuming = NativeSaveStore.TakeResume(out var saved, out var savePath);
            if (resuming)
            {
                ScenePortal2D.SetRequestedSpawn(null);
                mapScene = saved.mapScene; runtimeScene = saved.runtimeScene;
                useEntryPosition = true; entryPosition = new Vector2(saved.worldX, saved.worldY);
            }
            yield return LoadIfNeeded(coreScene);
            yield return LoadIfNeeded(mapScene);
            yield return LoadIfNeeded(runtimeScene);
            WorldRouteInstaller.EnsureForLoadedScenes();

            if (resuming)
            {
                var session = FindAnyObjectByType<BattleTransitionController>();
                if (session == null || !session.RestoreSaveState(saved))
                { Debug.LogError("Native save state could not be restored."); yield break; }
                session.ConfigureSaveFile(savePath);
            }
            else if (NativeCharacterProfile.TakePending(out var character, out var characterPath))
            {
                var session = FindAnyObjectByType<BattleTransitionController>();
                if (session == null || !session.InitializeCharacter(character))
                { Debug.LogError("Created native character could not be initialized."); yield break; }
                session.ConfigureSaveFile(characterPath);
            }

            var camera = FindAnyObjectByType<CameraFollow2D>();
            var player = FindAnyObjectByType<TopDownPlayerController>();
            var characterSession = FindAnyObjectByType<BattleTransitionController>();
            if (characterSession != null && characterSession.HasCreatedCharacter && player != null
                && !characterSession.TryRestoreCharacterArtwork(player))
            { player.enabled = false; Debug.LogError("Saved character class artwork is unavailable; world input stopped."); yield break; }
            if (player != null && useEntryPosition)
            {
                if(resuming && OriginalRestorationPolicy.AllowSinglePlayerRules && WorldLayoutMigration.AppliesTo(mapScene))
                {
                    if(!WorldLayoutMigration.TryResolve(mapScene,entryPosition.x,entryPosition.y,out float x,out float y))
                    {player.enabled=false;Debug.LogError("Later-layout resume geometry is unavailable; saved file retained.");yield break;}
                    entryPosition=new Vector2(x,y); // In-memory recovery only; no save, HP or inventory mutation.
                }
                player.transform.position = entryPosition;
                var body = player.GetComponent<Rigidbody2D>();
                if (body != null)
                {
                    body.position = entryPosition;
                    body.linearVelocity = Vector2.zero;
                }
            }
            if (camera == null || player == null) yield break;
            foreach (var tilemap in FindObjectsByType<Tilemap>())
            {
                if (tilemap.name != "Ground") continue;
                var bounds = tilemap.cellBounds;
                camera.Configure(player.transform, new Vector2(bounds.size.x, bounds.size.y));
                break;
            }
            DarkSaver.UI.FirstPlayTutorial.Ensure(characterSession, player);
            DarkSaver.Events.Scenario01Controller.EnsureForLoadedScene();
        }

        private static IEnumerator LoadIfNeeded(string sceneName)
        {
            if (!SceneManager.GetSceneByName(sceneName).isLoaded)
                yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }
    }
}
