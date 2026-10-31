using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Tutorial;

internal static class WaitingLessonChecks
{
    public static WaitingDefinition Definition() => JsonSerializer.Deserialize<WaitingDefinition>(
        File.ReadAllText("Assets/_Original/Data/Tutorial/Waiting1500.json"), new JsonSerializerOptions { IncludeFields = true });
    public static void Run(Func<WaitingView> snapshot, Func<WaitingInput, bool> send, Action<double> advance)
    {
        int guard = 0;
        while (!snapshot().Completed)
        {
            if (++guard > 10000) throw new Exception("Waiting lesson did not terminate at " + snapshot().ResourceId);
            var v = snapshot();
            if (v.ResourceId == 0 || v.Moving) { advance(20); continue; }
            WaitingInput input;
            switch (v.ResourceId)
            {
                case 61216: case 61217: case 61227: case 61235: case 61242: case 61243: case 61248: case 61249:
                    input = new WaitingInput(WaitingInputKind.Continue); break;
                case 61218: input = new WaitingInput(WaitingInputKind.Move, x: 15, y: 7); break;
                case 61219: input = new WaitingInput(WaitingInputKind.Move, x: 2, y: 7); break;
                case 61220: case 61222: input = new WaitingInput(WaitingInputKind.DoubleSelf); break;
                case 61221: input = new WaitingInput(WaitingInputKind.Button, 18, 38); break;
                case 61223: input = new WaitingInput(WaitingInputKind.Button, 18, 37); break;
                case 61224: input = new WaitingInput(WaitingInputKind.Restore, 18); break;
                case 61225: case 61226: input = new WaitingInput(WaitingInputKind.Key, value: 2); break;
                case 61228:
                    var main = v.Windows.First(w => w.Id == 18);
                    input = new WaitingInput(WaitingInputKind.DragWindow, 18, value: 1, x: main.X + 1, y: main.Y); break;
                case 61229: input = new WaitingInput(WaitingInputKind.Button, 18, 31); break;
                case 61230: case 61231: input = new WaitingInput(WaitingInputKind.Button, 19, 83); break;
                case 61232: input = new WaitingInput(WaitingInputKind.Button, 19, 62); break;
                case 61233: case 61234: input = new WaitingInput(WaitingInputKind.Key, value: 5); break;
                case 61239: input = new WaitingInput(WaitingInputKind.Button, 18, 21); break;
                case 61240: input = new WaitingInput(WaitingInputKind.Key, value: 3); break;
                case 61241: input = new WaitingInput(WaitingInputKind.Button, 18, 20); break;
                case 61244: input = new WaitingInput(WaitingInputKind.Move, x: 16, y: 8); break;
                case 61245: input = new WaitingInput(WaitingInputKind.Move, x: 16, y: 9); break;
                case 61246: input = new WaitingInput(WaitingInputKind.Equip, value: 1, x: 2); break;
                case 61247: input = new WaitingInput(WaitingInputKind.Equip, value: v.Job == 103 ? 777 : 762, x: 3); break;
                case 61250: input = new WaitingInput(WaitingInputKind.HoverDoor, x: 19, y: 7); break;
                case 61251: input = new WaitingInput(WaitingInputKind.Move, x: 19, y: 7); break;
                default: throw new Exception("Unexpected waiting lesson resource " + v.ResourceId);
            }
            if (!send(input)) throw new Exception("Waiting input rejected at " + v.ResourceId + ": " + input.Kind);
        }
    }
    public static int Contracts()
    {
        int count = 0;
        void Check(bool value, string message) { if (!value) throw new Exception(message); count++; }
        foreach (int job in new[] { 100, 101, 103 })
        {
            var session = new WaitingLesson(Definition(), job);
            Check(!session.Input(new WaitingInput(WaitingInputKind.Continue)), "Cannot click past the startup delay");
            session.Advance(double.NaN); session.Advance(double.PositiveInfinity);
            Check(session.Snapshot().Clock == 0, "Invalid timer input leaves original state intact");
            session.Advance(1001);
            session.Input(new WaitingInput(WaitingInputKind.Continue)); session.Input(new WaitingInput(WaitingInputKind.Continue));
            Check(session.Snapshot().ResourceId == 61218, "Intro text reaches actual movement wait");
            Check(!session.Input(new WaitingInput(WaitingInputKind.Continue)) && session.Snapshot().ResourceId == 61218, "Click cannot skip movement lesson");
            Check(!session.Input(new WaitingInput(WaitingInputKind.Move, x: -1, y: 7)), "Off-map move rejected without state change");
            Check(!session.Input(new WaitingInput(WaitingInputKind.Equip, value: 1, x: 2)), "Cannot fabricate tutorial item");
            var copy = session.Snapshot(); copy.Windows[0].Visible = true;
            Check(!session.Snapshot().Windows[0].Visible, "Client cannot mutate authoritative windows through snapshot");
            bool movedWrong = session.Input(new WaitingInput(WaitingInputKind.Move, x: 14, y: 7));
            Check(movedWrong, "Valid non-objective movement remains available");
            for (int i = 0; i < 300 && session.Snapshot().Moving; i++) session.Advance(20);
            Check(session.Snapshot().ResourceId == 61218, "Arrival at wrong tile does not advance teaching");
            var observed = new System.Collections.Generic.HashSet<int>();
            Run(() => { var v = session.Snapshot(); observed.Add(v.ResourceId); return v; }, input =>
            {
                if (input.Kind == WaitingInputKind.DragWindow)
                {
                    session.Input(new WaitingInput(WaitingInputKind.DragWindow, input.Window, value: 0, x: input.X, y: input.Y));
                    Check(session.Snapshot().ResourceId == 61228, "Changed rectangle waits until drag release");
                }
                if (input.Kind == WaitingInputKind.Equip && input.Value == 1)
                {
                    Check(!session.Input(new WaitingInput(WaitingInputKind.Equip, value: 1, x: 3)), "Sword cannot equip to body");
                    Check(session.Snapshot().Sword == 1 && session.Snapshot().Hand == -1, "Wrong drop preserves inventory and equipment");
                    bool accepted = session.Input(input);
                    Check(!session.Input(input) && session.Snapshot().Hand == 1, "Stale equip request cannot duplicate item");
                    return accepted;
                }
                if (input.Kind == WaitingInputKind.HoverDoor)
                    Check(!session.Input(new WaitingInput(WaitingInputKind.HoverDoor, x: 18, y: 7))
                        && session.Snapshot().ResourceId == 61250, "Wrong hover cannot skip exit lesson");
                return session.Input(input);
            }, session.Advance);
            var end = session.Snapshot();
            Check(end.Completed && end.Tile == new TutorialTile(19, 7), "Physical door arrival completes waiting lesson");
            Check(end.Hand == 1 && end.Body == (job == 103 ? 777 : 762), "Correct class-specific temporary gear equipped");
            Check(end.Sword == -1 && end.Armor == -1 && !end.SwordOnFloor && !end.ArmorOnFloor, "No duplicate inventory/floor items remain");
            Check(!observed.Contains(61236) && !observed.Contains(61237) && !observed.Contains(61238), "Original disabled lessons remain skipped");
            Check(!session.Input(new WaitingInput(WaitingInputKind.Continue)), "Ended lesson rejects stale requests");
        }
        return count;
    }
}
