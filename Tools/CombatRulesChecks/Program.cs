using DarkSaver.Combat;
using DarkSaver.Events;

var checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    checks++;
    Console.WriteLine($"PASS: {name}");
}

var world = new CombatWorld();
var boss = new CombatUnit { Id = 700, Enemy = true, Hp = 10 };
var survivor = new CombatUnit { Id = 710, Enemy = true, Hp = 10 };
world.Add(boss);
world.Add(survivor);
Check(!BattleVictoryRules.IsSatisfied(world, new short[] { 700 }), "live target blocks victory");
boss.Hp = 0;
boss.LifeState = UnitLifeState.Corpse;
Check(BattleVictoryRules.IsSatisfied(world, new short[] { 700 }), "target death wins with other enemies alive");
Check(!BattleVictoryRules.IsSatisfied(world, null), "elimination waits for every enemy");
Check(!BattleVictoryRules.IsSatisfied(world, new short[] { 701 }), "job ID is not a missing instance");
Check(!BattleVictoryRules.IsSatisfied(world, new short[] { 700, 710 }), "multiple targets require all deaths");
survivor.Hp = 0;
Check(!BattleVictoryRules.IsSatisfied(world, new short[] { 700, 710 }), "HP zero alone does not confirm target death");
survivor.LifeState = UnitLifeState.Corpse;
Check(BattleVictoryRules.IsSatisfied(world, new short[] { 700, 710 }), "all explicit targets defeated");
Check(BattleVictoryRules.IsSatisfied(world, Array.Empty<short>()), "demo elimination succeeds");
Check(!BattleVictoryRules.IsSatisfied(new CombatWorld(), null), "empty world cannot win");
world.Add(new CombatUnit { Id = 100, Hp = 0 });
Check(!BattleVictoryRules.IsSatisfied(world, new short[] { 100 }), "friendly target cannot win");

var combatWorld = new CombatWorld();
var actor = new CombatUnit { Id = 1, Hp = 50, Mp = 10, Attack = 100 };
var enemy = new CombatUnit { Id = 2, Enemy = true, Hp = 20 };
combatWorld.Add(actor);
combatWorld.Add(enemy);
var events = new List<string>();
var flow = new CombatFlow(combatWorld, new LocalCombatAuthority());
flow.ActionStarted += _ => events.Add("start");
flow.ResultApplied += _ => events.Add("result");
var result = flow.Execute(new CombatRequest(1, 2, CombatActionType.Magic, 101));
Check(result.AbsoluteHp == 0 && enemy.Hp == 0, "absolute HP result applied");
Check(actor.Mp == 0, "absolute MP result applied");
Check(enemy.LifeState == UnitLifeState.Corpse, "death retains corpse state");
Check(events.SequenceEqual(new[] { "start", "result" }), "action notification precedes applied result");
var experienceNotifications = 0;
flow.ExperienceApplied += update =>
{
    if (combatWorld.Get(update.UnitId).Experience != update.AbsoluteExperience)
        throw new Exception("XP notification precedes assignment");
    experienceNotifications++;
};
Check(flow.TryApplyExperience(new CombatExperienceResult(actor.Id, 3477000000u)) &&
    actor.Experience == 3477000000u, "XP preserves unsigned values above int32 max");
flow.TryApplyExperience(new CombatExperienceResult(actor.Id, 3477000000u));
Check(actor.Experience == 3477000000u, "repeated absolute XP is not added");
flow.TryApplyExperience(new CombatExperienceResult(actor.Id, 50));
Check(actor.Experience == 50, "lower authoritative XP replaces old value");
flow.TryApplyExperience(new CombatExperienceResult(actor.Id, uint.MaxValue));
Check(actor.Experience == uint.MaxValue, "XP preserves full uint32 range");
flow.TryApplyExperience(new CombatExperienceResult(actor.Id, 0));
Check(actor.Experience == 0 && experienceNotifications == 5, "zero XP applies before notification");
Check(!flow.TryApplyExperience(new CombatExperienceResult(701, 999)) &&
    actor.Experience == 0 && experienceNotifications == 5, "unknown XP instance leaves state and events unchanged");
flow.TryApplyExperience(new CombatExperienceResult(enemy.Id, 500));
Check(enemy.LifeState == UnitLifeState.Corpse && enemy.Hp == 0 && actor.Mp == 0,
    "XP update does not revive units or alter combat stats");
