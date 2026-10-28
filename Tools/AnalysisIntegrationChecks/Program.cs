using DarkSaver.Combat;
using DarkSaver.Core;
using DarkSaver.Editor;
using DarkSaver.Events;
using System.Text.Json.Nodes;
using Microsoft.VisualBasic.FileIO;
using DarkSaver.Rendering;

var checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    checks++;
}
var trainingResumeStart=checks;
DarkSaver.Tests.TrainingResumeChecks.Run(Check);
Console.WriteLine("Training resume checks: "+(checks-trainingResumeStart)+" (offline, not Unity).");
var buildings=System.Text.Json.JsonSerializer.Deserialize<DarkSaver.Network.BuildingTravelRoutes>(File.ReadAllText("Assets/_Game/Resources/World/BuildingRoutes.json"),new System.Text.Json.JsonSerializerOptions{IncludeFields=true});
DarkSaver.Tests.BuildingTravelChecks.Run(Check,buildings);
OriginalRestorationChecks.Run(Check);
DarkSaver.Tests.NativeEntryRouteChecks.Run(Check);
DarkSaver.Tests.LocalAuthenticationChecks.Run(Check);
DarkSaver.Tests.LocalServerSelectionChecks.Run(Check);
DarkSaver.Tests.OriginalTutorialWindowChecks.Run(Check,File.ReadAllText("Assets/_Game/Resources/Tutorial/TUTORIAL.txt"));
var onboardingStart=checks;
DarkSaver.Tests.LocalOnboardingChecks.Run(Check);
Console.WriteLine("Original tutorial/onboarding checks: "+(checks-onboardingStart)+" (offline, not Unity Play).");

// Explicit received checkpoint roundtrip; never execute removed local scenario authority.
foreach(var row in ScenarioCatalog.All)
{
    var file="Assets/_Game/Resources/Scenarios/"+row.Key+"/Content.json";
    if(!File.Exists(file))continue;
    var definition=UnityEngine.JsonUtility.FromJson<Scenario01Definition>(File.ReadAllText(file));
    Check(definition.IsValid,"scenario definition "+row.Key);
    var received=new Scenario01Progress {version=definition.version,scenarioKey=definition.Key,
        sourceHash=definition.sourceHash,profileHash=definition.profileHash,actorGuid=Guid.NewGuid().ToString(),hasBattleExperienceLedger=true};
    Check(received.Matches(definition),"explicit empty checkpoint matches "+row.Key);
    var restored=UnityEngine.JsonUtility.FromJson<Scenario01Progress>(UnityEngine.JsonUtility.ToJson(received));
    Check(restored.Matches(definition) && restored.enemies.Count==0 && !restored.clearRequested,
        "checkpoint roundtrip does not invent deployment or clear "+row.Key);
}

foreach (var name in new[] { "Kaosia_Map", "World1010_Map", "Brugos01_Map", "Map_01_브르고스궁", "Map_107_;", "Map_329_;" })
    Check(OriginalRenderRules.IsNativeMapScene(name), "native map render repair " + name);
foreach (var name in new[] { "Battle", "Training101_Runtime", "Brugos01_Runtime", "Map_invalid_name", "Map_-1_name", null })
    Check(!OriginalRenderRules.IsNativeMapScene(name), "exclude gameplay scene " + name);
Check(OriginalRenderRules.TryGetForegroundSortingOrder("Object_007", out var order) && order == 30007, "record order preserved");
Check(OriginalRenderRules.ForegroundSortingOrder(OriginalRenderRules.MaximumForegroundIndex) == short.MaxValue, "sorting order upper boundary");
foreach (var name in new[] { "Object_-1", "Object_3000", "Object_x", "NPC_001", "Object_ 1" })
    Check(!OriginalRenderRules.TryGetForegroundSortingOrder(name, out _), "reject invalid record index " + name);
