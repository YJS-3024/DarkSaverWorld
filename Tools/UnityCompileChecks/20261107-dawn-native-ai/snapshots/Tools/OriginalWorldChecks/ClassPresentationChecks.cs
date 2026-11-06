using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.UI;
using DarkSaver.Original.World;
internal static class ClassPresentationChecks
{
    static int count;static void Check(bool value,string label){count++;if(!value)throw new Exception(label);}
    static void Main(string[] args)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(args[1]));var json=new JsonSerializerOptions{IncludeFields=true};
        foreach(var row in doc.RootElement.GetProperty("magic").EnumerateArray())
        {
            var item=row.GetProperty("item").ValueKind==JsonValueKind.Null?null:JsonSerializer.Deserialize<MagicMenuEntry>(row.GetProperty("item").GetRawText(),json);var expected=row.GetProperty("expected");
            Check((item?.name??"")==expected[0].GetString()&&NativeClassPresentation.MagicKind(item)==expected[1].GetString()&&NativeClassPresentation.MagicDetails(item)==expected[2].GetString(),"actual462740 source name/type/positive detail branches");
        }
        foreach(var row in doc.RootElement.GetProperty("icons").EnumerateArray())
        {
            var item=JsonSerializer.Deserialize<MagicMenuEntry>(row.GetProperty("item").GetRawText(),json);var expected=row.GetProperty("expected");bool drawn=row.GetProperty("surface").GetBoolean()&&row.GetProperty("matching").GetBoolean()&&!row.GetProperty("drag").GetBoolean();
            Check(expected.GetArrayLength()==(drawn?1:0),"actual4635C0/463720 suppress missing/mismatched/dragged icons");
            if(drawn){var origin=NativeClassPresentation.MagicIconOrigin(item);var rect=expected[0].GetProperty("rect");Check(rect[0].GetInt32()==origin.X&&rect[1].GetInt32()==origin.Y&&rect[2].GetInt32()==origin.X+32&&rect[3].GetInt32()==origin.Y+32,"native class icons use current record coordinates without a synthetic clock phase");}
        }
        foreach(var row in doc.RootElement.GetProperty("ghosts").EnumerateArray())
        {
            int list=row.GetProperty("list").GetInt32(),opposite=row.GetProperty("opposite").GetInt32(),x=row.GetProperty("x").GetInt32(),y=row.GetProperty("y").GetInt32();var state=new NativeClassMagicState{available=Enumerable.Repeat(101,list==0?1:opposite).ToArray(),owned=Enumerable.Repeat(101,list==0?opposite:1).ToArray(),points=0};var expected=row.GetProperty("expected");
            Check(NativeClassMagicDrop.Allowed(state,list,0,x,y)==(expected.GetProperty("denied").GetArrayLength()==0),"actual464380 forbidden overlay/capacity/cell-gap predicate "+row.GetRawText());
            var icon=expected.GetProperty("icon")[0];Check(icon.GetProperty("x").GetInt32()==row.GetProperty("origin")[0].GetInt32()+x-16&&icon.GetProperty("y").GetInt32()==row.GetProperty("origin")[1].GetInt32()+y-16,"native cursor-centered32px ghost survives window offsets");
            if(expected.GetProperty("denied").GetArrayLength()!=0){var denied=expected.GetProperty("denied")[0];Check(denied.GetProperty("x").GetInt32()==icon.GetProperty("x").GetInt32()&&denied.GetProperty("y").GetInt32()==icon.GetProperty("y").GetInt32(),"native bitmap154 shares ghost position");}
        }
        foreach(var row in doc.RootElement.GetProperty("bars").EnumerateArray())
        {
            var current=new AttributeTriplet{before=row.GetProperty("before").GetInt32(),after=row.GetProperty("after").GetInt32(),limit=row.GetProperty("limit").GetInt32()};var initial=new AttributeTriplet{before=row.GetProperty("baseline").GetInt32()};var expected=row.GetProperty("expected");var actual=NativeClassPresentation.StatBar(initial,current);
            Check(actual.Filled==expected.GetProperty("filled").GetInt32()&&actual.Baseline==expected.GetProperty("baseline").GetInt32(),"actual464150 source field/width/min logic with isolated MulDiv");
            Check(NativeClassPresentation.StatText(current)=="장착전:"+current.before+"/장착후:"+current.after+"/한계치:"+current.limit,"original three-field format");
        }
        foreach(var row in doc.RootElement.GetProperty("headers").EnumerateArray())
        {
            var profile=new CommanderProfile{growth=new CharacterGrowthRecord{nativeLevels=new short[]{3,(short)row.GetProperty("level").GetInt32()},experience=row.GetProperty("xp").GetUInt64()}};var native=row.GetProperty("expected");
            Check(NativeClassPresentation.Level(profile)==native[2].GetProperty("text").GetString()&&NativeClassPresentation.Experience(profile)==native[3].GetProperty("text").GetString(),"actual462C50 second level WORD/signed low XP DWORD");
            Check(native[2].GetProperty("y").GetInt32()==row.GetProperty("y").GetInt32()+162&&native[3].GetProperty("y").GetInt32()==row.GetProperty("y").GetInt32()+179,"original header text row positions");
        }
        foreach(var row in doc.RootElement.GetProperty("selections").EnumerateArray())
        {
            var native=row.GetProperty("expected");Check(native.GetProperty("stock")[0].GetInt32()==5,"actual native CDC NULL_BRUSH selection");int slot=row.GetProperty("slot").GetInt32();
            if(slot>=0)
            {
                var origin=NativeClassPresentation.ChoiceOrigin(slot);var rect=native.GetProperty("fills")[0].GetProperty("rect");int x=origin.X+row.GetProperty("x").GetInt32(),y=origin.Y+row.GetProperty("y").GetInt32();
                Check(rect[0].GetInt32()==x&&rect[1].GetInt32()==y&&rect[2].GetInt32()==x+NativeClassPresentation.ChoiceSize&&rect[3].GetInt32()==y+NativeClassPresentation.ChoiceSize,"actual463CE0 class fill bounds");
            }
        }
        var labels=JsonSerializer.Deserialize<NativeClassLabelCatalog>(File.ReadAllText(Path.Combine(args[0],"Assets/_Original/Resources/OriginalClassLabels.json")),json);
        Check(labels.entries.Select(v=>v.job).Distinct().Count()==labels.entries.Length&&labels.Find(100).description=="보병타입의 제1단계","source label catalog identity/description");
        Check(NativeClassPresentation.Level(null)=="—"&&NativeClassPresentation.Experience(null)=="—"&&NativeClassPresentation.StatText(null)=="—","unknown raw profile remains unknown");
        Console.WriteLine(JsonSerializer.Serialize(new{assertions=count,mismatches=0,unityPlay=false}));
    }
}
