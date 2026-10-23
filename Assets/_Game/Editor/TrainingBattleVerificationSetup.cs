using System;
using System.Collections.Generic;
using DarkSaver.CameraSystem;
using DarkSaver.Combat;
using DarkSaver.Core;
using DarkSaver.Jobs;
using DarkSaver.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace DarkSaver.EditorTools
{
    // Editor-only preparation for the user-approved themed encounters.
    internal static class TrainingBattleVerificationSetup
    {
        public static void Prepare(int mapId, string savePath)
        {
            var scene = SceneManager.GetSceneByName("Map_" + mapId + "_;");
            Tilemap ground = null, terrain = null;
            foreach (var root in scene.GetRootGameObjects())
            foreach (var map in root.GetComponentsInChildren<Tilemap>())
            {
                if (map.name == "Ground") ground = map;
                if (map.name == "TerrainAttributes") terrain = map;
            }
            var player = UnityEngine.Object.FindAnyObjectByType<TopDownPlayerController>();
            var session = UnityEngine.Object.FindAnyObjectByType<BattleTransitionController>();
            var camera = UnityEngine.Object.FindAnyObjectByType<CameraFollow2D>();
            var runtime = SceneManager.GetSceneByName("Training"+mapId+"_Runtime");
            DarkSaver.World.SceneSpawnPoint spawn = null;
            foreach (var root in runtime.GetRootGameObjects())
                if (root.GetComponent<DarkSaver.World.SceneSpawnPoint>() is DarkSaver.World.SceneSpawnPoint marker) spawn = marker;
            if (ground == null || terrain == null || player == null || session == null || camera == null || spawn == null)
                throw new InvalidOperationException("Training verification dependencies are missing.");
            if (!NativeCharacterProfile.TakePending(out var profile,out _) || !session.InitializeCharacter(profile))
                throw new InvalidOperationException("Isolated training character could not be initialized.");
            session.ConfigureSaveFile(savePath);
            player.transform.position = spawn.transform.position;
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null) { body.position = player.transform.position; body.linearVelocity = Vector2.zero; }
            camera.Configure(player.transform,new Vector2(ground.cellBounds.size.x,ground.cellBounds.size.y));

        }

        public static bool TryFindFixtureCells(Tilemap ground, Tilemap terrain, IReadOnlyList<JobCatalog> catalogs, int enemyJob,
            out CombatGridCell start, out CombatGridCell target)
        {
            start = target = default;
            var bounds = ground.cellBounds;
            bool Pass(CombatGridCell cell) => BattleMapPresentation.TryGetPlacementMovement(ground,terrain,bounds,catalogs,100,cell,out var ally)
                && ally != CombatTerrainTraversal.ImpassableValue
                && BattleMapPresentation.TryGetPlacementMovement(ground,terrain,bounds,catalogs,enemyJob,cell,out var foe)
                && foe != CombatTerrainTraversal.ImpassableValue;
            int best = int.MaxValue;
            for (int y = 0; y < bounds.size.y; y++)
            for (int x = 0; x < bounds.size.x; x++)
            for (int direction = 0; direction < 2; direction++)
            {
                bool valid = true;
                for (int step = 0; step <= 3; step++)
                    if (!Pass(new CombatGridCell(x+(direction==0?step:0),y+(direction==1?step:0)))) { valid=false;break; }
                if (!valid) continue;
                int score = Math.Abs(x-bounds.size.x/2)+Math.Abs(y-bounds.size.y/2);
                if (score >= best) continue;
                best=score; start=new CombatGridCell(x,y);
                target=new CombatGridCell(x+(direction==0?3:0),y+(direction==1?3:0));
            }
            return best != int.MaxValue;
        }

    }
}
