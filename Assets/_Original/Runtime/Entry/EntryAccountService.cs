using System;
using System.Linq;
using System.Security.Cryptography;
using System.Runtime.Serialization;

namespace DarkSaver.Original.Entry
{
    [Serializable, DataContract] public sealed class LocalAccountRecord
    {
        [DataMember] public string id;
        [DataMember] public string salt;
        [DataMember] public string passwordHash;
        [DataMember] public int iterations;
        [DataMember] public EntryCharacter character;
        public LocalAccountRecord Copy() => new LocalAccountRecord { id = id, salt = salt, passwordHash = passwordHash, iterations = iterations, character = character?.Copy() };
    }
    [Serializable, DataContract] public sealed class LocalAccountDocument
    {
        [DataMember] public int version = 1;
        [DataMember] public LocalAccountRecord[] accounts = Array.Empty<LocalAccountRecord>();
        public LocalAccountDocument Copy() => new LocalAccountDocument { version = version, accounts = accounts.Select(a => a.Copy()).ToArray() };
    }
    public interface IEntryAccountStore
    {
        bool TryRead(out LocalAccountDocument document);
        bool TryWrite(LocalAccountDocument document);
    }

    // Explicit local hosting/storage policy, not recovered original account-server code.
    public static class LocalEntryPolicy
    {
        public const int ServerId = 1;
        public const string ServerName = "로컬 서버";
        public const int PasswordIterations = 100000;
        public static bool IsStartingJob(int job) => job == 100 || job == 101 || job == 103;
        public static bool IsInput(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 128) return false;
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsControl(value[i]) || char.IsLowSurrogate(value[i])) return false;
                if (char.IsHighSurrogate(value[i]) && (++i >= value.Length || !char.IsLowSurrogate(value[i]))) return false;
            }
            return true;
        }
        public static bool IsDocument(LocalAccountDocument document)
        {
            if (document == null || document.version != 1 || document.accounts == null || document.accounts.Any(a => a == null)) return false;
            if (document.accounts.Select(a => a.id).Distinct(StringComparer.Ordinal).Count() != document.accounts.Length) return false;
            foreach (var account in document.accounts)
            {
                if (!IsInput(account.id) || account.iterations != PasswordIterations) return false;
                try { if (Convert.FromBase64String(account.salt).Length != 16 || Convert.FromBase64String(account.passwordHash).Length != 32) return false; }
                catch (ArgumentException) { return false; }
                catch (FormatException) { return false; }
                if (account.character != null && (!IsInput(account.character.name) || !IsStartingJob(account.character.job) || !Guid.TryParseExact(account.character.id, "N", out _))) return false;
            }
            return true;
        }
        public static LocalAccountRecord NewAccount(string id, string password)
        {
            if (!IsInput(id) || !IsInput(password)) return null;
            var salt = new byte[16]; using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            using (var hash = new Rfc2898DeriveBytes(password, salt, PasswordIterations, HashAlgorithmName.SHA256))
                return new LocalAccountRecord { id = id, salt = Convert.ToBase64String(salt), passwordHash = Convert.ToBase64String(hash.GetBytes(32)), iterations = PasswordIterations };
        }
        public static bool Verify(LocalAccountRecord account, string password)
        {
            if (!IsInput(password)) return false;
            using (var hash = new Rfc2898DeriveBytes(password, Convert.FromBase64String(account.salt), account.iterations, HashAlgorithmName.SHA256))
            {
                var actual = hash.GetBytes(32); var expected = Convert.FromBase64String(account.passwordHash); int difference = 0;
                for (int i = 0; i < actual.Length; i++) difference |= actual[i] ^ expected[i];
                return difference == 0;
            }
        }
    }

    public sealed partial class EntryAccountService : IEntryTransport
    {
        private readonly IEntryAccountStore store;
        private LocalAccountDocument document;
        private string accountId;
        private int selectedServer;
        private bool available;
        public EntryAccountService(IEntryAccountStore store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            available = store.TryRead(out document) && LocalEntryPolicy.IsDocument(document);
        }
        // Development/server administration only. The original signup button opened a web page.
        public bool ProvisionLocalAccount(string id, string password)
        {
            available = store.TryRead(out document) && LocalEntryPolicy.IsDocument(document);
            if (!available || document.accounts.Any(a => a.id == id)) return false;
            var account = LocalEntryPolicy.NewAccount(id, password); if (account == null) return false;
            var candidate = document.Copy(); candidate.accounts = candidate.accounts.Concat(new[] { account }).ToArray();
            if (!store.TryWrite(candidate)) return false;
            document = candidate; return true;
        }
        public EntryReply Request(EntryRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var operation = request.Operation;
            if (operation == EntryOperation.Logout) { ResetTutorial(); accountId = null; selectedServer = 0; return new EntryReply(operation); }
            if (operation == EntryOperation.Login)
            {
                ResetTutorial(); accountId = null; selectedServer = 0;
                available = store.TryRead(out document) && LocalEntryPolicy.IsDocument(document);
                if (!available) return new EntryReply(operation, EntryFailure.StorageUnavailable);
                var account = document.accounts.FirstOrDefault(a => a.id == request.AccountId);
                if (account == null || !LocalEntryPolicy.Verify(account, request.Password)) return new EntryReply(operation, EntryFailure.InvalidCredentials);
                accountId = account.id; return new EntryReply(operation);
            }
            if (!available) return new EntryReply(operation, EntryFailure.StorageUnavailable);
            if (accountId == null) return new EntryReply(operation, EntryFailure.Unauthenticated);
            if (operation == EntryOperation.Servers)
                return new EntryReply(operation, servers: new[] { new EntryServer(LocalEntryPolicy.ServerId, LocalEntryPolicy.ServerName) });
            var owner = document.accounts.First(a => a.id == accountId);
            if (operation == EntryOperation.SelectServer)
            {
                ResetTutorial(); selectedServer = 0;
                if (request.ServerId != LocalEntryPolicy.ServerId) return new EntryReply(operation, EntryFailure.InvalidServer);
                selectedServer = request.ServerId; return new EntryReply(operation, character: owner.character);
            }
            if (operation == EntryOperation.CreateFirstCharacter)
            {
                if (selectedServer != LocalEntryPolicy.ServerId) return new EntryReply(operation, EntryFailure.InvalidServer);
                // An editor account provision may have committed since login. Never
                // overwrite other accounts with the service's earlier snapshot.
                if (!store.TryRead(out var fresh) || !LocalEntryPolicy.IsDocument(fresh)) return new EntryReply(operation, EntryFailure.StorageUnavailable);
                var freshOwner = fresh.accounts.FirstOrDefault(a => a.id == accountId);
                if (freshOwner == null || freshOwner.passwordHash != owner.passwordHash || freshOwner.salt != owner.salt)
                { accountId = null; selectedServer = 0; return new EntryReply(operation, EntryFailure.Unauthenticated); }
                document = fresh; owner = freshOwner;
                if (owner.character != null) return new EntryReply(operation, EntryFailure.AlreadyCreated);
                if (!LocalEntryPolicy.IsStartingJob(request.JobId) || !LocalEntryPolicy.IsInput(request.Name)) return new EntryReply(operation, EntryFailure.InvalidCharacter);
                var candidate = document.Copy();
                var character = new EntryCharacter { id = Guid.NewGuid().ToString("N"), job = request.JobId, name = request.Name };
                candidate.accounts.First(a => a.id == accountId).character = character;
                if (!store.TryWrite(candidate)) return new EntryReply(operation, EntryFailure.StorageUnavailable);
                document = candidate; return new EntryReply(operation, character: character);
            }
            throw new InvalidOperationException("Unsupported local entry request.");
        }
    }
}
