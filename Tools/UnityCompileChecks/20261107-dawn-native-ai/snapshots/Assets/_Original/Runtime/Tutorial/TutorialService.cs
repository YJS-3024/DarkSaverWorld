using System;
using System.Linq;
using DarkSaver.Original.Tutorial;

namespace DarkSaver.Original.Entry
{
    public enum TutorialFailure { None, Unauthenticated, InvalidStage, AlreadyCompleted, InvalidInput, StorageUnavailable }
    public sealed class WaitingReply
    {
        public TutorialFailure Failure { get; }
        public bool Accepted => Failure == TutorialFailure.None;
        public WaitingView View { get; }
        public WaitingReply(TutorialFailure failure, WaitingView view) { Failure = failure; View = view; }
    }
    public sealed class TutorialReply
    {
        public TutorialFailure Failure { get; }
        public bool Accepted => Failure == TutorialFailure.None;
        public TutorialView View { get; }
        private readonly EntryCharacter character;
        public EntryCharacter Character => character?.Copy();
        public TutorialReply(TutorialFailure failure, TutorialView view = null, EntryCharacter character = null)
        { Failure = failure; View = view; this.character = character?.Copy(); }
    }
    public sealed partial class EntryAccountService
    {
        private WaitingLesson waitingLesson;
        private TutorialSession tutorial;
        private string tutorialCharacterId;
        private bool tutorialCommitted;
        private void ResetTutorial() { ResetWorld(); waitingLesson = null; tutorial = null; tutorialCharacterId = null; tutorialCommitted = false; }
        public TutorialFailure BeginWaitingLesson(WaitingDefinition definition)
        {
            var failure = TutorialOwner(out var owner); if (failure != TutorialFailure.None) return failure;
            if (owner.character.tutorialCompleted) return TutorialFailure.AlreadyCompleted;
            if (waitingLesson != null || tutorial != null) return TutorialFailure.InvalidStage;
            try { waitingLesson = new WaitingLesson(definition, owner.character.job); }
            catch (ArgumentException) { return TutorialFailure.InvalidStage; }
            tutorialCharacterId = owner.character.id;
            return TutorialFailure.None;
        }
        public TutorialFailure WaitingLessonInput(WaitingInput input)
        {
            var failure = TutorialOwner(out var owner); if (failure != TutorialFailure.None) return failure;
            if (waitingLesson == null || tutorial != null || owner.character.id != tutorialCharacterId) return TutorialFailure.InvalidStage;
            return waitingLesson.Input(input) ? TutorialFailure.None : TutorialFailure.InvalidInput;
        }
        public WaitingView WaitingLessonSnapshot() => waitingLesson?.Snapshot();
        private TutorialFailure TutorialOwner(out LocalAccountRecord owner)
        {
            owner = null;
            if (accountId == null) return TutorialFailure.Unauthenticated;
            if (!available) return TutorialFailure.StorageUnavailable;
            if (selectedServer != LocalEntryPolicy.ServerId) return TutorialFailure.InvalidStage;
            owner = document.accounts.FirstOrDefault(a => a.id == accountId);
            return owner?.character != null ? TutorialFailure.None : TutorialFailure.InvalidStage;
        }
        public TutorialReply BeginTutorial(TutorialScript script, TutorialMap map, double magicDurationMs)
        {
            var failure = TutorialOwner(out var owner); if (failure != TutorialFailure.None) return new TutorialReply(failure);
            if (owner.character.tutorialCompleted) return new TutorialReply(TutorialFailure.AlreadyCompleted, character: owner.character);
            if (waitingLesson == null || !waitingLesson.Completed || owner.character.id != tutorialCharacterId)
                return new TutorialReply(TutorialFailure.InvalidStage);
            if (tutorial != null) return new TutorialReply(TutorialFailure.InvalidStage, tutorial.Snapshot());
            if (script == null || map == null || !map.IsValid || magicDurationMs < TutorialSession.MagicImpactMs) return new TutorialReply(TutorialFailure.InvalidStage);
            tutorial = new TutorialSession(script, map, owner.character.job) { MagicDurationMs = magicDurationMs };
            tutorialCharacterId = owner.character.id; tutorialCommitted = false;
            return new TutorialReply(TutorialFailure.None, tutorial.Snapshot(), owner.character);
        }
        public TutorialReply TutorialInput(TutorialInput input)
        {
            var failure = TutorialOwner(out var owner); if (failure != TutorialFailure.None) return new TutorialReply(failure);
            if (tutorial == null || owner.character.id != tutorialCharacterId || tutorialCommitted) return new TutorialReply(TutorialFailure.InvalidStage);
            bool accepted = tutorial.Input(input);
            return new TutorialReply(accepted ? TutorialFailure.None : TutorialFailure.InvalidInput, tutorial.Snapshot());
        }
        public TutorialView TutorialSnapshot() => tutorial?.Snapshot();
        public void AdvanceTutorial(double milliseconds)
        {
            if (TutorialOwner(out _) != TutorialFailure.None) return;
            if (tutorial != null && !tutorialCommitted) tutorial.Advance(milliseconds);
            else waitingLesson?.Advance(milliseconds);
        }
        public TutorialReply CompleteTutorial()
        {
            var failure = TutorialOwner(out var owner); if (failure != TutorialFailure.None) return new TutorialReply(failure);
            if (tutorial == null || !tutorial.Completed || !tutorial.HasEndedGame || owner.character.id != tutorialCharacterId)
                return new TutorialReply(TutorialFailure.InvalidStage, tutorial?.Snapshot());
            if (tutorialCommitted) return new TutorialReply(TutorialFailure.None, tutorial.Snapshot(), owner.character);
            // Persist only completion. Mid-lesson script cursor, HP and temporary actors are never a saved game.
            if (!store.TryRead(out var fresh) || !LocalEntryPolicy.IsDocument(fresh)) return new TutorialReply(TutorialFailure.StorageUnavailable, tutorial.Snapshot());
            var freshOwner = fresh.accounts.FirstOrDefault(a => a.id == accountId);
            if (freshOwner?.character?.id != tutorialCharacterId || freshOwner.passwordHash != owner.passwordHash || freshOwner.salt != owner.salt)
            { ResetTutorial(); return new TutorialReply(TutorialFailure.Unauthenticated); }
            var candidate = fresh.Copy(); candidate.accounts.First(a => a.id == accountId).character.tutorialCompleted = true;
            if (!store.TryWrite(candidate)) return new TutorialReply(TutorialFailure.StorageUnavailable, tutorial.Snapshot());
            document = candidate; tutorialCommitted = true;
            return new TutorialReply(TutorialFailure.None, tutorial.Snapshot(), candidate.accounts.First(a => a.id == accountId).character);
        }
    }
}
