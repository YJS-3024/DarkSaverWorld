using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace DarkSaver.UI
{
    public sealed partial class InGameUi
    {
        private sealed class ItemDragState
        {public int id,pointer;public bool armed,dragging;public Vector2 start,position;public Sprite icon;public Rect destination;public Action<int>dropped;}
        private readonly Dictionary<string,ItemDragState> itemDrags=new();
        private Vector2 LogicalPosition(Vector2 screen)
        {RectTransformUtility.ScreenPointToLocalPointInRectangle(root,screen,null,out var local);return new Vector2(local.x,-local.y);}
        public void ItemDragSource(string key,int itemId,Sprite icon,Rect destination,Action<int>dropped)
        {
            var button=Find<UnityEngine.UI.Button>(key);if(button==null)throw new ArgumentException("Create item slot first.");
            if(!itemDrags.TryGetValue(key,out var state))
            {
                state=new ItemDragState();itemDrags.Add(key,state);var captured=state;var events=Events(key);
                events.Down+=e=>{if(e.button!=PointerEventData.InputButton.Left||captured.id<=0||!button.interactable)return;CancelItemDrags();captured.armed=true;captured.pointer=e.pointerId;captured.start=captured.position=LogicalPosition(e.position);};
                events.Move+=e=>{if(!captured.armed||captured.pointer!=e.pointerId)return;captured.position=LogicalPosition(e.position);if(Vector2.Distance(captured.start,captured.position)>=GameConstants.TownItemDragThreshold)captured.dragging=true;if(captured.dragging){events.SuppressClick=true;e.eligibleForClick=false;}};
                events.Up+=e=>{if(!captured.armed||captured.pointer!=e.pointerId||e.button!=PointerEventData.InputButton.Left)return;bool accept=captured.dragging&&Active(key)&&button.interactable&&captured.destination.Contains(LogicalPosition(e.position));bool dragged=captured.dragging;int id=captured.id;var callback=captured.dropped;ResetDrag(captured);if(dragged){events.SuppressClick=true;e.eligibleForClick=false;}if(accept)callback?.Invoke(id);};
                events.Cancel+=()=>ResetDrag(captured);
            }
            if(state.id!=itemId||!button.interactable)ResetDrag(state);
            state.id=itemId;state.icon=icon;state.destination=destination;state.dropped=dropped;
        }
        private static void ResetDrag(ItemDragState state){state.armed=state.dragging=false;}
        public void CancelItemDrags(){foreach(var state in itemDrags.Values)ResetDrag(state);}
        private void OnApplicationFocus(bool focused){if(!focused)CancelItemDrags();}
        private void OnApplicationPause(bool paused){if(paused)CancelItemDrags();}
        private void DrawItemDrag(){foreach(var pair in itemDrags){var state=pair.Value;if(!used.Contains(pair.Key)){ResetDrag(state);continue;}if(state.dragging&&state.icon!=null){Portrait("item-drag-preview",new Rect(state.position-new Vector2(16,16),new Vector2(32,32)),state.icon);Raise("item-drag-preview");}}}
    }
}
