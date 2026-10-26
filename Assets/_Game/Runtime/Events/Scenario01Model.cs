using System;
using System.Collections.Generic;
using System.Linq;
using DarkSaver.Core;

namespace DarkSaver.Events
{
    // Authored single-player integration of source 01.DEO/EVT/DEE and the reviewed
    // placement supplement. It does not claim the missing original server roster.
    [Serializable] public sealed class Scenario01Command
    {
        public string name;
        public string[] args = Array.Empty<string>();
        public int line;
        public string text;
        public int group;
        public int Int(int index) => int.Parse(args[index], System.Globalization.CultureInfo.InvariantCulture);
    }
    [Serializable] public sealed class Scenario01Event
    {
        public int id;
        public string scope;
        public string mode;
        public Scenario01Command[] conditions = Array.Empty<Scenario01Command>();
        public Scenario01Command[] actions = Array.Empty<Scenario01Command>();
    }
    [Serializable] public sealed class Scenario01Unit
    {
        public int id, job, x, y, wave;
        public string name;
        public int hp, mp, attack, defense, magicAttack, magicDefense;
        // Single-player resource interpretation; the baked original -1 sentinel is preserved.
        public int CombatHp => OriginalEnemyOverrides.MaximumHp(job,hp);
        public int CombatMp => mp == -1 ? 0 : mp;
        public uint experience;
    }
    [Serializable] public sealed class ScenarioEnding
    { public string tag; public Scenario01Command[] commands; }
    [Serializable] public sealed class Scenario01Definition
    {
        public int version, width, height, entryX, entryY;
        public string sourceHash, profileHash, scenarioKey;
        public Scenario01Command[] intro, outro;
        public ScenarioEnding[] endings;
        public string defaultEnding;
        public Scenario01Command[] Outro(string variant)
        { var chosen = endings == null ? null : Array.Find(endings,e => e.tag == variant); return chosen?.commands ?? outro; }
        public Scenario01Event[] events;
        public Scenario01Unit[] units;
        public Scenario01Unit[] characters;
        public int[] traversalCells;
        public string Key => version == 1 ? "01" : scenarioKey;
        public bool IsValid => ((version == 1 && width == 40 && height == 40) || (version == 2 && ScenarioCatalog.IsProgressKey(scenarioKey) && width > 0 && width <= 512 && height > 0 && height <= 512)) && IsHash(sourceHash) && IsHash(profileHash)
            && intro != null && (version == 1 ? intro.Length == 72 : intro.Length > 0) && outro != null && outro.Length > 0
            && events != null && events.Select(e => e.id).Distinct().Count() == events.Length
            && traversalCells != null && traversalCells.Length > 0
            && units != null && (version == 1 ? units.Length == 33 : units.Length > 0 && units.Length <= 500) && units.Select(u => u.id).Distinct().Count() == units.Length
            && units.All(u => u.id >= 400 && u.id < 800 && u.hp > 0 && u.wave >= 0 && u.wave <= (version == 1 ? 2 : 100)
                && u.x >= 0 && u.x < width && u.y >= 0 && u.y < height && Array.IndexOf(traversalCells, u.y * width + u.x) >= 0);
        public static bool IsHash(string value) => value != null && value.Length == 64 && value.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f');
        public Scenario01Event Event(int id) => Array.Find(events, e => e.id == id);
    }
    [Serializable] public sealed class Scenario01Actor
    {
        public int id, job, facing;
        public bool partyProxy;
        public float x, y;
        public int targetX, targetY;
        public bool moving;
        public Scenario01Actor Clone() => (Scenario01Actor)MemberwiseClone();
    }
    [Serializable] public sealed class Scenario01EnemyState
    {
        public int id, hp, mp, x, y;
        public Scenario01EnemyState Clone() => (Scenario01EnemyState)MemberwiseClone();
    }
    [Serializable] public sealed class Scenario01Progress
    {
        public const int Intro = 0, Playing = 1, Outro = 2, Complete = 3;
        public int version = 1, phase, cursor, page, activeEvent, branchRoll = -1, wave;
        public int pendingBattle = -1;
        public string sourceHash, profileHash, actorGuid;
        public uint rng;
        public double wait;
        public float cameraX = 14, cameraY = 32;
        public bool clearRequested;
        public bool firstClearSaved, branchTest, repeatEvent;
        public string scenarioKey, nextScenario, presentationMap, endingVariant;
        public List<int> mapChanges = new(), unlockedGenerals = new();
        public List<string> deathSides = new();
        public string noticeText;
        public string musicSource;
        public int musicMode;
        public double noticeSeconds;
        public List<int> defeated = new(), activatedEvents = new(), insideEvents = new(), changedEvents = new(), removedActors = new();
        public List<string> dutySteps = new();
        public List<Scenario01Actor> actors = new();
        public List<Scenario01EnemyState> enemies = new();
        public Scenario01Progress Clone()
        {
            var copy = (Scenario01Progress)MemberwiseClone();
            copy.defeated = new List<int>(defeated); copy.activatedEvents = new List<int>(activatedEvents);
            copy.insideEvents = new List<int>(insideEvents); copy.changedEvents = new List<int>(changedEvents);
            copy.mapChanges = new List<int>(mapChanges ?? new List<int>()); copy.unlockedGenerals = new List<int>(unlockedGenerals ?? new List<int>()); copy.deathSides = new List<string>(deathSides ?? new List<string>());
            copy.removedActors = new List<int>(removedActors); copy.dutySteps = new List<string>(dutySteps);
            copy.actors = actors.Select(a => a.Clone()).ToList(); copy.enemies = enemies.Select(e => e.Clone()).ToList(); return copy;
        }
        public bool IsValid => (version == 1 || version == 2) && phase >= Intro && phase <= Complete
            && cursor >= 0 && cursor <= 10000 && page >= 0 && page <= 10000 && activeEvent >= 0
            && branchRoll >= -1 && branchRoll <= 100 && wave >= 0 && wave <= (version == 1 ? 2 : 100)
            && (pendingBattle == -1 || pendingBattle >= 400 && pendingBattle < 800)
            && Scenario01Definition.IsHash(sourceHash) && Scenario01Definition.IsHash(profileHash)
            && Guid.TryParse(actorGuid, out _) && !double.IsNaN(wait) && !double.IsInfinity(wait) && wait >= 0 && wait <= 3600
            && !double.IsNaN(noticeSeconds) && !double.IsInfinity(noticeSeconds) && noticeSeconds >= 0 && noticeSeconds <= 11
            && musicMode >= 0 && musicMode <= (version == 1 ? 1 : 2)
            && Finite(cameraX) && Finite(cameraY) && cameraX >= (version == 1 ? 0 : -1000) && cameraX < (version == 1 ? 40 : 1000) && cameraY >= (version == 1 ? 0 : -1000) && cameraY < (version == 1 ? 40 : 1000)
            && Unique(defeated) && Unique(activatedEvents) && Unique(insideEvents) && Unique(changedEvents) && Unique(removedActors)
            && dutySteps != null && dutySteps.Count <= 1000 && dutySteps.All(s => !string.IsNullOrEmpty(s)) && dutySteps.Distinct().Count() == dutySteps.Count
            && actors != null && actors.Count <= (version == 1 ? 100 : 1000) && actors.All(a => a != null && a.job > 0 && Finite(a.x) && Finite(a.y)
                && a.x >= (version == 1 ? 0 : -1000) && a.x < (version == 1 ? 40 : 1000) && a.y >= (version == 1 ? 0 : -1000) && a.y < (version == 1 ? 40 : 1000) && a.targetX >= (version == 1 ? 0 : -1000) && a.targetX < (version == 1 ? 40 : 1000)
                && a.targetY >= (version == 1 ? 0 : -1000) && a.targetY < (version == 1 ? 40 : 1000) && a.facing >= 0 && a.facing <= 3)
            && actors.Select(a => a.id).Distinct().Count() == actors.Count
            && enemies != null && enemies.Count <= (version == 1 ? 33 : 500) && enemies.All(e => e != null && e.id >= 400 && e.id < 800
                && e.hp >= 0 && e.mp >= 0 && e.x >= 0 && e.x < (version == 1 ? 40 : 512) && e.y >= 0 && e.y < (version == 1 ? 40 : 512))
            && enemies.Select(e => e.id).Distinct().Count() == enemies.Count
            && (mapChanges != null || version == 1) && (mapChanges == null || mapChanges.Count <= 10000)
            && (unlockedGenerals != null || version == 1) && (unlockedGenerals == null || unlockedGenerals.Count <= 200 && unlockedGenerals.All(id => id > 0 && id <= 32767) && unlockedGenerals.Distinct().Count() == unlockedGenerals.Count)
            && (deathSides != null || version == 1) && (deathSides == null || deathSides.Count <= 1000);
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Unique(List<int> values) => values != null && values.Count <= 1000 && values.Distinct().Count() == values.Count;
        public bool Matches(Scenario01Definition definition)
        {
            if (!IsValid || !definition.IsValid || (version == 2 && scenarioKey != definition.Key) || sourceHash != definition.sourceHash || profileHash != definition.profileHash
                || !enemies.All(e => definition.units.Any(u => u.id == e.id && e.hp <= u.CombatHp && e.mp <= u.CombatMp))
                || activeEvent != 0 && (phase != Playing || definition.Event(activeEvent) == null)
                || !defeated.All(id => definition.units.Any(u => u.id == id))
                || !activatedEvents.Concat(insideEvents).All(id => definition.Event(id) != null)
                || !changedEvents.All(id => activatedEvents.Contains(id) && definition.Event(id)?.actions.Any(c => c.name == "CHANGESPRITE") == true)) return false;
            if (version == 2 && mapChanges.Any(line => !definition.events.SelectMany(e => e.actions).Any(c => c.line == line && (c.name == "CHANGESPRITE" || c.name == "CHANGETILE2")))) return false;
            var commands = Scenario01Rules.CurrentCommands(definition, this);
            return cursor <= commands.Length && (page == 0 || cursor < commands.Length && commands[cursor].name == "MESSAGE"
                && page % 3 == 0 && page < ScenarioDialogueLayout.SplitLines(commands[cursor].text).Length);
        }
        public static Scenario01Progress Create(Scenario01Definition definition, bool legacyClear = false)
        {
            if (!definition.IsValid) throw new ArgumentException("Invalid scenario 01 definition.");
            var guid = Guid.NewGuid();
            return new Scenario01Progress { version = definition.version == 1 ? 1 : 2, scenarioKey = definition.Key, endingVariant = definition.defaultEnding, sourceHash = definition.sourceHash, profileHash = definition.profileHash,
                actorGuid = guid.ToString("N"), rng = BitConverter.ToUInt32(guid.ToByteArray(), 0),
                phase = legacyClear ? Complete : Intro };
        }
    }

