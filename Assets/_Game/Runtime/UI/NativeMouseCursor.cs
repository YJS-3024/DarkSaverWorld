using UnityEngine;
namespace DarkSaver.UI
{
    // Saver.exe WM_SETCURSOR loads group148; first32px image has hotspot0,0.
    public sealed class NativeMouseCursor:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var image=Resources.Load<Texture2D>("UI/NativeCursor");
            if(image!=null)Cursor.SetCursor(image,Vector2.zero,CursorMode.Auto);
        }
    }
}
