using System;
using UnityEngine;
using UnityEngine.UI;
namespace DarkSaver.UI
{
    public sealed partial class InGameUi
    {
        private readonly System.Collections.Generic.Dictionary<string,(string Text,int Width,int MaximumLines,string[] Lines)> bitmapTextLines=new();
        public void NativeStatusName(string key, Rect rect, string text)
        {
            var clip = Node(key, rect); Clip(clip);Events(key).Tooltip=text??"";
            int pen = 0;
            for (int i = 0; text != null && i < text.Length && pen < rect.width; i++)
            {
                if (text[i] != ' ') Portrait(key + "-glyph-" + i, new Rect(pen, 0, 14, 14), NativeStatusFont.Glyph(text[i]), clip);
                pen += NativeStatusFont.Advance(text[i]);
            }
        }
        public void NativeBitmapText(string key, Rect rect, string text, Color color, int maximumLines = 1,
            TextAnchor alignment = TextAnchor.MiddleLeft, float textScale = 1f)
        {
            textScale=Mathf.Clamp(textScale,.5f,1f);
            int width = Mathf.Max(14, Mathf.FloorToInt(rect.width/textScale));
            if (!bitmapTextLines.TryGetValue(key, out var cached) || cached.Text != text || cached.Width != width || cached.MaximumLines != maximumLines) {
                cached = (text, width, maximumLines, maximumLines == 1 ? new[] {text ?? ""} : NativeStatusFont.WrapLines(text, width)); bitmapTextLines[key] = cached;
            }
            var clip = Node(key, rect);
            Clip(clip);Events(key).Tooltip=text??"";
            int rows = Mathf.Min(Mathf.Min(cached.Lines.Length, Mathf.Max(0, maximumLines)), Mathf.FloorToInt(rect.height / (14*textScale)));
            float top = Mathf.Max(0, Mathf.Floor((rect.height - (rows * 16 - 2)*textScale) / 2));
            for (int row = 0; row < rows; row++) {
                string line = cached.Lines[row]; float measured = NativeStatusFont.Measure(line)*textScale;
                float pen = alignment == TextAnchor.MiddleRight ? rect.width - measured
                    : alignment == TextAnchor.MiddleCenter ? Mathf.Floor((rect.width - measured) / 2) : 0;
                for (int i = 0; i < line.Length; i++) {
                    if (line[i] != ' ') {
                        string glyphKey = key + "-glyph-" + row + "-" + i;
                        Portrait(glyphKey, new Rect(pen, top + row * 16*textScale, 14*textScale, 14*textScale), NativeStatusFont.WorldGlyph(line[i]), clip);
                        Find<RawImage>(glyphKey).color = color;
                    }
                    pen += NativeStatusFont.Advance(line[i])*textScale;
                }
            }
            clip.SetAsLastSibling();
        }
        public void NativeWorldName(string key, Vector2 bottomCenter, string text)
        {
            var width = NativeStatusFont.Measure(text);
            var rect = new Rect(Mathf.Round(bottomCenter.x - width * .5f), Mathf.Round(bottomCenter.y - 14), width, 14);
            var clip = Node(key, rect); Clip(clip);Events(key).Tooltip=text??"";
            int pen = 0;
            for (int i = 0; text != null && i < text.Length; i++)
            {
                if (text[i] != ' ')
                {
                    var glyphKey = key + "-glyph-" + i;
                    Portrait(glyphKey, new Rect(pen, 0, 14, 14), NativeStatusFont.WorldGlyph(text[i]), clip);
                    Find<RawImage>(glyphKey).color = Ink;
                }
                pen += NativeStatusFont.Advance(text[i]);
            }
        }
        public void NativeStatusGauge(string key, Vector2 position, int value, int maximum, int row, bool showNumbers = true, int gaugeWidth = 80)
        {
            int width = NativeBattleStatus.FillWidth(value, maximum, gaugeWidth);
            if (width > 0)
            {
                var clip = Node(key + "-clip", new Rect(position.x, position.y, width, 9));
                Clip(clip);
                // Crop the full 80px strip; do not squash its texture to the remaining fill width.
                Portrait(key + "-fill", new Rect(0, 0, 80, 9), NativeBattleStatus.Gauge(row), clip);
            }
            if (!showNumbers || maximum <= 0) return;
            var text = System.Math.Max(0, value).ToString(System.Globalization.CultureInfo.InvariantCulture) + "/"
                + maximum.ToString(System.Globalization.CultureInfo.InvariantCulture);
            NativeStatusNumber(key, new Vector2(position.x + 4, position.y + 1), text);
        }
        public void NativeBitmapScrollText(string key,Rect rect,string text,NativeBattleWindows.Control up,NativeBattleWindows.Control down,bool resetPosition=false)
        {
            const int lineHeight=16;int width=Mathf.Max(14,Mathf.FloorToInt(rect.width-19));
            bool changed=!bitmapTextLines.TryGetValue(key,out var cached)||cached.Text!=text||cached.Width!=width||cached.MaximumLines!=-1;
            if(changed){cached=(text,width,-1,NativeStatusFont.WrapLines(text,width));bitmapTextLines[key]=cached;}
            float height=Mathf.Max(rect.height,cached.Lines.Length*lineHeight);var content=Scroll(key+"-scroll",new Rect(rect.x,rect.y,width,rect.height),height,resetPosition||changed);
            float maximum=Mathf.Max(0,height-rect.height);float offset=Mathf.Clamp(content.anchoredPosition.y,0,maximum);content.anchoredPosition=new Vector2(0,offset);
            int first=Mathf.Clamp(Mathf.FloorToInt(offset/lineHeight),0,cached.Lines.Length-1);
            int visible=Mathf.Min(cached.Lines.Length-first,Mathf.CeilToInt(rect.height/lineHeight)+1);
            for(int row=0;row<visible;row++){string line=cached.Lines[first+row];int pen=0;for(int i=0;i<line.Length;i++){if(line[i]!=' ')Portrait(key+"-row-"+row+"-glyph-"+i,new Rect(pen,(first+row)*lineHeight,14,14),NativeStatusFont.Glyph(line[i]),content);pen+=NativeStatusFont.Advance(line[i]);}}
            Events(key+"-scroll").Tooltip=text??"";Raise(key+"-scroll");
            NativeImageButton(key+"-up",new Vector2(rect.xMax-up.rect.width,rect.y),up,()=>content.anchoredPosition=new Vector2(0,Mathf.Max(0,content.anchoredPosition.y-lineHeight)),offset>.5f,"설명 위로");
            NativeImageButton(key+"-down",new Vector2(rect.xMax-down.rect.width,rect.yMax-down.rect.height),down,()=>content.anchoredPosition=new Vector2(0,Mathf.Min(maximum,content.anchoredPosition.y+lineHeight)),offset<maximum-.5f,"설명 아래로");
        }
        public void NativeStatusNumber(string key,Vector2 position,string text,string tooltip=null)
        {
            if(text==null)return;
            for(int i=0;i<text.Length;i++){Portrait(key+"-digit-"+i,new Rect(position.x+i*5,position.y,5,7),NativeBattleStatus.Digit(text[i]));Raise(key+"-digit-"+i);}
            if(!string.IsNullOrEmpty(tooltip)&&text.Length>0){string hitKey=key+"-tooltip";Graphic(Node(hitKey,new Rect(position.x,position.y,text.Length*5,7)),null,Color.clear,true);var events=Events(hitKey);events.Tooltip=tooltip;events.Enter=()=>TryShowNativeStatusDetails(key);events.Exit=()=>{if(hoveredStatKey==hitKey)hoveredStatKey=null;};}
        }
        public bool TryShowNativeStatusDetails(string key){string hit=key+"-tooltip";if(!used.Contains(hit)||!elements.ContainsKey(hit))return false;hoveredStatKey=hit;return true;}
    }
}