try { OriginalRenderRules.ForegroundSortingOrder(3000); throw new Exception("sorting overflow accepted"); }
catch (ArgumentOutOfRangeException) { checks++; }
var folder = "분석/Saver_1.2/catalogs";
var rules = CombatAiCatalogConverter.Parse(File.ReadAllText(folder + "/ai_rules.csv"), File.ReadAllText(folder + "/ai_actions.csv"));
Check(rules.Length == 1134, "preserve all converted AI rules");
foreach (var rule in rules)
{
    int? expected = rule.AiType switch { "ODNORMAL" => 2, "ODATTACK" => 4, "ODDEFENCE" => 8, _ => null };
    Check(rule.ModeCode == expected, "AI native mode " + rule.Id);
    Check(rule.HasSupportedMode == expected.HasValue, "AI unsupported modes stay unsupported " + rule.Id);
}
var csv = File.ReadAllText(folder + "/ai_rules.csv");
try
{
    CombatAiCatalogConverter.Parse(csv.Replace("\"ODATTACK\",\"4\"", "\"ODATTACK\",\"8\""), File.ReadAllText(folder + "/ai_actions.csv"));
    throw new Exception("old inverted AI mode accepted");
}
catch (InvalidDataException) { checks++; }

var splitFixtures = new (string Source, string[] Expected)[] {
    ("", Array.Empty<string>()), ("a", new[]{"a"}), ("a|b",new[]{"a","b"}),
    ("a||b",new[]{"a","","b"}), ("|a",new[]{"","a"}), ("a|",new[]{"a"}),
    ("a||",new[]{"a",""}), ("|",new[]{""}), (" 안녕 | 용사여 ",new[]{"안녕","용사여"}), ("a\tb",new[]{"a\tb"})
};
foreach (var row in splitFixtures) Check(ScenarioDialogueLayout.SplitLines(row.Source).SequenceEqual(row.Expected), "native split " + row.Source);
using (var source = new TextFieldParser("닥세_리소스/분석결과/대사_원문표시연결.csv"))
{
    source.SetDelimiters(","); source.HasFieldsEnclosedInQuotes = true; source.TrimWhiteSpace = false;
    var header = source.ReadFields(); var nativeMessages = 0;
    int Column(string name) => Array.IndexOf(header, name);
    while (!source.EndOfData)
    {
        var row = source.ReadFields();
        if (row[Column("command")] != "MESSAGE") continue;
        var text = row[Column("text")];
        var expected = System.Text.Json.JsonSerializer.Deserialize<string[]>(row[Column("native_split_lines")]);
        Check(ScenarioDialogueLayout.SplitLines(text).SequenceEqual(expected), "original DLL split corpus " + row[Column("file")] + ":" + row[Column("line")]);
        nativeMessages++;
    }
    Check(nativeMessages == 1993, "all native source MESSAGE rows verified");
}
Check(ScenarioDialogueLayout.Page(1, "a|b||d|", 0) == "a\nb\n", "first three-row page");
Check(ScenarioDialogueLayout.Page(2, "a|b||d|", 3) == "d", "second page");
Check(ScenarioDialogueLayout.Page(2, "a|b||d|", 6) == "", "closed page");
Check(ScenarioDialogueLayout.DisplayText("0", " a|b ") == " a|b ", "notice is whole text");
uint start = uint.MaxValue - 50;
Check(!ScenarioDialogueLayout.NoticeExpired(start, unchecked(start + 10000)), "notice inclusive ten seconds");
Check(ScenarioDialogueLayout.NoticeExpired(start, unchecked(start + 10001)), "notice wraparound expiry");
foreach (var name in new[] { "CHARPOS", "CHARPOS2", "CHARPOS3", "CHARDEAD", "ITEM", "DUTY_STEP_TRUE", "DUTY_STEP_FALSE", "ONLY_CHAR", "ONLY_MONSTER", "CAUSE_OF_DEATH", "ALL_MOB_DEAD", "RANDOM", "TICK" })
{
    var command = EventScriptParser.Parse("3 " + name + " 6 15")[0];
    Check(command.Name == name && command.NumericPrefix == 3 && command.Arguments.SequenceEqual(new[] { "6", "15" }), "condition prefix " + name);
    Check(!command.TryReadScenePlacement(out _), "condition cannot spawn " + name);
}
Check(EventScriptParser.Parse("1 ALL_MOB_DEAD")[0].NumericPrefix == 1, "zero-operand condition");
Check(EventScriptParser.Parse("700")[0].Name == "700", "bare numeric source retained");
Check(EventScriptParser.Parse("7 UNKNOWN 42")[0].Name == "7", "unknown syntax retained");
Check(EventScriptParser.Parse("CHARPOS COM0 301 14 32")[0].TryReadScenePlacement(out _), "scene spawn still supported");

