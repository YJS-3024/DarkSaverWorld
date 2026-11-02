using System.Collections.Generic;
using System.Linq;
using DarkSaver.Original.Entry;
using DarkSaver.Original.Tutorial;
using DarkSaver.Original.UI;
using UnityEngine;
using UnityEngine.UI;
namespace DarkSaver.Original.World
{
    public sealed partial class WorldSceneRoot
    {
        private sealed class PrivateWindow
        {public string Id,Session;public RectTransform Root;public InputField Input;public readonly List<Text> Rows=new List<Text>();public long Revision=-1;}
        private readonly Dictionary<string,PrivateWindow> privateWindows=new Dictionary<string,PrivateWindow>();
        private string privateWindowSession;
        private void ClearPrivateWindows()
        {foreach(var p in privateWindows.Values)if(p.Root!=null){p.Root.gameObject.SetActive(false);Destroy(p.Root.gameObject);}privateWindows.Clear();privateWindowSession=null;}
        private PrivateWindow CreatePrivateWindow(NativePrivateChatView item,string session)
        {
            var spec=art.PrivateChatLayout;if(spec==null||art.privateChatWindow==null)return null;
            var p=new PrivateWindow{Id=item.id,Session=session,Root=Group(worldWindows,"Original private chat80 "+item.id,new Rect(item.x,item.y,spec.width,spec.height))};
            p.Root.gameObject.AddComponent<Image>().color=Color.clear;Picture(p.Root,"Original scene47 frame",new Rect(0,0,spec.width,spec.height),art.privateChatWindow);
            var title=Hit(p.Root,"Original private title",new Rect(spec.dragX,spec.dragY,spec.dragWidth,spec.dragHeight),()=>p.Root.SetAsLastSibling());Vector2? offset=null;
            title.gameObject.AddComponent<NativeTutorialDrag>().Configure(screen,(point,end)=>
            {p.Root.SetAsLastSibling();if(!offset.HasValue)offset=point-new Vector2(p.Root.anchoredPosition.x,-p.Root.anchoredPosition.y);var at=point-offset.Value;Place(p.Root,new Rect(Mathf.Clamp(Mathf.Round(at.x),0,640-spec.width),Mathf.Clamp(Mathf.Round(at.y),0,480-spec.height),spec.width,spec.height));if(end)offset=null;},onCancel:()=>offset=null);
            // 453380's16-bit native literal0x1F title, with a one-pixel black shadow.
            var shadow=Label(p.Root,"Original name shadow",new Rect(33,5,80,16),item.sender);shadow.color=Color.black;shadow.alignment=TextAnchor.UpperLeft;((NativeBitmapText)shadow).NativeParagraphWrap=true;
            var name=Label(p.Root,"Original private sender",new Rect(32,4,80,16),item.sender);name.color=Color.blue;name.alignment=TextAnchor.UpperLeft;((NativeBitmapText)name).NativeParagraphWrap=true;
            foreach(var b in spec.buttons)
            {
                int direction=b.id==77?-1:1;var hit=Hit(p.Root,"Original private control"+b.id,new Rect(b.x,b.y,b.width,b.height),()=>{if(b.id==38&&server.Request_ClosePrivateChat(p.Id,p.Session))PaintPrivateChats();});
                if(b.id==77||b.id==79)hit.gameObject.AddComponent<NativeRepeatButton>().Configure(()=>server.Request_ScrollPrivateChat(p.Id,p.Session,direction));
                var image=Picture((RectTransform)hit.transform,"Original private control states",new Rect(0,0,b.width,b.height),art.battle.waitingButtons);
                hit.gameObject.AddComponent<NativeEntryButton>().Configure(image,art.battle.waitingButtons,new Rect(b.sx,b.sy,b.width,b.height),new Rect(b.sx+b.width,b.sy,b.width,b.height),new Rect(b.sx+2*b.width,b.sy,b.width,b.height));
            }
            var bounds=Group(p.Root,"Original private rows",new Rect(4,27,177,88));bounds.gameObject.AddComponent<RectMask2D>();
            for(int row=0;row<6;row++)
            {
                var label=Label(bounds,"Private row"+row,new Rect(0,row*14,177,14),"");((NativeBitmapText)label).NativeParagraphWrap=true;
                var shadowEffect=label.gameObject.AddComponent<Shadow>();shadowEffect.effectColor=Color.black;shadowEffect.effectDistance=new Vector2(1,-1);p.Rows.Add(label);
            }
            var field=Group(p.Root,"Original private edit rectangle",new Rect(4,120,185,15));var background=field.gameObject.AddComponent<Image>();background.color=Color.clear;
            var text=Group(field,"Private edit text",new Rect(0,0,185,15)).gameObject.AddComponent<Text>();text.font=font;text.fontSize=14;text.color=Color.white;text.alignment=TextAnchor.MiddleLeft;text.supportRichText=false;text.raycastTarget=false;
            p.Input=field.gameObject.AddComponent<InputField>();p.Input.targetGraphic=background;p.Input.textComponent=text;p.Input.lineType=InputField.LineType.SingleLine;p.Input.transition=Selectable.Transition.None;p.Input.contentType=InputField.ContentType.Standard;
            p.Input.characterLimit=60;p.Input.onValidateInput=(value,index,added)=>server.AcceptsPrivateText(value.Insert(index,added.ToString()))?added:'\0';
            p.Input.onEndEdit.AddListener(value=>
            {
                if(!(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter))||!PersonalAvailable||BattleMap())return;
                if(server.Request_PrivateText(p.Id,p.Session,value))p.Input.SetTextWithoutNotify("");p.Input.ActivateInputField();
            });
            privateWindows.Add(p.Id,p);return p;
        }
        private void PaintPrivateChats()
        {
            string session=server.PrivateChatSession;if(privateWindowSession!=session){ClearPrivateWindows();privateWindowSession=session;}
            var items=server.PrivateChats;var live=new HashSet<string>(items.Select(v=>v.id));
            foreach(string id in privateWindows.Keys.Where(id=>!live.Contains(id)).ToArray())
            {var p=privateWindows[id];p.Root.gameObject.SetActive(false);Destroy(p.Root.gameObject);privateWindows.Remove(id);}
            foreach(var item in items)
            {
                if(!privateWindows.TryGetValue(item.id,out var p))p=CreatePrivateWindow(item,session);if(p==null)continue;
                bool visible=!BattleMap()&&(view.presentation==null||view.presentation.completed);p.Root.gameObject.SetActive(visible);p.Input.interactable=visible&&PersonalAvailable;
                if(p.Revision==item.revision)continue;p.Revision=item.revision;
                for(int row=0;row<p.Rows.Count;row++)
                {
                    var label=p.Rows[row];label.gameObject.SetActive(row<item.lines.Length);if(row>=item.lines.Length)continue;
                    label.text=item.lines[row].Text;label.color=item.lines[row].Kind==1?Color.white:Color.cyan;
                }
            }
        }
    }
}
