using System;
using UnityEngine;
using DarkSaver.Original.Tutorial;

namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        private NativeTutorialArt tutorialArt;
        public event Action<TutorialReply> Notify_Tutorial;
        public event Action<WaitingReply> Notify_WaitingLesson;
        public void ConfigureTutorial(NativeTutorialArt art) { tutorialArt = art; }
        public WaitingReply Request_BeginWaitingLesson()
        {
            var failure = !isActiveAndEnabled || service == null ? TutorialFailure.Unauthenticated
                : tutorialArt == null || !tutorialArt.WaitingConfigured ? TutorialFailure.InvalidStage
                : service.BeginWaitingLesson(tutorialArt.WaitingDefinition);
            var reply = new WaitingReply(failure, service?.WaitingLessonSnapshot());
            Notify_WaitingLesson?.Invoke(reply); return reply;
        }
        public WaitingReply Request_WaitingLessonInput(WaitingInput input)
        {
            var failure = isActiveAndEnabled && service != null ? service.WaitingLessonInput(input) : TutorialFailure.Unauthenticated;
            var reply = new WaitingReply(failure, service?.WaitingLessonSnapshot());
            Notify_WaitingLesson?.Invoke(reply); return reply;
        }
        public WaitingView WaitingSnapshot => isActiveAndEnabled ? service?.WaitingLessonSnapshot() : null;
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
        private void Update() { service?.AdvanceTutorial(Time.unscaledDeltaTime * 1000.0); service?.AdvanceWorld(Time.unscaledDeltaTime * 1000.0); FlushCampaignEffects(); }
    }
}
