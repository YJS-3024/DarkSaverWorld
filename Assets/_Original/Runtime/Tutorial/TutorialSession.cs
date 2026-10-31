using System;
using System.Collections.Generic;
using System.Linq;
using DarkSaver.Original.Entry;

namespace DarkSaver.Original.Tutorial
{
    // Clock units map to milliseconds in the Unity adapter. This is not a Windows timing measurement.
    public static class TutorialPresentationTiming
    {
        public const double MovementPhaseMs = 80, IdleFrameMs = 125, AttackPhaseMs = 20, MagicPhaseMs = 40, DeathPhaseMs = 20;
        public const int MovementSubsteps = 4;
    }
    public enum TutorialInputKind { Continue, Key, HoverUnit, Unit, Action, Tile, Magic, Order, Ally, CloseDialog, DialogButton, Back }
    public enum TutorialAction { Attack = 1, Special = 2, Magic = 4, Rest = 8, Move = 16, Item = 32, Order = 64, Options = 128 }
    public readonly struct TutorialInput
    {
        public TutorialInputKind Kind { get; }
        public int Unit { get; } public int Value { get; } public TutorialTile Tile { get; }
        public TutorialInput(TutorialInputKind kind, int unit = 0, int value = 0, int x = 0, int y = 0)
        { Kind = kind; Unit = unit; Value = value; Tile = new TutorialTile(x, y); }
    }
    public sealed class TutorialActor
    {
        public int Id { get; internal set; } public int Job { get; internal set; }
        public int Hp { get; internal set; } = 99; public int MaxHp { get; internal set; } = 99;
        public int Mp { get; internal set; } = 99; public int MaxMp { get; internal set; } = 99;
        public int Attack { get; internal set; } public int Defense { get; internal set; }
        public int Mask { get; internal set; } = 255;
        public int Tick { get; internal set; } = 99;
        public int Magic { get; internal set; }
        public int Skill { get; internal set; }
        public TutorialTile Tile { get; internal set; }
        public int Direction { get; internal set; } = 1; // Up, Down, Left, Right sheet rows.
        public double X { get; internal set; } public double Y { get; internal set; }
        public bool Moving => path.Count != 0;
        public bool Dying { get; internal set; }
        public int AttackCounter { get; internal set; } = -1;
        public int DeathCounter { get; internal set; } = -1;
        public int AttackOther { get; internal set; } = -1;
        public bool AttackImpact { get; internal set; }
        internal List<TutorialTile> path = new List<TutorialTile>();
        internal int substep;
        internal double movementClock, attackClock, deathClock;
        public TutorialActor Copy()
        {
            var result = (TutorialActor)MemberwiseClone(); result.path = new List<TutorialTile>(path); return result;
        }
    }
    public sealed class TutorialView
    {
        public int Cursor, SourceLine, MessageCount, MenuUnit, HoverUnit, AllyMode, RangeUnit, MagicCaster, MagicTarget;
        public double Clock, MagicAge;
        public string Message, Victory, Wait, Error;
        public bool Completed, EndGame, MusicStarted, MouseEnabled, KeyboardEnabled, HoverEnabled, MagicVisible, OrderVisible;
        public TutorialTile? DisplayPosition;
        public TutorialAction? Range;
        public TutorialActor[] Actors;
        public int[] Dialogs;
    }
    public sealed class TutorialSession
    {
        private readonly TutorialScript script;
        private readonly TutorialMap map;
        private readonly int startingJob;
        private readonly Dictionary<int, TutorialActor> actors = new Dictionary<int, TutorialActor>();
        private readonly HashSet<int> dialogs = new HashSet<int>();
        private readonly HashSet<string> enabled = new HashSet<string>();
        private TutorialCommand wait;
        private double clock, until, magicAge = -1;
        private int cursor, messageCount, menuUnit = -1, hoverUnit = 0, allyMode, rangeUnit, magicCaster = -1, magicTarget = -1;
        private string message = "", victory = "", error = "";
        private TutorialTile? display;
        private TutorialAction? range;
        private bool magicVisible, orderVisible, endGame, complete, music;
        public bool Completed => complete;
        public bool HasEndedGame => endGame;
        public TutorialSession(TutorialScript script, TutorialMap map, int job)
        {
            this.script = script ?? throw new ArgumentNullException(nameof(script)); this.map = map ?? throw new ArgumentNullException(nameof(map));
            if (!map.IsValid || !LocalEntryPolicy.IsStartingJob(job)) throw new ArgumentException("Invalid tutorial definition or starting job.");
            startingJob = job; Pump();
        }
        public TutorialView Snapshot() => new TutorialView
        {
            Cursor = cursor, SourceLine = wait?.SourceLine ?? (cursor > 0 ? script.Commands[cursor - 1].SourceLine : 0), MessageCount = messageCount,
            MenuUnit = menuUnit, HoverUnit = hoverUnit, AllyMode = allyMode, RangeUnit = rangeUnit, Clock = clock,
            MagicAge = magicAge, MagicCaster = magicCaster, MagicTarget = magicTarget,
            Message = message, Victory = victory, Wait = wait?.Name ?? "", Error = error,
            Completed = complete, EndGame = endGame, MusicStarted = music,
            MouseEnabled = enabled.Contains("MOUSEMENU"), KeyboardEnabled = enabled.Contains("KEYBOARD"), HoverEnabled = enabled.Contains("MOUSEMOVE"),
            MagicVisible = magicVisible, OrderVisible = orderVisible, DisplayPosition = display, Range = range,
            Actors = actors.Values.Select(a => a.Copy()).OrderBy(a => a.Id).ToArray(), Dialogs = dialogs.OrderBy(v => v).ToArray()
        };
        public void Advance(double milliseconds)
        {
            if (milliseconds < 0 || double.IsNaN(milliseconds) || double.IsInfinity(milliseconds)) throw new ArgumentOutOfRangeException(nameof(milliseconds));
            if (complete || error.Length > 0) return;
            clock += milliseconds;
            foreach (var actor in actors.Values.ToArray())
            {
                if (actor.Moving && clock - actor.movementClock >= TutorialPresentationTiming.MovementPhaseMs)
                {
                    actor.movementClock = clock; var next = actor.path[0];
                    var dx = next.X - actor.Tile.X; var dy = next.Y - actor.Tile.Y;
                    actor.Direction = dx != 0 ? (dx > 0 ? 3 : 2) : (dy > 0 ? 1 : 0);
                    actor.substep++; double amount = actor.substep / (double)TutorialPresentationTiming.MovementSubsteps;
                    actor.X = actor.Tile.X + dx * amount; actor.Y = actor.Tile.Y + dy * amount;
                    if (actor.substep == TutorialPresentationTiming.MovementSubsteps)
                    { actor.Tile = next; actor.path.RemoveAt(0); actor.substep = 0; }
                }
                if (actor.AttackCounter >= 0 && clock - actor.attackClock >= TutorialPresentationTiming.AttackPhaseMs)
                { actor.attackClock = clock; if (++actor.AttackCounter > 48) actor.AttackCounter = -1; }
                bool magicHoldsCorpse = magicAge >= 0 && (actor.Id == magicCaster || actor.Id == magicTarget);
                if (actor.Dying && !magicHoldsCorpse && clock - actor.deathClock >= TutorialPresentationTiming.DeathPhaseMs)
                {
                    actor.deathClock = clock; if (++actor.DeathCounter > 18) actors.Remove(actor.Id);
                }
            }
            if (magicAge >= 0) { magicAge += milliseconds; if (magicAge >= MagicDurationMs) { magicAge = -1; magicCaster = magicTarget = -1; } }
            if (wait != null)
            {
                if (wait.Name == "DO" && clock >= until) wait = null;
                else if ((wait.Name == "DO2" || wait.Name == "WAITMOVEEND") && (!actors.TryGetValue(wait.Int(0), out var moving) || !moving.Moving)) wait = null;
            }
            Pump();
        }
        public const double MagicImpactMs = 10 * TutorialPresentationTiming.MagicPhaseMs;
        // Pattern 2 duration is supplied by the original pattern record in the presentation asset.
        public double MagicDurationMs { get; set; } = 1200;
        public bool Input(TutorialInput input)
        {
            if (complete || error.Length > 0) return false;
            var pending = wait; bool matched = false, accepted = false;
            if (input.Kind == TutorialInputKind.Continue)
            { matched = pending?.Name == "WAITMOUSEBUTTON"; accepted = matched; }
            else if (input.Kind == TutorialInputKind.Key && enabled.Contains("KEYBOARD") && pending?.Name == "WAITKEYBOARD" && Key(pending.Arguments[0]) == input.Value)
            { accepted = Toggle(input.Value); matched = accepted && pending?.Name == "WAITKEYBOARD" && Key(pending.Arguments[0]) == input.Value; }
            else if (input.Kind == TutorialInputKind.HoverUnit && enabled.Contains("MOUSEMOVE") && actors.ContainsKey(input.Unit))
            { hoverUnit = input.Unit; accepted = true; matched = pending?.Name == "WAITMOUSEMOVE" && pending.Int(0) == input.Unit; }
            else if (input.Kind == TutorialInputKind.DialogButton)
            {
                if (input.Value == 5) accepted = Toggle(5);
                else if (input.Value == 1 || input.Value == 2 || input.Value == 3 || input.Value == 4 || input.Value == 6 || input.Value == 8 || input.Value == 9)
                { dialogs.Add(input.Value); accepted = true; }
            }
            else if (input.Kind == TutorialInputKind.CloseDialog)
            { accepted = dialogs.Remove(input.Value); }
            else if (input.Kind == TutorialInputKind.Ally && dialogs.Contains(3) && input.Unit == 100)
            { accepted = true; matched = pending?.Name == "WAITALLYBUTTON"; if (matched) allyMode = 1; }
            else if (enabled.Contains("MOUSEMENU"))
            {
                if (input.Kind == TutorialInputKind.Unit && actors.TryGetValue(input.Unit, out var selected) && !selected.Dying && !selected.Moving)
                {
                    if ((range == TutorialAction.Attack || range == TutorialAction.Magic) && input.Unit >= 100)
                        return Input(new TutorialInput(TutorialInputKind.Tile, x: selected.Tile.X, y: selected.Tile.Y));
                    if (input.Unit < 100) { menuUnit = input.Unit; magicVisible = orderVisible = false; accepted = true; matched = pending?.Name == "WAITTOPMENU" && pending.Int(0) == input.Unit; }
                }
                else if (input.Kind == TutorialInputKind.Action && menuUnit == input.Unit && actors.TryGetValue(input.Unit, out var owner) && !owner.Moving && !owner.Dying && ActionAllowed(owner, (TutorialAction)input.Value))
                {
                    accepted = true; rangeUnit = owner.Id;
                    var action = (TutorialAction)input.Value;
                    magicVisible = action == TutorialAction.Magic; orderVisible = action == TutorialAction.Order;
                    if (action == TutorialAction.Move || action == TutorialAction.Attack) range = action;
                    else if (action != TutorialAction.Magic) range = null;
                    if (action == TutorialAction.Options) Toggle(12);
                    if (pending != null)
                    {
                        matched = (pending.Name == "WAITMOVEMENU" && action == TutorialAction.Move && pending.Int(0) == owner.Id)
                            || (pending.Name == "WAITATTACKMENU" && action == TutorialAction.Attack && pending.Int(0) == owner.Id)
                            || (pending.Name == "WAITRESTMENU" && action == TutorialAction.Rest && pending.Int(0) == owner.Id)
                            || (pending.Name == "WAITMAGICMENU" && pending.Int(0) == 0 && pending.Int(1) == owner.Id && action == TutorialAction.Magic)
                            || (pending.Name == "WAITORDERMENU" && pending.Int(0) == 0 && pending.Int(1) == owner.Id && action == TutorialAction.Order);
                    }
                    if (action == TutorialAction.Move || action == TutorialAction.Attack || action == TutorialAction.Rest) menuUnit = -1;
                }
                else if (input.Kind == TutorialInputKind.Tile)
                {
                    if (pending?.Name == "WAITMOVEPOS" && range == TutorialAction.Move && rangeUnit == pending.Int(0) && pending.Int(1) == input.Tile.X && pending.Int(2) == input.Tile.Y)
                    {
                        // Native click acknowledgement precedes movement completion.
                        accepted = matched = Move(pending.Int(0), input.Tile, 12); if (matched) { range = null; menuUnit = -1; }
                    }
                    else if (pending?.Name == "WAITATTACKCHAR" && range == TutorialAction.Attack && rangeUnit == pending.Int(0) && actors.TryGetValue(pending.Int(1), out var target) && target.Tile == input.Tile
                        && actors.TryGetValue(pending.Int(0), out var attacker) && Distance(attacker.Tile, target.Tile) <= 1)
                    {
                        StartAttack(attacker, target); accepted = matched = true; range = null; menuUnit = -1;
                    }
                    else if (pending?.Name == "WAITMAGICMENU" && pending.Int(0) == 2 && range == TutorialAction.Magic && rangeUnit == pending.Int(1) && actors.TryGetValue(pending.Int(2), out var targetMagic)
                        && targetMagic.Tile == input.Tile && actors.TryGetValue(pending.Int(1), out var caster) && Distance(caster.Tile, targetMagic.Tile) <= 3)
                    {
                        magicCaster = caster.Id; magicTarget = targetMagic.Id; magicAge = 0;
                        accepted = matched = true; range = null; menuUnit = -1;
                    }
                }
                else if (input.Kind == TutorialInputKind.Magic && magicVisible && actors.TryGetValue(input.Unit, out var caster) && caster.Magic == input.Value)
                { range = TutorialAction.Magic; rangeUnit = caster.Id; magicVisible = false; menuUnit = -1; accepted = true; matched = pending?.Name == "WAITMAGICMENU" && pending.Int(0) == 1 && pending.Int(1) == caster.Id; }
                else if (input.Kind == TutorialInputKind.Order && orderVisible && input.Unit == 0 && (input.Value == 1 || input.Value == 2 || input.Value == 3))
                { accepted = true; orderVisible = false; matched = pending?.Name == "WAITORDERMENU" && pending.Int(1) == 0 && ((pending.Int(0) == 1 && input.Value == 1) || (pending.Int(0) == 2 && input.Value == 3)); }
                else if (input.Kind == TutorialInputKind.Back) { range = null; magicVisible = orderVisible = false; menuUnit = -1; accepted = true; }
            }
            if (matched && ReferenceEquals(wait, pending)) { wait = null; Pump(); }
            return accepted;
        }
        public static bool ActionAllowed(TutorialActor actor, TutorialAction action) => (actor.Mask & (int)action) != 0
            && (actor.Id == 0 || action == TutorialAction.Move || action == TutorialAction.Attack || action == TutorialAction.Rest);
        public HashSet<TutorialTile> Occupied(int except) => new HashSet<TutorialTile>(actors.Values.Where(a => a.Id != except && !a.Dying).Select(a => a.Tile));
        public bool CanMove(int unit, TutorialTile tile) => actors.TryGetValue(unit, out var actor) && !actor.Dying && map.Path(actor.Tile, tile, 12, Occupied(unit)) != null;
        private bool Move(int unit, TutorialTile tile, int budget)
        {
            if (!actors.TryGetValue(unit, out var actor) || actor.Dying || actor.Moving) return false;
            var path = map.Path(actor.Tile, tile, budget, Occupied(unit)); if (path == null) return false;
            actor.path = path; actor.substep = 0; actor.movementClock = clock; return true;
        }
        private static int Distance(TutorialTile a, TutorialTile b) => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
        private void StartAttack(TutorialActor a, TutorialActor b)
        {
            a.AttackCounter = b.AttackCounter = 0; a.attackClock = b.attackClock = clock;
            a.AttackOther = b.Id; b.AttackOther = a.Id; a.AttackImpact = false; b.AttackImpact = true;
            int dx = b.Tile.X - a.Tile.X, dy = b.Tile.Y - a.Tile.Y;
            a.Direction = dx != 0 ? (dx > 0 ? 3 : 2) : (dy > 0 ? 1 : 0);
            b.Direction = dx != 0 ? (dx > 0 ? 2 : 3) : (dy > 0 ? 0 : 1);
        }
        private static int Key(string value) => int.Parse(value.Substring(1), System.Globalization.CultureInfo.InvariantCulture);
        private bool Toggle(int key)
        {
            if (key == 7) return true; // GameCtl has no F7 window branch.
            if (key != 1 && key != 2 && key != 3 && key != 4 && key != 5 && key != 6 && key != 8 && key != 9 && key != 12) return false;
            if (!dialogs.Remove(key)) dialogs.Add(key); return true;
        }
        private void Pump()
        {
            while (wait == null && !complete && error.Length == 0 && cursor < script.Commands.Count)
            {
                var command = script.Commands[cursor++]; // Native cursor is advanced before dispatch.
                switch (command.Name)
                {
                    case "LOADSCENE": if (command.Int(0) != 301) error = "Unsupported tutorial scene."; break;
                    case "PLAYMIDI": music = true; break;
                    case "TUTORIAL": case "SETPOSITION": break; // Map301 exactly fits the logical 640x480 viewport.
                    case "ENABLE": enabled.Add(command.Arguments[0]); break;
                    case "DISABLE": enabled.Remove(command.Arguments[0]); break;
                    case "CHARPOS":
                        int job = command.Arguments[0] == "COM0" ? startingJob : command.Int(0), id = command.Int(1);
                        actors[id] = new TutorialActor { Id = id, Job = job, Tile = new TutorialTile(command.Int(2), command.Int(3)), X = command.Int(2), Y = command.Int(3) }; break;
                    case "CHARABILITY":
                        var actor = actors[command.Int(0)]; var ability = map.abilities.FirstOrDefault(a => a.job == actor.Job);
                        if (ability == null) { error = "Missing original tutorial ability: " + actor.Job; break; }
                        actor.Hp = ability.hp; actor.MaxHp = ability.maxHp; actor.Mp = ability.mp; actor.MaxMp = ability.maxMp;
                        // dmSetAbility0x1000EA34..EA6B also seeds the practice spell/skill.
                        // CHARSETSKILL is still a separate unrecognized literal, not an alias.
                        actor.Magic = 101; actor.Skill = 10101; break;
                    case "CHARSETMAGIC": actors[command.Int(0)].Magic = command.Int(2); break;
                    case "CHARSETSKILL": case "CHARDIR": case "SETHP": case "SETMP": case "WAITFORMOVEEND": break; // No native dispatch branch.
                    case "MESSAGE": message = command.Text; messageCount++; break;
                    case "CHARMENU":
                        var menu = actors[command.Int(0)]; if (command.Int(1) != 999) menu.Tick = command.Int(1); menu.Mask = command.Int(2); break;
                    case "SETTOPMENU": menuUnit = -1; range = null; magicVisible = orderVisible = false; break;
                    case "DISPLAYPOS": display = command.Arguments[0] == "CLEAR" ? (TutorialTile?)null : new TutorialTile(command.Int(0), command.Int(1)); break;
                    case "CHARMOVE": if (!Move(command.Int(0), new TutorialTile(command.Int(1), command.Int(2)), 19)) error = "Native tutorial movement rejected at source line " + command.SourceLine; break;
                    case "HPSET": actors[command.Int(0)].Hp = unchecked((short)command.Int(1)); break;
                    case "KILLCHAR1": actors.Remove(command.Int(0)); break;
                    case "KILLCHAR2": var killed = actors[command.Int(0)]; killed.Dying = true; killed.DeathCounter = 0; killed.deathClock = clock; break;
                    case "SHOWDIALOG": case "HIDEDIALOG": Toggle(Key(command.Arguments[0])); break;
                    case "CLEARALLDIALOG": dialogs.Clear(); break;
                    case "SETALLYINFO": allyMode = command.Int(0); break;
                    case "SETVICTORY": victory = command.Text; break;
                    case "DO": until = clock + command.Int(0); wait = command; break;
                    case "DO2": case "WAITMOVEEND": if (actors.TryGetValue(command.Int(0), out var moving) && moving.Moving) wait = command; break;
                    case "ENDGAME": endGame = true; break;
                    case "END": if (endGame) complete = true; else error = "END without ENDGAME."; break;
                    default: wait = command; break;
                }
            }
            if (wait == null && !complete && error.Length == 0 && cursor == script.Commands.Count) error = "Tutorial ended without ENDGAME/END.";
        }
    }
}
