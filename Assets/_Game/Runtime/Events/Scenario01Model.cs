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
        // Display-only aggregate; combat has already granted the experience.
        public long battleExperienceEarned;
        public bool hasBattleExperienceLedger;
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
            && battleExperienceEarned>=0
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
{throw new System.NotSupportedException("원작 서버 초기 상태가 필요합니다.");}    }

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
{}
        public static StepResult Step(Scenario01Definition definition, Scenario01Progress state, NativeInventory inventory,
            int playerJob, out Scenario01Progress next, out NativeInventory items)
{next=null;items=null;return StepResult.Waiting;}        public static bool AdvanceDialogue(Scenario01Definition definition, Scenario01Progress state, out Scenario01Progress next)
{next=default;return false;}
        public static int Roll(Scenario01Progress state)
{throw new System.NotSupportedException("원작 서버 난수 결과가 필요합니다.");}
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
{changed=default;changedInventory=default;return false;}
        public static void Grant(NativeInventory inventory, int id)
{throw new System.NotSupportedException("원작 서버 보상 응답이 필요합니다.");}
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
{return false;}
        public static void Advance(Scenario01Progress state) {}        public static void FinishScript(Scenario01Progress state)
{}    }
}
