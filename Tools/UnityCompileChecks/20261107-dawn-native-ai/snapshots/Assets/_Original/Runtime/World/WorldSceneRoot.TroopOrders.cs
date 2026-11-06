using System.Collections.Generic;
using UnityEngine;
using DarkSaver.Original.UI;
using UnityEngine.UI;
using DarkSaver.Original.Entry;

namespace DarkSaver.Original.World
{
    public sealed partial class WorldSceneRoot
    {
        private RectTransform battleOrderMenu;
        private bool showingBattleOrders;
        private readonly List<(int choice,Button hit,RawImage image)> battleOrderButtons=new List<(int,Button,RawImage)>();
        private bool BattleOrderAvailable()
        {
            var actor=view?.battle?.Commander;
            return BattleMap()&&actor?.canCommand==true&&OriginalBattleTicks.AllowsIcon(actor,4)&&OriginalCommandRange.Living(actor)&&!view.record.ghost&&!view.moving&&view.battle.pending==null
                &&view.facility==null&&notice==null;
        }
        private void BuildBattleOrders()
        {
            if(battleOrderMenu!=null)Destroy(battleOrderMenu.gameObject);battleOrderButtons.Clear();showingBattleOrders=false;
            battleOrderMenu=Group(worldWindows,"Native mercenary orders",new Rect(0,0,96,96));
            for(int i=1;i<=3;i++)
            {
                int choice=i;var hit=Hit(battleOrderMenu,"Native troop order "+OriginalTroopOrders.Name(choice),new Rect(OriginalTroopOrders.MenuX(choice),OriginalTroopOrders.MenuY(choice),32,32),()=>
                {
                    if(!BattleOrderAvailable())return;showingBattleOrders=false;
                    Handle(DisplayMap(view.record.map).training?server.Request_TrainingOrder(choice):server.Request_BattleOrder(choice));
                });
                var image=Picture((RectTransform)hit.transform,"Original direct attack defense icon",new Rect(0,0,32,32),art.battle.icons);
                battleOrderButtons.Add((choice,hit,image));
            }
            battleOrderMenu.gameObject.SetActive(false);
        }
        private void PaintBattleOrders()
        {
            if(battleOrderMenu==null)return;bool available=BattleOrderAvailable();
            if(!available)showingBattleOrders=false;
            battleOrderMenu.gameObject.SetActive(showingBattleOrders&&available&&!showingActions&&!showingBattleSpells);
            if(!battleOrderMenu.gameObject.activeSelf)return;
            Place(battleOrderMenu,new Rect((float)view.renderX*32+mapRoot.anchoredPosition.x-32,(float)view.renderY*32-mapRoot.anchoredPosition.y-32,96,96));
            foreach(var button in battleOrderButtons)
            {
                button.hit.interactable=available;
                button.image.uvRect=NativeEntryLayout.Uv(art.battle.icons,new Rect(OriginalTroopOrders.IconX(button.choice),NativeBattleMenuIcons.NormalY(64,BattleIconClock),32,32));
            }
        }
    }
}
