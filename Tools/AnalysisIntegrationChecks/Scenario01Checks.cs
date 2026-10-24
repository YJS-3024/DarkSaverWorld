using DarkSaver.Core;
using DarkSaver.Events;

internal static class Scenario01Checks
{
    public static void Run(Action<bool, string> check)
    {
        check(Scenario01Rules.ScenarioPortraitId("COM0", 103, "영웅") == 103, "COM0 portrait resolves current job");
        check(Scenario01Rules.ScenarioPortraitId("ME", 103, "영웅") == 0, "ME name alias uses native atoi fallback");
        check(Scenario01Rules.ScenarioPortraitId("ID", 103, " 301영웅") == 301, "numeric prefix name resolves native face");
        check(Scenario01Rules.ScenarioPortraitId("802", 103, "영웅") == 802, "literal NPC portrait preserved");
        check(Scenario01Rules.ScenarioPortraitId("COM1", 103, "영웅") == 0, "unbound party alias uses fallback");
        var coordinates = EventScriptParser.Parse("CHANGESPRITE 5 15 0 1 0 22\nCHANGETILE2 5 15 0 1 0 22 0");
        check(coordinates[0].ReadCoordinates(0) == (5, 15), "native chest origin stays source XY");
        check(coordinates[1].ReadCoordinates(0) == (5, 15), "native tile origin stays source XY");
        var tutorial = new NativeTutorialProgress();
        check(tutorial.IsValid && !tutorial.IsComplete, "new tutorial starts pending");
        check(!tutorial.TryAdvance(1, true, false, out _), "stale tutorial step rejected");
        check(tutorial.TryAdvance(0, false, false, out var movement), "welcome advances to practice");
        check(tutorial.step == 0 && movement.step == 1, "tutorial candidate does not mutate live state");
        check(!movement.TryAdvance(1, false, false, out _), "practice requires real movement");
        check(movement.TryAdvance(1, true, false, out var townGuide) && townGuide.step == 2, "movement unlocks town guide");
        check(townGuide.TryAdvance(2, false, false, out var scenarioGuide) && scenarioGuide.step == 3, "town guide precedes scenario guide");
        check(scenarioGuide.TryAdvance(3, false, false, out var finished) && finished.IsComplete, "tutorial completes after scenario guide");
        check(!finished.TryAdvance(4, true, false, out _), "completed tutorial cannot replay itself");
        check(movement.TryAdvance(1, false, true, out var skipped) && skipped.IsComplete, "explicit skip does not require practice");
        check(!new NativeTutorialProgress { step = -1 }.IsValid && !new NativeTutorialProgress { version = 2 }.IsValid, "invalid tutorial state rejected");
        check(NativeCharacterSlots.PathFor(0) == NativeSaveStore.DefaultPath, "slot one preserves legacy save path");
        check(Enumerable.Range(0,3).Select(NativeCharacterSlots.PathFor).Distinct().Count() == 3, "character slot paths are isolated");
        var definition = UnityEngine.JsonUtility.FromJson<Scenario01Definition>(File.ReadAllText("Assets/_Game/Resources/Scenarios/01/Content.json"));
        check(definition.IsValid, "01 baked source definition validates");
        check(definition.intro.Length == 72 && definition.outro.Length == 41 && definition.events.Length == 24, "complete 01 scripts baked");
        var state = Scenario01Progress.Create(definition); state.rng = 73;
        var inventory = new NativeInventory();
        var temp = Path.Combine(Path.GetTempPath(), "ds-scenario01-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        var store = new NativeSaveStore(Path.Combine(temp, "scenario.json"));
        NativeSaveData Snapshot() => new() { format = NativeSaveStore.Format, version = NativeSaveStore.ScenarioVersion,
            mapScene = "Brugos01_Map", runtimeScene = "Brugos01_Runtime", hasScenario01 = true, scenario01 = state.Clone(),
            hasInventory = true, inventory = inventory.Clone(), hasCharacter = true,
            character = new NativeCharacterProfile { name = "검증", jobId = 100 },
            clearedScenarios = state.phase == Scenario01Progress.Complete ? new[] { "01" } : Array.Empty<string>() };
        void Checkpoint()
        {
            check(state.Matches(definition), "01 checkpoint matches content");
            check(store.TrySave(Snapshot(), out _), "01 checkpoint atomically written");
            check(store.TryLoad(out var loaded, out _), "01 checkpoint loaded");
            state = loaded.scenario01; inventory = loaded.inventory;
        }
        void RunScript()
        {
            int limit = 0;
            while ((state.phase == Scenario01Progress.Intro || state.phase == Scenario01Progress.Outro || state.activeEvent != 0) && limit++ < 1000)
            {
                var result = Scenario01Rules.Step(definition, state, inventory, 100, out var next, out var items);
                if (result == Scenario01Rules.StepResult.Waiting)
                {
                    if (Scenario01Rules.AdvanceDialogue(definition, state, out var page)) state = page;
                    else Scenario01Rules.AdvanceTime(state, 1, 6);
                }
                else { state = next; inventory = items; }
                Checkpoint(); // Reload every step, including waits, pages and reward boundaries.
            }
            check(limit < 1000, "01 script terminates after checkpoint reloads");
        }
        bool Activate(int x, int y)
        {
            bool changed = Scenario01Rules.TryActivate(definition, state, inventory, x, y, out var next, out var items);
            if (changed) { state = next; inventory = items; Checkpoint(); }
            return changed;
        }
        try
        {
            Checkpoint(); RunScript();
            check(state.phase == Scenario01Progress.Playing && state.defeated.Count == 0, "intro has no gameplay death rewards");
            check(inventory.gold == 0 && inventory.items.Length == 0, "intro does not create loot");
            check(Activate(6, 15) && state.activeEvent == 1, "source chest 01 condition enters");
            long chestGold = inventory.gold; uint chestRng = state.rng;
            RunScript();
            check(inventory.gold == chestGold && state.changedEvents.Contains(1), "event presentation does not repeat economic reward");
            Activate(6, 15);
            check(state.activeEvent == 0 && state.rng == chestRng && inventory.gold == chestGold, "opened chest cannot reroll after load");
            check(Activate(6, 21) && state.activeEvent == 51, "source prisoner event 51 enters");
            check(inventory.gold == chestGold + 399 && inventory.Count(399) == 0, "GOLD399 is not silently converted into key399");
            RunScript();
            Activate(14, 5);
            check(state.activeEvent == 0 && inventory.Count(303) == 0, "rescue without key cannot consume or grant");
            // Fixture seed chooses a failed chance, followed by a successful reentry.
            uint Seed(bool success)
            {
                for (uint i = 0; ; i++) { var probe = state.Clone(); probe.rng = i; if ((Scenario01Rules.Roll(probe) < 70) == success) return i; }
            }
            state.rng = Seed(false); Activate(30, 22);
            check(state.activeEvent == 0 && state.insideEvents.Contains(53), "failed random entry recorded");
            uint failedRng = state.rng;
            check(!Activate(30, 22) && state.rng == failedRng, "standing on random trigger does not reroll each frame");
            Activate(0, 0); state.rng = Seed(true); Activate(30, 22); RunScript();
            check(inventory.Count(399) == 1, "successful reentry grants source key once");
            Activate(14, 5);
            check(inventory.Count(399) == 0 && inventory.Count(303) == 1, "consume key and grant herb in one candidate");
            RunScript();
            check(state.removedActors.Contains(800), "rescued source NPC stays removed after restore");
            Activate(14, 5);
            check(inventory.Count(303) == 1, "rescue cannot grant twice");
            foreach (var unit in definition.units.Where(u => u.wave < 2))
            {
                state.pendingBattle = unit.id;
                check(Scenario01Rules.RecordBattleReturn(definition, state, unit.id, true), "record defeated unit " + unit.id);
                Checkpoint();
            }
            check(state.wave == 2, "all required earlier cohorts retained and advanced");
            var survivor = definition.units.First(u => u.id == 700);
            state.pendingBattle = 700;
            var wounds = new Scenario01EnemyState { id = 700, hp = survivor.hp - 1, mp = survivor.mp, x = survivor.x, y = survivor.y };
            check(Scenario01Rules.RecordBattleReturn(definition, state, 700, false, wounds), "retreat preserves surviving enemy snapshot");
            Checkpoint();
            check(state.enemies.Single(e => e.id == 700).hp == survivor.hp - 1, "enemy wounds survive a disk reload");
            // World runtime processes each death event on return. The fixture
            // recorded several returns in a batch, so drain their source-order dialogue.
            for (int i = 0; i < 24; i++)
            {
                Activate(19, 1);
                if (state.activeEvent == 0) break;
                RunScript();
            }
            state.pendingBattle = 700;
            check(Scenario01Rules.RecordBattleReturn(definition, state, 700, false) && !state.defeated.Contains(700), "retreat cannot become boss victory");
            state.pendingBattle = 700;
            check(Scenario01Rules.RecordBattleReturn(definition, state, 700, true), "actual boss death recorded");
            Checkpoint(); Activate(19, 1);
            check(state.activeEvent == 99 && state.phase == Scenario01Progress.Playing, "boss event precedes completion");
            RunScript();
            check(state.phase == Scenario01Progress.Complete && inventory.Count(398) == 1, "DEE finishes and bomb is granted exactly once");
            check(state.actors.Any(a => a.id == 801) && !state.actors.Any(a => a.id == 301), "ending NPCs persist without a duplicate player stand-in");
            check(store.TryLoad(out var completed, out _) && completed.clearedScenarios.SequenceEqual(new[] { "01" }), "completed checkpoint carries clear flag");
            check(!Activate(19, 1) && inventory.Count(398) == 1, "completed scenario cannot retrigger reward");
            var withTutorial = Snapshot(); withTutorial.character.tutorial = new NativeTutorialProgress { step = 2 };
            check(store.TrySave(withTutorial, out _) && store.TryLoad(out var tutorialSave, out _) && tutorialSave.character.tutorial.step == 2, "tutorial checkpoint round-trips on disk");
            withTutorial.character.tutorial.step = 5;
            check(!store.TrySave(withTutorial, out _), "invalid tutorial cannot overwrite a valid checkpoint");
            check(NativeCharacterSlots.IsOccupied(store.FilePath), "existing slot cannot be recreated");
            check(NativeCharacterSlots.IsOccupied(store.FilePath + ".missing") == false, "missing slot remains available");
            var backupOnly = Path.Combine(temp, "backup-only.json"); File.WriteAllText(backupOnly + ".bak", "fixture");
            check(NativeCharacterSlots.IsOccupied(backupOnly), "backup-only slot protected from overwrite");
            var bad = state.Clone(); bad.sourceHash = new string('0', 64);
            check(!bad.Matches(definition), "content hash mismatch rejects resume");
            bad = state.Clone(); bad.cursor = 999;
            check(!bad.Matches(definition), "out-of-range scene cursor rejects resume");
            var invalid = Snapshot(); invalid.version = 2;
            check(!NativeSaveStore.IsValid(invalid), "scenario subdocument cannot be written as legacy save");
            invalid.hasScenario01 = false; invalid.scenario01 = null; invalid.version = 1;
            check(NativeSaveStore.IsValid(invalid) && !store.TrySave(invalid, out _), "legacy writer cannot erase scenario v3 checkpoint");
        }
        finally { Directory.Delete(temp, true); }
    }
}
