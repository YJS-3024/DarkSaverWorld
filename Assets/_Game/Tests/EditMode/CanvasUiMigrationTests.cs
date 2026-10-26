using System;
using System.Linq;
using DarkSaver.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace DarkSaver.Tests
{
    public sealed class CanvasUiMigrationTests
    {
        [Test]
        public void EntryAtlasButtonsUseNativeSizedPatchesAndCurrentEnabledState()
        {
            var root=new GameObject("Native login button fixture");
            try
            {
                var owner=root.AddComponent<StartMenuController>();InGameUi ui=null;InGameUi.Ensure(ref ui,owner);
                var atlas=Resources.Load<Texture2D>("UI/Entry/LoginAtlas");Assert.IsNotNull(atlas);
                int calls=0;
                void Draw(bool enabled){ui.Begin();ui.EntryAtlasButton("login",new Rect(239,339,55,19),atlas,new Rect(10,379,55,19),new Rect(77,379,50,16),"접속",()=>calls++,enabled);ui.End();}
                Draw(true);var button=ui.Find<Button>("login");var events=ui.Find<CanvasUiEvents>("login");
                var state=button.transform.Find("NativeState").GetComponent<RawImage>();
                Assert.AreEqual(Color.clear,state.color);
                events.OnPointerEnter(new PointerEventData(null));Assert.AreEqual(Color.white,state.color);
                events.OnPointerDown(new PointerEventData(null){button=PointerEventData.InputButton.Left});
                Assert.AreEqual(new Vector2(50,16),state.rectTransform.sizeDelta);
                Assert.That(state.uvRect.x,Is.EqualTo(77f/640).Within(.00001));
                events.OnPointerUp(new PointerEventData(null){button=PointerEventData.InputButton.Left});
                Assert.AreEqual(new Vector2(55,19),state.rectTransform.sizeDelta);
                button.onClick.Invoke();Assert.AreEqual(1,calls);
                events.OnPointerExit(new PointerEventData(null));Assert.AreEqual(Color.clear,state.color);
                Draw(false);events.OnPointerEnter(new PointerEventData(null));
                Assert.AreEqual(Color.clear,state.color);button.onClick.Invoke();Assert.AreEqual(1,calls);
                Draw(true);events.OnPointerEnter(new PointerEventData(null));
                Assert.AreEqual(Color.white,state.color);button.onClick.Invoke();Assert.AreEqual(2,calls);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [Test]
        public void InputFocusCyclesInFormOrderAndSkipsHiddenOrDisabledFields()
        {
            var previous=EventSystem.current;
            var events=new GameObject("Input navigation EventSystem",typeof(EventSystem));
            var root=new GameObject("Input navigation fixture");
            try
            {
                EventSystem.current=events.GetComponent<EventSystem>();
                var owner=root.AddComponent<StartMenuController>();InGameUi ui=null;InGameUi.Ensure(ref ui,owner);
                ui.Begin();
                ui.EntryInput("id",new Rect(0,0,100,20),"account",20,_=>{});
                ui.EntryInput("password",new Rect(0,30,100,20),"secret",64,_=>{},password:true);
                ui.TextInput("confirmation",new Rect(0,60,100,20),"secret",64,_=>{},password:true);
                ui.End();
                var id=ui.Find<InputField>("id");var password=ui.Find<InputField>("password");
                var confirmation=ui.Find<InputField>("confirmation");
                id.Select();Assert.IsTrue(ui.MoveInputFocus());
                Assert.AreSame(password.gameObject,EventSystem.current.currentSelectedGameObject);
                Assert.IsTrue(ui.MoveInputFocus());Assert.AreSame(confirmation.gameObject,EventSystem.current.currentSelectedGameObject);
                Assert.IsTrue(ui.MoveInputFocus());Assert.AreSame(id.gameObject,EventSystem.current.currentSelectedGameObject);
                Assert.IsTrue(ui.MoveInputFocus(true));Assert.AreSame(confirmation.gameObject,EventSystem.current.currentSelectedGameObject);
                confirmation.interactable=false;password.gameObject.SetActive(false);id.Select();
                Assert.IsTrue(ui.MoveInputFocus());Assert.AreSame(id.gameObject,EventSystem.current.currentSelectedGameObject);
                Assert.AreEqual("secret",password.text);Assert.AreEqual("secret",confirmation.text);
                ui.Hide();Assert.IsFalse(ui.MoveInputFocus());
                Assert.IsNull(EventSystem.current.currentSelectedGameObject);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(events);
                EventSystem.current=previous;
            }
        }
        [Test]
        public void CanvasControlsRetainCallbacksMaskPasswordsAndRejectHiddenDisabledOwners()
        {
            var root=new GameObject("Canvas migration fixture");
            try
            {
                var owner=root.AddComponent<StartMenuController>();InGameUi ui=null;InGameUi.Ensure(ref ui,owner);
                Assert.That(ui.Canvas.renderMode,Is.EqualTo(RenderMode.ScreenSpaceOverlay));
                Assert.That(ui.GetComponent<CanvasScaler>(),Is.Not.Null);Assert.That(ui.GetComponent<GraphicRaycaster>(),Is.Not.Null);
                Assert.That(root.GetComponentsInChildren<Component>(true).Any(c=>c.GetType().Name=="UIDocument"),Is.False);
                int calls=0;string edited="";
                void Draw(int value,bool enabled=true){ui.Begin();ui.EntryInput("password",new Rect(300,315,108,14),edited,64,v=>edited=v,true);ui.Button("confirm",new Rect(20,20,80,30),"확인",()=>calls+=value,enabled);ui.End();}
                Draw(1);var button=ui.Find<Button>("confirm");var password=ui.Find<InputField>("password");
                Assert.That(password.contentType,Is.EqualTo(InputField.ContentType.Password));Assert.That(password.characterLimit,Is.EqualTo(64));
                password.text="secret";Assert.That(edited,Is.EqualTo("secret"));button.onClick.Invoke();Assert.That(calls,Is.EqualTo(1));
                Draw(2);Assert.That(ui.Find<Button>("confirm"),Is.SameAs(button));button.onClick.Invoke();Assert.That(calls,Is.EqualTo(3));
                Draw(4,false);button.onClick.Invoke();Assert.That(calls,Is.EqualTo(3));
                Draw(8);owner.enabled=false;button.onClick.Invoke();Assert.That(calls,Is.EqualTo(3));
                owner.enabled=true;ui.Hide();button.onClick.Invoke();Assert.That(calls,Is.EqualTo(3));
                ui.Begin();ui.End();Assert.That(button.gameObject.activeSelf,Is.False);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [Test]
        public void NativeButtonsAndModalBlockerUseActualGraphicRaycastOrder()
        {
            var root=new GameObject("Canvas raycast fixture");var eventRoot=new GameObject("Fixture EventSystem",typeof(EventSystem));
            try
            {
                var owner=root.AddComponent<StartMenuController>();InGameUi ui=null;InGameUi.Ensure(ref ui,owner);
                ui.Begin();ui.Button("behind",new Rect(10,10,80,30),"뒤",()=>{});ui.ModalBlocker("block");ui.Button("front",new Rect(10,10,80,30),"앞",()=>{});ui.End();
                Canvas.ForceUpdateCanvases();
                var target=ui.Find<Button>("front");var point=RectTransformUtility.WorldToScreenPoint(null,target.transform.TransformPoint(new Vector3(20,-10)));
                var hits=new System.Collections.Generic.List<RaycastResult>();
                ui.GetComponent<GraphicRaycaster>().Raycast(new PointerEventData(eventRoot.GetComponent<EventSystem>()){position=point},hits);
                Assert.That(hits.Count,Is.GreaterThan(0));Assert.That(hits[0].gameObject,Is.SameAs(target.gameObject));
                Assert.That(ui.Find<RawImage>("block").raycastTarget,Is.True);
                Assert.That(ui.Find<Text>("front-caption").raycastTarget,Is.False);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(eventRoot);}
        }
    }
}