var commands = EventScriptParser.Parse("EVENT 99\r\nCHAR\r\n1 CHARDEAD 700\r\nSCENECLEAR\r\nEND");
Check(commands[2].Name == "CHARDEAD" && commands[2].NumericPrefix == 1 &&
    commands[2].Arguments.SequenceEqual(new[] { "700" }), "death condition prefix and instance separated");
Check(commands[2].LineNumber == 3, "event source line preserved");
var unknown = EventScriptParser.Parse("7 UNKNOWN 42")[0];
Check(unknown.Name == "7" && unknown.NumericPrefix == null, "unverified numeric syntax preserved");
var coordinates = EventScriptParser.Parse("CHANGESPRITE 5 15 0 1\nCHARPOS 800 15 5");
Check(coordinates[0].ReadCoordinates(0) == (5, 15) && coordinates[1].ReadCoordinates(1) == (15, 5),
    "command-specific coordinate order");
var spawnCommand = EventScriptParser.Parse("CHARPOS 701 700 19 10")[0];
Check(spawnCommand.TryReadScenePlacement(out var bossPlacement) && bossPlacement.DefinitionToken == "701" &&
    bossPlacement.InstanceId == 700 && bossPlacement.X == 19 && bossPlacement.Y == 10,
    "scene placement separates job reference and instance");
var dynamicCommand = EventScriptParser.Parse("CHARPOS COM0 301 14 32")[0];
Check(dynamicCommand.TryReadScenePlacement(out var commanderPlacement) && commanderPlacement.DefinitionToken == "COM0",
    "dynamic commander reference preserved");
foreach (var text in new[] { "CHARPOS 800 15 5", "1 CHARPOS 6 15", "CHARPOS 701 99999 19 10",
    "CHARPOS 701 700 invalid 10", "CHARPOS3 701 700 19 10" })
    Check(!EventScriptParser.Parse(text)[0].TryReadScenePlacement(out _), "reject non-spawn/invalid syntax: " + text);

var moveCommand = EventScriptParser.Parse("; intro\n  CHARMOVE 301 14 28  ")[0];
Check(moveCommand.TryReadSceneMove(out var sceneMove) && sceneMove.InstanceId == 301 &&
    sceneMove.X == 14 && sceneMove.Y == 28 && moveCommand.LineNumber == 2 &&
    moveCommand.SourceText == "  CHARMOVE 301 14 28  ", "scene move preserves target, coordinates, line and source");
foreach (var text in new[] { "CHARMOVE 301 14", "CHARMOVE 301 14 28 1", "CHARMOVE COM0 14 28",
    "CHARMOVE 99999 14 28", "CHARMOVE 301 invalid 28", "CHARMOVE 301 14 2147483648",
    "1 CHARMOVE 301 14 28", "CHARPOS 301 14 28" })
    Check(!EventScriptParser.Parse(text)[0].TryReadSceneMove(out _), "reject unsupported scene move: " + text);
var reusedCommands = EventScriptParser.Parse("CHARPOS 401 400 12 28\nCHARMOVE 400 13 28\nKILLCHAR2 400\nCHARPOS 301 400 16 7\nCHARMOVE 400 17 7");
Check(reusedCommands[1].TryReadSceneMove(out var firstMove) && firstMove.Y == 28 &&
    reusedCommands[4].TryReadSceneMove(out var laterMove) && laterMove.Y == 7 &&
    firstMove.InstanceId == laterMove.InstanceId && reusedCommands[2].Name == "KILLCHAR2" &&
    reusedCommands[3].Arguments[0] == "301", "reused scene instance IDs keep ordered moves");
Check(new EventCommand("CHARMOVE", new[] { "-1", "-2", "-3" }, 1).TryReadSceneMove(out var rawMove) &&
    rawMove.InstanceId == -1 && rawMove.X == -2 && rawMove.Y == -3, "negative move values preserved, not resolved");
Check(!default(EventCommand).TryReadSceneMove(out _) &&
    !new EventCommand("CHARMOVE", new[] { "301", "14", "28" }, 1, 1).TryReadSceneMove(out _),
    "null/default and prefixed scene move rejected");

