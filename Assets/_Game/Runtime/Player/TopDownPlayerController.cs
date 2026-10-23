using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using DarkSaver.UI;

namespace DarkSaver.Player
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(SpriteRenderer))]
    public sealed class TopDownPlayerController : MonoBehaviour
    {
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
        private bool suppressMouseUntilRelease;
        private WorldSessionHud worldHud;

        public bool UiInputBlocked => uiInputBlocked;

        public void SetUiInputBlocked(bool blocked)
        {
            if (uiInputBlocked == blocked) return;
            uiInputBlocked = blocked;
            suppressMouseUntilRelease = true;
            if (!blocked) return;
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
            if (spriteRenderer.sprite == null) spriteRenderer.sprite = CreateMarkerSprite();
        }

        private void Update()
        {
            if (uiInputBlocked) return;
            if (worldHud == null) worldHud = FindAnyObjectByType<WorldSessionHud>();
            if (Input.GetMouseButton(0) && worldHud != null && worldHud.BlocksWorldPointer(Input.mousePosition))
                suppressMouseUntilRelease = true;
            if (suppressMouseUntilRelease && !Input.GetMouseButton(0)) suppressMouseUntilRelease = false;
            if (!suppressMouseUntilRelease && Input.GetMouseButtonDown(0) && Camera.main != null
                && Rendering.GameScreen.TryScreenToGui(Input.mousePosition, out _)
                && DarkSaver.World.TownNpcInteraction.TryInteractAt(Camera.main.ScreenToWorldPoint(Input.mousePosition), this,
                    worldHud != null ? worldHud.GetComponent<TownServicesPanel>() : null)) return;
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
            if (uiInputBlocked) return false;
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
            if (uiInputBlocked) return;
            if (followingClickPath && waypointIndex < waypoints.Count)
            {
                var target = (Vector2)waypoints[waypointIndex];
                var delta = target - body.position;
                var step = GameConstants.PlayerMoveSpeed * Time.fixedDeltaTime;
                if (delta.magnitude <= step)
                {
                    body.MovePosition(target);
                    waypointIndex++;
                    if (waypointIndex == waypoints.Count)
                    {
                        followingClickPath = false;
                        movement = Vector2.zero;
                    }
                }
                else body.MovePosition(body.position + delta.normalized * step);
                return;
            }
            body.MovePosition(body.position + movement * (GameConstants.PlayerMoveSpeed * Time.fixedDeltaTime));
        }

        private void OnDisable()
        {
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
