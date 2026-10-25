using DarkSaver.Core;

internal static class LoginFlowChecks
{
    public static void Run(Action<bool,string> check)
    {
        string directory=Path.Combine(Path.GetTempPath(),"ds-login-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try
        {
            string history=Path.Combine(directory,"login.json");var slots=Enumerable.Range(0,NativeCharacterSlots.Count).Select(i=>Path.Combine(directory,"slot-"+i+".json")).ToArray();
            check(NativeLoginHistory.TryResolve(history,slots,out var path,out var selection,out _) && selection && path==null,"first login requires character selection");
            check(NativeLoginHistory.TryRemember(history,slots[1],out _),"selected new character is remembered atomically");
            check(NativeLoginHistory.TryRead(history,out path,out _) && path==slots[1],"last slot path survives reload");
            check(NativeLoginHistory.TryRemember(history,slots[0],out _) && NativeLoginHistory.TryRead(history,out path,out _) && path==slots[0],"existing login history is replaced with newly selected slot");
            byte[] previous=File.ReadAllBytes(history);
            using(var locked=new FileStream(history,FileMode.Open,FileAccess.Read,FileShare.Read))
                check(!NativeLoginHistory.TryRemember(history,slots[1],out var failure) && !string.IsNullOrEmpty(failure),"locked login history replacement reports failure");
            check(File.ReadAllBytes(history).SequenceEqual(previous) && NativeLoginHistory.TryRead(history,out path,out _) && path==slots[0],"failed replacement preserves previous login history bytes and selection");
            check(!Directory.GetFiles(directory,"*.tmp-*").Any(),"failed login replacement cleans temporary file");
            check(NativeLoginHistory.TryRemember(history,slots[1],out _),"login replacement succeeds after lock release");
            check(NativeLoginHistory.TryResolve(history,slots,out path,out selection,out _) && selection,"interrupted creation does not fabricate a resume save");
            NativeSaveData Save(string name,string map,string runtime,float x,float y)=>new NativeSaveData {format=NativeSaveStore.Format,version=NativeSaveStore.Version,mapScene=map,runtimeScene=runtime,
                worldX=x,worldY=y,hasCharacter=true,character=new NativeCharacterProfile {name=name,jobId=100},clearedScenarios=Array.Empty<string>()};
            var first=new NativeSaveStore(slots[0]);var last=new NativeSaveStore(slots[1]);
            check(first.TrySave(Save("다른인물","Kaosia_Map","Town_Runtime",21,21),out _),"other slot fixture saves");
            check(last.TrySave(Save("마지막인물","World1010_Map","Field_Runtime",17.5f,11.5f),out _),"last world position fixture saves");
            check(NativeLoginHistory.TryResolve(history,slots,out path,out selection,out _) && !selection && path==slots[1],"subsequent login bypasses character selection");
            check(new NativeSaveStore(path).TryLoad(out var restored,out _) && restored.mapScene=="World1010_Map" && restored.runtimeScene=="Field_Runtime" && restored.worldX==17.5f && restored.worldY==11.5f,"resume retains exact map and coordinates");
            File.SetLastWriteTimeUtc(slots[0],DateTime.UtcNow.AddMinutes(5));
            check(NativeLoginHistory.TryResolve(history,slots,out path,out selection,out _) && path==slots[1],"known last character wins over newer unrelated slot");
            check(last.TrySave(Save("마지막인물","Kaosia_Map","Town_Runtime",22.25f,19.75f),out _),"normal exit checkpoint replaces previous position");
            check(NativeLoginHistory.TryResolve(history,slots,out path,out selection,out _) && new NativeSaveStore(path).TryLoad(out restored,out _) && restored.worldX==22.25f && restored.worldY==19.75f,"next login sees latest exit position");
            File.Delete(slots[1]);
            check(NativeLoginHistory.TryResolve(history,slots,out path,out selection,out _) && !selection && path==slots[1],"backup-only last save can resume");
            File.WriteAllText(slots[1]+".bak","corrupt");
            check(!NativeLoginHistory.TryResolve(history,slots,out path,out selection,out _) && path==slots[1] && !selection,"corrupt last save is not silently replaced by another character");
            File.Delete(history);
            check(NativeLoginHistory.TryResolve(history,slots,out path,out selection,out _) && path==slots[0] && !selection,"legacy installation chooses latest valid character without history");
            File.WriteAllText(history,"invalid preference");
            check(!NativeLoginHistory.TryResolve(history,slots,out _,out selection,out _) && !selection,"corrupt history offers explicit recovery rather than auto-switch");
            check(NativeLoginHistory.TryRemember(history,slots[0],out _),"explicit character selection repairs preference");
            check(NativeLoginHistory.TryResolve(history,slots,out path,out selection,out _) && path==slots[0],"repaired preference can resume");
            check(NativeCharacterSlots.Count==10 && NativeCharacterSlots.AllPaths().Distinct().Count()==10,"ten unique default slot paths");
            check(NativeCharacterSlots.PathFor(0)==NativeSaveStore.DefaultPath && Path.GetFileName(NativeCharacterSlots.PathFor(1))=="native-character-2.json" && Path.GetFileName(NativeCharacterSlots.PathFor(2))=="native-character-3.json","legacy first three paths remain unchanged");
            check(Path.GetFileName(NativeCharacterSlots.PathFor(9))=="native-character-10.json","tenth slot has independent storage");
            var tenth=new NativeSaveStore(slots[9]);
            check(tenth.TrySave(Save("열번째인물","World1010_Map","Field_Runtime",26.5f,17.5f),out _),"tenth slot saves");
            check(NativeLoginHistory.TryRemember(history,slots[9],out _) && NativeLoginHistory.TryResolve(history,slots,out path,out selection,out _) && path==slots[9] && !selection,"tenth slot participates in login resume");
            check(new NativeSaveStore(path).TryLoad(out restored,out _) && restored.character.name=="열번째인물" && restored.worldX==26.5f && restored.worldY==17.5f,"tenth slot preserves character and location");
            try{NativeCharacterSlots.PathFor(10);throw new Exception("eleventh slot accepted");}catch(ArgumentOutOfRangeException){check(true,"slot beyond ten is rejected");}
            check(!Directory.GetFiles(directory,"*.tmp-*").Any(),"login preference leaves no temporary files");
        }
        finally{Directory.Delete(directory,true);}
    }
}