var dialogueCommand = EventScriptParser.Parse("  MESSAGE 1 701 700 A 으..  분하다.. | 물러나야겠군..  ")[0];
Check(dialogueCommand.TryReadMessage(out var dialogue) && dialogue.DefinitionToken == "701" &&
    dialogue.SpeakerToken == "700" && dialogue.VariantToken == "A", "message header tokens remain distinct");
Check(dialogue.Text == "으..  분하다.. | 물러나야겠군..  ", "dialogue spacing and trailing whitespace preserved");
Check(dialogue.DisplayText == "으..  분하다..\n물러나야겠군..", "portrait display trims line edges while preserving source text");
var dynamicMessage = EventScriptParser.Parse("MESSAGE 0 0 ME 0 %s가 상자를 열었습니다.")[0];
Check(dynamicMessage.TryReadMessage(out var dynamicDialogue) && dynamicDialogue.SpeakerToken == "ME" &&
    dynamicDialogue.Text.StartsWith("%s"), "dynamic speaker and printf placeholder remain unresolved");
var commanderMessage = EventScriptParser.Parse("MESSAGE 1 COM0 0 COM0 이런!")[0];
Check(commanderMessage.TryReadMessage(out var commanderDialogue) && commanderDialogue.DefinitionToken == "COM0" &&
    commanderDialogue.VariantToken == "COM0", "commander message references preserved");
foreach (var text in new[] { "MESSAGE 1 701 700 A", "MESSAGE 1 701 700", "1 MESSAGE 1 701 700 A 대사",
    "UNKNOWN 1 701 700 A 대사" })
    Check(!EventScriptParser.Parse(text)[0].TryReadMessage(out _), "reject unsupported dialogue: " + text);

if (args.Length >= 1)
{
    // Analysis-only input. This tool is outside Assets and is not part of the game build.
    System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    var original = File.ReadAllText(args[0], System.Text.Encoding.GetEncoding(949));
    var originalCommands = EventScriptParser.Parse(original);
    var placements = originalCommands.Where(c => c.Name == "CHARPOS").ToArray();
    Check(placements.Length > 0 && placements.All(c => c.TryReadScenePlacement(out _)),
        "all scene CHARPOS records match the confirmed four-field schema");
    Check(placements.Any(c => c.TryReadScenePlacement(out var p) && p.DefinitionToken == "701" &&
        p.InstanceId == 700 && p.X == 19 && p.Y == 10), "original Kisra placement matches native definition");
    Console.WriteLine($"Verified {placements.Length} ordered placement commands; not a simultaneous spawn list.");
    var moves = originalCommands.Where(c => c.Name == "CHARMOVE").ToArray();
    Check(moves.Length == 5 && moves.All(c => c.TryReadSceneMove(out _)), "original 01 intro has five valid scene moves");
    var expectedMoves = new[] { ((short)301, 14, 28), ((short)400, 13, 28), ((short)401, 15, 28),
        ((short)400, 17, 7), ((short)401, 21, 7) };
    Check(moves.Select(c => { c.TryReadSceneMove(out var m); return (m.InstanceId, m.X, m.Y); }).SequenceEqual(expectedMoves),
        "all original intro movement targets and destinations match in order");
    var messages = originalCommands.Where(c => c.Name == "MESSAGE").ToArray();
    Check(messages.Length > 0 && messages.All(c => c.TryReadMessage(out _)), "all original intro MESSAGE records match four-header schema");
    Console.WriteLine($"Verified {messages.Length} intro message records; no dynamic references resolved or commands executed.");
}
if (args.Length >= 2)
{
    var original = File.ReadAllText(args[1], System.Text.Encoding.GetEncoding(949));
    var eventCommands = EventScriptParser.Parse(original);
    var messages = eventCommands.Where(c => c.Name == "MESSAGE").ToArray();
    Check(messages.Length > 0 && messages.All(c => c.TryReadMessage(out _)), "all original EVT MESSAGE records match four-header schema");
    Check(messages.Any(c => c.TryReadMessage(out var m) && m.DefinitionToken == "701" && m.SpeakerToken == "700" &&
        m.Text == "으.. 분하다.. | 억울하지만.. 여기선 일단 물러나야겠군.."), "original Kisra completion dialogue preserved exactly");
    Console.WriteLine($"Verified {messages.Length} event message records including Kisra completion dialogue.");
}
Console.WriteLine($"Completed {checks} checks using linked Unity-independent runtime sources.");
