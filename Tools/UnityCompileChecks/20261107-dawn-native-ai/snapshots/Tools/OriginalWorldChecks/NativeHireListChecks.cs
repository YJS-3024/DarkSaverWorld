using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.World;
internal static class NativeHireListChecks
{
    static int count;static readonly JsonSerializerOptions Json=new JsonSerializerOptions{IncludeFields=true};
    static void Require(bool value,string message){count++;if(!value)throw new Exception(message);}
    static T Read<T>(JsonElement value)=>JsonSerializer.Deserialize<T>(value.GetRawText(),Json);
    static void Main(string[] args)
    {
        using var fixture=JsonDocument.Parse(File.ReadAllText(args[1]));
        foreach(var group in fixture.RootElement.EnumerateArray())
        {
            var catalog=Read<NativeMercenaryCatalog>(group.GetProperty("catalog"));
            foreach(var row in group.GetProperty("cases").EnumerateArray())
            {
                var actual=catalog.Resolve(Read<int[]>(row.GetProperty("equipment")),Read<int[]>(row.GetProperty("skills")));var expected=Read<NativeMercenaryChoice[]>(row.GetProperty("expected"));
                Require(JsonSerializer.Serialize(actual,Json)==JsonSerializer.Serialize(expected,Json),"actual setunit.GetSoldierList list/order/max mismatch: "+row.GetProperty("case").GetString());
            }
        }
        var source=JsonSerializer.Deserialize<NativeMercenaryCatalog>(File.ReadAllText(Path.Combine(args[0],"Assets/_Original/Resources/OriginalMercenarySkills.json")),Json);
        var record=new WorldRecord{growth=new CharacterGrowthRecord{job=100,skills=new[]{20101,20103,20201}}};
        var offers=source.Offers(record);Require(offers.Length==14,"actual learned skills replace class initial stock");
        Require(offers.First(o=>o.job==200).limit==4,"duplicate unit maximum not summed");Require(offers.First(o=>o.job==201).limit==2,"different hire family maximum");
        foreach(var row in offers){var unit=source.units.Single(u=>u.job==row.job);Require(row.name==unit.name&&row.picture==unit.picture&&row.price==unit.price&&row.commander==100,"unit table/selected commander projection");}
        Require(source.Offers(new WorldRecord()).Length==0,"missing learned skills cannot grant class stock");Require(source.Offers(null).Length==0,"missing record");
        var growth=JsonSerializer.Deserialize<CharacterGrowthCatalog>(File.ReadAllText(Path.Combine(args[0],"Assets/_Original/Data/Growth/CharacterGrowth.json")),Json);
        var mage=growth.Row(103,1);
        var mageOffers=source.Offers(new WorldRecord{growth=new CharacterGrowthRecord{job=103,skills=mage.skills}});
        Require(mage.skills.Contains(21701)&&mageOffers.Select(o=>o.job).SequenceEqual(new[]{200,201,202}),"explicit original first mage skills expose Soldier D/C/B");
        Require(mageOffers.All(o=>o.limit==4),"first mage hire maximum four, not sum of families");
        record.growth.skills=new[]{20101,20101,20101,20101,20101,20101,20101,20101,21707};Require(source.Offers(record).All(o=>o.job!=291),"ninth skill slot excluded by original loop");
        Console.WriteLine(JsonSerializer.Serialize(new{assertions=count,mismatches=0,unityPlay=false}));
    }
}
