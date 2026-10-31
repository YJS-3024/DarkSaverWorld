using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Xml;

namespace DarkSaver.Original.Entry
{
    public sealed class LocalAccountFileStore : IEntryAccountStore
    {
        private readonly string path;
        public LocalAccountFileStore(string path) { this.path = path ?? throw new ArgumentNullException(nameof(path)); }
        public bool TryRead(out LocalAccountDocument document)
        {
            document = null;
            try
            {
                if (!File.Exists(path)) { document = new LocalAccountDocument(); return true; }
                if (new FileInfo(path).Length > 1024 * 1024) return false;
                using (var input = File.OpenRead(path))
                    document = (LocalAccountDocument)new DataContractJsonSerializer(typeof(LocalAccountDocument)).ReadObject(input);
                return LocalEntryPolicy.IsDocument(document);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is SerializationException || e is XmlException) { return false; }
        }
        public bool TryWrite(LocalAccountDocument document)
        {
            if (!LocalEntryPolicy.IsDocument(document)) return false;
            string temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                byte[] bytes;
                using (var serialized = new MemoryStream())
                { new DataContractJsonSerializer(typeof(LocalAccountDocument)).WriteObject(serialized, document); bytes = serialized.ToArray(); }
                if (bytes.Length > 1024 * 1024) return false;
                using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { file.Write(bytes, 0, bytes.Length); file.Flush(true); }
                if (File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is NotSupportedException || e is ArgumentException || e is SerializationException || e is XmlException) { return false; }
            finally
            {
                try { if (File.Exists(temporary)) File.Delete(temporary); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }
}
