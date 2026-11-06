using System;
using System.IO;
using System.Linq;
using DarkSaver.Original.Entry;
namespace DarkSaver.Original.Editor
{
    // Editor utility only. The caller must stop Play before replacing its store.
    public static class LocalCharacterReset
    {
        public static bool TryReset(string path,string accountId,out string message,out string backup)
        {
            backup=null;
            try
            {
                if(!LocalEntryPolicy.IsInput(accountId)||!File.Exists(path)){message="해당 계정 저장 파일이 없습니다.";return false;}
                var store=new LocalAccountFileStore(path);
                if(!store.TryRead(out var original)){message="저장 파일을 읽지 못했습니다. 초기화하지 않았습니다.";return false;}
                var account=original.accounts.FirstOrDefault(a=>a.id==accountId);
                if(account==null){message="해당 계정이 없습니다.";return false;}
                if(account.character==null){message="이미 캐릭터가 없는 계정입니다.";return false;}
                byte[] bytes=File.ReadAllBytes(path);
                backup=path+".before-character-reset-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N")+".bak";
                using(var file=new FileStream(backup,FileMode.CreateNew,FileAccess.Write,FileShare.None)){file.Write(bytes,0,bytes.Length);file.Flush(true);}
                // Read the backed-up snapshot itself, preserving all other accounts and credentials.
                if(!new LocalAccountFileStore(backup).TryRead(out var snapshot)){message="백업 검증에 실패했습니다. 초기화하지 않았습니다.";return false;}
                var target=snapshot.accounts.FirstOrDefault(a=>a.id==accountId);
                if(target?.character==null||!File.ReadAllBytes(path).SequenceEqual(bytes)){message="저장 상태가 바뀌었습니다. 초기화하지 않았습니다.";return false;}
                target.character=null;
                if(!store.TryWrite(snapshot)){message="저장에 실패했습니다. 백업을 보존했습니다.";return false;}
                message="캐릭터를 초기화했습니다. 다음 시작에서 캐릭터 생성·튜토리얼부터 진행합니다.";return true;
            }
            catch(Exception e)when(e is IOException||e is UnauthorizedAccessException||e is ArgumentException||e is NotSupportedException)
            {message="초기화하지 못했습니다: "+e.Message;return false;}
        }
    }
}
