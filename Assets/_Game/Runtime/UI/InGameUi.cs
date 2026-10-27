using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace DarkSaver.UI
{
    // Canvas/uGUI renderer. All public coordinates remain original 640x480 top-left pixels.
    public sealed partial class InGameUi : MonoBehaviour
    {
        private readonly Dictionary<string,RectTransform> elements=new();
        private readonly HashSet<string> used=new();
        private readonly Dictionary<string,Action> actions=new();
        private readonly Dictionary<string,Action<string>> textChanges=new();
        private readonly List<string> inputOrder=new();
        private readonly Dictionary<string,(string Text,string[] Rows)> resultRows=new();
        private Behaviour owner;
        private Canvas canvas;
        private RectTransform root;
        private Font font;
        private int normalSortingOrder;
        private string hoveredStatKey;
        private static readonly Color PaperInk=new(.17f,.09f,.035f),Ink=new(.91f,.88f,.77f);
        public RectTransform Root=>root;
        public Canvas Canvas=>canvas;
        public T Find<T>(string key) where T:Component=>elements.TryGetValue(key,out var element)?element.GetComponent<T>():null;
        public Rect GetRect(string key)=>elements.TryGetValue(key,out var e)?new Rect(e.anchoredPosition.x,-e.anchoredPosition.y,e.sizeDelta.x,e.sizeDelta.y):default;
        public static InGameUi Ensure(ref InGameUi ui,Behaviour owner,int order=20)
        {
            if(ui!=null)return ui;
            var child=new GameObject("InGameUI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            child.transform.SetParent(owner.transform,false);ui=child.AddComponent<InGameUi>();ui.owner=owner;
            ui.canvas=child.GetComponent<Canvas>();ui.canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            ui.canvas.sortingOrder=order;ui.normalSortingOrder=order;ui.canvas.pixelPerfect=true;
            var scaler=child.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;
            ui.font=Font.CreateDynamicFontFromOSFont(new[]{"Malgun Gothic","Arial"},16);
            ui.root=NewRect("LogicalScreen",child.transform,new Rect(0,0,Rendering.GameScreen.Width,Rendering.GameScreen.Height));
            Clip(ui.root);
            ui.SyncViewport();
            if(Application.isPlaying && EventSystem.current==null)
            {
                var events=new GameObject("Runtime UI EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
                DontDestroyOnLoad(events);
            }
            return ui;
        }
        private static RectTransform NewRect(string name,Transform parent,Rect rect)
        {
            var go=new GameObject(name,typeof(RectTransform));var rt=go.GetComponent<RectTransform>();rt.SetParent(parent,false);Place(rt,rect);return rt;
        }
        private static void Place(RectTransform rt,Rect rect)
        {rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(rect.x,-rect.y);rt.sizeDelta=rect.size;}
        private RectTransform Node(string key,Rect rect,RectTransform parent=null)
        {
            if(!elements.TryGetValue(key,out var node)){node=NewRect(key,parent!=null?parent:root,rect);elements.Add(key,node);}
            else {var wanted=parent!=null?parent:root;if(node.parent!=wanted)node.SetParent(wanted,false);Place(node,rect);}
            // Reused graphics must follow this frame's draw order too. Otherwise a
            // raised background covers portraits/text created in an earlier frame.
            node.SetAsLastSibling();
            used.Add(key);node.gameObject.SetActive(true);return node;
        }
        private static T Component<T>(RectTransform node) where T:UnityEngine.Component
        {
            // Unity can return an Editor missing-component wrapper. CLR ?? does not
            // recognize Unity's null semantics and would return that invalid wrapper.
            var component=node.GetComponent<T>();
            return component!=null?component:node.gameObject.AddComponent<T>();
        }
        private RawImage Graphic(RectTransform node,Texture texture,Color color,bool raycast=false)
        {var image=Component<RawImage>(node);image.texture=texture;image.color=color;image.uvRect=new Rect(0,0,1,1);image.raycastTarget=raycast;return image;}
        private static void Clip(RectTransform node)=>Component<RectMask2D>(node);
        private bool Active(string key)=>owner!=null && owner.isActiveAndEnabled && root.gameObject.activeInHierarchy && used.Contains(key) && elements[key].gameObject.activeInHierarchy;
        private void Update()
        {
            if(owner==null || !owner.isActiveAndEnabled){Hide();return;}
            if(Input.GetKeyDown(KeyCode.Tab))MoveInputFocus(Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift));
        }
        public bool MoveInputFocus(bool reverse=false)
        {
            var system=EventSystem.current;
            if(system==null || root==null || !root.gameObject.activeInHierarchy || owner==null || !owner.isActiveAndEnabled)return false;
            var selected=system.currentSelectedGameObject;
            if(selected!=null && !selected.transform.IsChildOf(root))return false;
            var fields=new List<InputField>();
            foreach(var key in inputOrder)
            {
                var field=Find<InputField>(key);
                if(Active(key) && field!=null && field.isActiveAndEnabled && field.IsInteractable() && !field.readOnly)fields.Add(field);
            }
            if(fields.Count==0)return false;
            int current=fields.FindIndex(f=>f.gameObject==selected);
            int next=current<0?(reverse?fields.Count-1:0):(current+(reverse?-1:1)+fields.Count)%fields.Count;
            fields[next].Select();fields[next].ActivateInputField();return true;
        }
        private void SyncViewport()
        {
            var v=Rendering.GameScreen.Viewport;root.anchoredPosition=new Vector2(v.x,-v.y);
            float scale=v.width/Rendering.GameScreen.Width;root.localScale=new Vector3(scale,scale,1);
        }
        public void Begin(){canvas.sortingOrder=normalSortingOrder;SyncViewport();used.Clear();root.gameObject.SetActive(true);}
        public void Hide()
        {
            CancelItemDrags();hoveredStatKey=null;resultRows.Clear();
            if(root!=null)root.gameObject.SetActive(false);
            ReleaseHiddenSelection();
        }
        private void ReleaseHiddenSelection()
        {
            var system=EventSystem.current;var selected=system!=null?system.currentSelectedGameObject:null;
            if(root!=null && selected!=null && selected.transform.IsChildOf(root) && !selected.activeInHierarchy)
                system.SetSelectedGameObject(null);
        }
        public void End()
        {
            DrawItemDrag();
            if(hoveredStatKey!=null && used.Contains(hoveredStatKey))
            {
                var r=GetRect(hoveredStatKey);Panel("native-stat-detail",new Rect(Mathf.Clamp(r.x+8,0,380),Mathf.Clamp(r.y-60,0,424),260,56));
                Label("native-stat-detail-text",new Rect(Mathf.Clamp(r.x+8,0,380)+8,Mathf.Clamp(r.y-60,0,424)+5,244,46),Find<CanvasUiEvents>(hoveredStatKey)?.Tooltip,12);
            }
            foreach(var pair in elements)pair.Value.gameObject.SetActive(used.Contains(pair.Key));
            ReleaseHiddenSelection();
        }
        // Nested canvas keeps transient action icons above retained information windows.
        public RectTransform OverlayLayer(string key,int orderOffset)
        {
            var layer=Node(key,new Rect(0,0,Rendering.GameScreen.Width,Rendering.GameScreen.Height));
            var overlay=Component<Canvas>(layer);overlay.overrideSorting=true;overlay.sortingOrder=normalSortingOrder+orderOffset;
            Component<GraphicRaycaster>(layer);return layer;
        }
        public void Raise(string key){if(elements.TryGetValue(key,out var node))node.SetAsLastSibling();}
        public void SolidRect(string key,Rect rect,Color color){Graphic(Node(key,rect),null,color);}
        public void Panel(string key,Rect rect,string title="")
        {Graphic(Node(key,rect),Resources.Load<Texture2D>("UI/BattlePaper"),new Color(.86f,.71f,.48f));Raise(key);if(!string.IsNullOrEmpty(title))Label(key+"-title",new Rect(rect.x+10,rect.y+4,rect.width-20,28),title,18);}
        public void BattlePanel(string key,Rect rect,string title="")=>Panel(key,rect,title);
        public void NativeStatusPanel(string key,Rect rect,int rosterRows=0)
        {PortraitTexture(key,rect,Resources.Load<Texture2D>(rosterRows==-1?"UI/NativeRosterCollapsed":rosterRows==0?"UI/NativeStatusPanel":"UI/NativeRoster"+rosterRows));}
        public void Label(string key,Rect rect,string text,int size=14,RectTransform parent=null,TextAnchor alignment=TextAnchor.MiddleLeft,Color? color=null)
        {
            var node=Node(key,rect,parent);var label=Component<Text>(node);label.font=font;label.text=text??"";label.fontSize=size;
            label.alignment=alignment;label.color=color??PaperInk;label.raycastTarget=false;
            label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Truncate;label.supportRichText=false;
        }
        public void BattleLabel(string key,Rect rect,string text,int size=14,RectTransform parent=null,bool singleLine=false,string tooltip=null)
        {Label(key,rect,text,size,parent);Find<Text>(key).horizontalOverflow=singleLine?HorizontalWrapMode.Overflow:HorizontalWrapMode.Wrap;Clip(elements[key]);Events(key).Tooltip=tooltip??text;}
        private CanvasUiEvents Events(string key)=>Component<CanvasUiEvents>(elements[key]);
        public void Button(string key,Rect rect,string text,Action action,bool enabled=true,bool selected=false,string tooltip="",RectTransform parent=null,Sprite icon=null,bool singleLine=false,int fontSize=14)
        {
            bool fresh=!elements.ContainsKey(key);var node=Node(key,rect,parent);actions[key]=action;
            var image=Graphic(node,Resources.Load<Texture2D>("UI/BattlePaper"),selected?new Color(.58f,.37f,.17f):new Color(.86f,.71f,.48f),true);
            var button=Component<UnityEngine.UI.Button>(node);button.targetGraphic=image;button.interactable=enabled;
            button.navigation=new Navigation {mode=Navigation.Mode.None};
            if(fresh)button.onClick.AddListener(()=>{if(Active(key) && button.interactable && !Events(key).SuppressClick && actions.TryGetValue(key,out var callback))callback?.Invoke();});
            Events(key).Tooltip=tooltip;
            var outline=Component<Outline>(node);outline.effectColor=new Color(.55f,.43f,.22f);outline.effectDistance=new Vector2(2,-2);outline.enabled=true;
            Label(key+"-caption",new Rect(icon==null?0:36,0,Mathf.Max(0,rect.width-(icon==null?0:36)),rect.height),text,fontSize,node,icon==null?TextAnchor.MiddleCenter:TextAnchor.MiddleLeft,enabled?PaperInk:new Color(.4f,.32f,.24f));
            Find<Text>(key+"-caption").horizontalOverflow=singleLine?HorizontalWrapMode.Overflow:HorizontalWrapMode.Wrap;
            Clip(node);Raise(key);
            if(icon!=null)Portrait(key+"-list-icon",new Rect(rect.x,rect.y,32,32),icon,parent);
        }
        public void IconTextButton(string key,Rect rect,string text,Action action,Sprite icon)=>Button(key,rect,text,action,icon:icon);
        public void ItemSlotButton(string key,Rect rect,Action action,Sprite icon,int quantity,bool selected,string tooltip)
        {
            Button(key,rect,"",action,selected:selected,tooltip:tooltip);
            if(icon!=null)Portrait(key+"-icon",new Rect(rect.x,rect.y,32,32),icon);
            if(quantity>1)Label(key+"-count",rect,quantity.ToString(),10,alignment:TextAnchor.LowerRight,color:Color.white);
        }
        public void ActionButton(string key,Rect rect,string text,Action action,bool enabled,string tooltip,Sprite icon=null,RectTransform parent=null,bool dimDisabledIcon=true)
        {
            Button(key,rect,icon==null?text:"",action,enabled,tooltip:tooltip,parent:parent);
            if(icon!=null){Graphic(elements[key],null,Color.clear,true);Find<Outline>(key).enabled=false;float size=Mathf.Min(32,Mathf.Min(rect.width,rect.height));Portrait(key+"-icon",new Rect(rect.center.x-size/2,rect.center.y-size/2,size,size),icon,parent);Find<RawImage>(key+"-icon").color=new Color(1,1,1,enabled||!dimDisabledIcon?1:.4f);}
        }
        public void TextInput(string key,Rect rect,string value,int maximum,Action<string> changed,RectTransform parent=null,bool password=false)
        {
            bool fresh=!elements.ContainsKey(key);var node=Node(key,rect,parent);var image=Graphic(node,null,new Color(.86f,.71f,.48f),true);
            if(fresh)inputOrder.Add(key);
            var field=Component<InputField>(node);field.targetGraphic=image;field.characterLimit=maximum;field.contentType=password?InputField.ContentType.Password:InputField.ContentType.Standard;field.asteriskChar='*';
            if(fresh)Label(key+"-text",new Rect(2,0,Mathf.Max(0,rect.width-4),rect.height),"",14,node);
            else Node(key+"-text",new Rect(2,0,Mathf.Max(0,rect.width-4),rect.height),node);
            field.textComponent=Find<Text>(key+"-text");field.textComponent.horizontalOverflow=HorizontalWrapMode.Overflow;Clip(node);
            textChanges[key]=changed;
            if(fresh)field.onValueChanged.AddListener(v=>{if(Active(key) && textChanges.TryGetValue(key,out var callback))callback?.Invoke(v);});
            if(field.text!=(value??""))field.SetTextWithoutNotify(value??"");
            // Label() is retained-mode and must not clear an actively edited InputField's text.
            field.textComponent.text=password?new string('*',field.text.Length):field.text;
        }
        public RectTransform Scroll(string key,Rect rect,float contentHeight,bool resetPosition=false)
        {
            bool fresh=!elements.ContainsKey(key);var node=Node(key,rect);var scroll=Component<ScrollRect>(node);
            RectTransform viewport,content;
            if(fresh){viewport=NewRect("Viewport",node,new Rect(0,0,rect.width,rect.height));Clip(viewport);Graphic(viewport,null,Color.clear,true);content=NewRect("Content",viewport,new Rect(0,0,rect.width,Mathf.Max(rect.height,contentHeight)));scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=20;}
            else{viewport=scroll.viewport;content=scroll.content;Place(viewport,new Rect(0,0,rect.width,rect.height));content.sizeDelta=new Vector2(rect.width,Mathf.Max(rect.height,contentHeight));}
            if(resetPosition){scroll.StopMovement();content.anchoredPosition=Vector2.zero;}
            return content;
        }
        public void Bar(string key,Rect rect,string label,int value,int maximum,Color color,int fontSize=14)
        {SolidRect(key,rect,new Color(.12f,.13f,.14f));SolidRect(key+"-fill",new Rect(rect.x,rect.y,rect.width*(maximum>0?Mathf.Clamp01((float)value/maximum):0),rect.height),color);Label(key+"-text",rect,$" {label} {value}/{maximum}",fontSize);}
        public void NativeRosterSelection(string key,Rect rect,Action select,bool enabled,string tooltip)
        {Button(key,rect,"",select,enabled,tooltip:tooltip);Graphic(elements[key],null,Color.clear,true);Find<Outline>(key).enabled=false;}
        public void Portrait(string key,Rect rect,Sprite sprite,RectTransform parent=null)
        {
            var image=Graphic(Node(key,rect,parent),sprite==null?null:sprite.texture,sprite==null?Color.clear:Color.white);
            if(sprite!=null){var r=sprite.textureRect;image.uvRect=new Rect(r.x/sprite.texture.width,r.y/sprite.texture.height,r.width/sprite.texture.width,r.height/sprite.texture.height);}else image.uvRect=new Rect(0,0,1,1);
        }
        // Unit cells retain their transparent padding and aspect; tight sprite bounds must not stretch a body.
        public void NativeUnitPicture(string key,Rect rect,Sprite sprite)
        {
            if(sprite==null){Portrait(key,rect,null);return;}
            var source=sprite.rect;float scale=Mathf.Min(rect.width/source.width,rect.height/source.height);
            var logical=new Rect(rect.center.x-source.width*scale*.5f,rect.center.y-source.height*scale*.5f,source.width*scale,source.height*scale);
            if(!sprite.packed)
            {
                PortraitTexture(key,logical,sprite.texture);Find<RawImage>(key).uvRect=new Rect(source.x/sprite.texture.width,source.y/sprite.texture.height,source.width/sprite.texture.width,source.height/sprite.texture.height);
            }
            else
            {
                var trimmed=sprite.textureRect;var offset=sprite.textureRectOffset;
                Portrait(key,new Rect(logical.x+offset.x*scale,logical.y+(source.height-offset.y-trimmed.height)*scale,trimmed.width*scale,trimmed.height*scale),sprite);
            }
        }
        public void RenderTexture(string key,Rect rect,RenderTexture texture){Graphic(Node(key,rect),texture,Color.white);}
        public void PortraitTexture(string key,Rect rect,Texture2D texture){var image=Graphic(Node(key,rect),texture,texture==null?Color.clear:Color.white);image.uvRect=new Rect(0,0,1,1);}
        public void ModalBlocker(string key){Graphic(Node(key,new Rect(0,0,Rendering.GameScreen.Width,Rendering.GameScreen.Height)),null,Color.clear,true);Raise(key);}
        public void BattlePortrait(string key,Rect rect,Sprite portrait){Portrait(key+"-frame",rect,Resources.Load<Sprite>("UI/BattlePortraitFrame"));Portrait(key,new Rect(rect.x+3,rect.y+3,rect.width-6,rect.height-6),portrait);}
        private NativeBattleWindows.Control resultConfirm;
        public void DungeonResult(string key,Rect unused,string text,Action acknowledge,string enemies="",string experience="",string outcome="")
        {
            var r=new Rect(103,68,434,344);canvas.sortingOrder=Mathf.Max(normalSortingOrder,100);ModalBlocker(key+"-blocker");NativeTexturePanel(key,r,Resources.Load<Texture2D>("UI/NativeBattleResult"));
            ResultNames(key+"-text",key+"-allies-scroll",new Rect(r.x+20,r.y+90,394,70),text);
            ResultNames(key+"-enemies",key+"-enemies-scroll",new Rect(r.x+20,r.y+202,394,60),enemies);
            BattleLabel(key+"-experience",new Rect(r.x+238,r.y+62,52,20),experience,singleLine:true);Raise(key+"-experience");BattleLabel(key+"-status",new Rect(r.x+85,r.y+271,40,22),outcome,singleLine:true);Raise(key+"-status");
            resultConfirm??=new NativeBattleWindows.Control {rect=new Rect(0,0,46,19),frames=new[]{Resources.Load<Texture2D>("UI/NativeResultConfirm0"),Resources.Load<Texture2D>("UI/NativeResultConfirm1"),Resources.Load<Texture2D>("UI/NativeResultConfirm2")}};
            NativeImageButton(key+"-confirm",new Vector2(r.x+372,r.y+311),resultConfirm,acknowledge,true,"확인");
        }
        private void ResultNames(string key,string scrollKey,Rect rect,string text)
        {
            text??="";bool changed=!resultRows.TryGetValue(scrollKey,out var cached)||cached.Text!=text||!elements.TryGetValue(scrollKey,out var node)||!node.gameObject.activeSelf;
            if(changed){cached=(text,text.Split('\n'));resultRows[scrollKey]=cached;}
            var parent=Scroll(scrollKey,rect,Mathf.Max(rect.height,cached.Rows.Length*20),changed);
            // The native background is raised every frame; reused rows must follow it.
            Raise(scrollKey);
            for(int i=0;i<cached.Rows.Length;i++)
            {
                var rowKey=i==0?key:key+"-"+i;
                BattleLabel(rowKey,new Rect(0,i*20,rect.width-20,20),cached.Rows[i],14,parent,true);
                Find<Text>(rowKey).alignment=TextAnchor.UpperLeft;
            }
        }
        private void OnDestroy()
        {
            if(font!=null){if(Application.isPlaying)Destroy(font);else DestroyImmediate(font);}
            if(entryFont!=null){if(Application.isPlaying)Destroy(entryFont);else DestroyImmediate(entryFont);}
        }
    }
}
