using DarkSaver.Network;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using DarkSaver.UI;

namespace DarkSaver.Player
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(SpriteRenderer))]
    public sealed class TopDownPlayerController : MonoBehaviour
    {
        private bool RequestMove(Vector2 position)
        {
            var coordinator=FindAnyObjectByType<DarkSaver.World.WorldSceneCoordinator>();
            return coordinator!=null && coordinator.TryMoveWorld(position);
        }
        private Rigidbody2D body;
        private SpriteRenderer spriteRenderer;
        private DirectionalSpriteAnimator directionalAnimator;
        private Vector2 movement;
        private readonly List<Vector3> waypoints = new();
        private int waypointIndex;
        private bool followingClickPath;
        private Tilemap ground;
        private Tilemap terrain;
        private Vector3Int? heldMouseCell;
        private bool uiInputBlocked;
        private bool scenarioInputBlocked;
        private bool tutorialInputBlocked;
        private bool suppressMouseUntilRelease;
        private WorldSessionHud worldHud;

        public bool UiInputBlocked => uiInputBlocked || scenarioInputBlocked || tutorialInputBlocked;
        public bool IsMoving => followingClickPath || movement.sqrMagnitude > .0001f
            || (body != null ? body : GetComponent<Rigidbody2D>()).linearVelocity.sqrMagnitude > .0001f;

        public void SetTutorialInputBlocked(bool blocked)
        {
            if (tutorialInputBlocked == blocked) return;
            tutorialInputBlocked = blocked; suppressMouseUntilRelease = true;
            if (blocked) StopWorldMotion();
        }

        public void SetScenarioInputBlocked(bool blocked)
        {
            if (scenarioInputBlocked == blocked) return;
            scenarioInputBlocked = blocked;
            suppressMouseUntilRelease = true;
            if (blocked) StopWorldMotion();
        }

        public void SetUiInputBlocked(bool blocked)
        {
            if (uiInputBlocked == blocked) return;
            uiInputBlocked = blocked;
            suppressMouseUntilRelease = true;
            if (!blocked) return;
            StopWorldMotion();
        }

        private float worldMoveDebt;
        private System.Action pendingNpcInteraction;
        public bool QueueNpcInteraction(System.Action action)
        {
            if(action==null || UiInputBlocked || pendingNpcInteraction!=null)return false;
            if(body==null)body=GetComponent<Rigidbody2D>();
            var destination=followingClickPath && waypointIndex<waypoints.Count
                ? waypoints[waypointIndex] : new Vector3(Mathf.Floor(body.position.x)+.5f,Mathf.Floor(body.position.y)+.5f,0);
            if(((Vector2)destination-body.position).sqrMagnitude<.0001f){action();return true;}
            waypoints.Clear();waypoints.Add(destination);waypointIndex=0;followingClickPath=true;
            pendingNpcInteraction=action;movement=Vector2.zero;suppressMouseUntilRelease=true;
            if(directionalAnimator!=null)directionalAnimator.SetMotion((Vector2)destination-body.position);return true;
        }
        public void StopForSceneTravel() { StopWorldMotion(); suppressMouseUntilRelease = true; }

        private void StopWorldMotion()
        {
            pendingNpcInteraction=null;worldMoveDebt=0;
            movement = Vector2.zero;
            followingClickPath = false;
            waypoints.Clear();
            waypointIndex = 0;
            heldMouseCell = null;
            if (body == null) body = GetComponent<Rigidbody2D>();
            body.linearVelocity = Vector2.zero;
            if (directionalAnimator != null) directionalAnimator.SetMotion(Vector2.zero);
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            directionalAnimator = GetComponent<DirectionalSpriteAnimator>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation=RigidbodyInterpolation2D.None;
            if (spriteRenderer.sprite == null) spriteRenderer.sprite = CreateMarkerSprite();
        }

        private void Update()
        {
            if(NativeWorldWindows.IsTyping){StopWorldMotion();return;}
            if (UiInputBlocked || pendingNpcInteraction!=null) return;
            if (worldHud == null) worldHud = FindAnyObjectByType<WorldSessionHud>();
            if (Input.GetMouseButton(0) && FirstPlayTutorial.BlocksPointer(Input.mousePosition)) suppressMouseUntilRelease = true;
            if (Input.GetMouseButton(0) && worldHud != null && worldHud.BlocksWorldPointer(Input.mousePosition))
                suppressMouseUntilRelease = true;
            if (suppressMouseUntilRelease && !Input.GetMouseButton(0)) suppressMouseUntilRelease = false;
            if (!suppressMouseUntilRelease && Input.GetMouseButtonDown(0) && Camera.main != null
                && Rendering.GameScreen.TryScreenToGui(Input.mousePosition, out _)
                && (worldHud!=null && worldHud.TryRestoreStatusAt(Camera.main.ScreenToWorldPoint(Input.mousePosition))
                    || DarkSaver.World.ScenarioGuideNpc.TryInteractAt(Camera.main.ScreenToWorldPoint(Input.mousePosition),this)
                    || DarkSaver.World.TownServiceNpc.TryInteractAt(Camera.main.ScreenToWorldPoint(Input.mousePosition),this,
                        worldHud != null ? worldHud.GetComponent<TownServicesPanel>() : null)
                    || DarkSaver.World.ShopCounter2D.TryInteractAt(Camera.main.ScreenToWorldPoint(Input.mousePosition),this)
                    || DarkSaver.World.TownNpcInteraction.TryInteractAt(Camera.main.ScreenToWorldPoint(Input.mousePosition), this,
                    worldHud != null ? worldHud.GetComponent<TownServicesPanel>() : null))) return;
            var keyboard = new Vector2(
                (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D) ? 1f : 0f) -
                (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A) ? 1f : 0f),
                (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W) ? 1f : 0f) -
                (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S) ? 1f : 0f));
            if (keyboard.sqrMagnitude > 0f)
            {
                followingClickPath = false;
                waypoints.Clear();
                heldMouseCell = null;
                movement = Vector2.ClampMagnitude(keyboard, 1f);
            }
            else
            {
                if (!suppressMouseUntilRelease && Input.GetMouseButton(0)
                    && Rendering.GameScreen.TryScreenToGui(Input.mousePosition, out _) && Camera.main != null && ResolveMap())
                {
                    var destination = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    var cell = ground.WorldToCell(destination);
                    if (heldMouseCell != cell)
                    {
                        heldMouseCell = cell;
                        TryMoveToWorld(destination);
                    }
                }
                else heldMouseCell = null;
                movement = followingClickPath && waypointIndex < waypoints.Count
                    ? ((Vector2)(waypoints[waypointIndex] - transform.position)).normalized
                    : Vector2.zero;
            }
            if (directionalAnimator != null) directionalAnimator.SetMotion(movement);
            spriteRenderer.sortingOrder = 20000 - Mathf.RoundToInt(transform.position.y * 10f);
        }

        public bool TryMoveToWorld(Vector2 destination)
        {
            if (UiInputBlocked) return false;
            followingClickPath = false;
            waypoints.Clear();
            waypointIndex = 0;
            if (!ResolveMap() || !TilemapPathfinder.TryFind(ground, terrain, body.position, destination, waypoints))
                return false;
            followingClickPath = waypoints.Count > 0;
            return true;
        }

        private bool ResolveMap()
        {
            if (ground != null && terrain != null) return true;
            foreach (var tilemap in FindObjectsByType<Tilemap>())
            {
                if (tilemap.name == "Ground") ground = tilemap;
                else if (tilemap.name == "TerrainAttributes") terrain = tilemap;
            }
            return ground != null && terrain != null;
        }

        private void FixedUpdate()
        {
            if (UiInputBlocked) return;
            if(pendingNpcInteraction!=null && !followingClickPath)
            {
                var action=pendingNpcInteraction;StopWorldMotion();action();return;
            }
            worldMoveDebt+=Time.fixedDeltaTime;
            if(worldMoveDebt+.00001f<GameConstants.WorldMoveTickSeconds)return;
            worldMoveDebt=Mathf.Max(0,worldMoveDebt-GameConstants.WorldMoveTickSeconds);
            if (followingClickPath && waypointIndex < waypoints.Count)
            {
                if(!WorldMovementStep.TryAdvance(waypoints,waypointIndex,body.position,
                    GameConstants.PlayerMoveSpeed*GameConstants.WorldMoveTickSeconds,out var target,out int next)
                    || !RequestMove(target)){StopWorldMotion();return;}
                waypointIndex=next;
                if(waypointIndex==waypoints.Count){followingClickPath=false;movement=Vector2.zero;}
                return;
            }
            if(movement.sqrMagnitude>0 && !RequestMove(body.position+movement*(GameConstants.PlayerMoveSpeed*GameConstants.WorldMoveTickSeconds)))StopWorldMotion();
        }

        private void OnDisable()
        {
            pendingNpcInteraction=null;worldMoveDebt=0;
            movement = Vector2.zero;
            followingClickPath = false;
            waypoints.Clear();
            heldMouseCell = null;
            ground = null;
            terrain = null;
            if (directionalAnimator != null) directionalAnimator.SetMotion(Vector2.zero);
        }

        private static Sprite CreateMarkerSprite()
        {
            var texture = new Texture2D(16, 24, TextureFormat.RGBA32, false)
            {
                name = "Player Marker",
                filterMode = FilterMode.Point
            };
            var pixels = new Color32[16 * 24];
            for (var y = 0; y < 24; y++)
            for (var x = 0; x < 16; x++)
            {
                var inside = x >= 3 && x <= 12 && y >= 2 && y <= 21;
                pixels[y * 16 + x] = inside ? new Color32(40, 105, 220, 255) : new Color32(0, 0, 0, 0);
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 16, 24), new Vector2(.5f, .15f), 32f);
        }
    }
}
