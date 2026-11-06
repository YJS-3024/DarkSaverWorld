using System;
using System.IO;
using System.Text.Json;
using DarkSaver.Original.UI;
using DarkSaver.Original.World;
internal static class ShopDetailsChecks
{
    static int count;
    static void Require(bool value,string message){count++;if(!value)throw new Exception(message);}
    static void Main(string[] args)
    {
        var options=new JsonSerializerOptions{IncludeFields=true};using var fixture=JsonDocument.Parse(File.ReadAllText(args[1]));
        var catalog=JsonSerializer.Deserialize<NativeShopDetailsCatalog>(File.ReadAllText(Path.Combine(args[0],"Assets/_Original/Resources/OriginalShopDetails.json")),options);
        var mapping=JsonSerializer.Deserialize<NativeTextMapping>(File.ReadAllText(Path.Combine(args[0],"Assets/_Original/Data/World/NativeFontMapping.json")),options);
        foreach(var row in fixture.RootElement.GetProperty("items").EnumerateArray())
        {
            var item=JsonSerializer.Deserialize<WorldItemDefinition>(row.GetProperty("item").GetRawText(),options);var native=row.GetProperty("expected");
            var actual=NativeShopDetails.Paragraphs(item,NativeShopDetails.Usable(item,row.GetProperty("level").GetInt32(),row.GetProperty("kind").GetInt32()),catalog.Properties(item.id));
            Require(actual.Length==native.GetArrayLength(),"native paragraph count");for(int i=0;i<actual.Length;i++)Require(actual[i]==native[i].GetString(),"44B1C0 paragraph item"+item.id+" index"+i);
        }
        foreach(var row in fixture.RootElement.GetProperty("wrap").EnumerateArray())
            Require(NativeShopDetails.Lines(new[]{row.GetProperty("text").GetString()},mapping)==row.GetProperty("expected").GetString(),"43FDF0 strict-width stored line");
        foreach(var row in fixture.RootElement.GetProperty("selection").EnumerateArray())
        {
            var selected=new NativeShopSelection();selected.Select(row.GetProperty("right").GetBoolean(),row.GetProperty("index").GetInt32());var expected=row.GetProperty("expected");if(expected.GetProperty("cleared").GetBoolean())selected.Clear();bool visible=selected.Outline(row.GetProperty("offset").GetInt32(),out var rect);var rectangles=expected.GetProperty("rectangles");
            Require(visible==(rectangles.GetArrayLength()==1),"44AA70 selected region clipping/empty clearing");
            if(visible){var r=rectangles[0];Require(rect.Left+row.GetProperty("x").GetInt32()==r[0].GetInt32()&&rect.Top+row.GetProperty("y").GetInt32()==r[1].GetInt32()&&rect.Right+row.GetProperty("x").GetInt32()==r[2].GetInt32()&&rect.Bottom+row.GetProperty("y").GetInt32()==r[3].GetInt32(),"native33px exclusive Rectangle coordinates");Require(expected.GetProperty("stock")[0].GetInt32()==5,"actual CDC vtable selects NULL_BRUSH, not ROP2");}
        }
        var pixels=NativeShopSelection.BorderPixels();int visiblePixels=0;
        for(int y=0;y<33;y++)for(int x=0;x<33;x++){int at=(y*33+x)*4;bool edge=x==0||x==32||y==0||y==32;Require(pixels[at+3]==(edge?255:0),"null-brush border alpha");if(edge){Require(pixels[at]==255&&pixels[at+1]==255&&pixels[at+2]==64,"original COLORREF40FFFF pen");visiblePixels++;}}
        Require(visiblePixels==128,"one pixel border without filling item cell");
        var unknown=new WorldItemDefinition{id=-1,name="",description="",price=-1,requiredLevel=10};
        Require(NativeShopDetails.Usable(unknown,null,0)==null,"unreceived level remains unknown");Require(NativeShopDetails.Usable(unknown,9,null)==false,"known level rejection needs no class");Require(NativeShopDetails.Usable(unknown,10,null)==null,"unknown class is not fabricated");
        Require(NativeShopDetails.Paragraphs(unknown,null,null)[0]=="이름: 이름 없음   가격: —  판매여부: —","unknown price and source name fallback");Require(NativeShopDetails.Paragraphs(unknown,null,null)[1]=="설명: 설명 없음","source description fallback");
        Require(catalog.Properties(-1)==null,"unknown properties remain absent");Require(NativeShopDetails.Paragraphs(null,null,null).Length==0,"missing item");
        Console.WriteLine(JsonSerializer.Serialize(new{assertions=count,mismatches=0,unityPlay=false}));
    }
}
