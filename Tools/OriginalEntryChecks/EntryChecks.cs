using System;
using System.IO;
using System.Linq;
using DarkSaver.Original.Entry;

internal static class EntryChecks
{
    private static int count;
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); count++; }
    private sealed class MemoryStore : IEntryAccountStore
    {
        public LocalAccountDocument Data = new LocalAccountDocument();
        public bool FailWrite;
        public bool TryRead(out LocalAccountDocument document) { document = Data.Copy(); return true; }
        public bool TryWrite(LocalAccountDocument document) { if (FailWrite) return false; Data = document.Copy(); return true; }
    }
    private sealed class WrongReply : IEntryTransport
    {
        public EntryReply Request(EntryRequest request) => new EntryReply(EntryOperation.Logout);
    }
    private static void RequireRejected(EntryAccountService service, EntryRequest request, EntryFailure failure)
    { var reply = service.Request(request); Check(!reply.Accepted && reply.Failure == failure && reply.Operation == request.Operation, "Expected refusal: " + failure); }
    private static void Main()
    {
        var store = new MemoryStore(); var service = new EntryAccountService(store);
        RequireRejected(service, EntryRequest.Servers(), EntryFailure.Unauthenticated);
        RequireRejected(service, EntryRequest.SelectServer(1), EntryFailure.Unauthenticated);
        RequireRejected(service, EntryRequest.Create(100, "주인공"), EntryFailure.Unauthenticated);
        Check(!service.ProvisionLocalAccount("", "password"), "No empty account");
        Check(!service.ProvisionLocalAccount("first", ""), "No empty password");
        Check(!LocalEntryPolicy.IsInput("bad\ud800") && !LocalEntryPolicy.IsInput("bad\udc00"), "Reject malformed Unicode without normalization guesses");
        Check(LocalEntryPolicy.IsInput("캐릭터\ud83d\ude00"), "Well-formed Unicode is a local storage policy");
        Check(service.ProvisionLocalAccount("first", "password-one"), "Provision first");
        Check(!service.ProvisionLocalAccount("first", "replacement"), "Duplicate cannot replace credential");
        Check(store.Data.accounts.Length == 1 && store.Data.accounts[0].character == null, "Provision is not character creation");
        Check(store.Data.accounts[0].passwordHash != "password-one" && store.Data.accounts[0].salt.Length > 0, "Hash only");
        RequireRejected(service, EntryRequest.Login("first", "wrong"), EntryFailure.InvalidCredentials);
        RequireRejected(service, EntryRequest.Login(null, null), EntryFailure.InvalidCredentials);
        RequireRejected(service, EntryRequest.Servers(), EntryFailure.Unauthenticated);
        Check(service.Request(EntryRequest.Login("first", "password-one")).Accepted, "Authenticate");
        var servers = service.Request(EntryRequest.Servers());
        Check(servers.Accepted && servers.Servers.Length == 1 && servers.Servers[0].Id == 1, "One local server");
        var copy = servers.Servers; copy[0] = new EntryServer(99, "mutated");
        Check(servers.Servers[0].Id == 1, "Detached server list");
        RequireRejected(service, EntryRequest.Create(100, "주인공"), EntryFailure.InvalidServer);
        RequireRejected(service, EntryRequest.SelectServer(2), EntryFailure.InvalidServer);
        RequireRejected(service, EntryRequest.Create(100, "주인공"), EntryFailure.InvalidServer);
        Check(service.Request(EntryRequest.SelectServer(1)).Accepted, "Select server");
        RequireRejected(service, EntryRequest.Login("first", "wrong"), EntryFailure.InvalidCredentials);
        RequireRejected(service, EntryRequest.Create(100, "주인공"), EntryFailure.Unauthenticated);
        Check(service.Request(EntryRequest.Login("first", "password-one")).Accepted, "Reauthenticate");
        Check(service.Request(EntryRequest.SelectServer(1)).Character == null, "Empty account needs first creation");
        RequireRejected(service, EntryRequest.Create(102, "클레릭"), EntryFailure.InvalidCharacter);
        RequireRejected(service, EntryRequest.Create(100, "   "), EntryFailure.InvalidCharacter);
        RequireRejected(service, EntryRequest.Create(100, "bad\nname"), EntryFailure.InvalidCharacter);
        Check(store.Data.accounts[0].character == null, "Refusals do not create");
        store.FailWrite = true;
        RequireRejected(service, EntryRequest.Create(100, "저장실패"), EntryFailure.StorageUnavailable);
        Check(store.Data.accounts[0].character == null, "Failed save preserves empty account");
        store.FailWrite = false;
        var created = service.Request(EntryRequest.Create(100, "파이터"));
        Check(created.Accepted && created.Character.job == 100 && created.Character.name == "파이터", "First commander created");
        var detached = created.Character; detached.name = "mutated";
        Check(created.Character.name == "파이터" && store.Data.accounts[0].character.name == "파이터", "Detached character state");
        string originalId = created.Character.id;
        RequireRejected(service, EntryRequest.Create(103, "덮어쓰기"), EntryFailure.AlreadyCreated);
        Check(store.Data.accounts[0].character.id == originalId, "Existing character preserved");
        Check(service.Request(EntryRequest.Logout()).Accepted, "Logout");
        RequireRejected(service, EntryRequest.Servers(), EntryFailure.Unauthenticated);

        var client = new EntryClient(service); int ready = 0; client.CharacterReady += _ => ready++;
        Check(!client.Create(100, "주인공") && client.Stage == EntryStage.Login, "UI cannot bypass login");
        Check(!client.Login("first", "wrong") && client.Stage == EntryStage.Login, "Failed login stays at login");
        Check(client.Login("first", "password-one") && client.Stage == EntryStage.ServerSelection, "Login waits at server selection");
        Check(!client.SelectServer(99) && client.Stage == EntryStage.ServerSelection && ready == 0, "Invalid server does not advance");
        Check(client.SelectServer(1) && client.Stage == EntryStage.CharacterReady && ready == 1 && client.Character.id == originalId, "Existing character returns once, no slot UI");
        Check(!client.Create(101, "새이름") && ready == 1, "Cannot recreate existing character");
        client.Logout(); Check(client.Stage == EntryStage.Login && client.Character == null, "Clear client state");
        Check(!new EntryClient(new WrongReply()).Login("id", "password"), "Reject mismatched reply operation");

        // An independent account provision between login and creation must not be overwritten.
        Check(service.ProvisionLocalAccount("second", "password-two"), "Provision second");
        Check(service.Request(EntryRequest.Login("second", "password-two")).Accepted, "Authenticate second");
        Check(service.Request(EntryRequest.SelectServer(1)).Accepted, "Second selects server");
        Check(new EntryAccountService(store).ProvisionLocalAccount("third", "password-three"), "External provision");
        Check(service.Request(EntryRequest.Create(101, "나이트")).Accepted && store.Data.accounts.Length == 3, "Preserve externally provisioned account");
        Check(store.Data.accounts.First(a => a.id == "first").character.job == 100 && store.Data.accounts.First(a => a.id == "third").character == null, "Account isolation");
        var interrupted = new EntryClient(service);
        Check(interrupted.Login("third", "password-three") && interrupted.SelectServer(1), "Empty account before authentication loss");
        service.Request(EntryRequest.Logout());
        Check(!interrupted.Create(103, "중단된캐릭터") && interrupted.Stage == EntryStage.Login, "Authentication loss returns client to login");
        Check(store.Data.accounts.First(a => a.id == "third").character == null, "Authentication loss does not create or save");

        string temporary = Path.Combine(Path.GetTempPath(), "ds-original-entry-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(temporary);
        try
        {
            var path = Path.Combine(temporary, "accounts-v1.json"); var disk = new LocalAccountFileStore(path);
            string legacy = Path.Combine(temporary, "native-save-v1.json"); File.WriteAllText(legacy, "preserved-old-save");
            Check(disk.TryRead(out var empty) && empty.accounts.Length == 0 && !File.Exists(path), "Read missing does not write");
            foreach (int job in new[] { 100, 101, 103 })
            {
                string account = "class-" + job, password = "class-password-" + job;
                var authority = new EntryAccountService(disk);
                Check(authority.ProvisionLocalAccount(account, password), "Disk provision " + job);
                var flow = new EntryClient(authority); int notifications = 0; flow.CharacterReady += _ => notifications++;
                Check(flow.Login(account, password) && flow.Stage == EntryStage.ServerSelection, "Disk login " + job);
                Check(flow.SelectServer(1) && flow.Stage == EntryStage.FirstCharacterCreation, "Direct first creation " + job);
                Check(flow.Create(job, "캐릭터" + job) && flow.Stage == EntryStage.CharacterReady && notifications == 1, "Disk create " + job);
                string id = flow.Character.id;
                var reconnect = new EntryClient(new EntryAccountService(disk));
                Check(reconnect.Login(account, password) && reconnect.SelectServer(1), "Disk reconnect " + job);
                Check(reconnect.Stage == EntryStage.CharacterReady && reconnect.Character.id == id && reconnect.Character.job == job, "Persistent identity " + job);
            }
            Check(disk.TryRead(out var saved) && saved.accounts.Length == 3, "All disk accounts retained");
            var bytes = File.ReadAllText(path);
            Check(!bytes.Contains("class-password-") && !bytes.Contains("30000") && !bytes.Contains("gold") && bytes.Contains("\"tutorialCompleted\":false"), "No plaintext or gameplay grants");
            Check(File.ReadAllText(legacy) == "preserved-old-save", "Do not migrate/overwrite previous saves");
            var changedVersion = saved.Copy(); changedVersion.version = 2;
            Check(!disk.TryWrite(changedVersion) && File.ReadAllText(path) == bytes, "Invalid write preserves file");
            Check(!new LocalAccountFileStore("\0").TryWrite(saved), "Invalid storage path is a refusal, not a gameplay exception");
            File.WriteAllText(path, "broken-json");
            var damaged = new EntryAccountService(disk);
            RequireRejected(damaged, EntryRequest.Login("class-100", "class-password-100"), EntryFailure.StorageUnavailable);
            Check(!damaged.ProvisionLocalAccount("new", "new-password") && File.ReadAllText(path) == "broken-json", "Never replace damaged store");
            File.WriteAllText(path, "{\"version\":2,\"accounts\":[]}");
            Check(!disk.TryRead(out _), "Unsupported format rejected");
            Check(!Directory.EnumerateFiles(temporary, "*.tmp-*").Any(), "No leftover transaction files");
        }
        finally { Directory.Delete(temporary, true); }
        Console.WriteLine("{\"assertions\":" + count + ",\"jobs\":[100,101,103],\"diskRoundTrips\":3,\"unityPlayExecuted\":false}");
    }
}
