using System;
using UnityEngine;
using DarkSaver.Original.Tutorial;

namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        private NativeTutorialArt tutorialArt;
        public event Action<TutorialReply> Notify_Tutorial;
        public void ConfigureTutorial(NativeTutorialArt art) { tutorialArt = art; }
        public TutorialReply Request_BeginTutorial()
        {
            TutorialReply reply;
            if (!isActiveAndEnabled || service == null) reply = new TutorialReply(TutorialFailure.Unauthenticated);
            else if (tutorialArt == null || !tutorialArt.IsConfigured) reply = new TutorialReply(TutorialFailure.InvalidStage);
            else
            {
                var definition = tutorialArt.Definition;
                // Counter0..9 travel, then three pattern2 impact frames. No missing-pattern substitute.
                double duration = (10 + definition.magicImpactFrames) * TutorialPresentationTiming.MagicPhaseMs;
                reply = service.BeginTutorial(new TutorialScript(tutorialArt.script.text), definition.map, duration);
            }
            Notify_Tutorial?.Invoke(reply); return reply;
        }
        public TutorialReply Request_TutorialInput(TutorialInput input)
        {
            var reply = isActiveAndEnabled && service != null ? service.TutorialInput(input) : new TutorialReply(TutorialFailure.Unauthenticated);
            Notify_Tutorial?.Invoke(reply); return reply;
        }
        public TutorialReply Request_CompleteTutorial()
        {
            var reply = isActiveAndEnabled && service != null ? service.CompleteTutorial() : new TutorialReply(TutorialFailure.Unauthenticated);
            Notify_Tutorial?.Invoke(reply); return reply;
        }
        public TutorialView TutorialSnapshot => isActiveAndEnabled ? service?.TutorialSnapshot() : null;
        private void Update() { service?.AdvanceTutorial(Time.unscaledDeltaTime * 1000.0); }
    }
}
