using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.Tutorial;

internal static class TutorialChecks
{
    private static int count, fullRuns, simulatedFrames, acceptedInputs;
    private static void Check(bool value, string name)
    { if (!value) throw new Exception(name); count++; }
    private static void Require(bool value, string name)
    { if (!value) throw new Exception(name); }
    private sealed class Store : IEntryAccountStore
    {
        public LocalAccountDocument Data = new LocalAccountDocument();
        public bool Fail;
        public int Writes;
        public bool TryRead(out LocalAccountDocument document) { document = Data.Copy(); return true; }
        public bool TryWrite(LocalAccountDocument document) { if (Fail) return false; Writes++; Data = document.Copy(); return true; }
    }
    private static TutorialMap map;
    private static TutorialScript script;
    private static int sourceWaitCount, sourceMessageCount;
    private static readonly TutorialInput Continue = new TutorialInput(TutorialInputKind.Continue);
    private static TutorialCommand Waiting(TutorialView v) => script.Commands[v.Cursor - 1];
    private static void Run(Func<TutorialView> snapshot, Func<TutorialInput, bool> send, Action<double> advance)
    {
        int frames = 0, inputs = 0;
        var observedLines = new HashSet<int>();
        while (!snapshot().Completed)
        {
            var view = snapshot(); Require(view.Error == "", "No script error at line " + view.SourceLine + ": " + view.Error);
            Require(++frames < 20000, "Finite full tutorial path at " + view.Wait + "/" + view.SourceLine);
            var command = Waiting(view); observedLines.Add(command.SourceLine);
            var player = view.Actors.First(a => a.Id == 0);
            if (view.Wait == "DO" || view.Wait == "DO2" || view.Wait == "WAITMOVEEND") { advance(20); continue; }
            if (player.Moving && new[] { "WAITTOPMENU", "WAITMOVEMENU", "WAITATTACKMENU", "WAITRESTMENU", "WAITORDERMENU", "WAITMAGICMENU" }.Contains(view.Wait))
            { advance(20); continue; }
            bool result = false;
            switch (view.Wait)
            {
                case "WAITMOUSEBUTTON": result = send(Continue); break;
                case "WAITKEYBOARD": result = send(new TutorialInput(TutorialInputKind.Key, value: int.Parse(command.Arguments[0].Substring(1)))); break;
                case "WAITMOUSEMOVE": result = send(new TutorialInput(TutorialInputKind.HoverUnit, command.Int(0))); break;
                case "WAITTOPMENU": result = send(new TutorialInput(TutorialInputKind.Unit, command.Int(0))); break;
                case "WAITMOVEMENU": case "WAITATTACKMENU": case "WAITRESTMENU":
                    int unit = command.Int(0);
                    if (view.MenuUnit != unit) result = send(new TutorialInput(TutorialInputKind.Unit, unit));
                    else result = send(new TutorialInput(TutorialInputKind.Action, unit, (int)(view.Wait == "WAITMOVEMENU" ? TutorialAction.Move : view.Wait == "WAITATTACKMENU" ? TutorialAction.Attack : TutorialAction.Rest)));
                    break;
                case "WAITMOVEPOS":
                    if (player.Moving) { advance(20); continue; }
                    if (view.Range != TutorialAction.Move)
                    {
                        result = view.MenuUnit != command.Int(0) ? send(new TutorialInput(TutorialInputKind.Unit, command.Int(0)))
                            : send(new TutorialInput(TutorialInputKind.Action, command.Int(0), (int)TutorialAction.Move));
                    }
                    else result = send(new TutorialInput(TutorialInputKind.Tile, x: command.Int(1), y: command.Int(2)));
                    break;
                case "WAITATTACKCHAR":
                    if (player.Moving) { advance(20); continue; }
                    if (view.Range != TutorialAction.Attack)
                        result = view.MenuUnit != 0 ? send(new TutorialInput(TutorialInputKind.Unit, 0)) : send(new TutorialInput(TutorialInputKind.Action, 0, (int)TutorialAction.Attack));
                    else result = send(new TutorialInput(TutorialInputKind.Unit, command.Int(1)));
                    break;
                case "WAITMAGICMENU":
                    if (command.Int(0) == 0)
                        result = view.MenuUnit != 0 ? send(new TutorialInput(TutorialInputKind.Unit, 0)) : send(new TutorialInput(TutorialInputKind.Action, 0, (int)TutorialAction.Magic));
                    else if (command.Int(0) == 1) result = send(new TutorialInput(TutorialInputKind.Magic, 0, 101));
                    else result = send(new TutorialInput(TutorialInputKind.Unit, command.Int(2)));
                    break;
                case "WAITORDERMENU":
                    if (command.Int(0) == 0)
                        result = view.MenuUnit != 0 ? send(new TutorialInput(TutorialInputKind.Unit, 0)) : send(new TutorialInput(TutorialInputKind.Action, 0, (int)TutorialAction.Order));
                    else result = send(new TutorialInput(TutorialInputKind.Order, 0, command.Int(0) == 1 ? 1 : 3));
                    break;
                case "WAITALLYBUTTON": result = send(new TutorialInput(TutorialInputKind.Ally, 100)); break;
                default: throw new Exception("Unhandled wait " + view.Wait);
            }
            Require(result, "Input accepted at " + view.Wait + "/" + view.SourceLine);
            Require(++inputs < 350, "No input loop");
        }
        var final = snapshot(); Check(final.EndGame && final.Cursor == 420 && final.MessageCount == 100, "Exact native completion boundary");
        Check(final.Actors.All(a => a.Id != 1 && a.Id != 2 && a.Id != 400 && a.Id != 410), "Removed temporary units");
        Check(final.Actors.First(a => a.Id == 0).Hp == 99, "Absolute HPSET99, not percent/full-heal");
        Check(final.Actors.First(a => a.Id == 0).Magic == 101, "Native tutorial magic seeded");
        Check(final.Actors.First(a => a.Id == 800).Direction == 0, "Messenger faces actual upward movement, not no-op CHARDIR");
        fullRuns++; simulatedFrames += frames; acceptedInputs += inputs; sourceWaitCount = observedLines.Count; sourceMessageCount = final.MessageCount;
    }
    private static TutorialSession Session(string text) => new TutorialSession(new TutorialScript(text), map, 100) { MagicDurationMs = 600 };
    private static void NativeContracts()
    {
        Check(script.Commands.Count == 420, "420 source commands");
        Check(script.Commands.Select(c => c.Name).Distinct().Count() == 44, "44 source spellings");
        Check(script.Commands.Count(c => c.Name == "MESSAGE") == 100, "100 original dialogue messages");
        Check(script.Commands.Count(c => new[] { "CHARDIR", "CHARSETSKILL", "SETHP", "SETMP", "WAITFORMOVEEND" }.Contains(c.Name)) == 5, "Five literal native no-ops");
        var source = script.Commands.First(c => c.Name == "MESSAGE"); Check(source.Text.StartsWith("이제부터는 전투에 대해") && source.Text.Contains('|'), "Unrewritten dialogue");
        var idle = new TutorialSession(script, map, 100); var initial = idle.Snapshot();
        Check(initial.Actors.Length == 4 && initial.Actors.First(a => a.Id == 0).Tile == new TutorialTile(2, 6), "Original initial layout");
        Check(initial.Actors.First(a => a.Id == 0).MaxMp == 65, "Raw ability MP maximum, no old permanent stats");
        Check(!idle.Input(new TutorialInput(TutorialInputKind.Key, value: 2)), "Keyboard disabled initially");
        Check(!idle.Input(new TutorialInput(TutorialInputKind.Unit, 0)), "Menu disabled initially");
        Check(idle.Input(Continue), "Single continue press accepted"); Check(idle.Snapshot().MessageCount == 2 && idle.Snapshot().Cursor < 25, "One press cannot drain repeated waits");
        initial.Actors[0].Tile = new TutorialTile(19, 14); Check(idle.Snapshot().Actors[0].Tile != initial.Actors[0].Tile, "Snapshot detached from authority");

        string spawn = "CHARPOS COM0 0 2 6\nCHARABILITY 0\nCHARSETMAGIC 0 1 101\nCHARPOS 301 400 7 6\nCHARABILITY 400\n";
        var noops = Session(spawn + "CHARDIR 0 UP\nSETHP 0 0\nSETMP 0 0\nCHARSETSKILL 0 1 99999\nWAITFORMOVEEND 0\nWAITMOUSEBUTTON\nENDGAME\nEND");
        var n = noops.Snapshot().Actors[0]; Check(n.Hp == 200 && n.Mp == 45 && n.Skill == 10101 && n.Direction == 1 && noops.Snapshot().Wait == "WAITMOUSEBUTTON", "No aliasing of unsupported source spellings");
        var timer = Session("MESSAGE 1 125 0 0 first\nDO 500\nMESSAGE 1 125 0 0 second\nWAITMOUSEBUTTON\nENDGAME\nEND");
        timer.Advance(499); Check(timer.Snapshot().Message == "first", "Timer below boundary"); timer.Advance(1); Check(timer.Snapshot().Message == "second", "Timer at boundary");
        var keyboard = Session(spawn + "ENABLE KEYBOARD\nWAITKEYBOARD F2\nWAITMOUSEBUTTON\nENDGAME\nEND");
        Check(!keyboard.Input(new TutorialInput(TutorialInputKind.Key, value: 3)) && keyboard.Snapshot().Dialogs.Length == 0, "Wrong tutorial key neither toggles nor advances");
        Check(keyboard.Input(new TutorialInput(TutorialInputKind.Key, value: 2)) && keyboard.Snapshot().Dialogs.Contains(2), "Expected F2 toggles actual window");
        var hover = Session(spawn + "ENABLE MOUSEMOVE\nWAITMOUSEMOVE 0\nWAITMOUSEBUTTON\nENDGAME\nEND");
        Check(hover.Input(new TutorialInput(TutorialInputKind.HoverUnit, 400)) && hover.Snapshot().Wait == "WAITMOUSEMOVE", "Wrong target hover does not unblock");
        Check(hover.Input(new TutorialInput(TutorialInputKind.HoverUnit, 0)) && hover.Snapshot().Wait == "WAITMOUSEBUTTON", "Unit-specific hover unblock");
        var toggle = Session("SHOWDIALOG F2\nSHOWDIALOG F2\nHIDEDIALOG F3\nSHOWDIALOG F7\nWAITMOUSEBUTTON\nENDGAME\nEND");
        Check(toggle.Snapshot().Dialogs.SequenceEqual(new[] { 3 }), "SHOW/HIDE both toggle, F7 no invented window");
        var movement = Session(spawn + "ENABLE MOUSEMENU\nCHARMENU 0 999 255\nWAITMOVEPOS 0 4 7\nDO2 0\nWAITMOUSEBUTTON\nENDGAME\nEND");
        Check(!movement.Input(new TutorialInput(TutorialInputKind.Tile, x: 4, y: 7)), "Move tile needs move action");
        Check(movement.Input(new TutorialInput(TutorialInputKind.Unit, 0)) && movement.Input(new TutorialInput(TutorialInputKind.Action, 0, 16)), "Arm movement");
        Check(!movement.Input(new TutorialInput(TutorialInputKind.Tile, x: 5, y: 7)), "Wrong lesson destination rejected");
        Check(movement.Input(new TutorialInput(TutorialInputKind.Tile, x: 4, y: 7)), "Correct move request acknowledged");
        Check(movement.Snapshot().Wait == "DO2" && movement.Snapshot().Actors[0].Moving, "WAITMOVEPOS distinct from movement end");
        movement.Advance(10000); Check(movement.Snapshot().Wait == "DO2", "Lag does not teleport actor across movement phases");
        for (int i = 0; i < 20; i++) movement.Advance(80);
        Check(movement.Snapshot().Wait == "WAITMOUSEBUTTON" && movement.Snapshot().Actors[0].Tile == new TutorialTile(4, 7), "Arrival releases DO2");
        Check(movement.Snapshot().Actors[0].Direction == 2, "Final move direction right");
        var death = Session(spawn + "HPSET 400 0\nKILLCHAR2 400\nWAITMOUSEBUTTON\nENDGAME\nEND");
        Check(death.Snapshot().Actors.Any(a => a.Id == 400 && a.Dying), "KILLCHAR2 retains corpse initially");
        for (int i = 0; i < 18; i++) death.Advance(20); Check(death.Snapshot().Actors.Any(a => a.Id == 400), "Death counter18 retained");
        death.Advance(20); Check(!death.Snapshot().Actors.Any(a => a.Id == 400), "Death counter19 removed");
        var magic = Session("CHARPOS COM0 0 4 7\nCHARABILITY 0\nCHARPOS 301 410 6 7\nCHARABILITY 410\nENABLE MOUSEMENU\nWAITTOPMENU 0\nWAITMAGICMENU 0 0\nWAITMAGICMENU 1 0\nWAITMAGICMENU 2 0 410\nKILLCHAR2 410\nDO 1000\nWAITMOUSEBUTTON\nENDGAME\nEND");
        Check(magic.Input(new TutorialInput(TutorialInputKind.Unit, 0)) && magic.Input(new TutorialInput(TutorialInputKind.Action, 0, 4)), "Open actual magic submenu");
        Check(!magic.Input(new TutorialInput(TutorialInputKind.Magic, 0, 301)), "Unknown spell cannot satisfy fireball step");
        Check(magic.Input(new TutorialInput(TutorialInputKind.Magic, 0, 101)) && magic.Snapshot().Range == TutorialAction.Magic, "Spell selection arms yellow range");
        Check(magic.Input(new TutorialInput(TutorialInputKind.Unit, 410)), "Click actual original target");
        Check(magic.Snapshot().Actors.First(a => a.Id == 410).Hp == 130, "KILLCHAR2 does not alias SETHP no-op");
        for (int i = 0; i < 29; i++) magic.Advance(20);
        Check(magic.Snapshot().Actors.First(a => a.Id == 410).DeathCounter == 0 && magic.Snapshot().MagicAge == 580, "Magic holds death counter");
        magic.Advance(20);
        for (int i = 0; i < 18; i++) magic.Advance(20);
        Check(magic.Snapshot().Actors.Any(a => a.Id == 410), "Corpse receives independent18 death steps after magic");
        magic.Advance(20); Check(!magic.Snapshot().Actors.Any(a => a.Id == 410), "Magic corpse removed after19 steps");
        var remove = Session(spawn + "KILLCHAR1 400\nWAITMOUSEBUTTON\nENDGAME\nEND"); Check(remove.Snapshot().Actors.Length == 1, "KILLCHAR1 immediate");
        var merc = Session("CHARPOS 201 1 1 5\nCHARMENU 1 999 255\nWAITMOUSEBUTTON\nENDGAME\nEND").Snapshot().Actors[0];
        Check(TutorialSession.ActionAllowed(merc, TutorialAction.Move) && TutorialSession.ActionAllowed(merc, TutorialAction.Attack) && TutorialSession.ActionAllowed(merc, TutorialAction.Rest), "Mercenary three allowed actions");
        Check(!TutorialSession.ActionAllowed(merc, TutorialAction.Special) && !TutorialSession.ActionAllowed(merc, TutorialAction.Magic) && !TutorialSession.ActionAllowed(merc, TutorialAction.Order), "Other mercenary actions remain grey");
        Check(merc.Hp == 99 && merc.MaxHp == 99 && merc.Magic == 0 && merc.Skill == 0, "CHARPOS constructor defaults without implicit CHARABILITY");
        Check(n.Attack == 99 && n.Defense == 99, "QUNIT defaults copied into character combat fields; CHARABILITY leaves them intact");
        var rawMask = Session(spawn + "CHARMENU 0 999 0\nWAITMOUSEBUTTON\nENDGAME\nEND").Snapshot().Actors[0];
        Check(rawMask.Mask == 0 && !TutorialSession.ActionAllowed(rawMask, TutorialAction.Move), "Zero mask fixed grey and rejects interaction");
        var beforeEnd = Session("ENDGAME\nWAITMOUSEBUTTON\nEND"); Check(beforeEnd.HasEndedGame && !beforeEnd.Completed, "ENDGAME alone not completion");
        Check(beforeEnd.Input(Continue) && beforeEnd.Completed, "END commits script termination");
        foreach (int job in new[] { 100, 101, 103 })
        {
            var session = new TutorialSession(script, map, job) { MagicDurationMs = 600 };
            int initialMp = session.Snapshot().Actors[0].Mp;
            Run(session.Snapshot, session.Input, session.Advance);
            Check(session.Snapshot().Actors.First(a => a.Id == 0).Mp == initialMp, "SETMP no-op does not invent spell cost " + job);
        }
    }
    private static void DirectFeedbackContracts()
    {
        var origin = new TutorialTile(4, 7);
        int attackCells = 0, magicCells = 0;
        for (int y = -4; y <= 4; y++) for (int x = -4; x <= 4; x++)
        {
            var cell = new TutorialTile(origin.X + x, origin.Y + y);
            if (TutorialGeometry.InRange(origin, cell, 1)) attackCells++;
            if (TutorialGeometry.InRange(origin, cell, 3)) magicCells++;
        }
        Check(attackCells == 4 && magicCells == 24, "Native cardinal/diamond areas exclude own tile");
        Check(!TutorialGeometry.InRange(origin, new TutorialTile(5, 8), 1), "Attack1 excludes diagonal neighbor");
        Check(!TutorialGeometry.InRange(origin, new TutorialTile(6, 9), 3), "Magic3 excludes square corner at Manhattan4");
        var destinations = new[] { new TutorialTile(4, 6), new TutorialTile(4, 8), new TutorialTile(5, 7), new TutorialTile(3, 7) };
        for (int row = 0; row < destinations.Length; row++)
        {
            var destination = destinations[row];
            var moving = Session("CHARPOS COM0 0 4 7\nCHARABILITY 0\nCHARMOVE 0 " + destination.X + " " + destination.Y + "\nWAITMOVEEND 0\nWAITMOUSEBUTTON\nENDGAME\nEND");
            for (int i = 0; i < 4; i++) moving.Advance(80);
            var actor = moving.Snapshot().Actors[0];
            Check(actor.Tile == destination && actor.Direction == row, "Original sheet cardinal row " + row);
        }
        string setup = "CHARPOS COM0 0 4 7\nCHARABILITY 0\nCHARPOS 301 400 5 8\nCHARABILITY 400\nENABLE MOUSEMENU\nWAITTOPMENU 0\nWAITATTACKMENU 0\nWAITATTACKCHAR 0 400\nWAITMOUSEBUTTON\nENDGAME\nEND";
        var diagonalAttack = Session(setup);
        diagonalAttack.Input(new TutorialInput(TutorialInputKind.Unit, 0)); diagonalAttack.Input(new TutorialInput(TutorialInputKind.Action, 0, 1));
        Check(!diagonalAttack.Input(new TutorialInput(TutorialInputKind.Unit, 400)) && diagonalAttack.Snapshot().Wait == "WAITATTACKCHAR", "Server enforces same diamond as display");
        var cornerMagic = Session("CHARPOS COM0 0 4 7\nCHARABILITY 0\nCHARPOS 301 410 6 9\nCHARABILITY 410\nENABLE MOUSEMENU\nWAITTOPMENU 0\nWAITMAGICMENU 0 0\nWAITMAGICMENU 1 0\nWAITMAGICMENU 2 0 410\nWAITMOUSEBUTTON\nENDGAME\nEND");
        cornerMagic.Input(new TutorialInput(TutorialInputKind.Unit, 0)); cornerMagic.Input(new TutorialInput(TutorialInputKind.Action, 0, 4)); cornerMagic.Input(new TutorialInput(TutorialInputKind.Magic, 0, 101));
        Check(!cornerMagic.Input(new TutorialInput(TutorialInputKind.Unit, 410)), "Server rejects magic square-corner target");
        var menu = Session("CHARPOS COM0 0 4 7\nCHARABILITY 0\nENABLE MOUSEMENU\nWAITTOPMENU 0\nWAITMOVEMENU 0\nWAITMOVEPOS 0 2 7\nWAITMOUSEBUTTON\nENDGAME\nEND");
        menu.Input(new TutorialInput(TutorialInputKind.Unit, 0)); menu.Input(new TutorialInput(TutorialInputKind.Action, 0, 16));
        Check(menu.Snapshot().Range == TutorialAction.Move, "Move action arms native range");
        Check(menu.Input(new TutorialInput(TutorialInputKind.Unit, 0)) && menu.Snapshot().Range == null && menu.Snapshot().MenuUnit == 0, "Reopening unit menu clears movement range");
        Check(!menu.CanMove(0, origin), "Own tile is not a movement destination");
        menu.Input(new TutorialInput(TutorialInputKind.Action, 0, 16));
        Check(menu.Input(new TutorialInput(TutorialInputKind.Tile, x: 2, y: 7)), "Can continue lesson after reopening menu");
        var lethal = Session(setup.Replace("400 5 8", "400 5 7").Replace("WAITATTACKCHAR 0 400\nWAITMOUSEBUTTON", "WAITATTACKCHAR 0 400\nHPSET 400 0\nKILLCHAR2 400\nDO 1000\nWAITMOUSEBUTTON"));
        lethal.Input(new TutorialInput(TutorialInputKind.Unit, 0)); lethal.Input(new TutorialInput(TutorialInputKind.Action, 0, 1)); lethal.Input(new TutorialInput(TutorialInputKind.Unit, 400));
        Check(lethal.Snapshot().Actors.Count(a => a.ShowsAttackEffect) == 1, "One attack burst, no duplicated target burst");
        var contact = lethal.Snapshot().Actors.First(a => a.Id == 0);
        Check(contact.AttackFxX == 4.5 && contact.AttackFxY == 7, "Native half-cell contact captured");
        for (int i = 0; i < 19; i++) lethal.Advance(20);
        var after = lethal.Snapshot().Actors.First(a => a.Id == 0);
        Check(!lethal.Snapshot().Actors.Any(a => a.Id == 400) && after.ShowsAttackEffect, "Target removed while original attack counter remains active");
        Check(after.AttackFxX == contact.AttackFxX && after.AttackFxY == contact.AttackFxY, "Target removal cannot move remaining burst onto attacker");
        Check(after.MaximumTick == 10 && after.Attack == 99 && after.Defense == 99 && after.Level == 0, "Copied native temporary record values, no server-stat fiction");
        Check(after.JobName == "파이터", "F2 job label comes from native catalog");
        var blank = Session("MESSAGE 1 125 0 0 \nWAITMOUSEBUTTON\nENDGAME\nEND");
        Check(string.IsNullOrWhiteSpace(blank.Snapshot().Message), "Blank source MESSAGE leaves no visible dialogue text");
        Check(script.Commands.Count(c => c.Name == "CHARMENU" && c.Int(2) == 0) == 9, "Original grey explanation masks retained as requested");
    }
    private static void FinishWaiting(EntryAccountService server)
    {
        Require(server.BeginWaitingLesson(WaitingLessonChecks.Definition()) == TutorialFailure.None, "Start mandatory waiting lesson");
        WaitingLessonChecks.Run(server.WaitingLessonSnapshot,
            input => server.WaitingLessonInput(input) == TutorialFailure.None, server.AdvanceTutorial);
    }
    private static void ServerContracts()
    {
        var memory = new Store(); var authority = new EntryAccountService(memory);
        Check(authority.BeginTutorial(script, map, 600).Failure == TutorialFailure.Unauthenticated, "Unauthenticated tutorial rejected");
        Check(authority.ProvisionLocalAccount("tutorial-check", "test-password"), "Provision test identity");
        authority.Request(EntryRequest.Login("tutorial-check", "test-password"));
        Check(authority.BeginTutorial(script, map, 600).Failure == TutorialFailure.InvalidStage, "No selected server");
        authority.Request(EntryRequest.SelectServer(1)); Check(authority.BeginTutorial(script, map, 600).Failure == TutorialFailure.InvalidStage, "No character");
        authority.Request(EntryRequest.Create(100, "검사자"));
        Check(authority.CompleteTutorial().Failure == TutorialFailure.InvalidStage, "Cannot complete before starting");
        Check(authority.BeginTutorial(script, map, 600).Failure == TutorialFailure.InvalidStage, "Cannot bypass earlier lesson");
        Check(authority.BeginWaitingLesson(WaitingLessonChecks.Definition()) == TutorialFailure.None, "Begin earlier lesson");
        Check(authority.BeginWaitingLesson(WaitingLessonChecks.Definition()) == TutorialFailure.InvalidStage, "Cannot reset earlier lesson through repeated begin");
        authority.AdvanceTutorial(1001); authority.WaitingLessonInput(new WaitingInput(WaitingInputKind.Continue));
        authority.Request(EntryRequest.Logout());
        Check(authority.WaitingLessonSnapshot() == null, "Logout discards partial earlier lesson");
        authority.Request(EntryRequest.Login("tutorial-check", "test-password")); authority.Request(EntryRequest.SelectServer(1));
        Check(authority.BeginTutorial(script, map, 600).Failure == TutorialFailure.InvalidStage, "Interrupted earlier lesson cannot be bypassed after login");
        FinishWaiting(authority);
        Check(authority.CompleteTutorial().Failure == TutorialFailure.InvalidStage && !memory.Data.accounts[0].character.tutorialCompleted, "Earlier lesson alone does not persist tutorial completion");
        Check(authority.BeginTutorial(script, map, 600).Accepted, "Start from server-owned character after earlier lesson");
        Check(authority.BeginTutorial(script, map, 600).Failure == TutorialFailure.InvalidStage, "No duplicate begin reset");
        authority.TutorialInput(Continue);
        Check(authority.CompleteTutorial().Failure == TutorialFailure.InvalidStage, "Cannot skip script by completion request");
        int interruptedCursor = authority.TutorialSnapshot().Cursor;
        authority.Request(EntryRequest.Logout()); Check(authority.TutorialSnapshot() == null, "Logout discards transient tutorial");
        authority.Request(EntryRequest.Login("tutorial-check", "test-password")); authority.Request(EntryRequest.SelectServer(1)); FinishWaiting(authority); authority.BeginTutorial(script, map, 600);
        Check(authority.TutorialSnapshot().Cursor < interruptedCursor && authority.TutorialSnapshot().MessageCount == 1, "Interrupted reconnect starts at first message");
        Run(authority.TutorialSnapshot, input => authority.TutorialInput(input).Accepted, authority.AdvanceTutorial);
        Check(!memory.Data.accounts[0].character.tutorialCompleted, "END alone does not fake persistence");
        memory.Fail = true; Check(authority.CompleteTutorial().Failure == TutorialFailure.StorageUnavailable && !memory.Data.accounts[0].character.tutorialCompleted, "Failed write preserves incomplete saved identity");
        memory.Fail = false;
        var freshProvision = new EntryAccountService(memory); Check(freshProvision.ProvisionLocalAccount("parallel-account", "another-password"), "External account before completion");
        var completed = authority.CompleteTutorial(); Check(completed.Accepted && completed.Character.tutorialCompleted && memory.Data.accounts.Length == 2, "Atomic completion keeps fresh accounts");
        int writes = memory.Writes; Check(authority.CompleteTutorial().Accepted && memory.Writes == writes, "Idempotent completion does not write twice");
        Check(authority.TutorialInput(Continue).Failure == TutorialFailure.InvalidStage, "Finished session rejects inputs");
        var reconnect = new EntryClient(new EntryAccountService(memory)); Check(reconnect.Login("tutorial-check", "test-password") && reconnect.SelectServer(1) && reconnect.Character.tutorialCompleted, "Completed reconnect boundary");
        var other = new EntryAccountService(memory); other.Request(EntryRequest.Login("parallel-account", "another-password")); other.Request(EntryRequest.SelectServer(1)); other.Request(EntryRequest.Create(103, "다른계정"));
        FinishWaiting(other);
        Check(other.BeginTutorial(script, map, 600).Accepted && other.TutorialSnapshot().Actors[0].Job == 103, "Other identity gets independent fresh tutorial");
        authority.Request(EntryRequest.Login("tutorial-check", "test-password")); authority.Request(EntryRequest.SelectServer(1));
        Check(authority.BeginTutorial(script, map, 600).Failure == TutorialFailure.AlreadyCompleted, "Completed character not replayed");

        string directory = Path.Combine(Path.GetTempPath(), "ds-original-tutorial-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            string path = Path.Combine(directory, "accounts-v1.json"), legacy = Path.Combine(directory, "old-save.json"); File.WriteAllText(legacy, "preserved");
            var file = new LocalAccountFileStore(path); var disk = new EntryAccountService(file); disk.ProvisionLocalAccount("disk", "disk-password");
            disk.Request(EntryRequest.Login("disk", "disk-password")); disk.Request(EntryRequest.SelectServer(1)); disk.Request(EntryRequest.Create(101, "디스크"));
            // A pre-tutorial account file without the new optional field remains readable and incomplete.
            string json = File.ReadAllText(path); File.WriteAllText(path, json.Replace(",\"tutorialCompleted\":false", ""));
            Check(file.TryRead(out var old) && !old.accounts[0].character.tutorialCompleted, "Missing optional completion field compatible");
            var restart = new EntryAccountService(file); restart.Request(EntryRequest.Login("disk", "disk-password")); restart.Request(EntryRequest.SelectServer(1)); FinishWaiting(restart); restart.BeginTutorial(script, map, 600);
            Run(restart.TutorialSnapshot, input => restart.TutorialInput(input).Accepted, restart.AdvanceTutorial);
            Check(restart.CompleteTutorial().Accepted, "Real disk completion");
            var again = new EntryClient(new EntryAccountService(file)); Check(again.Login("disk", "disk-password") && again.SelectServer(1) && again.Character.tutorialCompleted, "Completion survives actual disk re-open");
            Check(File.ReadAllText(legacy) == "preserved", "Legacy save untouched");
            Check(!File.ReadAllText(path).Contains("HPSET") && !File.ReadAllText(path).Contains("cursor") && !File.ReadAllText(path).Contains("30000"), "No old gameplay grants/cursor persisted");
        }
        finally { Directory.Delete(directory, true); }
    }
    private static void DirectProfileTutorialContracts()
    {
        var store = new Store(); var server = new EntryAccountService(store); var client = new EntryClient(server);
        Check(client.OpenLocalProfile() && client.Create(100, "직접시작"), "Direct profile creates before tutorial without login/server requests");
        string id = client.Character.id;
        Check(server.BeginTutorial(script, map, 600).Failure == TutorialFailure.InvalidStage, "Direct entry still requires earlier lesson");
        Check(server.BeginWaitingLesson(WaitingLessonChecks.Definition()) == TutorialFailure.None, "Direct profile owns earlier lesson");
        server.AdvanceTutorial(1001); server.WaitingLessonInput(new WaitingInput(WaitingInputKind.Continue));
        client.Logout();
        Check(client.OpenLocalProfile() && client.Character.id == id && !client.Character.tutorialCompleted, "Direct interrupted restart preserves identity and incomplete flag");
        Check(server.BeginWaitingLesson(WaitingLessonChecks.Definition()) == TutorialFailure.None, "Direct restart begins fresh waiting lesson");
        server.AdvanceTutorial(1001);
        Check(server.WaitingLessonSnapshot().ResourceId == 61216, "Direct reentry starts from first waiting message");
        WaitingLessonChecks.Run(server.WaitingLessonSnapshot, input => server.WaitingLessonInput(input) == TutorialFailure.None, server.AdvanceTutorial);
        Check(server.BeginTutorial(script, map, 600).Accepted, "Direct profile proceeds to battle after actual earlier lesson");
        Run(server.TutorialSnapshot, input => server.TutorialInput(input).Accepted, server.AdvanceTutorial);
        Check(server.CompleteTutorial().Accepted && store.Data.accounts[0].character.tutorialCompleted, "Direct profile saves full tutorial completion");
        var restored = new EntryClient(new EntryAccountService(store));
        Check(restored.OpenLocalProfile() && restored.Character.id == id && restored.Character.tutorialCompleted, "Direct profile restores completed tutorial without login");
    }
    private static void Main()
    {
        using (var json = JsonDocument.Parse(File.ReadAllText("Assets/_Original/Data/Tutorial/Tutorial301.json")))
            map = JsonSerializer.Deserialize<TutorialMap>(json.RootElement.GetProperty("map").GetRawText(), new JsonSerializerOptions { IncludeFields = true });
        script = new TutorialScript(File.ReadAllText("Assets/_Original/SourceData/Decoded/gameres/tutorial.arc/tutorial.deo.txt"));
        NativeContracts(); DirectFeedbackContracts(); count += WaitingLessonChecks.Contracts(); ServerContracts(); DirectProfileTutorialContracts();
        Console.WriteLine(JsonSerializer.Serialize(new { assertions = count, fullTutorialRuns = fullRuns, simulatedFrames, acceptedInputs, sourceCommands = 420, sourceSpellings = 44, sourceMessages = sourceMessageCount, observedWaitLocations = sourceWaitCount, testedJobs = new[] { 100, 101, 103 }, actualUnityPlay = false }));
    }
}
