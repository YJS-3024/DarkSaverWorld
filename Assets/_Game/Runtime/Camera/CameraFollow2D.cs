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
            view.orthographicSize = GameConstants.WorldCameraOrthographicSize;
            minimum = new Vector2(GameConstants.WorldCameraOrthographicSize * view.aspect,
                GameConstants.WorldCameraOrthographicSize);
            maximum = mapSize - minimum;
        }

        private void LateUpdate()
        {
            if (target == null) return;
            var desired = new Vector3(
                Mathf.Clamp(target.position.x, minimum.x, maximum.x),
                Mathf.Clamp(target.position.y, minimum.y, maximum.y),
                transform.position.z);
            transform.position = Vector3.Lerp(transform.position, desired,
                1f - Mathf.Exp(-GameConstants.CameraFollowSmoothing * Time.deltaTime));
        }
    }
}
