using System;
using System.Text.Json;
using DarkSaver.Original.World;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Managed Unity lifetime/EventSystem boundaries. Production edit methods are extracted unchanged.
namespace UnityEngine
{
    public enum KeyCode { Escape,Return,KeypadEnter }
    public static class Input
    {
        public static KeyCode? key;
        public static bool GetKeyDown(KeyCode value)=>key==value;
        public static bool GetMouseButtonDown(int value)=>false;
    }
}
namespace UnityEngine.EventSystems
{
    public class EventSystem:UnityEngine.Object
    {
        public static EventSystem current;
        GameObject selected;
        public int restores;
        public GameObject currentSelectedGameObject {get {if(destroyed)throw new MissingReferenceException();return selected;}}
        public void Select(GameObject value){selected=value;}
        public void SetSelectedGameObject(GameObject value){if(destroyed||value==null)throw new MissingReferenceException();selected=value;restores++;}
    }
}
namespace UnityEngine.UI
{
    public class InputField:Component
    {
        string value="";bool focused;
        void Alive(){if(this==null||gameObject==null)throw new MissingReferenceException();}
        public string text {get{Alive();return value;}set{Alive();this.value=value;}}
        public bool isFocused {get{Alive();return focused;}}
        public RectTransform transform=>gameObject.GetComponent<RectTransform>();
        public void SetTextWithoutNotify(string text){Alive();value=text;}
        public void ActivateInputField(){Alive();focused=true;if(EventSystem.current!=null)EventSystem.current.Select(gameObject);}
        public sealed class EndEvent
        {Action<string> callback;public void AddListener(Action<string> value){callback+=value;}public void Invoke(){callback?.Invoke("");}}
        public EndEvent onEndEdit=new EndEvent();
    }
}
internal static class CommanderNameUiChecks
{
    static int checks;
    static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
    static InputField Field()=>new GameObject("Input",typeof(RectTransform),typeof(InputField)).GetComponent<InputField>();
    static void Destroy(InputField field){field.destroyed=true;field.gameObject.destroyed=true;}
    static void Main(string[] args)
    {
        var fixture=CommanderNameChecks.CreateFixture(args[0]);var ui=new WorldSceneRoot(fixture.manager);
        EventSystem.current=new EventSystem();var dead=Field();Destroy(dead);EventSystem.current.Select(dead.gameObject);
#if BASELINE
        int exceptions=0;try{ui.Poll();}catch(MissingReferenceException){exceptions++;}
        Check(exceptions==1,"previous production Handle must reproduce destroyed GetComponent");
        Console.WriteLine(JsonSerializer.Serialize(new{checks,destroyedReferenceExceptions=exceptions,unityPlay=false}));
#else
        ui.Last=dead.gameObject;Check(!ui.Poll()&&ui.Last==null,"destroyed selection/cache never accessed");
        EventSystem.current=null;Check(!ui.Poll(),"absent EventSystem");
        EventSystem.current=new EventSystem{destroyed=true};Check(!ui.Poll(),"destroyed EventSystem");
        var events=new EventSystem();EventSystem.current=events;var prior=Field();prior.ActivateInputField();ui.Poll();Check(ui.Last==prior.gameObject,"focused live field cached");
        prior.gameObject.SetActive(false);ui.Poll();Check(ui.Last==null,"inactive focus discarded");prior.gameObject.SetActive(true);
        var component=Field();component.destroyed=true;events.Select(component.gameObject);Check(!ui.Poll(),"destroyed InputField component not read");
        ui.Last=dead.gameObject;events.Select(prior.gameObject);ui.Open();Check(ui.Editing&&ui.Previous==prior.gameObject&&ui.Field.gameObject.activeSelf,"dead cache falls back to current live selection");
        ui.Finish(false);Check(!ui.Editing&&!ui.Field.gameObject.activeSelf&&events.currentSelectedGameObject==prior.gameObject,"cancel hides edit and restores prior focus");
        int packets=0;fixture.manager.Notify_CommanderNamePacket+=packet=>packets++;
        string before=JsonSerializer.Serialize(fixture.store.data,new JsonSerializerOptions{IncludeFields=true});
        ui.Open();ui.Field.text="새 이름";ui.Finish(true);Check(packets==1&&!ui.Editing,"submit original manager request once");
        Check(before==JsonSerializer.Serialize(fixture.store.data,new JsonSerializerOptions{IncludeFields=true}),"UI request does not mutate account, money or health");
        ui.Open();Destroy(ui.Field);Check(!ui.Poll()&&!ui.Editing&&packets==1,"destroyed active edit cancels without submit");
        ui.Replace();ui.Open();Destroy(prior);ui.Finish(false);Check(!ui.Editing,"destroyed prior focus never restored");
        ui.Open();ui.WindowRoot.destroyed=true;ui.Paint();Check(!ui.Editing,"destroyed window cancels edit");
        ui=new WorldSceneRoot(fixture.manager);ui.Open();Input.key=KeyCode.Escape;ui.Poll();Input.key=null;Check(!ui.Editing,"Escape closes live edit");
        var callbackUi=ui;Action<byte[]> destroy=p=>Destroy(callbackUi.Field);fixture.manager.Notify_CommanderNamePacket+=destroy;
        ui.Open();ui.Finish(true);Check(!ui.Editing,"synchronous send callback can destroy field safely");fixture.manager.Notify_CommanderNamePacket-=destroy;
        ui.Replace();Action<byte[]> reopen=p=>callbackUi.Open();fixture.manager.Notify_CommanderNamePacket+=reopen;
        ui.Open();ui.Finish(true);Check(ui.Editing&&ui.Field.gameObject.activeSelf,"old completion does not hide callback's new edit");fixture.manager.Notify_CommanderNamePacket-=reopen;ui.Finish(false);
        Action<byte[]> aba=p=>{callbackUi.Open();callbackUi.Finish(false);};fixture.manager.Notify_CommanderNamePacket+=aba;
        var replacementFocus=Field();ui.Last=replacementFocus.gameObject;
        int focusBefore=events.restores;ui.Open();ui.Finish(true);Check(!ui.Editing&&events.restores==focusBefore+1,"closed replacement edit is not followed by stale outer focus restoration");fixture.manager.Notify_CommanderNamePacket-=aba;
        ui.Open();var old=ui.Field;ui.Replace();ui.Open();old.onEndEdit.Invoke();Check(ui.Editing,"old field callback cannot cancel replacement edit");ui.Finish(false);
        ui.Open();ui.isActiveAndEnabled=false;focusBefore=events.restores;ui.Finish(false);Check(events.restores==focusBefore,"disabled world does not restore focus");
        Console.WriteLine(JsonSerializer.Serialize(new{checks,destroyedReferenceExceptions=0,unityPlay=false,scope="Extracted production name-edit methods/listener; real manager/domain with isolated memory store; managed Unity destroyed-object and EventSystem boundaries."}));
#endif
    }
}
