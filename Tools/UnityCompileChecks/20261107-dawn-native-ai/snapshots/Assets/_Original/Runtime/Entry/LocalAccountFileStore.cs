using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Xml;

namespace DarkSaver.Original.Entry
{
    // Local storage diagnostics only. No paths, account data or exception messages.
    public enum LocalAccountStoreFailure { None, InvalidDocument, TooLarge, Io, AccessDenied, Unsupported, Serialization }
    public sealed class LocalAccountFileStore : IEntryAccountStore
    {
        private readonly string path;
        public LocalAccountStoreFailure LastFailure { get; private set; }
        public int LastErrorCode { get; private set; }
        private void ClearFailure(){LastFailure=LocalAccountStoreFailure.None;LastErrorCode=0;}
        private bool Refuse(LocalAccountStoreFailure failure){LastFailure=failure;return false;}
        private bool Refuse(Exception error)
        {
            LastErrorCode=error.HResult;
            return Refuse(error is UnauthorizedAccessException?LocalAccountStoreFailure.AccessDenied
                :error is IOException?LocalAccountStoreFailure.Io
                :error is SerializationException||error is XmlException?LocalAccountStoreFailure.Serialization
                :LocalAccountStoreFailure.Unsupported);
        }
        public LocalAccountFileStore(string path) { this.path = path ?? throw new ArgumentNullException(nameof(path)); }
        public bool TryRead(out LocalAccountDocument document)
        {
            document = null; ClearFailure();
            try
            {
                if (!File.Exists(path)) { document = new LocalAccountDocument(); return true; }
                if (new FileInfo(path).Length > 1024 * 1024) return Refuse(LocalAccountStoreFailure.TooLarge);
                using (var input = File.OpenRead(path))
                    document = (LocalAccountDocument)new DataContractJsonSerializer(typeof(LocalAccountDocument)).ReadObject(input);
                return LocalEntryPolicy.IsDocument(document) || Refuse(LocalAccountStoreFailure.InvalidDocument);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is SerializationException || e is XmlException) { return Refuse(e); }
        }
        public bool TryWrite(LocalAccountDocument document)
        {
            ClearFailure();
            if (!LocalEntryPolicy.IsDocument(document)) return Refuse(LocalAccountStoreFailure.InvalidDocument);
            string temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                byte[] bytes;
                using (var serialized = new MemoryStream())
                { new DataContractJsonSerializer(typeof(LocalAccountDocument)).WriteObject(serialized, document); bytes = serialized.ToArray(); }
                if (bytes.Length > 1024 * 1024) return Refuse(LocalAccountStoreFailure.TooLarge);
                using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { file.Write(bytes, 0, bytes.Length); file.Flush(true); }
                if (File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is NotSupportedException || e is ArgumentException || e is SerializationException || e is XmlException) { return Refuse(e); }
            finally
            {
                try { if (File.Exists(temporary)) File.Delete(temporary); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }
}
