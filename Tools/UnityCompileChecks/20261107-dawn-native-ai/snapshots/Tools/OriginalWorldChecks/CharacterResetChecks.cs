using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.Editor;
internal static class CharacterResetChecks
{
 static int checks;static void Check(bool ok,string text){checks++;if(!ok)throw new Exception(text);}
 static void Main(string[] args)
 {
  string dir=Path.Combine(Path.GetTempPath(),"ds-character-reset-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
  try
  {
   var first=LocalEntryPolicy.NewAccount("first","first-password");var other=LocalEntryPolicy.NewAccount("other","other-password");
   foreach(var row in new[]{first,other})row.character=new EntryCharacter{id=Guid.NewGuid().ToString("N"),name=row.id,job=103,tutorialCompleted=true};
   var path=Path.Combine(dir,"accounts.json");var store=new LocalAccountFileStore(path);Check(store.TryWrite(new LocalAccountDocument{accounts=new[]{first,other}}),"isolated seed");
   byte[] before=File.ReadAllBytes(path);Check(LocalCharacterReset.TryReset(path,"first",out var message,out var backup),"reset selected character");
   Check(File.Exists(backup)&&File.ReadAllBytes(backup).SequenceEqual(before),"backup is exact original bytes");
   Check(store.TryRead(out var after)&&after.accounts.Length==2&&after.accounts[0].character==null,"only target character removed");
   Check(after.accounts[0].id==first.id&&after.accounts[0].salt==first.salt&&after.accounts[0].passwordHash==first.passwordHash&&LocalEntryPolicy.Verify(after.accounts[0],"first-password"),"target credentials preserved");
   Check(after.accounts[1].character.id==other.character.id&&after.accounts[1].character.tutorialCompleted&&LocalEntryPolicy.Verify(after.accounts[1],"other-password"),"other character and credentials preserved");
   var service=new EntryAccountService(store);var reply=service.Request(EntryRequest.Login("first","first-password"));Check(reply.Accepted,"reset account can authenticate");
   byte[] reset=File.ReadAllBytes(path);Check(!LocalCharacterReset.TryReset(path,"first",out message,out var unused)&&unused==null&&reset.SequenceEqual(File.ReadAllBytes(path)),"already empty is no-op");
   Check(!LocalCharacterReset.TryReset(path,"missing",out message,out unused)&&unused==null&&reset.SequenceEqual(File.ReadAllBytes(path)),"missing account preserves store");
   string local=Path.Combine(dir,"profile.json");var profile=LocalEntryPolicy.NewAccount(LocalEntryPolicy.LocalProfileAccountId,"local-password");profile.character=other.character.Copy();
   Check(new LocalAccountFileStore(local).TryWrite(new LocalAccountDocument{accounts=new[]{profile}}),"isolated local profile");
   Check(LocalCharacterReset.TryReset(local,LocalEntryPolicy.LocalProfileAccountId,out message,out backup)&&new LocalAccountFileStore(local).TryRead(out var localAfter)&&localAfter.accounts[0].character==null,"login bypass profile reset");
   string bad=Path.Combine(dir,"bad.json");File.WriteAllText(bad,"broken");Check(!LocalCharacterReset.TryReset(bad,"first",out message,out backup)&&File.ReadAllText(bad)=="broken"&&backup==null,"corrupt store untouched");
   string absent=Path.Combine(dir,"absent.json");Check(!LocalCharacterReset.TryReset(absent,"first",out message,out backup)&&!File.Exists(absent),"absent store not created");
   Console.WriteLine(JsonSerializer.Serialize(new{checks,unityPlay=false,scope="Actual editor reset helper and production file store against isolated temporary accounts; exact backup, credential/other-account preservation, login and bypass reset, missing/corrupt no-op. No real player save modified."}));
  }
  finally{Directory.Delete(dir,true);}
 }
}
