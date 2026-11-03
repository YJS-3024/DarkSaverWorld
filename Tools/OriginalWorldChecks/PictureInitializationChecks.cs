using System;
using System.Collections.Generic;
using System.Text.Json;
using DarkSaver.Original.Entry;
using UnityEngine;
using UnityEngine.UI;

// Managed component/projection boundaries only; production factory bodies and UV
// conversion are compiled unchanged by check_picture_initialization.py.
namespace UnityEngine
{
    public class Object
    {
        public bool destroyed;
        static bool Empty(Object value)=>ReferenceEquals(value,null)||value.destroyed;
        public static bool operator ==(Object a,Object b)=>Empty(a)?Empty(b):!Empty(b)&&ReferenceEquals(a,b);
        public static bool operator !=(Object a,Object b)=>!(a==b);
        public override bool Equals(object value)=>ReferenceEquals(this,value);
        public override int GetHashCode()=>base.GetHashCode();
    }
    public class MissingReferenceException:InvalidOperationException{public MissingReferenceException():base("Destroyed Unity object access") {}}
    public class ScriptableObject:Object{}
    public class Canvas:Component{}
    public class Texture:Object
    {
        public int reads;readonly int w,h;
        public Texture(int width,int height){w=width;h=height;}
        public int width{get{if(destroyed)throw new InvalidOperationException("Destroyed texture read");reads++;return w;}}
        public int height{get{if(destroyed)throw new InvalidOperationException("Destroyed texture read");reads++;return h;}}
    }
    public class Texture2D:Texture{public Texture2D(int w,int h):base(w,h){}}
    public struct Color{}
    public struct Vector2{public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
    public struct Rect
    {
        public float x,y,width,height;
        public Rect(float x,float y,float width,float height){this.x=x;this.y=y;this.width=width;this.height=height;}
        public float xMin=>x;public float xMax=>x+width;public float yMin=>y;public float yMax=>y+height;
        public Vector2 size=>new Vector2(width,height);
    }
    public class Component:Object{public GameObject gameObject;}
    public class RectTransform:Component
    {
        public RectTransform parent;public Vector2 anchorMin,anchorMax,pivot,anchoredPosition,sizeDelta;
        public void SetParent(RectTransform value,bool worldPositionStays){parent=value;}
        public void SetAsLastSibling(){}
    }
    public class GameObject:Object
    {
        public bool activeSelf=true;
        public Action<bool> activeChanged;
        public void SetActive(bool value){if(destroyed)throw new MissingReferenceException();bool changed=activeSelf!=value;activeSelf=value;if(changed)activeChanged?.Invoke(value);}
        public bool activeInHierarchy=>activeSelf&&(!components.TryGetValue(typeof(RectTransform),out var c)||((RectTransform)c).parent==null||((RectTransform)c).parent.gameObject.activeInHierarchy);
        readonly Dictionary<Type,Component> components=new Dictionary<Type,Component>();
        public GameObject(string name,params Type[] types){foreach(var type in types){var c=(Component)Activator.CreateInstance(type);c.gameObject=this;components.Add(type,c);}}
        public T AddComponent<T>()where T:Component,new(){var c=new T{gameObject=this};components.Add(typeof(T),c);return c;}
        public T GetComponent<T>()where T:Component{if(destroyed)throw new MissingReferenceException();return components.TryGetValue(typeof(T),out var value)?(T)value:null;}
    }
}
namespace UnityEngine.UI
{
    public class CanvasScaler:Component{}
    public class GraphicRaycaster:Component{}
    public class RawImage:Component{public Texture texture;public Rect uvRect=new Rect(0,0,1,1);public bool enabled=true,raycastTarget=true;}
    public class Text:Component{public string text;}
}
internal sealed class HiredWindow
{public RectTransform Root,Full;public RawImage Hired;public Text HiredCount=new Text();}
internal sealed class HiredProfile{public int hiredJob=-1,hiredCount;}
internal static class PictureInitializationChecks
{
    static int assertions;static void Check(bool ok,string label){assertions++;if(!ok)throw new Exception(label);}
    static bool Same(Rect a,Rect b)=>Math.Abs(a.x-b.x)<.00001&&Math.Abs(a.y-b.y)<.00001&&Math.Abs(a.width-b.width)<.00001&&Math.Abs(a.height-b.height)<.00001;
    static RectTransform Parent()=>new GameObject("Host",typeof(RectTransform)).GetComponent<RectTransform>();
    static int Visibility(bool baseline)
    {
        int exposed=0;
        for(int stage=0;stage<6;stage++)
        {
            GameObject root=null;try{StartupVisibilityHost.Construct(stage,Parent(),g=>{root=g;var child=new GameObject("Unfinished game window",typeof(RectTransform));child.GetComponent<RectTransform>().SetParent(g.GetComponent<RectTransform>(),false);throw new InvalidOperationException("Simulated child construction failure");});}catch(InvalidOperationException){}
            Check(root!=null,"construction failure retains an observable parent fixture");if(root.activeInHierarchy)exposed++;
            Check(root.activeInHierarchy==baseline,"partial UI visibility matches production publication guard");
        }
        if(baseline)return exposed;
        for(int stage=0;stage<3;stage++)
        {
            var root=StartupVisibilityHost.Construct(stage,Parent(),null);Check(!root.activeInHierarchy,"complete construction still waits for first paint");
            try{StartupVisibilityHost.Publish(stage,root,()=>{throw new InvalidOperationException("Simulated paint failure");});}catch(InvalidOperationException){}
            Check(!root.activeInHierarchy,"failed first paint does not publish a game canvas");
            bool paintedHidden=false;StartupVisibilityHost.Publish(stage,root,()=>paintedHidden=!root.activeInHierarchy);Check(paintedHidden&&root.activeInHierarchy,"successful first paint precedes showing the canvas");
        }
        var canvas=StartupVisibilityHost.Construct(0,Parent(),null);var windowRoot=WorldPictureHost.CreateGroup(canvas.GetComponent<RectTransform>(),"Personal window",new Rect(0,0,320,320));var personal=WorldPictureHost.BuildHired(windowRoot);
        StartupVisibilityHost.Publish(0,canvas,()=>{});Check(canvas.activeInHierarchy&&!windowRoot.gameObject.activeInHierarchy,"publishing the world keeps prebuilt personal windows closed");
        StartupVisibilityHost.ApplyPersonalVisibility(personal,true);Check(windowRoot.gameObject.activeInHierarchy,"an explicit personal-window request still opens it normally");
        StartupVisibilityHost.ApplyPersonalVisibility(personal,false);Check(!windowRoot.gameObject.activeInHierarchy,"closing the personal window does not hide the world");
        foreach(bool enabled in new[]{false,true})foreach(EntryStage stage in Enum.GetValues<EntryStage>())
        {
            var login=Parent();var servers=Parent();var creation=Parent();StartupVisibilityHost.ApplyEntryStage(login,servers,creation,enabled,stage);
            Check(login.gameObject.activeSelf==(enabled&&stage==DarkSaver.Original.Entry.EntryStage.Login)&&servers.gameObject.activeSelf==(enabled&&stage==DarkSaver.Original.Entry.EntryStage.ServerSelection)&&creation.gameObject.activeSelf==(stage==DarkSaver.Original.Entry.EntryStage.FirstCharacterCreation),"real stage visibility preserves bypass mode and activates only the correct entry panel");
        }
        return exposed;
    }
    static void Main()
    {
#if BASELINE
        int reproduced=0;
        try{WorldPictureHost.BuildHired(Parent());}catch(NullReferenceException){reproduced++;}
        try{TutorialPictureHost.Create(Parent(),"Deferred crop",new Rect(0,0,32,32),null,new Rect(32,32,32,32));}catch(NullReferenceException){reproduced++;}
        int exposed=Visibility(true);Check(exposed==6,"baseline exposes all unfinished canvas/entry-panel parents");
        Check(reproduced==2,"baseline must reproduce the reported F3 and shared tutorial null-UV failure");
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,reproducedNullExceptions=reproduced,partialUiLeaks=exposed,unityPlay=false}));
#else
        var parent=Parent();var pending=WorldPictureHost.BuildHired(parent);
        Check(pending.Hired.texture==null&&!pending.Hired.enabled,"F3 builds an empty hidden mercenary slot before ownership is known");
        Check(!pending.Hired.raycastTarget&&Same(pending.Hired.uvRect,new Rect(0,0,1,1)),"pending crop does not dereference a missing texture or capture input");
        var transform=(RectTransform)pending.Hired.gameObject.GetComponent<RectTransform>();
        Check(transform.parent==parent&&transform.anchoredPosition.x==29&&transform.anchoredPosition.y==-178&&transform.sizeDelta.x==32&&transform.sizeDelta.y==32,"F3 original slot position/size are retained");
        WorldPictureHost.PaintHired(pending,null,null);Check(!pending.Hired.enabled&&pending.HiredCount.text=="","unknown profile remains hidden");
        var profile=new HiredProfile{hiredJob=302,hiredCount=2};WorldPictureHost.PaintHired(pending,profile,null);Check(!pending.Hired.enabled,"unresolved owned picture is not a white square or a substitute sprite");
        var texture=new Texture2D(128,256);WorldPictureHost.PaintHired(pending,profile,texture);
        Check(pending.Hired.enabled&&ReferenceEquals(pending.Hired.texture,texture)&&pending.HiredCount.text=="X 2"&&Same(pending.Hired.uvRect,new Rect(.25f,.75f,.25f,.125f)),"later received mercenary picture is enabled with its actual atlas dimensions and original32px crop");
        profile.hiredJob=-1;profile.hiredCount=0;WorldPictureHost.PaintHired(pending,profile,null);Check(!pending.Hired.enabled&&pending.Hired.texture==null,"later ownership clear hides the old picture");
        foreach(bool tutorial in new[]{false,true})
        {
            RawImage Create(Texture value,Rect? crop)=>tutorial?TutorialPictureHost.Create(parent,"Texture",new Rect(0,0,32,32),value,crop):WorldPictureHost.Create(parent,"Texture",new Rect(0,0,32,32),value,crop);
            var empty=Create(null,new Rect(32,32,32,32));Check(Same(empty.uvRect,new Rect(0,0,1,1)),"both factories defer UV for an absent texture");
            var destroyed=new Texture2D(128,128){destroyed=true};var released=Create(destroyed,new Rect(32,32,32,32));Check(destroyed.reads==0&&Same(released.uvRect,new Rect(0,0,1,1)),"Unity-style destroyed texture does not enter UV conversion");
            var whole=Create(texture,null);Check(ReferenceEquals(whole.texture,texture)&&Same(whole.uvRect,new Rect(0,0,1,1)),"valid uncropped pictures retain full texture");
            var cropped=Create(texture,new Rect(16,64,32,48));Check(Same(cropped.uvRect,new Rect(.125f,.5625f,.25f,.1875f)),"valid top-left atlas conversion is unchanged");
            Check(!cropped.raycastTarget,"picture creation does not steal input");
        }
        int exposed=Visibility(false);Check(exposed==0,"no partial UI parent is exposed");
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,reproducedNullExceptions=0,partialUiLeaks=exposed,unityPlay=false,scope="Production world/tutorial Picture, Group, Place bodies, NativeEntryLayout.Uv, F3 hired-slot fragments and startup construction/first-paint/entry/personal visibility statements. Managed uGUI component, Unity-null and texture boundaries; not a Unity scene/Canvas/import/Play execution."}));
#endif
    }
}
