using System;
using System.Linq;
using DarkSaver.Original.Tutorial;

namespace DarkSaver.Original.Entry
{
    public enum TutorialFailure { None, Unauthenticated, InvalidStage, AlreadyCompleted, InvalidInput, StorageUnavailable }
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
        private TutorialSession tutorial;
        private string tutorialCharacterId;
        private bool tutorialCommitted;
        private void ResetTutorial() { tutorial = null; tutorialCharacterId = null; tutorialCommitted = false; }
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
        { if (tutorial != null && !tutorialCommitted && TutorialOwner(out _) == TutorialFailure.None) tutorial.Advance(milliseconds); }
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
