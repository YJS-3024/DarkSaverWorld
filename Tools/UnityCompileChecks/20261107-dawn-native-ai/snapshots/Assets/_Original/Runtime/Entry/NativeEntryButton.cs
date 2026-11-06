using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DarkSaver.Original.Entry
{
    [RequireComponent(typeof(Button))]
    public sealed class NativeEntryButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private RawImage picture;
        private Texture atlas;
        private Rect normal, hover, pressed;
        private bool over, down;
        public void Configure(RawImage target, Texture texture, Rect normalRect, Rect hoverRect, Rect pressedRect)
        { picture = target; atlas = texture; normal = normalRect; hover = hoverRect; pressed = pressedRect; Refresh(); }
        public void OnPointerEnter(PointerEventData e) { over = true; Refresh(); }
        public void OnPointerExit(PointerEventData e) { over = false; down = false; Refresh(); }
        public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) { down = true; Refresh(); } }
        public void OnPointerUp(PointerEventData e) { down = false; Refresh(); }
        private void OnDisable() { over = down = false; Refresh(); }
        private void Refresh()
        {
            if (picture == null || atlas == null) return;
            var rect = over && GetComponent<Button>().IsInteractable() ? down ? pressed : hover : normal;
            picture.texture = atlas; picture.uvRect = NativeEntryLayout.Uv(atlas, rect);
            picture.rectTransform.sizeDelta = rect.size;
        }
    }
}
