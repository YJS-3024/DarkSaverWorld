using DarkSaver.Core;
using DarkSaver.Network;
internal static class WorldCharacterChecks
{
    public static void Run(System.Action<bool,string> check)
    {
        var path=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"ds-world-character-"+System.Guid.NewGuid()+".json");
        try
        {
            var save=new NativeSaveData{format=NativeSaveStore.Format,version=NativeSaveStore.Version,mapScene="Kaosia_Map",runtimeScene="Town_Runtime",clearedScenarios=System.Array.Empty<string>(),hasCharacter=true,character=new NativeCharacterProfile{name="검사",jobId=100}};
            var store=new NativeSaveStore(path);
            check(store.TrySave(save,out _),"legacy world-character absent save");
            check(store.TryLoad(out var old,out _) && (old.worldCharacters?.Length??0)==0,"legacy no inferred deployment grants");
            var bytes=new byte[724];bytes[0]=100;
            save.worldCharacters=new[]{new WorldCharacterData{RecordId=100,Flag=1,Info=bytes,StatCosts=new short[]{1,2,3,4,5,6},ClassOptions=new byte[]{0x87,0,0,0xff,0xff,0xff,0xff,0,0}}};
            check(store.TrySave(save,out _),"world-character authority data save");
            check(store.TryLoad(out var restored,out _),"world-character authority data reload");
            check(restored.worldCharacters.Length==1 && restored.worldCharacters[0].Flag==1 && restored.worldCharacters[0].StatCosts[5]==6 && restored.worldCharacters[0].Info.Length==724,"world-character JSON fields roundtrip");
            var copy=NativeSaveStore.CopyForTransfer(restored);copy.worldCharacters[0].Info[0]=42;
            check(restored.worldCharacters[0].Info[0]==100,"world-character transfer copy isolated");
            var before=System.IO.File.ReadAllBytes(path);save.worldCharacters[0].Flag=3;
            check(!store.TrySave(save,out _),"invalid world-character state rejected");
            check(before.SequenceEqual(System.IO.File.ReadAllBytes(path)),"invalid world-character write leaves save unchanged");
        }
        finally{if(System.IO.File.Exists(path))System.IO.File.Delete(path);}
    }
}
