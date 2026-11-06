using System;
using System.IO;
using System.Text.Json;
using DarkSaver.Original.UI;
internal static class WindowDragChecks
{
    static int count;static void Check(bool value,string label){count++;if(!value)throw new Exception(label);}
    static void Main(string[] args)
    {
        using var fixture=JsonDocument.Parse(File.ReadAllText(args[1]));
        foreach(var row in fixture.RootElement.EnumerateArray())
        {
            var down=row.GetProperty("down");var region=row.GetProperty("region");var expected=row.GetProperty("expected");var drag=new NativeWindowDrag();int x=row.GetProperty("x").GetInt32(),y=row.GetProperty("y").GetInt32();
            bool active=drag.Press(x,y,down[0].GetInt32(),down[1].GetInt32(),region[0].GetInt32(),region[1].GetInt32(),region[2].GetInt32(),region[3].GetInt32(),row.GetProperty("enabled").GetBoolean());
            Check(active==expected.GetProperty("active").GetBoolean(),"actual433BE0 title hit");int step=0;
            foreach(var item in row.GetProperty("steps").EnumerateArray())
            {
                var at=drag.Move(x,y,item[0].GetInt32(),item[1].GetInt32(),item[2].GetBoolean(),item[3].GetBoolean());x=at.X;y=at.Y;var target=expected.GetProperty("positions")[step++];Check(x==target[0].GetInt32()&&y==target[1].GetInt32(),"actual433EC0 unclamped motion/lock");
            }
            drag.Cancel();var stopped=drag.Move(x,y,999,999);Check(!drag.Active&&stopped.X==x&&stopped.Y==y,"release/disable capture cancellation");
        }
        var options=new JsonSerializerOptions{IncludeFields=true};var catalog=JsonSerializer.Deserialize<NativeFacilityWindows>(File.ReadAllText(Path.Combine(args[0],"Assets/_Original/Resources/OriginalFacilityWindows.json")),options);
        foreach(var spec in catalog.windows)
        {
            Check(catalog.Find(spec.id,spec.scene,spec.width,spec.height)==spec,"exact source native window lookup");Check(catalog.Find(spec.id,spec.scene+100,spec.width,spec.height)==null&&catalog.Find(spec.id,spec.scene,spec.width+1,spec.height)==null,"incompatible artwork cannot bind title geometry");
        }
        Check(NativeFacilityWindows.DefaultId(9)==24&&NativeFacilityWindows.DefaultId(18)==34&&NativeFacilityWindows.DefaultId(20)==37,"unambiguous facility identity");Check(NativeFacilityWindows.DefaultId(10)==0&&NativeFacilityWindows.DefaultId(17)==0,"unknown shop/scene17 reuse does not infer identity");
        Check(NativeFacilityWindows.ShopId(0)==29&&NativeFacilityWindows.ShopId(100)==30&&NativeFacilityWindows.ShopId(200)==31&&NativeFacilityWindows.ShopId(300)==32&&NativeFacilityWindows.ShopId(null)==48&&NativeFacilityWindows.ShopId(50)==0,"explicit native shop kind/category mapping");
        Console.WriteLine(JsonSerializer.Serialize(new{assertions=count,mismatches=0,unityPlay=false}));
    }
}
