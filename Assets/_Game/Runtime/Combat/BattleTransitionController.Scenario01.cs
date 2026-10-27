using System;
using System.Collections.Generic;
using DarkSaver.Core;
using DarkSaver.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Combat
{
    public sealed partial class BattleTransitionController
    {
        private Scenario01Progress scenario01;
        public bool CanRetryScenarioSave { get; private set; }
        public string ScenarioSaveDiagnostic { get; private set; } = string.Empty;
        public int Scenario01Revision { get; private set; }
        public Scenario01Progress CaptureScenario01() => scenario01?.Clone();

        // Disk and live economy/progress are committed together. Visual effects
        // are applied only after this returns true, and retries cannot regrant.
        public bool TryCommitScenario01(int expectedRevision, Scenario01Progress progress, NativeInventory items,
            out string reason)
{reason=DarkSaver.Core.OriginalRestorationPolicy.PendingServerRule;return false;}    }
}
