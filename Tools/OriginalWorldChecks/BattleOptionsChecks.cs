using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.UI;
internal static class BattleOptionsChecks
{
    static int count;
    static void Check(bool value,string label){count++;if(!value)throw new Exception(label);}
    static void Main(string[] args)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(args[1]));
        foreach(var row in doc.RootElement.GetProperty("options").EnumerateArray())
        {
            var active=row.GetProperty("active").EnumerateArray().Select(v=>v.GetBoolean()).ToArray();var enabled=row.GetProperty("enabled").EnumerateArray().Select(v=>v.GetBoolean()).ToArray();var exists=row.GetProperty("existing").EnumerateArray().Select(v=>v.GetInt32()).ToArray();var trace=new List<int[]>();
            NativeBattleOptions.Activate(row.GetProperty("control").GetInt32(),NativeBattleOptions.ActiveCommanderCount(active,enabled),(message,scene,x,y)=>
            {
                if(message==NativeBattleOptions.Position&&!NativeBattleOptions.PositionAllowed(scene,false))return -1;
                trace.Add(new[]{message,scene,x,y});return message==NativeBattleOptions.Exists&&exists.Contains(scene)?1:0;
            });
            Check(JsonSerializer.Serialize(trace)==row.GetProperty("expected").GetRawText().Replace(" ",""),"native complete ordered UI trace "+row.GetRawText());
        }
        foreach(var row in doc.RootElement.GetProperty("keys").EnumerateArray())
        {
            var active=row.GetProperty("active").EnumerateArray().Select(v=>v.GetBoolean()).ToArray();var enabled=row.GetProperty("enabled").EnumerateArray().Select(v=>v.GetBoolean()).ToArray();var exists=row.GetProperty("existing").EnumerateArray().Select(v=>v.GetInt32()).ToArray();var trace=new List<int[]>();
            NativeBattleOptions.Key(row.GetProperty("key").GetInt32(),NativeBattleOptions.ActiveCommanderCount(active,enabled),row.GetProperty("team").GetInt32()>=0,(message,scene,x,y)=>
            {
                if(message==NativeBattleOptions.Position&&!NativeBattleOptions.PositionAllowed(scene,false))return -1;
                trace.Add(new[]{message,scene,x,y});return message==NativeBattleOptions.Exists&&exists.Contains(scene)?1:0;
            });
            Check(JsonSerializer.Serialize(trace)==row.GetProperty("expected").GetRawText().Replace(" ",""),"native key UI trace "+row.GetRawText());
        }
        foreach(var row in doc.RootElement.GetProperty("scroll").EnumerateArray())
            Check(NativeBattleOptions.ScrollHelp(row.GetProperty("offset").GetInt32(),row.GetProperty("control").GetInt32(),row.GetProperty("lines").GetInt32())==row.GetProperty("expected").GetInt32(),"native help repeat bounds");
        foreach(var row in doc.RootElement.GetProperty("compact").EnumerateArray())
        {
            var clock=new NativeBattleCompactClickClock();int index=0;
            foreach(var value in row.GetProperty("clocks").EnumerateArray())
            {bool actual=clock.Press(unchecked((int)value.GetInt64()));var target=row.GetProperty("expected")[index++];Check(actual==(target.GetProperty("trace").GetArrayLength()!=0),"native400ms compact status press restoration");}
        }
        foreach(var row in doc.RootElement.GetProperty("minimap").EnumerateArray())
        {
            bool moved=NativeBattleMinimap.Camera(row.GetProperty("x").GetInt32(),row.GetProperty("y").GetInt32(),row.GetProperty("width").GetInt32(),row.GetProperty("height").GetInt32(),out int left,out int top);
            if(!moved){left=13;top=12;}var expected=row.GetProperty("expected");Check(left==expected[0].GetInt32()&&top==expected[1].GetInt32(),"native minimap title guard and camera bounds");
        }
        foreach(var row in doc.RootElement.GetProperty("hovers").EnumerateArray())
            Check(NativeBattleStatusSelection.Hover(row.GetProperty("selected").GetInt32(),row.GetProperty("found").GetInt32())==row.GetProperty("expected").GetInt32(),"actual native mouse hover preserves last unit on empty tile");
        foreach(var row in doc.RootElement.GetProperty("statuses").EnumerateArray())
        {
            int scene=row.GetProperty("scene").GetInt32(),width=scene==1?80:48,x=scene==1?30:scene==9?67:65,y=scene==1?143:scene==9?8:34;
            bool troop=scene==24,dead=troop&&row.GetProperty("hp").GetInt32()==0;var blits=new List<int[]>();var digits=new List<object[]>();
            void Bar(int value,int maximum,int sourceY,int dy,int height=9){if(maximum==0)return;int n=NativeBattleStatusFields.SignedWidth(value,maximum,width);blits.Add(new[]{x,y+dy,2,sourceY,n+2,sourceY+height});}
            if(!dead)
            {
                int ap=row.GetProperty("ap").GetInt32(),maxAp=row.GetProperty("maxAp").GetInt32();Bar(ap,maxAp,370+(ap>=maxAp?(row.GetProperty("phase").GetInt32()+1)%3*12:0),NativeBattleStatusFields.ApOffset(troop,ap>=maxAp),NativeBattleStatusFields.ApHeight(scene==1,ap>=maxAp));
                int hp=row.GetProperty("hp").GetInt32(),maxHp=row.GetProperty("maxHp").GetInt32(),mp=row.GetProperty("mp").GetInt32(),maxMp=row.GetProperty("maxMp").GetInt32();
                Bar(hp,maxHp,382,NativeBattleStatusFields.HpOffset(troop));Bar(mp,maxMp,394,NativeBattleStatusFields.MpOffset(troop));
                digits.Add(new object[]{x+4,y+NativeBattleStatusFields.HpOffset(troop)+(scene==9?0:1),hp+"/"+maxHp,2});digits.Add(new object[]{x+4,y+NativeBattleStatusFields.MpOffset(troop)+(scene==9?0:1),mp+"/"+maxMp,2});
                if(scene==1)
                {
                    ulong xp=row.GetProperty("xp").GetUInt64();uint next=row.GetProperty("nextXp").GetUInt32();if(next!=0){int n=NativeBattleStatusFields.ExperienceWidth(xp,next,width);blits.Add(new[]{30,182,2,406,n+2,415});digits.Add(new object[]{34,183,NativeBattleStatusFields.Experience(xp,next),2});}
                    digits.Add(new object[]{32,197,"60",2});digits.Add(new object[]{87,197,"20",2});
                }
            }
            var expected=row.GetProperty("expected");var nativeBars=expected.GetProperty("blits").EnumerateArray().Select(b=>new[]{b[0].GetInt32(),b[1].GetInt32()}.Concat(b[2].EnumerateArray().Select(v=>v.GetInt32())).ToArray()).ToArray();
            Check(JsonSerializer.Serialize(blits)==JsonSerializer.Serialize(nativeBars),"actual full/compact/troop native bars and32-bit widths "+row.GetRawText()+" calculated="+JsonSerializer.Serialize(blits));
            Check(JsonSerializer.Serialize(digits)==expected.GetProperty("digits").GetRawText().Replace(", ",","),"actual native numeric strings and baseline positions");
            if(dead)Check(expected.GetProperty("bodies").GetArrayLength()==1&&expected.GetProperty("bodies")[0][2].GetInt32()==224,"dead troop uses mcursor skull and skips gauges/numbers");
        }
        foreach(var row in doc.RootElement.GetProperty("menuClocks").EnumerateArray())
        {
            var expected=row.GetProperty("expected");double clock=row.GetProperty("clock").GetDouble();bool disabled=row.GetProperty("disabled").GetBoolean();int sourceY=disabled?NativeBattleMenuIcons.ActionY(false,clock):NativeBattleMenuIcons.NormalY(row.GetProperty("baseY").GetInt32(),clock);
            Check(expected.GetArrayLength()==1&&expected[0][2][1].GetInt32()==sourceY&&expected[0][2][3].GetInt32()==sourceY+32,"actual1000A800 signed300-clock phase and fixed disabled row128");
        }
        foreach(var row in doc.RootElement.GetProperty("conditions").EnumerateArray())
        {
            uint flags=row.GetProperty("flags").GetUInt32();var expected=row.GetProperty("expected");var glyphs=new List<object[]>();
            for(int i=0;i<NativeBattleStatusFields.ConditionCount;i++)if((flags&NativeBattleStatusFields.ConditionMask(i))!=0){int source=NativeBattleStatusFields.ConditionSourceX(i),slot=glyphs.Count;glyphs.Add(new object[]{NativeBattleStatusFields.ConditionX(slot),NativeBattleStatusFields.ConditionY(slot),new[]{source,520,source+16,536}});}
            Check(JsonSerializer.Serialize(glyphs)==expected.GetRawText().Replace(", ",","),"actual native condition-bit order/16px overlay cells, high bits ignored");
        }
        foreach(var row in doc.RootElement.GetProperty("glyphs").EnumerateArray())
        {
            string text=row.GetProperty("text").GetString();int color=row.GetProperty("color").GetInt32(),i=0;
            foreach(var b in row.GetProperty("expected").EnumerateArray()){Check(b[0].GetInt32()==13+i*5&&b[1].GetInt32()==17&&b[2][0].GetInt32()==NativeBattleStatusFields.GlyphX(text[i])&&b[2][1].GetInt32()==328+color*7,"actual source numeric glyph mapping/spacing/palette");i++;}Check(i==text.Length,"all native numeric characters rendered without arbitrary field truncation");
        }
        foreach(var row in doc.RootElement.GetProperty("briefings").EnumerateArray())
        {var expected=row.GetProperty("expected").EnumerateArray().Select(v=>v.GetString()).ToArray();Check(NativeBattleOptions.Briefing(row.GetProperty("source").GetString(),row.GetProperty("team").GetInt32()).SequenceEqual(expected),"native SRF exact comparison/defaults/token mutation "+row.GetRawText());}
        Check(NativeBattleOptions.Briefing("V1 늑대인간 처치\nV2\n").SequenceEqual(new[]{"늑대인간 처치",""}),"source SRF explicit empty defeat");
        Check(NativeBattleOptions.Briefing("v1 lowercase\nv2\n").SequenceEqual(new[]{"적의 격파","주지휘관의 사망"}),"source exact uppercase token comparison");
        Check(NativeBattleOptions.Briefing(" V1 ignored\nV1  first \nV1 last\nV2  defeat  ").SequenceEqual(new[]{"last","defeat  "}),"source first literal space, left-only remainder trim and final assignment");
        Check(NativeBattleOptions.Briefing(null,-99).SequenceEqual(new[]{"관전모드",""})&&NativeBattleOptions.Briefing(null,-1).SequenceEqual(new[]{"???",""}),"source observer defaults");
        Check(NativeBattleOptions.ActiveCommanderCount(null,null)==0&&NativeBattleOptions.ActiveCommanderCount(new[]{true},new[]{true})==1,"missing partial caller context");
        Console.WriteLine(JsonSerializer.Serialize(new{assertions=count,mismatches=0,unityPlay=false}));
    }
}
