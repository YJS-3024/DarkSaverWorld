using UnityEngine;
namespace DarkSaver.Rendering
{
    [DefaultExecutionOrder(-10000)]
    public sealed class GameScreenViewport : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (FindAnyObjectByType<GameScreenViewport>() != null) return;
            var go = new GameObject("Logical 640x480 Viewport"); DontDestroyOnLoad(go); go.AddComponent<GameScreenViewport>();
        }
        void LateUpdate() => GameScreen.ApplyCamera(Camera.main);
        void OnGUI()
        {
            var oldMatrix = GUI.matrix; var oldColor = GUI.color; int oldDepth = GUI.depth;
            GUI.matrix = Matrix4x4.identity; GUI.depth = -10000; GUI.color = Color.black;
            var v = GameScreen.Viewport;
            if (v.x > 0)
            {
                GUI.DrawTexture(new Rect(0, 0, v.x, Screen.height), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(v.xMax, 0, Screen.width - v.xMax, Screen.height), Texture2D.whiteTexture);
            }
            if (v.y > 0)
            {
                GUI.DrawTexture(new Rect(0, 0, Screen.width, v.y), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(0, v.yMax, Screen.width, Screen.height - v.yMax), Texture2D.whiteTexture);
            }
            GUI.color = oldColor; GUI.matrix = oldMatrix; GUI.depth = oldDepth;
        }
    }
}
