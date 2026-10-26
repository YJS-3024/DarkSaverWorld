// Transitional query helpers for existing visual assertions. Every value is read from
// actual RectTransform/uGUI components; no UI Toolkit document or event tree is created.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DarkSaver.UI;
namespace DarkSaver.EditorTools.CanvasTesting
{
    public enum DisplayStyle{None,Flex} public enum Overflow{Visible,Hidden} public enum PickingMode{Ignore,Position}
    public enum WhiteSpace{Normal,NoWrap} public enum TextOverflow{Clip,Ellipsis} public enum ScrollerVisibility{Hidden}
    public struct Value<T>{public T value;public Value(T v){value=v;}}
    public struct Length{public float value;public Length(float v){value=v;}}
    public struct Background{public Texture2D texture;public Sprite sprite;}
    public sealed class Styles
    {
        private readonly VisualElement node;public Styles(VisualElement n){node=n;}
        RectTransform rt=>node.Transform;
        Text text=>rt.GetComponent<Text>()??rt.GetComponentInChildren<Text>(true);
        RawImage image=>rt.GetComponent<RawImage>();
        public Value<Length> left=>new(new Length(rt.anchoredPosition.x));public Value<Length> top=>new(new Length(-rt.anchoredPosition.y));
        public Value<Length> width=>new(new Length(rt.sizeDelta.x));public Value<Length> height=>new(new Length(rt.sizeDelta.y));
        public Value<DisplayStyle> display=>new(rt.gameObject.activeSelf?DisplayStyle.Flex:DisplayStyle.None);
        public Value<Overflow> overflow=>new(rt.GetComponent<RectMask2D>()!=null?Overflow.Hidden:Overflow.Visible);
        public Value<float> opacity=>new(image!=null?image.color.a:1);
        public Value<Color> color=>new(text!=null?text.color:Color.clear);
        public Value<Color> backgroundColor=>new(image!=null&&image.texture==null?image.color:Color.clear);
        public Value<Color> unityBackgroundImageTintColor=>new(image!=null?image.color:Color.white);
        public Value<Background> backgroundImage=>new(new Background{texture=image?.texture as Texture2D,sprite=SpriteFor(image)});
        static Sprite SpriteFor(RawImage image)
        {
            if(image?.texture==null)return null;
            return Resources.FindObjectsOfTypeAll<Sprite>().FirstOrDefault(s=>s.texture==image.texture&&SameRect(s,image.uvRect));
        }
        static bool SameRect(Sprite s,Rect uv){var r=s.textureRect;return Vector2.Distance(r.position,new Vector2(uv.x*s.texture.width,uv.y*s.texture.height))<.01f&&Vector2.Distance(r.size,new Vector2(uv.width*s.texture.width,uv.height*s.texture.height))<.01f;}
        public Value<Length> fontSize=>new(new Length(text!=null?text.fontSize:0));
        public Value<TextAnchor> unityTextAlign=>new(text!=null?text.alignment:TextAnchor.UpperLeft);
        public Value<WhiteSpace> whiteSpace=>new(text!=null&&text.horizontalOverflow==HorizontalWrapMode.Overflow?WhiteSpace.NoWrap:WhiteSpace.Normal);
        public Value<TextOverflow> textOverflow=>new(TextOverflow.Clip);
        float Border=>rt.GetComponent<Outline>() is Outline o&&o.enabled?Mathf.Abs(o.effectDistance.x):0;
        public Value<float> borderTopWidth=>new(Border);public Value<float> borderLeftWidth=>new(Border);
        public Value<Length> borderTopLeftRadius=>new(new Length(0));public Value<Length> paddingRight=>new(new Length(0));
    }
    public class VisualElement
    {
        public RectTransform Transform{get;internal set;}
        public Styles style=>new(this);
        public VisualElement rootVisualElement=>this;
        public VisualElement panelSettings=>this;
        public float sortingOrder=>Transform.GetComponent<Canvas>().sortingOrder;
        public VisualElement parent=>CanvasProbe.Wrap(Transform.parent as RectTransform);
        public int childCount=>Transform.childCount;
        public string name=>Transform.name;
        public string tooltip=>Transform.GetComponent<CanvasUiEvents>()?.Tooltip??"";
        public bool enabledSelf=>Transform.GetComponent<Selectable>() is Selectable s?s.interactable:Transform.gameObject.activeSelf;
        public void SetEnabled(bool value){if(Transform.GetComponent<Selectable>() is Selectable s)s.interactable=value;}
        public bool enabledInHierarchy=>enabledSelf&&Transform.gameObject.activeInHierarchy;
        public PickingMode pickingMode=>Transform.GetComponent<Graphic>() is Graphic g&&g.raycastTarget?PickingMode.Position:PickingMode.Ignore;
        public VisualElement Q(string name)=>CanvasProbe.Wrap(Transform.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(t=>t.name==name));
        public T Q<T>(string name)where T:VisualElement=>Q(name) as T;
        public IEnumerable<VisualElement> Children()=>Enumerable.Range(0,Transform.childCount).Select(i=>CanvasProbe.Wrap(Transform.GetChild(i) as RectTransform));
        public int IndexOf(VisualElement child)=>child.Transform.GetSiblingIndex();
        public Vector2 LocalToWorld(Vector2 p){var point=RectTransformUtility.WorldToScreenPoint(null,Transform.TransformPoint(new Vector3(p.x,-p.y)));return new Vector2(point.x,Screen.height-point.y);}
        public bool Contains(VisualElement child)=>child!=null && (child.Transform==Transform||child.Transform.IsChildOf(Transform));
        public Rect layout=>new Rect(Transform.anchoredPosition,Transform.sizeDelta);
        public Rect worldBound {get{var corners=new Vector3[4];Transform.GetWorldCorners(corners);var a=RectTransformUtility.WorldToScreenPoint(null,corners[1]);var b=RectTransformUtility.WorldToScreenPoint(null,corners[3]);return new Rect(a.x,Screen.height-a.y,b.x-a.x,a.y-b.y);}}
        public ResolvedStyles resolvedStyle=>new(this);
        public CanvasPanel panel=>Transform.gameObject.activeInHierarchy?new CanvasPanel(this):null;
        public Vector2 WorldToLocal(Vector2 p){RectTransformUtility.ScreenPointToLocalPointInRectangle(Transform,new Vector2(p.x,Screen.height-p.y),null,out var local);return new Vector2(local.x,-local.y);}
        public void SendEvent(UiEvent e)
        {
            var data=new PointerEventData(EventSystem.current){position=new Vector2(e.position.x,Screen.height-e.position.y),button=PointerEventData.InputButton.Left,pointerId=-1,eligibleForClick=true};
            if(e is PointerEnterEvent)ExecuteEvents.Execute(Transform.gameObject,data,ExecuteEvents.pointerEnterHandler);
            else if(e is PointerLeaveEvent)ExecuteEvents.Execute(Transform.gameObject,data,ExecuteEvents.pointerExitHandler);
            else if(e is PointerDownEvent)ExecuteEvents.Execute(Transform.gameObject,data,ExecuteEvents.pointerDownHandler);
            else if(e is PointerMoveEvent)ExecuteEvents.Execute(Transform.gameObject,data,ExecuteEvents.dragHandler);
            else if(e is PointerUpEvent){ExecuteEvents.Execute(Transform.gameObject,data,ExecuteEvents.pointerUpHandler);if(Transform.GetComponent<CanvasUiEvents>()?.SuppressClick!=true)ExecuteEvents.Execute(Transform.gameObject,data,ExecuteEvents.pointerClickHandler);}
            else if(e is NavigationSubmitEvent)ExecuteEvents.Execute(Transform.gameObject,data,ExecuteEvents.submitHandler);
        }
    }
    public class Button:VisualElement{public string text=>Transform.GetComponentInChildren<Text>(true)?.text??"";}
    public class Label:VisualElement{public string text=>Transform.GetComponent<Text>().text;}
    public class TextField:VisualElement
    {InputField field=>Transform.GetComponent<InputField>();public bool isPasswordField=>field.contentType==InputField.ContentType.Password;public char maskChar=>field.asteriskChar;public int maxLength=>field.characterLimit;public string value{get=>field.text;set=>field.text=value;}}
    public class ScrollView:VisualElement
    {ScrollRect scroll=>Transform.GetComponent<ScrollRect>();public VisualElement contentContainer=>CanvasProbe.Wrap(scroll.content);public VisualElement contentViewport=>CanvasProbe.Wrap(scroll.viewport);public ScrollerVisibility verticalScrollerVisibility=>ScrollerVisibility.Hidden;public Vector2 scrollOffset{get=>scroll.content.anchoredPosition;set=>scroll.content.anchoredPosition=value;}}
    public sealed class ResolvedStyles
    {private readonly VisualElement node;public ResolvedStyles(VisualElement n){node=n;}public DisplayStyle display=>node.style.display.value;public Background backgroundImage=>node.style.backgroundImage.value;public float width=>node.Transform.sizeDelta.x;public float height=>node.Transform.sizeDelta.y;}
    public sealed class CanvasPanel
    {
        private readonly VisualElement node;public CanvasPanel(VisualElement n){node=n;}
        public VisualElement Pick(Vector2 point)
        {
            var canvas=node.Transform.GetComponentInParent<Canvas>();var raycaster=canvas.GetComponent<GraphicRaycaster>();
            var hits=new List<RaycastResult>();raycaster.Raycast(new PointerEventData(EventSystem.current){position=new Vector2(point.x,Screen.height-point.y)},hits);
            return hits.Count==0?null:CanvasProbe.Wrap(hits[0].gameObject.GetComponent<RectTransform>());
        }
    }
    public static class CanvasProbe
    {
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<RectTransform,VisualElement> cache=new();
        public static VisualElement For(InGameUi ui)=>Wrap(ui.GetComponent<RectTransform>());
        public static VisualElement Wrap(RectTransform rt)=>rt==null?null:cache.GetValue(rt,t=>
        {VisualElement result=t.GetComponent<InputField>()!=null?new TextField():t.GetComponent<ScrollRect>()!=null?new ScrollView():t.GetComponent<UnityEngine.UI.Button>()!=null?new Button():t.GetComponent<Text>()!=null?new Label():new VisualElement();result.Transform=t;return result;});
    }
    public static class UQueryExtensions
    {public static T Q<T>(VisualElement root,string name)where T:VisualElement=>root.Q<T>(name);public static VisualElement Q(VisualElement root,string name)=>root.Q(name);}
    public class UiEvent:IDisposable{public object target;public Vector2 position;public void Dispose(){} }
    public class PointerDownEvent:UiEvent{public static PointerDownEvent GetPooled(Event e)=>new(){position=e.mousePosition};}
    public class PointerMoveEvent:UiEvent{public static PointerMoveEvent GetPooled(Event e)=>new(){position=e.mousePosition};}
    public class PointerUpEvent:UiEvent{public static PointerUpEvent GetPooled(Event e)=>new(){position=e.mousePosition};}
    public class PointerEnterEvent:UiEvent{public static PointerEnterEvent GetPooled()=>new();public static PointerEnterEvent GetPooled(Event e)=>new(){position=e.mousePosition};public static PointerEnterEvent GetPooled(UiEvent e)=>new(){position=e.position};}
    public class PointerLeaveEvent:UiEvent{public static PointerLeaveEvent GetPooled()=>new();public static PointerLeaveEvent GetPooled(Event e)=>new(){position=e.mousePosition};public static PointerLeaveEvent GetPooled(UiEvent e)=>new(){position=e.position};}
    public class NavigationSubmitEvent:UiEvent{public static NavigationSubmitEvent GetPooled()=>new();}
}
