using DarkSaver.Core;
using System.Text.Json.Nodes;

internal static class AccountChecks
{
    public static void Run(Action<bool,string> check)
    {
        string directory=Path.Combine(Path.GetTempPath(),"ds-accounts-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try
        {
            string path=Path.Combine(directory,"account.json");var store=new NativeAccountStore(path);
            check(!store.HasAccount && store.AccountId==null,"empty installation has no registered account");
            foreach(string id in new[]{"", "ab", "a name", "한글아이디", "../foo",new string('a',21)})
                check(!store.TryRegister(id,"sample-password!","sample-password!",out _) && !File.Exists(path),"invalid account ID leaves storage empty");
            foreach(string password in new[]{"short",new string('x',65),new string(' ',8)})
                check(!store.TryRegister("hero",password,password,out _) && !File.Exists(path),"invalid password leaves storage empty");
            check(!store.TryRegister("hero","sample-password!","different-password!",out _) && !File.Exists(path),"confirmation mismatch is rejected without a partial account");
            check(store.TryRegister("  Hero_01  ","sample-password!","sample-password!",out _),"valid account registers");
            check(store.HasAccount && store.AccountId=="hero_01","canonical ID survives reload");
            string bytes=File.ReadAllText(path);
            check(!bytes.Contains("sample-password!") && !bytes.Contains("confirmation",StringComparison.OrdinalIgnoreCase),"file does not persist plaintext password or confirmation");
            var record=JsonNode.Parse(bytes);check(Convert.FromBase64String(record["salt"].GetValue<string>()).Length==16 && Convert.FromBase64String(record["hash"].GetValue<string>()).Length==32,"stored salt/hash lengths");
            check(new NativeAccountStore(path).TryAuthenticate("HERO_01","sample-password!",out _),"registered credentials authenticate after restart");
            check(!store.TryAuthenticate("hero_01","wrong-password!",out _),"wrong password is rejected");
            check(!store.TryAuthenticate("other_name","sample-password!",out _),"wrong ID is rejected");
            check(!store.TryAuthenticate("hero_01",null,out _),"empty credentials cannot bypass registration");
            check(!store.TryRegister("new_user","different-password!","different-password!",out _) && File.ReadAllText(path)==bytes,"duplicate registration never overwrites an account");
            var second=new NativeAccountStore(Path.Combine(directory,"other.json"));
            check(second.TryRegister("hero_01","sample-password!","sample-password!",out _),"independent installation can register same ID");
            var other=JsonNode.Parse(File.ReadAllText(Path.Combine(directory,"other.json")));
            check(record["salt"].ToJsonString()!=other["salt"].ToJsonString() && record["hash"].ToJsonString()!=other["hash"].ToJsonString(),"equal passwords receive independent salts/hashes");
            record["iterations"]=1;File.WriteAllText(path,record.ToJsonString());
            check(store.HasAccount && !store.TryAuthenticate("hero_01","sample-password!",out _),"tampered cost is rejected rather than weakening verification");
            File.WriteAllText(path,"corrupt");check(store.HasAccount && !store.TryAuthenticate("hero_01","sample-password!",out _),"corrupt account cannot fall back to guest login");
            check(!store.TryRegister("new_user","sample-password!","sample-password!",out _) && File.ReadAllText(path)=="corrupt","corrupt account is not overwritten by registration");
            File.WriteAllText(path,new string('a',4097));check(!store.TryAuthenticate("hero_01","sample-password!",out _),"oversize account file is rejected");
            string blocking=Path.Combine(directory,"blocked");File.WriteAllText(blocking,"retained");
            check(!new NativeAccountStore(Path.Combine(blocking,"account.json")).TryRegister("hero","sample-password!","sample-password!",out _),"write failure returns registration failure");
            check(File.ReadAllText(blocking)=="retained" && Directory.GetFiles(directory,"*.tmp-*").Length==0,"failed registration preserves existing files and cleans temporary data");
        }
        finally{Directory.Delete(directory,true);}
    }
}
