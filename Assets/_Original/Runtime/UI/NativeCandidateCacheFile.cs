using System;
using System.IO;
namespace DarkSaver.Original.UI
{
    //Client metadata only. Never reads or writes player/account saves.
    public sealed class NativeCandidateCacheFile
    {
        private readonly string path;
        public NativeCandidateCacheFile(string path){this.path=path;}
        public bool Exists=>File.Exists(path);
        public bool TryRead(out byte[] bytes)
        {
            bytes=null;try{var info=new FileInfo(path);if(!info.Exists||info.Length<8||info.Length>8+NativeMemoTransfer.MaximumCompressed)return false;bytes=File.ReadAllBytes(path);return true;}
            catch(IOException){return false;}catch(UnauthorizedAccessException){return false;}
        }
        public bool TryWrite(byte[] bytes)
        {
            if(bytes==null||bytes.Length<8||bytes.Length>8+NativeMemoTransfer.MaximumCompressed)return false;
            string temporary=null;
            try
            {
                string directory=Path.GetDirectoryName(path);if(!string.IsNullOrEmpty(directory))Directory.CreateDirectory(directory);
                temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";File.WriteAllBytes(temporary,bytes);
                if(File.Exists(path))File.Replace(temporary,path,null);else File.Move(temporary,path);return true;
            }
            catch(IOException){return false;}catch(UnauthorizedAccessException){return false;}
            finally{if(temporary!=null)try{if(File.Exists(temporary))File.Delete(temporary);}catch(IOException){}catch(UnauthorizedAccessException){}}
        }
    }
}
