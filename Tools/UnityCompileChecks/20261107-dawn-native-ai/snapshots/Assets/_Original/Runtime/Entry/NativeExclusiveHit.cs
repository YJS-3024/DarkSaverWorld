using UnityEngine;
using UnityEngine.UI;

namespace DarkSaver.Original.Entry
{
    public sealed class NativeExclusiveHit : MonoBehaviour, ICanvasRaycastFilter
    {
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            var rect = (RectTransform)transform;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPoint, eventCamera, out var point)
                && NativeEntryLayout.ContainsExclusive(rect.rect, point);
        }
    }
}
