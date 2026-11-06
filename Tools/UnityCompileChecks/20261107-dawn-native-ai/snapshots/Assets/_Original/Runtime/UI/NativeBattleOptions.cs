using System;
namespace DarkSaver.Original.UI
{
    // GameCtl10004A80 scene4. These are process-local UI messages, not network opcodes.
    public static class NativeBattleOptions
    {
        public const int Open=0x4ca,Close=0x4cb,Minimize=0x4cf,Position=0x4d0,Exists=0x4d1;
        public static void Activate(int control,int activeCommanders,Func<int,int,int,int,int> request)
        {
            if(request==null)throw new ArgumentNullException(nameof(request));
            switch(control)
            {
                case 0:if(request(Exists,28,0,0)==0){request(Open,28,0,0);request(Position,28,228,186);}break;
                case 1:request(Open,7,0,0);request(Position,7,10,10);break;
                // The original opens6 but sends the position to2; preserve the source request.
                case 2:request(Open,6,0,0);request(Position,2,512,136);break;
                case 3:
                    int scene=24+Math.Max(0,activeCommanders-1);
                    if(request(Exists,scene,0,0)==0){request(Open,scene,0,0);request(Position,scene,5,231);}
                    else for(int id=24;id<=27;id++)request(Close,id,0,0);
                    break;
                case 4:request(Open,0,0,0);request(Position,0,515,5);break;
                case 5:request(Open,5,0,0);request(Position,5,228,186);break;
                case 6:request(Open,3,0,0);request(Position,3,450,302);break;
                case 7:request(Open,1,0,0);request(Close,9,0,0);request(Position,1,5,5);break;
                case 8:request(Close,4,0,0);break;
            }
        }
        //10002FB0 keyboard toggles differ from scene4's open-only buttons.
        public static void Key(int key,int activeCommanders,bool hasOwnTeam,Func<int,int,int,int,int> request)
        {
            if(request==null)throw new ArgumentNullException(nameof(request));
            int scene,x,y;
            switch(key)
            {
                case 0x70:scene=7;x=10;y=10;break;
                case 0x71:scene=1;x=5;y=180;break;
                case 0x72:scene=3;x=450;y=302;break;
                case 0x73:scene=5;x=228;y=186;break;
                case 0x74:
                    if(hasOwnTeam)
                    {
                        scene=24+Math.Max(0,activeCommanders-1);
                        if(request(Exists,scene,0,0)==0){request(Open,scene,0,0);request(Position,scene,5,231);return;}
                    }
                    for(int id=24;id<=27;id++)request(Close,id,0,0);return;
                case 0x75:scene=0;x=515;y=5;break;
                case 0x77:scene=6;x=512;y=136;break;
                case 0x78:scene=28;x=228;y=186;break;
                case 0x7b:request(Open,4,0,0);return;
                default:return;
            }
            if(request(Exists,scene,0,0)!=0){request(Close,scene,0,0);return;}
            request(Open,scene,0,0);if(scene==1)request(Close,9,0,0);request(Position,scene,x,y);
        }
        public static string[] Briefing(string source,int playerTeam=0)
        {
            string victory=playerTeam<0?(playerTeam==-99?"관전모드":"???"):"적의 격파",defeat=playerTeam<0?"":"주지휘관의 사망";
            foreach(string row in (source??"").Replace("\r\n","\n").Split('\n'))
            {
                int split=row.IndexOf(' ');string token=split<0?row:row.Substring(0,split).Trim();
                string text=split<0?"":row.Substring(split+1).TrimStart();
                if(token=="V1")victory=text;else if(token=="V2")defeat=text;
            }
            return new[]{victory,defeat};
        }
        public static bool PositionAllowed(int scene,bool tutorial)=>tutorial||scene==8||scene==12||scene==28;
        public static int ActiveCommanderCount(bool[] active,bool[] enabled)
        {int count=0;for(int i=0;i<4;i++)if(active!=null&&enabled!=null&&i<active.Length&&i<enabled.Length&&active[i]&&enabled[i])count++;return count;}
        //10004DD0/10005200 use the inclusive19-line draw loop and a count-18 limit.
        public static int ScrollHelp(int offset,int control,int lineCount)
        {if(control==0)return Math.Max(0,unchecked(offset-1));if(control==1)return Math.Min(unchecked(offset+1),unchecked(lineCount-18));return offset;}
    }
    public static class NativeBattleMenuIcons
    {
        //1000A800 animates icon-surface menus; disabled action replacements stay on row128.
        public static int NormalY(int sourceY,double milliseconds)
        {int clock=unchecked((int)(long)milliseconds);return unchecked(sourceY+((clock/300)%2==0?160:0));}
        public static int ActionY(bool enabled,double milliseconds)=>enabled?NormalY(0,milliseconds):128;
    }
    public static class NativeBattleStatusFields
    {
        private static readonly uint[] conditionBits={1,0x100,0x80,8,0x40,0x20,4,0x10,0x400,0x200,0x800,0x4000,0x1000,0x2000,2,0x8000};
        private static readonly int[] conditionSources={251,267,283,363,315,331,347,299,379,395,411,427,443,459,475,491};
        private static readonly int[] conditionX={23,42,61,80,23,42,61,80,23,80,23,80,23,23,23,23};
        private static readonly int[] conditionY={59,59,59,59,116,116,116,116,78,78,97,97,59,59,59,59};
        public const int ConditionCount=16;
        public static int ConditionX(int visibleIndex)=>visibleIndex>=0&&visibleIndex<ConditionCount?conditionX[visibleIndex]:0;
        public static int ConditionY(int visibleIndex)=>visibleIndex>=0&&visibleIndex<ConditionCount?conditionY[visibleIndex]:0;
        public static uint ConditionMask(int index)=>index>=0&&index<ConditionCount?conditionBits[index]:0;
        public static int ConditionSourceX(int index)=>index>=0&&index<ConditionCount?conditionSources[index]:-1;
        //100054C0/10006830/100063E0 compute native32-bit products before division.
        public static int SignedWidth(int value,int maximum,int width)=>maximum==0?0:unchecked(Math.Min(value,maximum)*width)/maximum;
        public static int ExperienceWidth(ulong value,uint maximum,int width)=>maximum==0?0:(int)(unchecked(Math.Min((uint)value,maximum)*(uint)width)/maximum);
        public static string Experience(ulong value,uint maximum)=>unchecked((uint)value).ToString(System.Globalization.CultureInfo.InvariantCulture)+"/"+maximum.ToString(System.Globalization.CultureInfo.InvariantCulture);
        //100053F0 maps every non-digit byte to the same slash glyph.
        public static int GlyphX(char value)=>value>='0'&&value<='9'?380+(value-'0')*5:375;
        public static int ApHeight(bool full,bool ready)=>full||!ready?9:8;
        public static int ApOffset(bool troop,bool ready)=>troop&&ready?1:0;
        public static int HpOffset(bool troop)=>troop?14:13;
        public static int MpOffset(bool troop)=>troop?27:26;
    }
    public static class NativeBattleStatusSelection
    {
        //10002D50 preserves the inspected unit when the mouse moves over an empty tile.
        public static int Hover(int current,int found)=>found>=0?found:current;
        //10009890 case0 writes even an empty hit; Draw then falls back to the local commander.
        public static int Click(int found)=>found;
        public static int Display(int selected,int ownCommander)=>selected>=0?selected:ownCommander;
    }
    public sealed class NativeBattleCompactClickClock
    {
        private int last;
        public bool Press(int milliseconds){bool restore=unchecked(milliseconds-last)<400;last=milliseconds;return restore;}
    }
    public static class NativeBattleMinimap
    {
        // GameCtl10004830, whole-window coordinates; the title does not move the camera.
        public static bool Camera(int x,int y,int width,int height,out int left,out int top)
        {
            left=top=0;if(y<=20)return false;
            left=Math.Min(Math.Max(0,unchecked((x-11)*width)/100-10),width-20);
            top=Math.Min(Math.Max(0,unchecked((y-25)*height)/100-7),height-15);return true;
        }
    }
}
