using UnityEngine;
using UnityEngine.Tilemaps;
using DarkSaver.Rendering;

namespace DarkSaver.Player
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class WaterSubmergeOverlay : MonoBehaviour
    {
        [SerializeField] private Sprite[] surfaceFrames;
        [SerializeField] private TileBase[] waterTiles;
        [SerializeField] private Material surfaceMaterial;
        [SerializeField] private Material submergedBodyMaterial;
        [Tooltip("Original rendering ID only; not a job/sprite ID.")]
        [SerializeField] private int originalUnitId = 100;
        [Tooltip("Original unit rendering state. Do not map Combat.UnitLifeState into this field.")]
        [SerializeField] private int originalUnitState = 0;

        private SpriteRenderer characterRenderer;
        private SpriteRenderer surfaceRenderer;
        private Tilemap terrainAttributes;
        private Material originalBodyMaterial;
        private MaterialPropertyBlock bodyProperties;
        private static readonly int ClipEnabled = Shader.PropertyToID("_WaterClipEnabled");
        private static readonly int ClipY = Shader.PropertyToID("_WaterClipLocalY");

        public bool IsSubmerged => surfaceRenderer != null && surfaceRenderer.enabled;

        public void Configure(Sprite[] frames, TileBase[] tiles, Material material = null, Material bodyMaterial = null)
        {
            // Restore using the old material before replacing its identity.
            RestoreBody();
            if (surfaceRenderer != null) surfaceRenderer.enabled = false;
            surfaceFrames = frames;
            waterTiles = tiles;
            surfaceMaterial = material;
            submergedBodyMaterial = bodyMaterial;
            if (surfaceRenderer != null) surfaceRenderer.sharedMaterial = material;
        }

        public void SetOriginalRenderState(int unitId, int state)
        {
            originalUnitId = unitId;
            originalUnitState = state;
            if (!OriginalRenderRules.ShouldShowWaterSubmerge(true, unitId, state) && surfaceRenderer != null)
            {
                surfaceRenderer.enabled = false;
                RestoreBody();
            }
        }

        private void OnDisable()
        {
            if (surfaceRenderer != null) surfaceRenderer.enabled = false;
            RestoreBody();
        }

        private void Awake()
        {
            characterRenderer = GetComponent<SpriteRenderer>();
            bodyProperties = new MaterialPropertyBlock();
            var surface = new GameObject("Water Surface", typeof(SpriteRenderer));
            surface.transform.SetParent(transform, false);
            surface.transform.localPosition = Vector3.zero;
            surfaceRenderer = surface.GetComponent<SpriteRenderer>();
            if (surfaceMaterial != null) surfaceRenderer.sharedMaterial = surfaceMaterial;
            surfaceRenderer.enabled = false;
        }

        private void LateUpdate()
        {
            if (terrainAttributes == null)
            {
                foreach (var map in FindObjectsByType<Tilemap>())
                {
                    if (map.name != "TerrainAttributes") continue;
                    terrainAttributes = map;
                    break;
                }
            }

            var submerged = false;
            if (terrainAttributes != null && waterTiles != null)
            {
                var tile = terrainAttributes.GetTile(terrainAttributes.WorldToCell(transform.position));
                foreach (var waterTile in waterTiles)
                {
                    if (tile == null || tile != waterTile) continue;
                    submerged = true;
                    break;
                }
            }

            var body = characterRenderer.sprite;
            surfaceRenderer.enabled = OriginalRenderRules.ShouldShowWaterSubmerge(submerged, originalUnitId, originalUnitState)
                && characterRenderer.enabled && surfaceFrames != null && surfaceFrames.Length >= 2
                && surfaceFrames[0] != null && surfaceFrames[1] != null
                && surfaceMaterial != null && submergedBodyMaterial != null && body != null
                && OriginalRenderRules.WaterBodyVisiblePixels((int)body.rect.height) > 0;
            if (!surfaceRenderer.enabled)
            {
                RestoreBody();
                surfaceRenderer.transform.localPosition = Vector3.zero;
                return;
            }
            surfaceRenderer.sprite = surfaceFrames[OriginalRenderRules.WaterFrame(
                (long)(Time.realtimeSinceStartupAsDouble * 1000d))];
            if (body != null && submergedBodyMaterial != null && OriginalRenderRules.WaterBodyVisiblePixels((int)body.rect.height) > 0)
            {
                if (originalBodyMaterial == null) originalBodyMaterial = characterRenderer.sharedMaterial;
                characterRenderer.sharedMaterial = submergedBodyMaterial;
                characterRenderer.GetPropertyBlock(bodyProperties);
                bodyProperties.SetFloat(ClipEnabled, 1f);
                bodyProperties.SetFloat(ClipY, body.bounds.max.y - OriginalRenderRules.WaterBodyVisiblePixels((int)body.rect.height) / body.pixelsPerUnit);
                characterRenderer.SetPropertyBlock(bodyProperties);
                var water = surfaceRenderer.sprite;
                // +5px belongs only to the 64px path. Normal 32px water uses the body top-left.
                var inset = body.rect.height == 64f ? 16f + OriginalRenderRules.WaterOverlayYOffset : 0f;
                surfaceRenderer.transform.localPosition = new Vector3(0f, body.bounds.max.y
                    - inset / body.pixelsPerUnit
                    - (water.rect.height - water.pivot.y) / water.pixelsPerUnit, 0f);
            }
            else RestoreBody();
            surfaceRenderer.sortingOrder = characterRenderer.sortingOrder + 1;
            surfaceRenderer.sortingLayerID = characterRenderer.sortingLayerID;
        }

        private void RestoreBody()
        {
            if (characterRenderer == null || originalBodyMaterial == null) return;
            if (characterRenderer.sharedMaterial == submergedBodyMaterial)
                characterRenderer.sharedMaterial = originalBodyMaterial;
            characterRenderer.GetPropertyBlock(bodyProperties);
            bodyProperties.SetFloat(ClipEnabled, 0f);
            characterRenderer.SetPropertyBlock(bodyProperties);
            originalBodyMaterial = null;
        }
    }
}
