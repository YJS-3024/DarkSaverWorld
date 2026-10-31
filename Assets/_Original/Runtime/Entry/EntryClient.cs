using System;

namespace DarkSaver.Original.Entry
{
    // UI advances only from accepted replies. No local character/economic defaults.
    public sealed class EntryClient
    {
        private readonly IEntryTransport transport;
        public EntryStage Stage { get; private set; }
        public EntryFailure Failure { get; private set; }
        public EntryServer[] Servers { get; private set; } = Array.Empty<EntryServer>();
        private EntryCharacter character;
        public EntryCharacter Character => character?.Copy();
        public event Action<EntryCharacter> CharacterReady;
        public EntryClient(IEntryTransport transport) { this.transport = transport ?? throw new ArgumentNullException(nameof(transport)); }
        private bool Accepted(EntryReply reply, EntryOperation expected)
        {
            if (reply == null || reply.Operation != expected) { Failure = EntryFailure.StorageUnavailable; return false; }
            Failure = reply.Failure;
            if (Failure == EntryFailure.Unauthenticated)
            { Stage = EntryStage.Login; Servers = Array.Empty<EntryServer>(); character = null; }
            return reply.Accepted;
        }
        public bool Login(string id, string password)
        {
            Stage = EntryStage.Login; character = null; Servers = Array.Empty<EntryServer>();
            if (!Accepted(transport.Request(EntryRequest.Login(id, password)), EntryOperation.Login)) return false;
            var reply = transport.Request(EntryRequest.Servers());
            if (!Accepted(reply, EntryOperation.Servers)) return false;
            if (reply.Servers.Length == 0) { Failure = EntryFailure.StorageUnavailable; return false; }
            Servers = reply.Servers; Stage = EntryStage.ServerSelection; return true;
        }
        public bool SelectServer(int id)
        {
            if (Stage != EntryStage.ServerSelection) return false;
            var reply = transport.Request(EntryRequest.SelectServer(id));
            if (!Accepted(reply, EntryOperation.SelectServer)) return false;
            character = reply.Character;
            Stage = character == null ? EntryStage.FirstCharacterCreation : EntryStage.CharacterReady;
            if (character != null) CharacterReady?.Invoke(character.Copy());
            return true;
        }
        public bool OpenLocalProfile()
        {
            Stage = EntryStage.Login; character = null; Servers = Array.Empty<EntryServer>();
            var reply = transport.Request(EntryRequest.OpenLocalProfile());
            if (!Accepted(reply, EntryOperation.OpenLocalProfile)) return false;
            character = reply.Character;
            Stage = character == null ? EntryStage.FirstCharacterCreation : EntryStage.CharacterReady;
            if (character != null) CharacterReady?.Invoke(character.Copy());
            return true;
        }
        public bool Create(int job, string name)
        {
            if (Stage != EntryStage.FirstCharacterCreation) return false;
            var reply = transport.Request(EntryRequest.Create(job, name));
            if (!Accepted(reply, EntryOperation.CreateFirstCharacter)) return false;
            if (reply.Character == null) { Failure = EntryFailure.StorageUnavailable; return false; }
            character = reply.Character; Stage = EntryStage.CharacterReady; CharacterReady?.Invoke(character.Copy()); return true;
        }
        public void Logout()
        {
            transport.Request(EntryRequest.Logout()); Stage = EntryStage.Login; Failure = EntryFailure.None;
            Servers = Array.Empty<EntryServer>(); character = null;
        }
    }
}
