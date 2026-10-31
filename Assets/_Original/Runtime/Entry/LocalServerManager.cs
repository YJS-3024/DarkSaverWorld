using System;
using UnityEngine;

namespace DarkSaver.Original.Entry
{
    [DisallowMultipleComponent]
    public sealed partial class LocalServerManager : MonoBehaviour, IEntryTransport
    {
        public static string AccountPath => System.IO.Path.Combine(Application.persistentDataPath, "OriginalLocalServer", "accounts-v1.json");
        private EntryAccountService service;
        public event Action<EntryReply> ResponseReceived;
        public EntryReply Request(EntryRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var reply = !isActiveAndEnabled ? new EntryReply(request.Operation, EntryFailure.StorageUnavailable)
                : (service ??= new EntryAccountService(new LocalAccountFileStore(AccountPath))).Request(request);
            ResponseReceived?.Invoke(reply); return reply;
        }
        public EntryReply Request_Login(string id, string password) => Request(EntryRequest.Login(id, password));
        public EntryReply Request_ServerList() => Request(EntryRequest.Servers());
        public EntryReply Request_SelectServer(int id) => Request(EntryRequest.SelectServer(id));
        public EntryReply Request_CreateFirstCharacter(int job, string name) => Request(EntryRequest.Create(job, name));
        private void OnDisable() { service?.Request(EntryRequest.Logout()); service = null; }
    }
}
