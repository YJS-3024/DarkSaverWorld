using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.World;
internal static class BodyPaletteDifferential
{
    private static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
    public sealed class Case {public bool connected,sameForce,allied;public int appearance,team,role,own,friend,other;}
    public static void Main(string[] args)
    {
        var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[0]),Options);
        File.WriteAllText(args[1],JsonSerializer.Serialize(cases.Select(c=>NativeBodyPalette.Resolve(c.appearance,c.connected,c.team,c.sameForce,c.allied,c.role,c.own,c.friend,c.other)).ToArray()));
    }
}
