using DarkSaver.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
namespace DarkSaver.Tests
{
    public sealed class TutorialPresentationTests
    {
        [Test] public void TutorialPresentationContracts()=>TutorialPresentationChecks.Run((ok,name)=>Assert.IsTrue(ok,name));
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
