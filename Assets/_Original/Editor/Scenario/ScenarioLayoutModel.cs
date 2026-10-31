using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace DarkSaver.Original.Editor.Scenarios
{
    [Serializable, DataContract] public sealed class ScenarioPlacement
    {
        [DataMember] public int unit, job, x, y;
        [DataMember] public int setLine, posLine;
        public ScenarioPlacement Copy() => (ScenarioPlacement)MemberwiseClone();
    }
    [Serializable, DataContract] public sealed class ScenarioLayout
    {
        [DataMember] public int version = 1, width, height;
        [DataMember] public string mapKey, mapHash, setHash, posHash, source;
        [DataMember] public bool edited;
        [DataMember] public ScenarioPlacement[] units = Array.Empty<ScenarioPlacement>();
        public ScenarioLayout Copy()
        { var copy = (ScenarioLayout)MemberwiseClone(); copy.units = units.Select(u => u.Copy()).ToArray(); return copy; }
    }
    public sealed class ScenarioJob
    { public int id, picture; public string name, category; public override string ToString() => id + "  " + name; }
    public sealed class ScenarioMarker
    { public int x, y, line; public string kind, label, file; }
    public sealed class ScenarioActor
    { public int unit, x, y, line; public string job, file; }
    public sealed class ScenarioSource
    {
        public ScenarioLayout layout;
        public ScenarioMarker[] entries, events;
        public string[] scripts;
    }

    // No Unity dependencies: original source joins and editor-owned file transactions.
    public static class ScenarioLayoutData
    {
        public static bool IsKey(string key) => key != null && Regex.IsMatch(key, @"^\d{2,4}$");
        public static string Hash(byte[] bytes)
        { using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
        public static string[] Lines(string text) => text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        public static string[] Tokens(string line) => line.Split(';')[0].Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        public static bool Number(string token, out int value) => int.TryParse(token, out value);
        public static ScenarioSource ReadSource(string key, int width, int height, byte[] map, string set, string pos, string evt, string[] scripts)
        {
            if (!IsKey(key)) throw new InvalidDataException("Invalid map key.");
            var jobs = new Dictionary<int, (int job, int line)>();
            var positions = new Dictionary<int, (int x, int y, int line)>();
            var entries = new List<ScenarioMarker>();
            if (set != null)
            {
                var lines = Lines(set);
                for (int i = 0; i < lines.Length; i++)
                {
                    var t = Tokens(lines[i]);
                    if (t.Length < 2 || !Number(t[0], out int unit) || !Number(t[1], out int job)) continue;
                    if (unit < 400) continue;
                    if (jobs.ContainsKey(unit)) throw new InvalidDataException("Duplicate SET unit " + unit);
                    jobs.Add(unit, (job, i + 1));
                }
            }
            if (pos != null)
            {
                var lines = Lines(pos);
                for (int i = 0; i < lines.Length; i++)
                {
                    var t = Tokens(lines[i]);
                    if (t.Length != 3 || !Number(t[0], out int unit) || !Number(t[1], out int x) || !Number(t[2], out int y)) continue;
                    if (unit < 400)
                    { entries.Add(new ScenarioMarker { kind = "entry", label = "아군 슬롯 " + unit, x = x, y = y, line = i + 1, file = key + ".pos" }); continue; }
                    if (positions.ContainsKey(unit)) throw new InvalidDataException("Duplicate POS unit " + unit);
                    positions.Add(unit, (x, y, i + 1));
                }
            }
            var units = new List<ScenarioPlacement>();
            foreach (var pair in jobs.OrderBy(p => p.Key))
            {
                if (!positions.TryGetValue(pair.Key, out var at)) throw new InvalidDataException("SET unit has no POS: " + pair.Key);
                units.Add(new ScenarioPlacement { unit = pair.Key, job = pair.Value.job, x = at.x, y = at.y, setLine = pair.Value.line, posLine = at.line });
            }
            return new ScenarioSource {
                layout = new ScenarioLayout { mapKey = key, width = width, height = height, mapHash = Hash(map),
                    setHash = set == null ? "" : Hash(Encoding.UTF8.GetBytes(set)), posHash = pos == null ? "" : Hash(Encoding.UTF8.GetBytes(pos)),
                    source = set == null || pos == null ? "original-missing" : units.Count == 0 ? "original-empty" : "original-set-pos", units = units.ToArray() },
                entries = entries.ToArray(),
                events = ReadEvents(evt, key + ".evt"), scripts = scripts ?? Array.Empty<string>()
            };
        }
        public static ScenarioMarker[] ReadEvents(string text, string file)
        {
            if (text == null) return Array.Empty<ScenarioMarker>();
            var result = new List<ScenarioMarker>(); var block = new List<(string[] t, int line)>(); string id = "";
            Action flush = () => {
                if (block.Count == 0) return;
                bool chest = block.Any(v => string.Join(" ", v.t).Contains("상자"));
                string rewards = string.Join(", ", block.Where(v => v.t.Length > 0 && (v.t[0] == "GETITEM" || v.t[0] == "GOLD")).Select(v => string.Join(" ", v.t)));
                var anchors = chest ? block.Where(v => v.t.Length >= 3 && v.t[0] == "CHANGESPRITE").ToArray() : Array.Empty<(string[] t, int line)>();
                foreach (var row in block)
                {
                    var t = row.t; int x, y;
                    bool condition = t.Length == 4 && Number(t[0], out _) && t[1] == "CHARPOS" && Number(t[2], out x) && Number(t[3], out y);
                    bool anchor = chest && t.Length >= 3 && t[0] == "CHANGESPRITE" && Number(t[1], out x) && Number(t[2], out y);
                    if (!condition && !anchor) continue;
                    x = int.Parse(t[anchor ? 1 : 2]); y = int.Parse(t[anchor ? 2 : 3]);
                    result.Add(new ScenarioMarker { x = x, y = y, line = row.line, file = file, kind = anchor || (chest && anchors.Length == 0) ? "chest" : "event",
                        label = "EVENT " + id + (anchor ? " 상자 그림" : " 조건 위치") + (rewards.Length == 0 ? "" : " · " + rewards) });
                }
                block.Clear();
            };
            var lines = Lines(text);
            for (int i = 0; i < lines.Length; i++)
            {
                var t = Tokens(lines[i]);
                if (t.Length > 0 && t[0] == "EVENT") { flush(); id = t.Length > 1 ? t[1] : "?"; }
                else if (id.Length > 0 && t.Length > 0) block.Add((t, i + 1));
            }
            flush(); return result.ToArray();
        }
        public static ScenarioActor[] Cutscene(string text, string file, string key, int throughLine)
        {
            var actors = new Dictionary<int, ScenarioActor>(); string scene = key; var lines = Lines(text ?? "");
            for (int i = 0; i < Math.Min(lines.Length, throughLine); i++)
            {
                var t = Tokens(lines[i]); if (t.Length == 0) continue;
                if (t[0] == "LOADSCENE" && t.Length > 1) { scene = t[1]; actors.Clear(); }
                if (!int.TryParse(scene, out int current) || !int.TryParse(key, out int expected) || current != expected) continue;
                if (t[0] == "CHARPOS" && t.Length >= 5 && Number(t[2], out int id) && Number(t[3], out int x) && Number(t[4], out int y))
                    actors[id] = new ScenarioActor { unit = id, job = t[1], x = x, y = y, line = i + 1, file = file };
                else if (t[0] == "CHARMOVE" && t.Length >= 4 && Number(t[1], out id) && actors.TryGetValue(id, out var actor) && Number(t[2], out x) && Number(t[3], out y))
                { actor.x = x; actor.y = y; actor.line = i + 1; }
                else if ((t[0] == "KILLCHAR" || t[0] == "KILLCHAR2") && t.Length > 1 && Number(t[1], out id)) actors.Remove(id);
                else if (t[0] == "CLEARALLCHAR") actors.Clear();
            }
            return actors.Values.OrderBy(a => a.unit).ToArray();
        }
        public static void Validate(ScenarioLayout value, ScenarioLayout source, ISet<int> jobs)
        {
            if (value == null || source == null || value.version != 1 || !IsKey(value.mapKey) || value.mapKey != source.mapKey
                || value.width != source.width || value.height != source.height || value.mapHash != source.mapHash
                || value.setHash != source.setHash || value.posHash != source.posHash || value.source != source.source || value.units == null)
                throw new InvalidDataException("맵/원본 버전이 다른 편집 파일입니다. 원본을 덮어쓰지 않았습니다.");
            var ids = new HashSet<int>();
            foreach (var u in value.units)
                if (u == null || u.unit < 400 || !ids.Add(u.unit) || !jobs.Contains(u.job) || u.x < 0 || u.x >= value.width || u.y < 0 || u.y >= value.height)
                    throw new InvalidDataException("중복 unit, 잘못된 job 또는 맵 밖 좌표가 있습니다.");
        }
        public static int NextUnit(ScenarioLayout layout)
        { var ids = new HashSet<int>(layout.units.Select(u => u.unit)); int id = 400; while (ids.Contains(id)) id++; return id; }
        public static bool SameUnits(ScenarioLayout a, ScenarioLayout b) => a.units.Length == b.units.Length
            && a.units.OrderBy(u => u.unit).Zip(b.units.OrderBy(u => u.unit), (x, y) => x.unit == y.unit && x.job == y.job && x.x == y.x && x.y == y.y).All(v => v);
        public static byte[] Encode(ScenarioLayout layout)
        { using (var stream = new MemoryStream()) { new DataContractJsonSerializer(typeof(ScenarioLayout)).WriteObject(stream, layout); return stream.ToArray(); } }
        public static ScenarioLayout Decode(byte[] bytes)
        { using (var stream = new MemoryStream(bytes)) return (ScenarioLayout)new DataContractJsonSerializer(typeof(ScenarioLayout)).ReadObject(stream); }
        public static string Save(string folder, ScenarioLayout layout, ScenarioLayout source, ISet<int> jobs, string expectedFileHash)
        {
            Validate(layout, source, jobs); Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, layout.mapKey + ".json");
            string actual = File.Exists(path) ? Hash(File.ReadAllBytes(path)) : null;
            if (actual != expectedFileHash) throw new IOException("편집 파일이 외부에서 변경되었습니다. 다시 불러온 뒤 저장하세요.");
            var copy = layout.Copy(); copy.edited = !SameUnits(layout, source); var bytes = Encode(copy);
            // Verify the actual serialized payload before replacing an existing user file.
            Validate(Decode(bytes), source, jobs);
            string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try { File.WriteAllBytes(temp, bytes); if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path); }
            finally { if (File.Exists(temp)) File.Delete(temp); }
            return Hash(bytes);
        }
        public static IEnumerable<string[]> Csv(string text)
        {
            var fields = new List<string>(); var value = new StringBuilder(); bool quote = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '"') { if (quote && i + 1 < text.Length && text[i + 1] == '"') { value.Append('"'); i++; } else quote = !quote; }
                else if (!quote && (c == ',' || c == '\n'))
                { fields.Add(value.ToString().TrimEnd('\r')); value.Clear(); if (c == '\n') { yield return fields.ToArray(); fields.Clear(); } }
                else value.Append(c);
            }
            if (quote) throw new InvalidDataException("Invalid CSV quote.");
            if (value.Length > 0 || fields.Count > 0) { fields.Add(value.ToString().TrimEnd('\r')); yield return fields.ToArray(); }
        }
    }
}
