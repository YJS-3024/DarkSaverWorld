using System;
using System.Collections.Generic;
using System.Linq;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Campaign
{
    [Serializable] public sealed class CampaignUnit
    { public int unit, job, x, y, direction = 1; public string name; public CampaignUnit Copy() => (CampaignUnit)MemberwiseClone(); }
    [Serializable] public sealed class CampaignEntry { public int unit, x, y; }
    [Serializable] public sealed class CampaignMap
    {
        public int id; public string key, kind, hash, rosterSource;
        public int nativeIndex, introIndex = -1, outroIndex = -1, eventIndex = -1;
        public string intro, outro, events;
        public WorldMapDefinition world;
        public CampaignUnit[] units;
        public CampaignEntry[] entries;
    }
    [Serializable] public sealed class CampaignJob { public int job, picture; public string name; }
    [Serializable] public sealed class CampaignConnection
    { public int fromMap, toMap, x, y; public string evidence; public WorldPoint[] cells; }
    [Serializable] public sealed class CampaignDefinition
    {
        public CampaignMap[] maps; public CampaignJob[] jobs;
        public CampaignConnection[] connections;
        public CampaignMap Map(int id) => maps?.FirstOrDefault(m => m.id == id);
        public WorldDefinition Merge(WorldDefinition world)
        {
            // The original town/interiors remain authoritative for their existing ids.
            var original = new HashSet<int>(world.maps.Select(m => m.id));
            world.maps = world.maps.Concat(maps.Where(m => !original.Contains(m.id)).Select(m => m.world)).ToArray();
            world.campaign = this;
            foreach (var connection in connections)
            {
                if (string.IsNullOrWhiteSpace(connection.evidence)) continue;
                var from = world.Map(connection.fromMap); if (from == null || world.Map(connection.toMap) == null) continue;
                string id = "native-" + connection.fromMap + "-" + connection.toMap;
                if (from.portals.Any(p => p.id == id)) continue;
                from.portals = from.portals.Concat(new[] { new WorldPortal { id = "native-" + connection.fromMap + "-" + connection.toMap,
                    targetMap = connection.toMap, spawnX = connection.x, spawnY = connection.y, cells = connection.cells } }).ToArray();
            }
            return world;
        }
    }
    public sealed class CampaignCommand
    {
        public string name, text, raw; public string[] args; public int line;
        public int Int(int i, int fallback = 0) => i < args.Length && int.TryParse(args[i], out int value) ? value : fallback;
        public CampaignCommand(string value, int sourceLine)
        {
            raw = value; line = sourceLine; var t = value.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            name = t[0].ToUpperInvariant(); args = t.Skip(1).ToArray(); text = "";
            if (name == "MESSAGE")
            {
                int at = 0;
                for (int i = 0; i < 5; i++) { while (at < value.Length && char.IsWhiteSpace(value[at])) at++; while (at < value.Length && !char.IsWhiteSpace(value[at])) at++; }
                text = at < value.Length ? value.Substring(at + 1) : "";
            }
        }
        public static CampaignCommand[] Parse(string source)
        {
            var result = new List<CampaignCommand>(); int line = 0;
            foreach (var raw in (source ?? "").Replace("\r", "").Split('\n'))
            { line++; var value = raw.Split(';')[0].Trim(); if (value.Length > 0) result.Add(new CampaignCommand(value, line)); }
            return result.ToArray();
        }
    }
    public sealed class CampaignActor
    {
        public int unit, job, x, y, direction = 1; public double renderX, renderY; public bool moving, dead;
        public string sourceJob;
        internal int targetX, targetY; internal double movement;
        public CampaignActor Copy() => (CampaignActor)MemberwiseClone();
    }
    public sealed class CampaignPresentationView
    {
        public string message, heldCommand; public int face, speaker, line;
        public int cameraX, cameraY, scene; public bool hasCamera, waitingMessage, completed;
        public CampaignActor[] actors;
    }
    [Serializable] public sealed class CampaignClearNotice
    { public string session, id; public int map; public bool approved; }
    // Presentation only. Conditional outcomes/rewards/scene clear require authority.
    public sealed class CampaignPresentation
    {
        private readonly CampaignCommand[] commands;
        private readonly Dictionary<int, CampaignActor> actors = new Dictionary<int, CampaignActor>();
        private readonly int commanderJob;
        private readonly Func<int,bool> canLoad;
        private int scene;
        private int at, face, speaker, cameraX, cameraY;
        private double delay; private int waitUnit = -1;
        private string message = "", held = ""; private bool waiting, completed, hasCamera;
        public event Action<CampaignCommand> Effect;
        public CampaignPresentation(string script, int commanderJob, int map, Func<int,bool> canLoad = null)
        { commands = CampaignCommand.Parse(script); this.commanderJob = commanderJob; scene = map; this.canLoad = canLoad ?? (id => id == map); }
        private int Job(string value) => value == "COM0" ? commanderJob : int.TryParse(value, out int job) ? job : -1;
        private void Pump()
        {
            while (!completed && !waiting && delay <= 0 && held.Length == 0 && (waitUnit < 0 || !actors.TryGetValue(waitUnit, out var moving) || !moving.moving))
            {
                waitUnit = -1; if (at >= commands.Length) { completed = true; break; }
                var c = commands[at++];
                switch (c.name)
                {
                    case "LOADSCENE": if (!canLoad(c.Int(0, -1))) held = c.raw; else { scene = c.Int(0); actors.Clear(); hasCamera = false; } break;
                    case "SETPOSITION": case "MOVEPOSITION": cameraX = c.Int(0); cameraY = c.Int(1); hasCamera = true; break;
                    case "CHARPOS":
                        actors[c.Int(1)] = new CampaignActor { unit = c.Int(1), job = Job(c.args[0]), sourceJob = c.args[0], x = c.Int(2), y = c.Int(3), renderX = c.Int(2), renderY = c.Int(3) }; break;
                    case "CHARMOVE":
                        if (actors.TryGetValue(c.Int(0), out var actor)) { actor.targetX = c.Int(1); actor.targetY = c.Int(2); actor.moving = actor.renderX != actor.targetX || actor.renderY != actor.targetY; actor.movement = 0; } break;
                    case "CHARDIR":
                        if (actors.TryGetValue(c.Int(0), out actor)) actor.direction = c.args.Length > 1 ? c.args[1] == "UP" ? 0 : c.args[1] == "DOWN" ? 1 : c.args[1] == "RIGHT" ? 2 : 3 : 1; break;
                    case "KILLCHAR1": actors.Remove(c.Int(0)); break;
                    case "KILLCHAR2": if (actors.TryGetValue(c.Int(0), out actor)) { actor.dead = true; actor.moving = false; } break;
                    case "DO": delay = Math.Max(0, c.Int(0)); break;
                    case "DO2": waitUnit = c.Int(0); break;
                    case "MESSAGE": message = c.text; face = c.args.Length > 1 ? Job(c.args[1]) : -1; speaker = c.Int(2); waiting = !string.IsNullOrWhiteSpace(message); break;
                    case "WAITMOUSEBUTTON": waiting = true; break;
                    case "PLAYMIDI": case "MAGIC": case "ATTACK": case "FX1": Effect?.Invoke(c); break;
                    case "END": completed = true; message = ""; break;
                    default: held = c.raw; break;
                }
            }
        }
        public void Continue() { if (!waiting) return; waiting = false; message = ""; Pump(); }
        public void Advance(double ms)
        {
            if (double.IsNaN(ms) || double.IsInfinity(ms) || ms < 0) return;
            foreach (var actor in actors.Values.Where(a => a.moving))
            {
                actor.movement += ms;
                while (actor.moving && actor.movement >= 80)
                {
                    actor.movement -= 80; double dx = actor.targetX - actor.renderX, dy = actor.targetY - actor.renderY;
                    actor.direction = DarkSaver.Original.Tutorial.TutorialGeometry.FacingRow(Math.Sign(dx), Math.Sign(dy));
                    actor.renderX += Math.Sign(dx) * Math.Min(Math.Abs(dx), .5); actor.renderY += Math.Sign(dy) * Math.Min(Math.Abs(dy), .5);
                    if (actor.renderX == actor.targetX && actor.renderY == actor.targetY) { actor.x = actor.targetX; actor.y = actor.targetY; actor.moving = false; }
                }
            }
            if (delay > 0) delay = Math.Max(0, delay - ms); Pump();
        }
        public CampaignPresentationView Snapshot() => new CampaignPresentationView { message = message, heldCommand = held, face = face, speaker = speaker,
            cameraX = cameraX, cameraY = cameraY, scene = scene, hasCamera = hasCamera, waitingMessage = waiting, completed = completed, line = at == 0 ? 0 : commands[Math.Min(at, commands.Length) - 1].line,
            actors = actors.Values.Select(a => a.Copy()).ToArray() };
    }
}
