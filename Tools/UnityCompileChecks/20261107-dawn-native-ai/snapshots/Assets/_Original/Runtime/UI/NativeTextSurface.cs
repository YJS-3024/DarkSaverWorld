using UnityEngine;
namespace DarkSaver.Original.UI
{
    // The original software font clips whole14px glyphs against the destination surface.
    [DisallowMultipleComponent,RequireComponent(typeof(RectTransform))]
    public sealed class NativeTextSurface : MonoBehaviour
    {
        private RectTransform surface;
        public RectTransform Surface=>surface!=null?surface:surface=(RectTransform)transform;
    }
}
