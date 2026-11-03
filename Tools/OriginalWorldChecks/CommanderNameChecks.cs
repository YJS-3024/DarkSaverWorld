using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.UI;
using DarkSaver.Original.World;
internal static class CommanderNameChecks
{
 static int checks;static string root;static readonly JsonSerializerOptions Json=new JsonSerializerOptions{IncludeFields=true};
 static string Id()=>Guid.NewGuid().ToString("N");static void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
 static T Read<T>(string p)=>JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(root,"Assets/_Original",p)),Json);
 static string State(object o)=>JsonSerializer.Serialize(o,Json);
 sealed class Store:IEntryAccountStore
 {public LocalAccountDocument data=new LocalAccountDocument();public bool fail;public bool TryRead(out LocalAccountDocument d){d=data.Copy();return true;}public bool TryWrite(LocalAccountDocument d){if(fail)return false;data=d.Copy();return true;}}
 sealed class Fixture
 {
  public readonly Store store=new Store();public EntryAccountService service;public EntryClient client;public LocalServerManager manager;public WorldDefinition definition;
  public WorldView View=>service.WorldSnapshot();
  public Fixture(bool roster=true)
  {
   definition=Read<WorldDefinition>("Data/World/KaosiaWorld.json");definition.growthCatalog=Read<CharacterGrowthCatalog>("Data/Growth/CharacterGrowth.json");service=new EntryAccountService(store);client=new EntryClient(service);client.OpenLocalProfile();client.Create(100,"원래이름");store.data.accounts[0].character.tutorialCompleted=true;
   store.data.accounts[0].character.world=new WorldRecord{healthKnown=true,hp=17,maxHp=200,mp=3,maxMp=45,goldKnown=true,gold=123,inventory=new[]{new WorldOwnedItem{id=Id(),item=1,slot=0}}};client.OpenLocalProfile();Check(service.BeginWorld(definition).Accepted,"isolated wounded world");
   manager=new LocalServerManager(service);manager.ConfigureClientText(Read<NativeCodePage949>("Resources/OriginalCodePage949.json"));if(roster)Push("원래이름");
  }
  public void Push(string name)
  {var v=View;Check(service.ReceiveCommanderRoster(new CommanderRosterPush{id=Id(),approved=true,character=v.characterId,session=v.commanderSession,expectedRevision=v.commanders?.revision??0,state=new CommanderRoster{rows=new[]{new OwnedCommander{id="owner",name=name,job=100,nativeRecord=513,deployment=1},new OwnedCommander{id="other",name="다른캐릭터",job=101,nativeRecord=37,deployment=2}}}}).Accepted,"explicit original record/roster update");}
 }
 static void Main(string[] args)
 {
  root=args[0];var codec=Read<NativeCodePage949>("Resources/OriginalCodePage949.json");using var fixture=JsonDocument.Parse(File.ReadAllText(args[1]));
  foreach(var c in fixture.RootElement.GetProperty("packets").EnumerateArray())
  {codec.TryEncode(c.GetProperty("name").GetString(),out var bytes);var packet=NativeCommanderNamePackets.Build(c.GetProperty("record").GetInt32(),bytes);Check(Convert.ToHexString(packet).Equals(c.GetProperty("packet").GetString(),StringComparison.OrdinalIgnoreCase),"original4587C0 bytes");}
  foreach(int length in new[]{0,1,255,256,257,512})
  {var bytes=Enumerable.Range(0,length).Select(i=>(byte)(i%255+1)).ToArray();var p=NativeCommanderNamePackets.Build(513,bytes);Check(p.Length==length+4&&p[0]==0x3e&&p[1]==1&&p[2]==2&&p[3]==(byte)length&&p.Skip(4).SequenceEqual(bytes),"BYTE length with full copy; large cases are managed/static semantics only");}
  Check(NativeCommanderNamePackets.Build(-1,new byte[0])==null&&NativeCommanderNamePackets.Build(65536,new byte[0])==null&&NativeCommanderNamePackets.Build(0,null)==null,"invalid packet inputs");
  var unknown=new Fixture(false);Check(unknown.manager.Request_EditCommanderName()==null,"no inferred native record for legacy roster");
  var f=new Fixture();string before=State(f.store.data);int packets=0;CommanderNameRequest observed=null;f.manager.Notify_CommanderNamePacket+=p=>{packets++;p[0]=0;};f.manager.Notify_CommanderNameRequested+=r=>observed=r;
  var edit=f.manager.Request_EditCommanderName();Check(edit!=null&&edit.NativeRecord==513&&edit.OriginalName=="원래이름","capture actual record rather than index");
  Check(f.manager.Request_SetCommanderName(edit,"새 이름")&&packets==1&&observed.packet[0]==0x3e&&observed.nativeRecord==513,"emit detached raw/typed request once");
  Check(State(f.store.data)==before&&f.View.commanders.rows[0].name=="원래이름","sending does not rename, pay, heal or save locally");Check(!f.manager.Request_SetCommanderName(edit,"중복")&&packets==1,"duplicate callback rejected");
  edit=f.manager.Request_EditCommanderName();Check(f.manager.Request_CancelCommanderName(edit)&&!f.manager.Request_SetCommanderName(edit,"취소")&&State(f.store.data)==before,"Escape cancellation preserves state");
  var old=f.manager.Request_EditCommanderName();edit=f.manager.Request_EditCommanderName();Check(old.Closed&&!f.manager.Request_SetCommanderName(old,"이전"),"new edit invalidates older gesture");f.manager.Request_CancelCommanderName(edit);
  foreach(var value in new[]{"😀","bad\nname","bad\0name"})
  {edit=f.manager.Request_EditCommanderName();Check(!f.manager.Request_SetCommanderName(edit,value)&&State(f.store.data)==before,"unsupported encoding/control cannot send");f.manager.Request_CancelCommanderName(edit);}
  edit=f.manager.Request_EditCommanderName();Check(f.manager.Request_SetCommanderName(edit,"")&&observed.packet.Length==4&&observed.packet[3]==0,"native empty edit is sent for server decision");
  edit=f.manager.Request_EditCommanderName();var foreign=new LocalServerManager(f.service);foreign.ConfigureClientText(codec);Check(!foreign.Request_SetCommanderName(edit,"잘못된매니저"),"edit is bound to the manager that opened it");f.manager.Request_CancelCommanderName(edit);
  edit=f.manager.Request_EditCommanderName();Check(f.service.InspectCommander(1).Accepted,"inspect other received general");Check(!f.manager.Request_SetCommanderName(edit,"다른대상")&&f.View.commanders.rows[1].name=="다른캐릭터","old selected identity cannot target new general");
  edit=f.manager.Request_EditCommanderName();Check(edit.NativeRecord==37&&f.manager.Request_SetCommanderName(edit,"공백 유지 ")&&observed.nativeRecord==37&&observed.name=="공백 유지 ","secondary record and whitespace preserved");
  f.service.InspectCommander(0);edit=f.manager.Request_EditCommanderName();f.Push("수신이름");Check(!f.manager.Request_SetCommanderName(edit,"늦은값")&&f.View.commanders.rows[0].name=="수신이름","roster revision invalidates edit; received name wins");
  Check(f.store.data.accounts[0].character.world.roster.rows[0].name=="수신이름"&&f.View.record.hp==17&&f.View.record.gold==123,"approved roster reception persists name while preserving wounds/wallet");
  f.client.Logout();Check(f.manager.Request_EditCommanderName()==null&&!f.manager.Request_SetCommanderName(edit,"로그아웃"),"logout rejects old context");
  f.client.OpenLocalProfile();f.service.BeginWorld(f.definition);Check(f.View.commanders.rows[0].name=="수신이름","received name restores on reconnect");Check(!f.manager.Request_SetCommanderName(edit,"재접속"),"reconnect cannot reuse a prior edit");
  edit=f.manager.Request_EditCommanderName();f.manager.isActiveAndEnabled=false;Check(!f.manager.Request_SetCommanderName(edit,"꺼진매니저"),"disabled component cannot send");f.manager.isActiveAndEnabled=true;f.manager.Request_CancelCommanderName(edit);
  var v=f.View;foreach(string field in new[]{"moving","facility","portal","request","battle","training","presentation","identity","session","record","nullrows"})
  {
   var snapshot=f.View;switch(field){case"moving":snapshot.moving=true;break;case"facility":snapshot.facility=new WorldFacilityView();break;case"portal":snapshot.pendingPortal="x";break;case"request":snapshot.commanderRequest=new CommanderRequest();break;case"battle":snapshot.battle=new TrainingView();break;case"training":snapshot.training=new TrainingView();break;case"presentation":snapshot.presentation=new DarkSaver.Original.Campaign.CampaignPresentationView();break;case"identity":snapshot.characterId=null;break;case"session":snapshot.commanderSession=null;break;case"record":snapshot.commanders.rows[0].nativeRecord=null;break;case"nullrows":snapshot.commanders.rows=null;break;}
   Check(NativeCommanderNameEdit.Begin(snapshot)==null,"unavailable edit: "+field);
  }
  edit=f.manager.Request_EditCommanderName();var changed=f.View;changed.commanders.rows=null;Check(!edit.Matches(changed),"malformed async snapshot does not throw");f.manager.Request_CancelCommanderName(edit);
  var pending=f.manager.Request_EditCommanderName();var saved=State(f.store.data);f.store.fail=true;Check(f.manager.Request_SetCommanderName(pending,"서버대기")&&State(f.store.data)==saved,"request-only feature never needs local save or changes save contents");
  Console.WriteLine(JsonSerializer.Serialize(new{assertions=checks,unityPlay=false,scope="Production account/service/manager request integration with isolated Unity component lifetime. Original serializer comparison, stale identity/session/revision, cancel/replay and unchanged saves. No server name-approval policy or raw response layout."}));
 }
}
