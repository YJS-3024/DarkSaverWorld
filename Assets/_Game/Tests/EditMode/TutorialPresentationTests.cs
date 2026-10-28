using DarkSaver.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
namespace DarkSaver.Tests
{
    public sealed class TutorialPresentationTests
    {
        [Test] public void TutorialPresentationContracts()=>TutorialPresentationChecks.Run((ok,name)=>Assert.IsTrue(ok,name));
        [Test] public void ActionCanvasStaysAboveStatusWithoutChangingActorAnchor()
        {
            var go=new GameObject("tutorial action depth fixture");
            try
            {
                var tutorial=go.AddComponent<FirstPlayTutorial>();InGameUi ui=null;InGameUi.Ensure(ref ui,tutorial,140);
                ui.Begin();
                ui.NativeStatusPanel("status",new Rect(5,180,120,213));
                var layer=ui.OverlayLayer("tutorial-actions-layer",10);
                DarkSaver.Events.TutorialPresentationRules.PlaceMenu(60,250,out var x,out var y);
                ui.ActionButton("action",new Rect(x,y,32,32),"",()=>{},true,"",null,parent:layer);
                ui.End();
                var action=ui.Find<RectTransform>("action");
                Assert.AreEqual(layer,action.parent);
                Assert.AreEqual(new Vector2(12,-202),action.anchoredPosition);
                var canvas=layer.GetComponent<Canvas>();
                Assert.IsTrue(canvas.overrideSorting);
                Assert.AreEqual(150,canvas.sortingOrder);
                Assert.Greater(canvas.sortingOrder,ui.Find<RectTransform>("status").GetComponentInParent<Canvas>().sortingOrder);
                Assert.IsNotNull(layer.GetComponent<GraphicRaycaster>());
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void HelpTextHasInsetAndKeepsBottomPaddingAfterWrapping()
        {
            var go=new GameObject("tutorial text inset fixture");
            try
            {
                var tutorial=go.AddComponent<FirstPlayTutorial>();InGameUi ui=null;InGameUi.Ensure(ref ui,tutorial,140);
                ui.Begin();ui.NativeBattleWindowText("help",new Rect(10,10,160,60),"설명 본문의 여백 확인\n두 번째 줄\n세 번째 줄\n네 번째 줄");ui.End();
                var text=ui.Find<Text>("help");var content=(RectTransform)text.transform.parent;
                Assert.AreEqual(new Vector2(6,-6),text.rectTransform.anchoredPosition);
                Assert.AreEqual(148,text.rectTransform.sizeDelta.x);
                Assert.GreaterOrEqual(content.rect.height,text.preferredHeight+12);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
