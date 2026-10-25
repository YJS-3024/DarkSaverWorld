using System;
using System.IO;
using UnityEngine;

namespace DarkSaver.Core
{
    // Login preference only; character/world state remains in the checksummed save file.
    public static class NativeLoginHistory
    {
        [Serializable] private sealed class Record { public int version; public string savePath; public string accountId; }
        public static string DefaultPath=>Path.Combine(Application.persistentDataPath,"native-login.json");
        public static bool TryRead(string historyPath,out string savePath,out string reason, string accountId=null)
        {
            savePath=null;reason=null;
            try
            {
                if(!File.Exists(historyPath))return false;
                var record=JsonUtility.FromJson<Record>(File.ReadAllText(historyPath));
                if(record==null || (record.version!=1 && record.version!=2) || string.IsNullOrWhiteSpace(record.savePath))
                {reason="마지막 캐릭터 기록이 올바르지 않습니다.";return false;}
                // Old records can originate from automatic test-save migration, not an explicit selection.
                if(record.version!=2 || !string.Equals(record.accountId,accountId,StringComparison.Ordinal))return false;
                savePath=Path.GetFullPath(record.savePath);return true;
            }
            catch(Exception){reason="마지막 캐릭터 기록을 읽을 수 없습니다.";return false;}
        }
        public static bool TryResolve(string historyPath,string[] slots,out string resumePath,out bool requiresSelection,out string reason,string accountId=null)
        {
            resumePath=null;requiresSelection=true;
            if(TryRead(historyPath,out var last,out reason,accountId))
            {
                if(!NativeCharacterSlots.IsOccupied(last))return true;
                resumePath=last;requiresSelection=false;
                if(!new NativeSaveStore(last).TryLoad(out var saved,out reason))return false;
                if(!saved.hasCharacter){reason="마지막 저장에 캐릭터가 없습니다.";return false;}
                return true;
            }
            if(reason!=null){requiresSelection=false;return false;}
            // Existing save files alone do not establish which character the player selected.
            // Keep them available in the selection screen without silently resuming one.
            return true;
        }
        public static bool TryRemember(string historyPath,string savePath,out string reason,string accountId=null)
        {
            reason=null;string temporary=null;
            try
            {
                var record=new Record {version=2,savePath=Path.GetFullPath(savePath),accountId=accountId};
                string path=Path.GetFullPath(historyPath);Directory.CreateDirectory(Path.GetDirectoryName(path));
                temporary=path+".tmp-"+Guid.NewGuid().ToString("N");
                using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                {var bytes=System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(record));stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                // Unity's .NET profile has no File.Move(source, destination, overwrite).
                // Replace an existing record without deleting it before the new file is ready.
                if(File.Exists(path))
                {
                    // Unix rename can replace a file opened by another reader. Honor the
                    // same sharing guard before replacing the record on every platform.
                    using(var access=new FileStream(path,FileMode.Open,FileAccess.ReadWrite,FileShare.None)) { }
                    File.Replace(temporary,path,null);
                }
                else File.Move(temporary,path);
                temporary=null;return true;
            }
            catch(Exception){reason="마지막 캐릭터 기록을 저장할 수 없습니다.";return false;}
            finally{if(temporary!=null)try{File.Delete(temporary);}catch(IOException){}}
        }
    }
}
