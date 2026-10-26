using System;
using System.IO;
namespace DarkSaver.Core
{
    // Local multi-account directory. The original account/slots remain in place for its owner.
    public sealed class NativeAccountDirectory
    {
        private readonly string legacyPath,accountsPath;
        private readonly NativeAccountStore legacy;
        public NativeAccountDirectory(string legacyAccountPath)
        {legacyPath=Path.GetFullPath(legacyAccountPath);legacy=new NativeAccountStore(legacyPath);accountsPath=Path.Combine(Path.GetDirectoryName(legacyPath),"accounts");}
        public static string NormalizeId(string id)=>(id??"").Trim().ToLowerInvariant();
        public string LegacyAccountId=>legacy.AccountId;
        public bool HasAccounts
        {
            get
            {
                if(legacy.HasAccount || File.Exists(accountsPath))return true;
                try
                {
                    if(!Directory.Exists(accountsPath))return false;
                    foreach(var folder in Directory.EnumerateDirectories(accountsPath))
                        if(File.Exists(Path.Combine(folder,"native-account.json")))return true;
                    return false;
                }
                catch(IOException){return true;}catch(UnauthorizedAccessException){return true;}
            }
        }
        public string AccountFolder(string id)
        {
            if(!NativeAccountStore.IsValidId(id))throw new ArgumentException("Invalid account ID.");
            return Path.Combine(accountsPath,NormalizeId(id));
        }
        private NativeAccountStore Store(string id)=>new NativeAccountStore(Path.Combine(AccountFolder(id),"native-account.json"));
        public bool TryRegister(string id,string password,string confirmation,out string reason)
        {
            reason="아이디는 영문·숫자·밑줄 3~20자로 입력하세요.";
            if(!NativeAccountStore.IsValidId(id))return false;
            if(legacy.HasAccount && legacy.AccountId==null){reason="기존 계정 파일을 확인할 수 없습니다. 기존 파일을 보존했습니다.";return false;}
            if(NormalizeId(id)==legacy.AccountId || Store(id).HasAccount){reason="이미 등록된 아이디입니다. 다른 아이디를 입력해 주세요.";return false;}
            // The first account retains the historical root slots, including existing
            // unregistered local characters. Later accounts always get separate folders.
            if(!legacy.HasAccount && !HasAccounts)return legacy.TryRegister(id,password,confirmation,out reason);
            return Store(id).TryRegister(id,password,confirmation,out reason);
        }
        public bool TryAuthenticate(string id,string password,out string reason)
        {
            reason="아이디 또는 비밀번호가 일치하지 않습니다.";
            if(!NativeAccountStore.IsValidId(id))return false;
            bool original=NormalizeId(id)==legacy.AccountId;
            if(original && Store(id).HasAccount){reason="동일 아이디의 계정 경로가 충돌합니다. 기존 파일을 확인해 주세요.";return false;}
            return (original?legacy:Store(id)).TryAuthenticate(id,password,out reason);
        }
        public string[] SlotPaths(string id,string[] legacySlots)
        {
            if(string.IsNullOrEmpty(id) || NormalizeId(id)==legacy.AccountId)return (string[])legacySlots.Clone();
            string folder=AccountFolder(id);var paths=new string[NativeCharacterSlots.Count];
            for(int i=0;i<paths.Length;i++)paths[i]=Path.Combine(folder,i==0?"native-save-v1.json":"native-character-"+(i+1)+".json");
            return paths;
        }
        public string HistoryPath(string id,string legacyHistory)
            =>string.IsNullOrEmpty(id)||NormalizeId(id)==legacy.AccountId?legacyHistory:Path.Combine(AccountFolder(id),"native-login.json");
        public static bool SamePath(string first,string second)
        {
            try{return string.Equals(Path.GetFullPath(first),Path.GetFullPath(second),Path.DirectorySeparatorChar=='\\'?StringComparison.OrdinalIgnoreCase:StringComparison.Ordinal);}
            catch(ArgumentException){return false;}catch(NotSupportedException){return false;}
        }
    }
}
