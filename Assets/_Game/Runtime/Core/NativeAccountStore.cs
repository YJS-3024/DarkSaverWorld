using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace DarkSaver.Core
{
    // One password-hashed account record. NativeAccountDirectory manages multiple records.
    // This is a native single-player contract, not an original online account database.
    public sealed class NativeAccountStore
    {
        private const int Iterations=100000;
        private readonly string path;
        [Serializable] private sealed class Account
        {public int version,iterations;public string userId,salt,hash;}
        public NativeAccountStore(string filePath){path=Path.GetFullPath(filePath);}
        public bool HasAccount=>File.Exists(path);
        public string AccountId=>TryRead(out var account,out _)?account.userId:null;
        private static string Normalize(string id)=>(id ?? "").Trim().ToLowerInvariant();
        public static bool IsValidId(string id)
        {
            id=Normalize(id);if(id.Length<3 || id.Length>20)return false;
            foreach(char c in id)if(!(c>='a' && c<='z' || c>='0' && c<='9' || c=='_'))return false;
            return true;
        }
        private static byte[] Derive(string password,byte[] salt)
        {using(var algorithm=new Rfc2898DeriveBytes(password,salt,Iterations,HashAlgorithmName.SHA256))return algorithm.GetBytes(32);}
        public bool TryRegister(string id,string password,string confirmation,out string reason)
        {
            reason=null;string temporary=null;
            if(HasAccount){reason="이미 등록된 아이디입니다. 로그인해 주세요.";return false;}
            if(!IsValidId(id)){reason="아이디는 영문·숫자·밑줄 3~20자로 입력하세요.";return false;}
            if(string.IsNullOrWhiteSpace(password) || password.Length<8 || password.Length>64)
            {reason="비밀번호는 8~64자로 입력하세요.";return false;}
            if(!string.Equals(password,confirmation,StringComparison.Ordinal)){reason="비밀번호 확인이 일치하지 않습니다.";return false;}
            try
            {
                var salt=new byte[16];using(var random=RandomNumberGenerator.Create())random.GetBytes(salt);
                var account=new Account {version=1,iterations=Iterations,userId=Normalize(id),salt=Convert.ToBase64String(salt),hash=Convert.ToBase64String(Derive(password,salt))};
                Directory.CreateDirectory(Path.GetDirectoryName(path));temporary=path+".tmp-"+Guid.NewGuid().ToString("N");
                using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                {var bytes=Encoding.UTF8.GetBytes(JsonUtility.ToJson(account));stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                File.Move(temporary,path);temporary=null;return true; // Never overwrite an existing account.
            }
            catch(Exception){reason="회원가입 정보를 저장할 수 없습니다.";return false;}
            finally{if(temporary!=null)try{File.Delete(temporary);}catch(IOException){}}
        }
        private bool TryRead(out Account account,out string reason)
        {
            account=null;reason="계정 정보를 읽을 수 없습니다.";
            try
            {
                if(!HasAccount || new FileInfo(path).Length>4096)return false;
                var record=JsonUtility.FromJson<Account>(File.ReadAllText(path));
                if(record==null || record.version!=1 || record.iterations!=Iterations || !IsValidId(record.userId)
                    || Convert.FromBase64String(record.salt).Length!=16 || Convert.FromBase64String(record.hash).Length!=32)return false;
                account=record;reason=null;return true;
            }
            catch(Exception){return false;}
        }
        public bool TryAuthenticate(string id,string password,out string reason)
        {
            if(!TryRead(out var account,out reason))return false;
            reason="아이디 또는 비밀번호가 일치하지 않습니다.";
            if(password==null || password.Length<8 || password.Length>64)return false;
            var expected=Convert.FromBase64String(account.hash);var actual=Derive(password,Convert.FromBase64String(account.salt));
            int difference=0;for(int i=0;i<expected.Length;i++)difference |= expected[i]^actual[i];
            if(difference!=0 || Normalize(id)!=account.userId)return false;
            reason=null;return true;
        }
    }
}