NativeSaveData Sample() => new() { format = NativeSaveStore.Format, version = 1,
    mapScene = "Kaosia_Map", runtimeScene = "Town_Runtime", experience = uint.MaxValue,
    clearedScenarios = new[] { "01" }, hasVitals = true, hp = 37, maxHp = 120, mp = 30, maxMp = 30 };
var temporary = Path.Combine(Path.GetTempPath(), "ds-analysis-checks-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(temporary);
try
{
    var path = Path.Combine(temporary, "save.json"); var store = new NativeSaveStore(path);
    Check(!store.TryLoad(out _, out _), "missing save fails");
    File.WriteAllText(path, UnityEngine.JsonUtility.ToJson(Sample()));
    Check(store.TryLoad(out var legacy, out _) && legacy.experience == uint.MaxValue, "legacy save loads");
    Check(store.TrySave(legacy, out _), "legacy migrates to checked envelope");
    Check(JsonNode.Parse(File.ReadAllText(path))["sha256"].GetValue<string>().Length == 64, "checksum written");
    Check(store.TryLoad(out var first, out _) && first.hp == 37, "checked save roundtrip");
    first.experience = 12; Check(store.TrySave(first, out _), "atomic replacement");
    var backup = File.ReadAllText(path + ".bak");
    var envelope = JsonNode.Parse(File.ReadAllText(path));
    envelope["payload"] = envelope["payload"].GetValue<string>().Replace("\"experience\":12", "\"experience\":13");
    File.WriteAllText(path, envelope.ToJsonString());
    Check(store.TryLoad(out var recovered, out _) && recovered.experience == uint.MaxValue, "valid JSON tampering rejected, backup loaded");
    Check(store.TrySave(recovered, out _), "save after backup recovery");
    Check(File.ReadAllText(path + ".bak") == backup, "corrupt primary does not poison good backup");
    Check(store.TryLoad(out _, out _), "recovered primary valid");
    File.WriteAllText(path, "{\"format\":\"DarkSaverWorld.NativeSave\",\"version\":99}");
    Check(!store.TryLoad(out _, out _), "future payload blocks fallback");
    Check(!store.TrySave(Sample(), out _), "future payload blocks overwrite");
    File.WriteAllText(path, "{\"format\":\"DarkSaverWorld.NativeSave.Checked\",\"version\":99}");
    Check(!store.TryLoad(out _, out _), "future envelope blocks fallback");
    File.WriteAllText(path, "broken"); File.WriteAllText(path + ".bak", "broken");
    Check(!store.TryLoad(out _, out _), "two corrupt snapshots fail closed");
    Check(!store.TrySave(Sample(), out _), "no fresh game over two corrupt snapshots");
    File.Delete(path);
    Check(!store.TrySave(Sample(), out _), "missing primary cannot overwrite only corrupt backup");
    File.WriteAllText(path + ".bak", backup);
    Check(store.TryLoad(out _, out _), "missing primary restores valid backup");
    Check(store.TrySave(Sample(), out _), "missing primary can resume valid backup");
    Check(Directory.GetFiles(temporary, "*.tmp-*").Length == 0, "no abandoned write temporary");
}
finally { Directory.Delete(temporary, true); }






Check(!DarkSaver.Network.ScenarioRoomRules.IsOngoing(null), "no saved progress is not an ongoing room");
for(int phase=-1;phase<=4;phase++)
    Check(DarkSaver.Network.ScenarioRoomRules.IsOngoing(new DarkSaver.Events.Scenario01Progress {phase=phase})==(phase>=0 && phase<3), "local room phase "+phase);





Console.WriteLine($"PASS {checks} checks: linked production C# with offline host/JSON adapters; NOT Unity PlayMode or JsonUtility verification.");
