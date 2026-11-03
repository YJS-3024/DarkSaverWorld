using System;
using System.Collections.Generic;
using System.Globalization;
using DarkSaver.Original.World;
namespace DarkSaver.Original.UI
{
    public readonly struct NativeShopOutline
    {public readonly int Left,Top,Right,Bottom;public NativeShopOutline(int x,int y){Left=x;Top=y;Right=x+33;Bottom=y+33;}}
    public sealed class NativeShopSelection
    {
        public int Index{get;private set;}=-1;public bool Right{get;private set;}
        public void Clear(){Index=-1;}
        public void Select(bool right,int index){Right=right;Index=index;}
        public bool Cell(bool right,int cell,int offset)=>Index>=0&&right==Right&&Index==(right?offset+cell:cell);
        public bool Outline(int offset,out NativeShopOutline rectangle)
        {
            rectangle=default;int cell=Right?Index-offset:Index;if(cell<0||cell>=(Right?12:28))return false;
            rectangle=new NativeShopOutline((Right?278:20)+cell%(Right?3:7)*35,40+cell/(Right?3:7)*35);return true;
        }
        //44AA70 uses a1px COLORREF40FFFF pen and CDC::SelectStockObject(5),
        //NULL_BRUSH. It draws before colorkey item sprites, not over them.
        public static byte[] BorderPixels()
        {
            var result=new byte[33*33*4];for(int y=0;y<33;y++)for(int x=0;x<33;x++)if(x==0||x==32||y==0||y==32)
            {int at=(y*33+x)*4;result[at]=255;result[at+1]=255;result[at+2]=64;result[at+3]=255;}return result;
        }
    }
    [Serializable] public sealed class NativeShopDetail
    {public int id;public string properties;}
    [Serializable] public sealed class NativeShopDetailsCatalog
    {
        public NativeShopDetail[] items;
        private Dictionary<int,string> lookup;
        public string Properties(int id)
        {
            if(lookup==null){lookup=new Dictionary<int,string>();foreach(var row in items??Array.Empty<NativeShopDetail>())lookup[row.id]=row.properties;}
            return lookup.TryGetValue(id,out var value)?value:null;
        }
    }
    public static class NativeShopDetails
    {
        public const int X=16,Y=187,Width=388,Height=63;
        //44B1C0 calls this predicate "판매여부" even though it reads the
        //commander's class and required level. Do not substitute a sale quote.
        public static bool? Usable(WorldItemDefinition item,int? level,int? kind)
        {
            if(item==null||!level.HasValue)return null;
            if(item.requiredLevel>level.Value)return false;
            if(!kind.HasValue||item.jobKinds==null)return null;
            return Array.IndexOf(item.jobKinds,kind.Value)>=0;
        }
        public static string[] Paragraphs(WorldItemDefinition item,bool? usable,string properties)
        {
            if(item==null)return Array.Empty<string>();
            string name=string.IsNullOrEmpty(item.name)?"이름 없음":item.name,description=string.IsNullOrEmpty(item.description)?"설명 없음":item.description;
            string price=item.price<0?"—":item.price.ToString(CultureInfo.InvariantCulture),allowed=usable.HasValue?(usable.Value?"가능":"불가"):"—";
            return new[]{"이름: "+name+"   가격: "+price+"  판매여부: "+allowed,"설명: "+description,properties??""};
        }
        //436B00 stores each paragraph as separate CStringList lines.43FDF0
        //splits BEFORE the glyph reaching the limit, unlike the memo renderer.
        public static string Lines(string[] paragraphs,NativeTextMapping map)
        {
            if(map==null)return string.Join("\n",paragraphs??Array.Empty<string>());
            var lines=new List<string>();
            foreach(string paragraph in paragraphs??Array.Empty<string>())
            {
                string remaining=paragraph??"";
                while(NativeTextMetrics.Width(remaining,map)>Width)
                {
                    int x=0,split=0;
                    for(;split<remaining.Length;split++)
                    {char c=remaining[split];if(c=='\r'||c=='\n')x=0;else x+=map.Advance(c);if(x>=Width)break;}
                    if(split<1||split>=remaining.Length)break;
                    lines.Add(remaining.Substring(0,split));remaining=remaining.Substring(split);
                }
                lines.Add(remaining);
            }
            return string.Join("\n",lines);
        }
    }
}
