using System;
using System.IO;
using UnityEngine;

namespace DarkSaver.Core
{
    // Login preference only; character/world state remains in the checksummed save file.
    public static class NativeLoginHistory
    {
        [Serializable] private sealed class Record { public int version; public string savePath; }
        public static string DefaultPath=>Path.Combine(Application.persistentDataPath,"native-login.json");
        public static bool TryRead(string historyPath,out string savePath,out string reason)
        {
            savePath=null;reason=null;
            try
            {
                if(!File.Exists(historyPath))return false;
                var record=JsonUtility.FromJson<Record>(File.ReadAllText(historyPath));
                if(record==null || record.version!=1 || string.IsNullOrWhiteSpace(record.savePath))
                {reason="마지막 캐릭터 기록이 올바르지 않습니다.";return false;}
                savePath=Path.GetFullPath(record.savePath);return true;
            }
            catch(Exception){reason="마지막 캐릭터 기록을 읽을 수 없습니다.";return false;}
        }
        public static bool TryResolve(string historyPath,string[] slots,out string resumePath,out bool requiresSelection,out string reason)
        {
            resumePath=null;requiresSelection=true;
            if(TryRead(historyPath,out var last,out reason))
            {
                if(!NativeCharacterSlots.IsOccupied(last))return true;
                resumePath=last;requiresSelection=false;
                if(!new NativeSaveStore(last).TryLoad(out var saved,out reason))return false;
                if(!saved.hasCharacter){reason="마지막 저장에 캐릭터가 없습니다.";return false;}
                return true;
            }
            if(reason!=null){requiresSelection=false;return false;}
            // Migrate old installs with no history; never silently switch away from a known corrupt last save.
            DateTime timestamp=DateTime.MinValue;
            foreach(string path in slots)
                if(new NativeSaveStore(path).TryLoad(out var saved,out _) && saved.hasCharacter)
                {
                    DateTime current;
                    try{current=File.GetLastWriteTimeUtc(File.Exists(path)?path:path+".bak");}
                    catch(IOException){continue;}
                    catch(UnauthorizedAccessException){continue;}
                    if(resumePath==null || current>timestamp){resumePath=path;timestamp=current;}
                }
            requiresSelection=resumePath==null;return true;
        }
        public static bool TryRemember(string historyPath,string savePath,out string reason)
        {
            reason=null;string temporary=null;
            try
            {
                var record=new Record {version=1,savePath=Path.GetFullPath(savePath)};
                string path=Path.GetFullPath(historyPath);Directory.CreateDirectory(Path.GetDirectoryName(path));
                temporary=path+".tmp-"+Guid.NewGuid().ToString("N");
                using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                {var bytes=System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(record));stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                // Unity's .NET profile has no File.Move(source, destination, overwrite).
                // Replace an existing record without deleting it before the new file is ready.
                if(File.Exists(path))File.Replace(temporary,path,null);
                else File.Move(temporary,path);
                temporary=null;return true;
            }
            catch(Exception){reason="마지막 캐릭터 기록을 저장할 수 없습니다.";return false;}
            finally{if(temporary!=null)try{File.Delete(temporary);}catch(IOException){}}
        }
    }
}