    public static class Scenario01Rules
    {
        // Native alias conversion is followed by atoi: ME/ID expand to the
        // player's name, while COM0 expands to the numeric current job.
        public static int ScenarioPortraitId(string token, int job, string playerName)
        {
            if (string.Equals(token, "COM0", StringComparison.OrdinalIgnoreCase)) return job;
            if (string.Equals(token, "ME", StringComparison.OrdinalIgnoreCase) || string.Equals(token, "ID", StringComparison.OrdinalIgnoreCase)) token = playerName;
            if (string.IsNullOrWhiteSpace(token)) return 0;
            token = token.TrimStart(); int i = 0, sign = 1; long value = 0;
            if (token[0] == '-' || token[0] == '+') { sign = token[0] == '-' ? -1 : 1; i++; }
            while (i < token.Length && token[i] >= '0' && token[i] <= '9')
            { value = value * 10 + token[i++] - '0'; if (value > int.MaxValue) return 0; }
            return (int)value * sign;
        }


        public enum StepResult { Advanced, Waiting, Finished }

        public static void AdvanceTime(Scenario01Progress state, double seconds, float movementSpeed)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0 || movementSpeed <= 0)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            state.wait = Math.Max(0, state.wait - seconds);
            state.noticeSeconds = Math.Max(0, state.noticeSeconds - seconds);
            if (state.noticeSeconds == 0) state.noticeText = null;
            foreach (var actor in state.actors)
            {
                if (!actor.moving) continue;
                double dx = actor.targetX - actor.x, dy = actor.targetY - actor.y, distance = Math.Sqrt(dx * dx + dy * dy);
                if (distance > .000001) actor.facing = Math.Abs(dx) > .0001 ? dx < 0 ? 2 : 3 : dy < 0 ? 0 : 1;
                var step = movementSpeed * seconds;
                if (distance <= step) { actor.x = actor.targetX; actor.y = actor.targetY; actor.moving = false; }
                else { actor.x += (float)(dx / distance * step); actor.y += (float)(dy / distance * step); }
            }
        }

        public static StepResult Step(Scenario01Definition definition, Scenario01Progress state, NativeInventory inventory,
            int playerJob, out Scenario01Progress next, out NativeInventory items)
        {
            if (!state.Matches(definition) || inventory == null || !inventory.IsValid) throw new ArgumentException("Invalid scenario checkpoint.");
            next = state.Clone(); items = inventory.Clone();
            if (state.wait > 0) return StepResult.Waiting;
            var commands = CurrentCommands(definition, state);
            if (state.cursor == commands.Length) { FinishScript(next); return StepResult.Finished; }
            var command = commands[state.cursor];
            if (command.name == "MESSAGE" && command.args[0] != "0") return StepResult.Waiting;
            switch (command.name)
            {
                case "LOADSCENE": if (command.args.Length != 1 || definition.version == 1 && command.args[0] != "01" || !DarkSaver.Core.ScenarioCatalog.TryGet(command.args[0], out _)) throw new InvalidOperationException("Unknown scene."); next.presentationMap = command.args[0]; break;
                case "SETPOSITION": case "MOVEPOSITION": next.cameraX = command.Int(0); next.cameraY = command.Int(1); break;
                case "CHARPOS":
                    var job = command.args[0].StartsWith("COM", StringComparison.OrdinalIgnoreCase) ? playerJob : command.Int(0);
                    next.actors.RemoveAll(a => a.id == command.Int(1));
                    next.actors.Add(new Scenario01Actor { id = command.Int(1), job = job, partyProxy = command.args[0].StartsWith("COM", StringComparison.OrdinalIgnoreCase), x = command.Int(2), y = command.Int(3),
                        targetX = command.Int(2), targetY = command.Int(3), facing = 1 }); break;
                case "CHARMOVE":
                    var actor = next.actors.Find(a => a.id == command.Int(0));
                    if (actor == null) { if (definition.version == 1) throw new InvalidOperationException("Missing movement actor."); break; }
                    actor.targetX = command.Int(1); actor.targetY = command.Int(2); actor.moving = true; break;
                case "CHARDIR":
                    var facing = next.actors.Find(a => a.id == command.Int(0));
                    if (facing == null) { if (definition.version == 1) throw new InvalidOperationException("Missing direction actor."); break; }
                    facing.facing = command.args[1] switch { "UP" => 0, "DOWN" => 1, "LEFT" => 2, "RIGHT" => 3, _ => throw new InvalidOperationException("Unknown direction.") }; break;
                case "DO2":
                    var waiting = next.actors.Find(a => a.id == command.Int(0));
                    if (waiting == null) { if (definition.version == 1) throw new InvalidOperationException("Missing wait actor."); break; }
                    if (waiting.moving) return StepResult.Waiting;
                    break;
                case "KILLCHAR1": case "KILLCHAR2":
                    int killed = command.Int(0); next.actors.RemoveAll(a => a.id == killed);
                    if (next.phase == Scenario01Progress.Playing && !next.removedActors.Contains(killed)) next.removedActors.Add(killed); break;
                case "MESSAGE": next.noticeText = command.text; next.noticeSeconds = 10.001; break;
                case "GETITEM": if (next.activeEvent == 0) Grant(items, command.Int(0)); break;
                case "GOLD": case "USEITEM": case "SET_DUTY_STEP":
                    if (next.activeEvent == 0) throw new InvalidOperationException("Unbound economy command."); break;
                case "SCENECLEAR": next.clearRequested = true; break;
                case "FIRSTCLEAR": next.branchTest = !next.firstClearSaved; break;
                case "TEST": case "TESTNOT":
                    bool include = command.name == "TEST" ? next.branchTest : !next.branchTest;
                    if (!include)
                    {
                        int depth = 0, index = next.cursor + 1;
                        for (; index < commands.Length; index++)
                        { if (commands[index].name == "(") depth++; else if (commands[index].name == ")" && --depth == 0) break; }
                        if (index >= commands.Length) throw new InvalidOperationException("Unclosed scene branch.");
                        next.cursor = index;
                    }
                    break;
                case "SAVECLEAR": next.firstClearSaved = true; break;
                case "GETGENERAL":
                    if (!next.unlockedGenerals.Contains(command.Int(0))) next.unlockedGenerals.Add(command.Int(0)); break;
                // Original GameCtl does not dispatch the underscored spelling in scenario10.
                case "GET_GENERAL": break;
                case "NEXTSCENE": next.nextScenario = command.args[0]; break;
                case "SAVE": case "ENDING_DEMO": case "FX1": case "NATIVE_INERT": case "(": case ")": break;
                case "PLAYMIDI": next.musicSource = command.args[0]; next.musicMode = command.args.Length > 1 ? command.Int(1) : 1; break;
                case "DO": case "ATTACK": case "MAGIC":
                case "CHANGESPRITE": case "CHANGETILE": case "CHANGETILE2": case "END": break;
                default: throw new InvalidOperationException("Unsupported command: " + command.name);
            }
            Advance(next);
            if (command.name == "DO") next.wait = Math.Max(0, command.Int(0)) / 1000d;
            if (!next.Matches(definition) || !items.IsValid) throw new InvalidOperationException("Invalid resulting checkpoint.");
            return StepResult.Advanced;
        }

        public static bool AdvanceDialogue(Scenario01Definition definition, Scenario01Progress state, out Scenario01Progress next)
        {
            next = state.Clone(); var commands = CurrentCommands(definition, state);
            if (state.wait > 0 || state.cursor >= commands.Length || commands[state.cursor].name != "MESSAGE") return false;
            var command = commands[state.cursor]; int.TryParse(command.args[0], out int mode);
            if (!ScenarioDialogueLayout.IsPortrait(mode)) return false;
            if (next.page + 3 < ScenarioDialogueLayout.SplitLines(command.text).Length) next.page += 3;
            else Advance(next);
            return true;
        }

        public static int Roll(Scenario01Progress state)
        { state.rng = unchecked(1664525u * state.rng + 1013904223u); return (int)(state.rng % 101u); }

        private static bool Predicate(Scenario01Command c, Scenario01Progress state, NativeInventory inventory, int x, int y)
        {
            switch (c.name)
            {
                case "CHARPOS": case "CHARPOS3": return x == c.Int(0) && y == c.Int(1);
                case "CHARPOS2": return state.dutySteps.Contains("charpos2:" + string.Join(":", c.args));
                case "ONLY_CHAR": return state.deathSides.Contains(c.args[0] + ":party");
                case "ONLY_MONSTER": return state.deathSides.Contains(c.args[0] + ":enemy");
                case "CAUSE_OF_DEATH": return state.dutySteps.Contains("cause:" + string.Join(":", c.args));
                case "CHARDEAD": return state.defeated.Contains(c.Int(0));
                case "ITEM": return inventory.Count(c.Int(0)) > 0;
                case "DUTY_STEP_TRUE": return state.dutySteps.Contains(c.args[0] + ":" + c.args[1]);
                case "DUTY_STEP_FALSE": return !state.dutySteps.Contains(c.args[0] + ":" + c.args[1]);
                case "RANDOM": return true; // Draw only on a predicate entry, never each frame.
                default: throw new InvalidOperationException("Unsupported 01 condition: " + c.name);
            }
        }

        public static bool TryActivate(Scenario01Definition definition, Scenario01Progress state, NativeInventory inventory,
            int x, int y, out Scenario01Progress changed, out NativeInventory changedInventory)
        {
            changed = state.Clone(); changedInventory = inventory.Clone();
            var candidate = changed;
            if (!state.Matches(definition) || !inventory.IsValid || state.phase != Scenario01Progress.Playing || state.activeEvent != 0) return false;
            var inside = new List<int>();
            foreach (var e in definition.events.OrderBy(e => e.id))
            {
                if (changed.activatedEvents.Contains(e.id)) continue;
                var groups = e.conditions.GroupBy(c => c.group).Where(g => g.All(c => Predicate(c, candidate, inventory, x, y))).ToArray();
                if (groups.Length == 0) continue;
                inside.Add(e.id);
                if (state.insideEvents.Contains(e.id)) continue;
                bool passes = groups.Any(g => g.Where(c => c.name == "RANDOM").All(c => Roll(candidate) < Math.Max(0, Math.Min(101, c.Int(0)))));
                if (!passes) continue;
                changed.branchRoll = e.actions.Any(c => c.name == "RANDOM") ? Roll(changed) : -1;
                var actions = ResolveActions(e, changed.branchRoll, definition.version == 2);
                // Prepare the complete economic candidate before publishing any presentation.
                foreach (var action in actions)
                {
                    switch (action.name)
                    {
                        case "GOLD":
                            if (action.Int(0) < 0) throw new InvalidOperationException("Negative GOLD.");
                            changedInventory.gold = checked(changedInventory.gold + action.Int(0)); break;
                        case "GETITEM": Grant(changedInventory, action.Int(0)); break;
                        case "USEITEM":
                            if (!changedInventory.TryConsume(action.Int(0), 1, out var consumed)) return false;
                            changedInventory = consumed; break;
                        case "SET_DUTY_STEP":
                            var step = action.args[0] + ":" + action.args[1];
                            if (!changed.dutySteps.Contains(step)) changed.dutySteps.Add(step); break;
                    }
                }
                foreach (var action in actions.Where(a => a.name == "CHANGESPRITE" || a.name == "CHANGETILE2"))
                { changed.mapChanges.Remove(action.line); changed.mapChanges.Add(action.line); }
                changed.activatedEvents.Add(e.id);
                if (actions.Any(c => c.name == "CHANGESPRITE")) changed.changedEvents.Add(e.id);
                changed.repeatEvent = definition.version == 2 && e.mode == "INF";
                changed.activeEvent = e.id; changed.cursor = 0; changed.page = 0;
                changed.cameraX = x; changed.cameraY = y;
                changed.insideEvents = inside;
                return changedInventory.IsValid && changed.IsValid;
            }
            changed.insideEvents = inside;
            return !inside.SequenceEqual(state.insideEvents) || changed.rng != state.rng;
        }

        public static void Grant(NativeInventory inventory, int id)
        {
            if (id <= 0) throw new InvalidOperationException("Invalid reward item.");
            var item = Array.Find(inventory.items, s => s.itemId == id);
            if (item != null) item.quantity = checked(item.quantity + 1);
            else inventory.items = inventory.items.Concat(new[] { new NativeItemStack { itemId = id, quantity = 1 } }).ToArray();
        }

        public static Scenario01Command[] ResolveActions(Scenario01Event e, int roll, bool directRandomGate = false)
        {
            var result = new List<Scenario01Command>(); bool test = false, include = true, inBlock = false;
            foreach (var command in e.actions)
            {
                switch (command.name)
                {
                    case "RANDOM":
                        if (roll < 0 || roll > 100) throw new InvalidOperationException("Missing persisted branch roll.");
                        test = roll < command.Int(0);
                        if (directRandomGate && !e.actions.Any(a => a.name == "TEST" || a.name == "TESTNOT")) include = test;
                        break;
                    case "TEST": include = test; break;
                    case "TESTNOT": include = !test; break;
                    case "(": if (inBlock) throw new InvalidOperationException("Nested 01 branch unsupported."); inBlock = true; break;
                    case ")": inBlock = false; include = true; break;
                    default: if (include) result.Add(command); break;
                }
            }
            if (inBlock) throw new InvalidOperationException("Unclosed event block.");
            return result.ToArray();
        }

        public static Scenario01Command[] CurrentCommands(Scenario01Definition definition, Scenario01Progress state)
            => state.activeEvent != 0 ? ResolveActions(definition.Event(state.activeEvent), state.branchRoll, definition.version == 2)
                : state.phase == Scenario01Progress.Intro ? definition.intro : state.phase == Scenario01Progress.Outro ? definition.Outro(state.endingVariant) : Array.Empty<Scenario01Command>();

        public static bool RecordBattleReturn(Scenario01Definition definition, Scenario01Progress state, int id, bool defeated,
            Scenario01EnemyState snapshot = null)
        {
            if (!state.Matches(definition) || state.phase != Scenario01Progress.Playing || state.pendingBattle != id
                || !definition.units.Any(u => u.id == id && u.wave == state.wave) || state.defeated.Contains(id)) return false;
            if (snapshot != null && (snapshot.id != id || snapshot.hp < 0 || snapshot.mp < 0 || snapshot.x < 0 || snapshot.x >= definition.width
                || snapshot.y < 0 || snapshot.y >= definition.height || !definition.units.Any(u => u.id == id && snapshot.hp <= u.CombatHp && snapshot.mp <= u.CombatMp))) return false;
            state.pendingBattle = -1;
            if (snapshot != null)
            { state.enemies.RemoveAll(e => e.id == id); state.enemies.Add(snapshot.Clone()); }
            if (!defeated) return true;
            if (!state.defeated.Contains(id)) state.defeated.Add(id);
            state.deathSides ??= new List<string>();
            if (!state.deathSides.Contains(id + ":party")) state.deathSides.Add(id + ":party");
            // Clear wins before a pending wave. The boss's death runs EVENT99/DEE.
            if ((definition.version != 1 || id != 700) && definition.units.Where(u => u.wave == state.wave).All(u => state.defeated.Contains(u.id)))
                state.wave = Math.Min(definition.version == 1 ? 2 : definition.units.Max(u => u.wave), state.wave + 1);
            return true;
        }

        public static void Advance(Scenario01Progress state) { state.cursor++; state.page = 0; state.wait = 0; }
        public static void FinishScript(Scenario01Progress state)
        {
            state.cursor = 0; state.page = 0; state.wait = 0;
            if (state.phase == Scenario01Progress.Outro && state.activeEvent == 0) state.actors.RemoveAll(a => a.id == 301 || state.version == 2 && a.partyProxy);
            else state.actors.Clear();
            if (state.activeEvent != 0)
            { if (state.repeatEvent) state.activatedEvents.Remove(state.activeEvent); state.repeatEvent = false; state.activeEvent = 0; if (state.clearRequested) state.phase = Scenario01Progress.Outro; }
            else if (state.phase == Scenario01Progress.Intro) state.phase = Scenario01Progress.Playing;
            else if (state.phase == Scenario01Progress.Outro) state.phase = Scenario01Progress.Complete;
        }
    }
}
