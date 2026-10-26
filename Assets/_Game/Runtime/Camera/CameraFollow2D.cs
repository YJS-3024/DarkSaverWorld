using UnityEngine;

namespace DarkSaver.CameraSystem
{
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector2 minimum;
        [SerializeField] private Vector2 maximum;

        private void Awake()
        {
            GetComponent<Camera>().orthographicSize = GameConstants.WorldCameraOrthographicSize;
            Rendering.GameScreen.ApplyCamera(GetComponent<Camera>());
        }

        public void Configure(Transform followTarget, Vector2 mapSize)
        {
            target = followTarget;
            var view = GetComponent<Camera>();
            Rendering.GameScreen.ApplyCamera(view);
            float size=Mathf.Min(GameConstants.WorldCameraOrthographicSize,Mathf.Max(.5f,mapSize.y*.5f),Mathf.Max(.5f,mapSize.x/(2*view.aspect)));
            view.orthographicSize = size;
            minimum = new Vector2(size * view.aspect,size);
            maximum = mapSize - minimum;
            // Configure is called after spawning/teleporting into a region.
            // Follow smoothing must not reveal movement from the previous map.
            if (target != null) transform.position = DesiredPosition();
        }

        private Vector3 DesiredPosition() => new Vector3(
            Mathf.Clamp(target.position.x, minimum.x, maximum.x),
            Mathf.Clamp(target.position.y, minimum.y, maximum.y),
            transform.position.z);

        private void LateUpdate()
        {
            if (target == null) return;
            transform.position = Vector3.Lerp(transform.position, DesiredPosition(),
                1f - Mathf.Exp(-GameConstants.CameraFollowSmoothing * Time.deltaTime));
        }
    }
}
