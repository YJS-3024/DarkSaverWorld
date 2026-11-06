using System;
using System.Runtime.Serialization;

namespace DarkSaver.Original.Entry
{
    // In-process contracts. No original wire opcode or missing server formula is invented.
    public enum EntryOperation { Login, Servers, SelectServer, CreateFirstCharacter, Logout, OpenLocalProfile }
    public enum EntryStage { Login, ServerSelection, FirstCharacterCreation, CharacterReady }
    public enum EntryFailure { None, InvalidCredentials, Unauthenticated, InvalidServer, InvalidCharacter, AlreadyCreated, StorageUnavailable }

    public sealed class EntryRequest
    {
        public EntryOperation Operation { get; }
        public string AccountId { get; }
        public string Password { get; }
        public int ServerId { get; }
        public int JobId { get; }
        public string Name { get; }
        private EntryRequest(EntryOperation operation, string id = null, string password = null, int server = 0, int job = 0, string name = null)
        { Operation = operation; AccountId = id; Password = password; ServerId = server; JobId = job; Name = name; }
        public static EntryRequest Login(string id, string password) => new EntryRequest(EntryOperation.Login, id, password);
        public static EntryRequest Servers() => new EntryRequest(EntryOperation.Servers);
        public static EntryRequest SelectServer(int id) => new EntryRequest(EntryOperation.SelectServer, server: id);
        public static EntryRequest Create(int job, string name) => new EntryRequest(EntryOperation.CreateFirstCharacter, job: job, name: name);
        public static EntryRequest Logout() => new EntryRequest(EntryOperation.Logout);
        public static EntryRequest OpenLocalProfile() => new EntryRequest(EntryOperation.OpenLocalProfile);
    }

    [Serializable, DataContract] public sealed class EntryCharacter
    {
        [DataMember] public string id;
        [DataMember] public string name;
        [DataMember] public int job;
        [DataMember] public bool tutorialCompleted;
        [DataMember(EmitDefaultValue = false)] public DarkSaver.Original.World.WorldRecord world;
        public EntryCharacter Copy() => new EntryCharacter { id = id, name = name, job = job, tutorialCompleted = tutorialCompleted, world = world?.Copy() };
    }
    public sealed class EntryServer
    {
        public int Id { get; }
        public string Name { get; }
        public EntryServer(int id, string name) { Id = id; Name = name; }
    }
    public sealed class EntryReply
    {
        public EntryOperation Operation { get; }
        public EntryFailure Failure { get; }
        public bool Accepted => Failure == EntryFailure.None;
        private readonly EntryCharacter character;
        private readonly EntryServer[] servers;
        public EntryCharacter Character => character?.Copy();
        public EntryServer[] Servers => (EntryServer[])servers.Clone();
        public EntryReply(EntryOperation operation, EntryFailure failure = EntryFailure.None, EntryCharacter character = null, EntryServer[] servers = null)
        { Operation = operation; Failure = failure; this.character = character?.Copy(); this.servers = servers == null ? Array.Empty<EntryServer>() : (EntryServer[])servers.Clone(); }
    }
    public interface IEntryTransport { EntryReply Request(EntryRequest request); }
}
