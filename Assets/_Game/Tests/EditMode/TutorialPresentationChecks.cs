using System;
using DarkSaver.Events;
namespace DarkSaver.Tests
{
    public static class TutorialPresentationChecks
    {
        public static void Run(Action<bool,string> check)
        {
            foreach(int bit in new[]{1,2,4,8,16,32,64,128})
            {
                check(TutorialPresentationRules.IconMask(bit,255,false)==bit,"new lesson selects its own icon "+bit);
                check(TutorialPresentationRules.IconMask(0,bit,false)==bit,"zero-mask blink keeps previous lesson icon "+bit);
                check(TutorialPresentationRules.IconMask(0,bit,true)==0,"interactive zero mask remains unavailable "+bit);
            }
            check(TutorialPresentationRules.IconMask(0,0,false)==0,"no icon invented before first highlight");
            check(TutorialPresentationRules.RangeMode("Move",false,false,false,false)==TutorialRangeMode.None,"source command alone cannot pre-display movement");
            check(TutorialPresentationRules.RangeMode("Attack",false,false,false,false)==TutorialRangeMode.None,"source command alone cannot pre-display attack");
            check(TutorialPresentationRules.RangeMode("Move",true,false,false,false)==TutorialRangeMode.Move,"movement appears after explicit selection");
            check(TutorialPresentationRules.RangeMode("Attack",false,true,false,false)==TutorialRangeMode.Attack,"attack appears after explicit selection");
            check(TutorialPresentationRules.RangeMode("",true,true,true,false)==TutorialRangeMode.None,"cleared source command invalidates stale selection");
            check(TutorialPresentationRules.RangeMode("Attack",true,false,false,false)==TutorialRangeMode.None,"stale movement selection cannot expose attack range");
            check(TutorialPresentationRules.RangeMode("Move",true,false,false,true)==TutorialRangeMode.None,"open menu or movement hides ranges");
            check(TutorialPresentationRules.RangeMode("MagicTarget",false,false,true,false)==TutorialRangeMode.Magic,"explicit spell selection still displays range");
            TutorialPresentationRules.PlaceMenu(60,250,out float x,out float y);
            check(x==12 && y==202,"menu remains centered on actor even inside status window area");
            TutorialPresentationRules.PlaceMenu(60,250,out x,out y);
            check(x+48==60 && y+48==250,"status overlap cannot displace actor anchor");
            TutorialPresentationRules.PlaceMenu(60,250,out x,out y);
            check(x==12 && y==202,"repeated placement preserves actor anchor");
            foreach(var pos in new[]{(-50f,-50f),(0f,0f),(640f,480f),(700f,550f)})
            {
                TutorialPresentationRules.PlaceMenu(pos.Item1,pos.Item2,out x,out y);
                check(x>=8 && x+96<=632 && y>=8 && y+96<=472,"menu remains inside logical screen "+pos);
            }
        }
    }
}
