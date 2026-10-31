using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DarkSaver.Original.Tutorial
{
    public sealed class TutorialCommand
    {
        public string Name { get; }
        public string[] Arguments { get; }
        public string Raw { get; }
        public string Text { get; }
        public int SourceLine { get; }
        public int Int(int index) => index < Arguments.Length ? int.Parse(Arguments[index], CultureInfo.InvariantCulture) : 0;
        internal TutorialCommand(string raw, int line)
        {
            Raw = raw; SourceLine = line;
            var pieces = raw.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            Name = pieces[0]; Arguments = pieces.Skip(1).ToArray();
            if (Name == "MESSAGE")
            {
                int position = 0;
                for (int field = 0; field < 5; field++)
                {
                    while (position < raw.Length && char.IsWhiteSpace(raw[position])) position++;
                    while (position < raw.Length && !char.IsWhiteSpace(raw[position])) position++;
                }
                // Consume the field separator only: keep intentional spaces in the dialogue.
                Text = position < raw.Length ? raw.Substring(position + 1) : "";
            }
            else Text = raw.Length > Name.Length ? raw.Substring(Name.Length).TrimStart() : "";
        }
    }
    public sealed class TutorialScript
    {
        private readonly TutorialCommand[] commands;
        public IReadOnlyList<TutorialCommand> Commands => commands;
        private static readonly HashSet<string> vocabulary = new HashSet<string>(
            ("LOADSCENE PLAYMIDI SETPOSITION TUTORIAL DISABLE ENABLE CHARPOS CHARABILITY CHARSETMAGIC CHARSETSKILL MESSAGE WAITMOUSEBUTTON " +
            "SETTOPMENU CHARMENU WAITTOPMENU DO WAITMOVEMENU DISPLAYPOS WAITMOVEPOS DO2 WAITKEYBOARD WAITMOUSEMOVE CHARMOVE WAITMOVEEND " +
            "WAITATTACKMENU WAITATTACKCHAR HPSET KILLCHAR2 WAITRESTMENU WAITMAGICMENU SETHP SETMP WAITORDERMENU KILLCHAR1 " +
            "CLEARALLDIALOG SHOWDIALOG HIDEDIALOG SETALLYINFO SETVICTORY WAITALLYBUTTON WAITFORMOVEEND CHARDIR ENDGAME END").Split(' '));
        public TutorialScript(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            var result = new List<TutorialCommand>(); int line = 0;
            foreach (string raw in text.Replace("\r", "").Split('\n'))
            {
                line++; var value = raw.TrimStart(); if (value.Length == 0 || value.StartsWith(";", StringComparison.Ordinal)) continue;
                var command = new TutorialCommand(value, line);
                if (!vocabulary.Contains(command.Name)) throw new FormatException("Unsupported native tutorial command at line " + line + ": " + command.Name);
                result.Add(command);
            }
            commands = result.ToArray();
        }
    }
    public readonly struct TutorialTile : IEquatable<TutorialTile>
    {
        public int X { get; } public int Y { get; }
        public TutorialTile(int x, int y) { X = x; Y = y; }
        public bool Equals(TutorialTile other) => X == other.X && Y == other.Y;
        public override bool Equals(object other) => other is TutorialTile tile && Equals(tile);
        public override int GetHashCode() => X * 397 ^ Y;
        public static bool operator ==(TutorialTile a, TutorialTile b) => a.Equals(b);
        public static bool operator !=(TutorialTile a, TutorialTile b) => !a.Equals(b);
    }
    [Serializable] public sealed class TutorialAbility
    {
        public int job, hp, maxHp, mp, maxMp, attack, defense;
    }
    [Serializable] public sealed class TutorialMap
    {
        public int width, height;
        public int[] attributes, terrain;
        public TutorialAbility[] abilities;
        public bool Contains(TutorialTile p) => p.X >= 0 && p.Y >= 0 && p.X < width && p.Y < height;
        public int Attribute(TutorialTile p) => Contains(p) ? attributes[p.Y * width + p.X] : -1;
        public int Terrain(TutorialTile p, int field)
        {
            int attribute = Attribute(p); return attribute >= 0 && attribute * 30 + field < terrain.Length ? terrain[attribute * 30 + field] : 99;
        }
        public bool IsValid => width > 0 && height > 0 && attributes != null && attributes.Length == width * height
            && terrain != null && terrain.Length == 27 * 30 && abilities != null;
        // Native cardinal score/backtrack, then compress consecutive perpendicular steps.
        // There is deliberately no extra diagonal-corner rejection.
        public List<TutorialTile> Path(TutorialTile start, TutorialTile end, int budget, HashSet<TutorialTile> occupied)
        {
            var cost = new Dictionary<TutorialTile, int> { [start] = 0 };
            var previous = new Dictionary<TutorialTile, TutorialTile>(); var open = new List<TutorialTile> { start };
            var offsets = new[] { new TutorialTile(0, -1), new TutorialTile(-1, 0), new TutorialTile(1, 0), new TutorialTile(0, 1) };
            while (open.Count > 0)
            {
                int at = 0; for (int i = 1; i < open.Count; i++) if (cost[open[i]] < cost[open[at]]) at = i;
                var here = open[at]; open.RemoveAt(at); if (here == end) break;
                foreach (var offset in offsets)
                {
                    var next = new TutorialTile(here.X + offset.X, here.Y + offset.Y);
                    if (!Contains(next) || occupied.Contains(next)) continue;
                    int movement = Terrain(next, 2);
                    if (movement <= 0 || movement == 99 || movement == -99) continue;
                    int score = cost[here] + movement; if (score > budget || (cost.TryGetValue(next, out int old) && old <= score)) continue;
                    cost[next] = score; previous[next] = here; if (!open.Contains(next)) open.Add(next);
                }
            }
            if (!cost.ContainsKey(end)) return null;
            var reversed = new List<TutorialTile>(); var cursor = end;
            while (cursor != start) { reversed.Add(cursor); cursor = previous[cursor]; }
            reversed.Reverse(); var result = new List<TutorialTile>(); cursor = start;
            for (int i = 0; i < reversed.Count; i++)
            {
                var next = reversed[i];
                if (i + 1 < reversed.Count && Math.Abs(reversed[i + 1].X - cursor.X) == 1 && Math.Abs(reversed[i + 1].Y - cursor.Y) == 1)
                    next = reversed[++i];
                result.Add(next); cursor = next;
            }
            return result;
        }
    }
}
