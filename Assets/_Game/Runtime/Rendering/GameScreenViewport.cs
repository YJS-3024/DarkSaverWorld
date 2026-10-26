using UnityEngine;
using UnityEngine.UI;
namespace DarkSaver.Rendering
{
    [DefaultExecutionOrder(-10000)]
    public sealed class GameScreenViewport:MonoBehaviour
    {
        private readonly RectTransform[] bars=new RectTransform[4];
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if(FindAnyObjectByType<GameScreenViewport>()!=null)return;
            var go=new GameObject("Logical 640x480 Viewport");DontDestroyOnLoad(go);go.AddComponent<GameScreenViewport>();
        }
        private void CreateBars()
        {
            var layer=new GameObject("Letterbox Canvas",typeof(RectTransform),typeof(Canvas));layer.transform.SetParent(transform,false);
            var canvas=layer.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32760;
            for(int i=0;i<4;i++)
            {
                var go=new GameObject("Letterbox "+i,typeof(RectTransform),typeof(Image));bars[i]=go.GetComponent<RectTransform>();bars[i].SetParent(layer.transform,false);
                bars[i].anchorMin=bars[i].anchorMax=bars[i].pivot=new Vector2(0,1);
                var image=go.GetComponent<Image>();image.color=Color.black;image.raycastTarget=false;
            }
        }
        void LateUpdate()
        {
            GameScreen.ApplyCamera(Camera.main);
            if(bars[0]==null)CreateBars();var v=GameScreen.Viewport;
            SetBar(0,new Rect(0,0,v.x,Screen.height));SetBar(1,new Rect(v.xMax,0,Screen.width-v.xMax,Screen.height));
            SetBar(2,new Rect(0,0,Screen.width,v.y));SetBar(3,new Rect(0,v.yMax,Screen.width,Screen.height-v.yMax));
        }
        private void SetBar(int index,Rect rect)
        {var bar=bars[index];bar.anchoredPosition=new Vector2(rect.x,-rect.y);bar.sizeDelta=rect.size;bar.gameObject.SetActive(rect.width>0 && rect.height>0);}
    }
}
