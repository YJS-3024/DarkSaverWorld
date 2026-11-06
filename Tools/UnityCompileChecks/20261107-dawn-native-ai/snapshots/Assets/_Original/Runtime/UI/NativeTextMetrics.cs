using System;
using System.Collections.Generic;
namespace DarkSaver.Original.UI
{
    [Serializable] public sealed class NativeTextMapping
    {
        public int[] codepoints,glyphs;
        private Dictionary<int,int> lookup;
        public int Glyph(char value)
        {
            if(lookup==null){lookup=new Dictionary<int,int>();if(codepoints!=null&&glyphs?.Length==codepoints.Length)for(int i=0;i<codepoints.Length;i++)lookup[codepoints[i]]=glyphs[i];}
            return lookup.TryGetValue(value,out int glyph)?glyph:31;
        }
        public int Advance(char value,bool paragraph=false)
        {
            if(value==' '||paragraph&&value=='\t')return 5;
            if(value<'\x80')return 7;
            if(Glyph(value)!=31)return 14;
            return 7;
        }
    }
    public readonly struct NativeTextCell
    {
        public readonly int Glyph,X,Y;
        public NativeTextCell(int glyph,int x,int y){Glyph=glyph;X=x;Y=y;}
    }
    // Pixel font metrics are native. Window wrapping/alignment is the uGUI adapter's bounded layout.
    public static class NativeTextMetrics
    {
        public const int GlyphSize=14,Columns=64,GlyphCount=2592;
        // Single-line43DDF0/43E0C0 uses literal GDI TA_RIGHT(2)/TA_CENTER(6),
        // including truncating width/2 before subtraction for odd advances.
        public static int LineOriginX(int anchor,int width,int alignment)=>alignment==2?anchor-width:alignment==6?anchor-width/2:anchor;
        public static bool GlyphVisible(double x,double y,double width,double height)=>x>=0&&y>=0&&x+GlyphSize<=width&&y+GlyphSize<=height;
        public static int Width(string text,NativeTextMapping map)
        {
            int width=0;foreach(char c in text??""){if(c=='\0')break;if(c=='\n'||c=='\r')width=0;else width+=map.Advance(c);}return width;
        }
        private static List<(List<(int glyph,int x)> cells,int width)> Lines(string text,NativeTextMapping map,int width,bool wrap,bool nativeParagraph=false)
        {
            var lines=new List<(List<(int,int)>,int)>();var line=new List<(int,int)>();int x=0;
            Action finish=()=>{lines.Add((line,x));line=new List<(int,int)>();x=0;};
            foreach(char c in (text??"").Replace("\r\n","\n"))
            {
                if(c=='\0')break;
                if(nativeParagraph&&c=='\r')continue;
                if(c=='\n'||c=='\r'){finish();continue;}
                int advance=map.Advance(c,true);if(!nativeParagraph&&wrap&&x>0&&x+advance>width)finish();
                if(c!=' '&&c!='\t')line.Add((map.Glyph(c),x));x+=advance;
                if(nativeParagraph&&wrap&&x>=width)finish();
            }
            finish();return lines;
        }
        public static int ParagraphHeight(string text,NativeTextMapping map,int width,bool wrap)=>Lines(text,map,width,wrap).Count*GlyphSize;
        public static List<NativeTextCell> Layout(string text,NativeTextMapping map,int width,int height,bool wrap,int horizontal,int vertical,bool truncate,bool nativeParagraph=false)
        {
            var result=new List<NativeTextCell>();var lines=Lines(text,map,width,wrap,nativeParagraph);
            int shown=truncate?Math.Min(lines.Count,Math.Max(0,height/GlyphSize)):lines.Count;
            int total=shown*GlyphSize,y=vertical==1?(height-total)/2:vertical==2?height-total:0;
            for(int i=0;i<shown;i++)
            {
                int left=horizontal==1?(width-lines[i].width)/2:horizontal==2?width-lines[i].width:0;
                foreach(var cell in lines[i].cells)result.Add(new NativeTextCell(cell.glyph,left+cell.x,y+i*GlyphSize));
            }
            return result;
        }
    }
}
