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
        private ServerManager protocol;
        private System.IDisposable movementBinding;
        private bool RequestMove(Vector2 position)
        {
            if(protocol==null)
            {
                protocol=GetComponent<ServerManager>();if(protocol==null)protocol=gameObject.AddComponent<ServerManager>();
                movementBinding=protocol.RegisterRequestHandler<WorldMoveRequest>(HandleMove);
                protocol.ResponseReceived+=ReceiveMove;
            }
            return protocol.TryRequest(new WorldMoveRequest(position.x,position.y),out _);
        }
        private LocalCommandResponse HandleMove(WorldMoveRequest request)
        {
            var target=new Vector2(request.X,request.Y);
            if(!isActiveAndEnabled || UiInputBlocked || !body.simulated || float.IsNaN(request.X) || float.IsNaN(request.Y)
                || float.IsInfinity(request.X) || float.IsInfinity(request.Y)
                || Vector2.Distance(body.position,target)>GameConstants.PlayerMoveSpeed*Time.fixedDeltaTime+.001f)
                return new WorldMoveNotification(reason:"이동할 수 없는 상태 또는 거리입니다.");
            // Collision resolution remains the native Unity Rigidbody2D solver after approval.
            // The packet does not bypass walls by assigning the Transform.
            return new WorldMoveNotification(true,request.X,request.Y);
        }
        private void ReceiveMove(ServerPacket packet)
        {if(packet is WorldMoveNotification result && result.Accepted)body.MovePosition(new Vector2(result.X,result.Y));}
        private void OnDestroy()
        {movementBinding?.Dispose();if(protocol!=null)protocol.ResponseReceived-=ReceiveMove;}
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

        public void StopForSceneTravel() { StopWorldMotion(); suppressMouseUntilRelease = true; }

        private void StopWorldMotion()
        {
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
            if (UiInputBlocked) return;
            if (worldHud == null) worldHud = FindAnyObjectByType<WorldSessionHud>();
            if (Input.GetMouseButton(0) && FirstPlayTutorial.BlocksPointer(Input.mousePosition)) suppressMouseUntilRelease = true;
            if (Input.GetMouseButton(0) && worldHud != null && worldHud.BlocksWorldPointer(Input.mousePosition))
                suppressMouseUntilRelease = true;
            if (suppressMouseUntilRelease && !Input.GetMouseButton(0)) suppressMouseUntilRelease = false;
            if (!suppressMouseUntilRelease && Input.GetMouseButtonDown(0) && Camera.main != null
                && Rendering.GameScreen.TryScreenToGui(Input.mousePosition, out _)
                && (DarkSaver.World.ShopCounter2D.TryInteractAt(Camera.main.ScreenToWorldPoint(Input.mousePosition),this)
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
            if (followingClickPath && waypointIndex < waypoints.Count)
            {
                var target = (Vector2)waypoints[waypointIndex];
                var delta = target - body.position;
                var step = GameConstants.PlayerMoveSpeed * Time.fixedDeltaTime;
                if (delta.magnitude <= step)
                {
                    if(!RequestMove(target)){StopWorldMotion();return;}
                    waypointIndex++;
                    if (waypointIndex == waypoints.Count)
                    {
                        followingClickPath = false;
                        movement = Vector2.zero;
                    }
                }
                else if(!RequestMove(body.position+delta.normalized*step))StopWorldMotion();
                return;
            }
            if(movement.sqrMagnitude>0 && !RequestMove(body.position+movement*(GameConstants.PlayerMoveSpeed*Time.fixedDeltaTime)))StopWorldMotion();
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
