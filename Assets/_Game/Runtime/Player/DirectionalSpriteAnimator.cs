using UnityEngine;

namespace DarkSaver.Player
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class DirectionalSpriteAnimator : MonoBehaviour
    {
        [SerializeField] private Sprite[] up;
        [SerializeField] private Sprite[] down;
        [SerializeField] private Sprite[] left;
        [SerializeField] private Sprite[] right;

        private SpriteRenderer spriteRenderer;
        private Sprite[] current;
        private Vector2 facing = Vector2.down;
        private bool moving;
        private readonly System.Collections.Generic.Dictionary<Sprite,Sprite> centeredFrames=new();
        public Vector2 Facing => facing;
        public bool IsMoving => moving;
        // Native status source: (32,32)-(64,64), or (64,64)-(128,128)
        // for 64px units. Both are row 1, column 1 of the converted 3-column sheet.
        public Sprite StatusPicture => down != null && down.Length > 1 ? down[1] : null;

        public bool HasCompleteDirections => Complete(up) && Complete(down) && Complete(left) && Complete(right);
        private static bool Complete(Sprite[] frames)
        {
            if (frames == null || frames.Length == 0) return false;
            foreach (var frame in frames) if (frame == null) return false;
            return true;
        }

        // Copy only sprite configuration, never movement/control components or shared mutable arrays.
        public bool TryCopyConfigurationTo(DirectionalSpriteAnimator target)
        {
            if (target == null || target == this || !HasCompleteDirections) return false;
            target.Configure((Sprite[])up.Clone(), (Sprite[])down.Clone(), (Sprite[])left.Clone(), (Sprite[])right.Clone());
            target.facing = Vector2.down; target.moving = false; target.PresentFrame(0); return true;
        }

        // Visual cadence only; never changes world position, velocity or facing.
        public void SetMovingPresentation(bool value) => moving = value;

        // Screen-facing axes shared by movement and native combat presentation.
        // Facing alone does not start movement or imply attack animation frames.
        public bool TrySetFacing(Vector2 direction)
        {
            if (float.IsNaN(direction.x) || float.IsNaN(direction.y)
                || float.IsInfinity(direction.x) || float.IsInfinity(direction.y)
                || direction.sqrMagnitude <= .001f) return false;
            facing = Mathf.Abs(direction.x) > .0001f ? (direction.x < 0f ? Vector2.left : Vector2.right)
                : direction.y < 0f ? Vector2.down : Vector2.up;
            return true;
        }

        public void Configure(Sprite[] upFrames, Sprite[] downFrames, Sprite[] leftFrames, Sprite[] rightFrames)
        {
            up = upFrames;
            down = downFrames;
            left = leftFrames;
            right = rightFrames;
        }

        public void SetMotion(Vector2 motion)
        {
            moving = motion.sqrMagnitude > .001f;
            if (moving)
            {
                // Original diagonal movement uses the horizontal facing row.
                if (Mathf.Abs(motion.x) > .0001f) facing = motion.x < 0f ? Vector2.left : Vector2.right;
                else facing = motion.y < 0f ? Vector2.down : Vector2.up;
            }
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            current = down;
        }

        private void Update()
        {
            PresentFrame((long)(Time.realtimeSinceStartupAsDouble * 1000d));
        }

        public void PresentFrame(long elapsedMilliseconds)
        {
            // Extracted Left/Right names are opposite to the original screen-facing rows.
            current = facing == Vector2.up ? up : facing == Vector2.left ? right : facing == Vector2.right ? left : down;
            if (current == null || current.Length == 0) return;
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            var frame = FrameAtMilliseconds(elapsedMilliseconds, current.Length, moving);
            spriteRenderer.sprite = GridCenteredFrame(current[frame]);
        }

        private Sprite GridCenteredFrame(Sprite source)
        {
            if(source==null || (source.pivot-source.rect.size*.5f).sqrMagnitude<.001f)return source;
            if(centeredFrames.TryGetValue(source,out var centered))return centered;
            // Root transforms, colliders and grid occupancy stay at tile centres.
            // Early player sheets used a foot pivot while scenario actors used a centre pivot.
            centered=Sprite.Create(source.texture,source.rect,new Vector2(.5f,.5f),source.pixelsPerUnit,0,SpriteMeshType.FullRect);
            centered.name=source.name;centeredFrames.Add(source,centered);return centered;
        }
        private void OnDestroy()
        {
            foreach(var sprite in centeredFrames.Values)
                if(sprite!=null){if(Application.isPlaying)Destroy(sprite);else DestroyImmediate(sprite);}
            centeredFrames.Clear();
        }

        public static int FrameAtMilliseconds(long elapsedMilliseconds, int frameCount, bool isMoving)
        {
            if (frameCount <= 0) return -1;
            var interval = System.Math.Max(1, isMoving
                ? GameConstants.CharacterMoveFrameMilliseconds : GameConstants.CharacterIdleFrameMilliseconds);
            var cycleLength = System.Math.Max(1L, frameCount * 2L - 2L);
            var phase = (System.Math.Max(0L, elapsedMilliseconds) / interval) % cycleLength;
            return (int)(phase < frameCount ? phase : cycleLength - phase);
        }
    }
}
