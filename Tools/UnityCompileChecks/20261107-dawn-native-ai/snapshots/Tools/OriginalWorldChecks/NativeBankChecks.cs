using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.UI;
using DarkSaver.Original.World;
internal static class NativeBankChecks
{
 static int checks;static string root;static readonly JsonSerializerOptions Json=new JsonSerializerOptions{IncludeFields=true};
 static string Id()=>Guid.NewGuid().ToString("N");static string State(object value)=>JsonSerializer.Serialize(value,Json);
 static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
 static T Read<T>(string path)=>JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(root,"Assets/_Original",path)),Json);
 static byte[] Balance(ulong value){var packet=new byte[9];packet[0]=0x94;for(int i=0;i<8;i++)packet[i+1]=(byte)(value>>(8*i));return packet;}
 sealed class Store:IEntryAccountStore
 {public LocalAccountDocument data=new LocalAccountDocument();public bool fail;public bool TryRead(out LocalAccountDocument value){value=data.Copy();return true;}public bool TryWrite(LocalAccountDocument value){if(fail)return false;data=value.Copy();return true;}}
 sealed class Fixture
 {
  public Store store=new Store();public EntryAccountService service;public EntryClient client;public LocalServerManager manager;public WorldDefinition definition;public string npc;
  public WorldView View=>service.WorldSnapshot();public string Token=>View.facility.token;public NativeBankQuery query;public int sends;public byte[] last;public NativeBankAmountRequest amount;
  public Fixture(bool queryNow=true,bool walletKnown=true)
  {
   definition=Read<WorldDefinition>("Data/World/KaosiaWorld.json");var map=definition.maps.First(m=>m.id==2400);var staff=map.npcs.First(n=>n.kind==(int)FacilityKind.Bank);npc=staff.id;
   service=new EntryAccountService(store);client=new EntryClient(service);client.OpenLocalProfile();client.Create(100,"은행검사");store.data.accounts[0].character.tutorialCompleted=true;
   store.data.accounts[0].character.world=new WorldRecord{map=2400,x=staff.x-1,y=staff.y,goldKnown=walletKnown,gold=walletKnown?1000UL:0,healthKnown=true,hp=17,maxHp=200,mp=3,maxMp=45,inventory=new[]{new WorldOwnedItem{id=Id(),item=1,slot=0}}};
   client.OpenLocalProfile();Check(service.BeginWorld(definition).Accepted,"isolated bank-interior world");Check(service.OpenWorldFacility(npc).Accepted,"actual bank facility and adjacency");manager=new LocalServerManager(service);
   manager.Notify_BankPacket+=packet=>{sends++;last=(byte[])packet.Clone();packet[0]=0;};manager.Notify_BankBalanceRequested+=value=>query=value;manager.Notify_BankAmountRequested+=value=>amount=value;
   if(queryNow)Check(manager.Request_BankBalance(Token).Accepted,"native93 queued without invented balance authority");
  }
  public NativeBankBalanceNotice Notice(ulong value)=>new NativeBankBalanceNotice{id=Id(),query=query.id,character=query.character,session=query.session,token=query.token,expectedRevision=View.record.currency?.revision??0,approved=true,packet=Balance(value)};
  public void Loaded(ulong value=500){Check(manager.Notify_BankBalance(Notice(value)).Accepted,"approved94 balance receipt");}
  public BankTransferNotice Transfer(ulong gold,ulong bank)
  {var p=View.bankTransferRequest;return new BankTransferNotice{id=Id(),request=p.id,character=p.character,session=p.session,token=p.token,npc=p.npc,map=p.map,approved=true,deposit=p.deposit,amount=p.amount,expectedRevision=p.currencyRevision,gold=gold,bank=bank,fee=0,feeSource=BankFeeSource.Wallet};}
 }
 static void Main(string[] args)
 {
  root=args[0];using var doc=JsonDocument.Parse(File.ReadAllText(args[1]));
  foreach(var row in doc.RootElement.GetProperty("labels").EnumerateArray())
  {
   int x=row.GetProperty("x").GetInt32(),y=row.GetProperty("y").GetInt32(),depth=row.GetProperty("depth").GetInt32();bool loaded=row.GetProperty("loaded").GetBoolean();var expected=row.GetProperty("expected");
   Check(expected.GetArrayLength()==(loaded?2:1),"native94 loaded gate on savings text");
   for(int i=0;i<expected.GetArrayLength();i++)
   {var text=expected[i];Check(text.GetProperty("text").GetString()==(i==0?NativeCurrencyText.Wallet(row.GetProperty("wallet").GetUInt64()):NativeCurrencyText.Savings(row.GetProperty("savings").GetUInt64())),"4453B0 DWORD wallet / QWORD savings text");Check(text.GetProperty("x").GetInt32()==x+(i==0?NativeBankLayout.WalletX:NativeBankLayout.SavingsX)&&text.GetProperty("y").GetInt32()==y+(i==0?NativeBankLayout.WalletY:NativeBankLayout.SavingsY)&&text.GetProperty("alignment").GetInt32()==2&&text.GetProperty("color").GetInt32()==(depth==16?65535:255),"original bank anchors/right alignment/white");}
  }
  foreach(var row in doc.RootElement.GetProperty("controls").EnumerateArray())
  {int control=row.GetProperty("control").GetInt32();if(control==0||control==1)Check(NativeBankLayout.CanOpenAmount(control==0,row.GetProperty("loaded").GetBoolean(),row.GetProperty("wallet").GetUInt64(),row.GetProperty("savings").GetUInt64())==(row.GetProperty("opened").GetArrayLength()!=0),"4451E0 loaded/32bit wallet/64bit savings gate");}
  foreach(var row in doc.RootElement.GetProperty("balances").EnumerateArray())
  {ulong value=row.GetProperty("value").GetUInt64();Check(NativeBankPackets.TryBalance(Balance(value),out var parsed)&&parsed==value,"original94 unsigned64 balance");}
  foreach(var row in doc.RootElement.GetProperty("amounts").EnumerateArray())
  {
   string text=row.GetProperty("text").GetString();bool deposit=row.GetProperty("deposit").GetBoolean();Check(NativeBankPackets.TryAmount(text,out int value),"defined representable atoi prefix");Check(Convert.ToHexString(NativeBankPackets.Amount(deposit,value)).Equals(row.GetProperty("packet").GetString(),StringComparison.OrdinalIgnoreCase),"original95/96 DWORD bytes");
  }
  Check(NativeBankPackets.Query().SequenceEqual(new byte[]{0x93}),"actual one-byte query");
  foreach(var invalid in new[]{Array.Empty<byte>(),new byte[8],new byte[10],new byte[]{0x95,0,0,0,0,0,0,0,0}})Check(!NativeBankPackets.TryBalance(invalid,out _),"bounded94 framing rejects invalid segment");
  foreach(string overflow in new[]{"2147483648","-2147483649","999999999999999999999999"})Check(!NativeBankPackets.TryAmount(overflow,out _),"CRT overflow is not invented as a server limit");
  var f=new Fixture();Check(f.sends==1&&f.last[0]==0x93&&!f.manager.BankBalanceReady(f.Token)&&!f.View.record.bankKnown,"query does not synthesize a known balance");Check(f.manager.Request_EditBankAmount(true,f.Token)==null,"input waits for bank snapshot");string before=State(f.store.data);Check(f.manager.Request_BankBalance(f.Token).Accepted&&f.sends==1,"same window query is issued once");
  foreach(string bad in new[]{"query","owner","session","token","revision","id","packet"})
  {
   var n=f.Notice(500);switch(bad){case"query":n.query=Id();break;case"owner":n.character=Id();break;case"session":n.session=Id();break;case"token":n.token=Id();break;case"revision":n.expectedRevision++;break;case"id":n.id="bad";break;case"packet":n.packet=new byte[8];break;}
   Check(!f.manager.Notify_BankBalance(n).Accepted&&State(f.store.data)==before&&!f.manager.BankBalanceReady(f.Token),"invalid bank notice preserves all state: "+bad);
  }
  var notice=f.Notice(500);f.store.fail=true;Check(f.manager.Notify_BankBalance(notice).Failure==WorldFailure.StorageUnavailable&&!f.manager.BankBalanceReady(f.Token)&&State(f.store.data)==before,"save failure leaves query retryable and funds unchanged");f.store.fail=false;Check(f.manager.Notify_BankBalance(notice).Accepted&&f.View.record.bank==500&&f.View.record.gold==1000&&f.View.record.hp==17,"same94 receipt retries atomically");Check(!f.manager.Notify_BankBalance(notice).Accepted,"94 replay rejected");
  var edit=f.manager.Request_EditBankAmount(true,f.Token);var altered=edit.Query;altered.token="forged";Check(edit.Query.token==f.Token,"amount context is detached and immutable to caller");var originalQuery=f.query;originalQuery.token="forged";Check(f.manager.BankBalanceReady(f.Token),"query event is a detached copy");f.query=f.manager.Request_EditBankAmount(true,f.Token).Query;
  edit=f.manager.Request_EditBankAmount(true,f.Token);int count=f.sends;Check(f.manager.Request_CancelBankAmount(edit)&&!f.manager.Request_BankAmount(edit,"200",Id()).Accepted&&f.sends==count,"Escape does not send or charge");
  before=State(f.store.data);edit=f.manager.Request_EditBankAmount(true,f.Token);string id=Id();Check(f.manager.Request_BankAmount(edit,"200",id).Accepted&&f.amount.packet[0]==0x95&&f.amount.amount==200&&f.View.bankTransferRequest.amount==200,"native confirmation queues server decision");Check(State(f.store.data)==before,"no default fee or balance delta before approval");count=f.sends;Check(!f.manager.Request_BankAmount(edit,"200",id).Accepted&&f.sends==count,"duplicate confirm does not send");var reused=f.manager.Request_EditBankAmount(true,f.Token);Check(!f.manager.Request_BankAmount(reused,"200",id).Accepted&&f.sends==count,"action ID cannot replay through another edit");f.manager.Request_CancelBankAmount(reused);
  Check(f.manager.Notify_BankTransfer(f.Transfer(800,700)).Accepted&&f.View.record.gold==800&&f.View.record.bank==700,"explicit fee0 receipt applies exact bank/wallet result");
  edit=f.manager.Request_EditBankAmount(false,f.Token);before=State(f.store.data);count=f.sends;Check(f.manager.Request_BankAmount(edit,"0",Id()).Accepted&&f.sends==count+1&&f.last.SequenceEqual(new byte[]{0x96,0,0,0,0})&&State(f.store.data)==before,"original zero wire request does not create a local zero transaction");
  edit=f.manager.Request_EditBankAmount(false,f.Token);count=f.sends;Check(f.manager.Request_BankAmount(edit,"-1",Id()).Accepted&&f.sends==count+1&&f.last.Skip(1).All(b=>b==255)&&State(f.store.data)==before,"negative DWORD is observable but never interpreted as a local unsigned purchase");
  edit=f.manager.Request_EditBankAmount(false,f.Token);notice=f.Notice(700);Check(f.manager.Notify_BankBalance(notice).Accepted&&!f.manager.Request_BankAmount(edit,"1",Id()).Accepted,"new monetary revision invalidates old editor");
  edit=f.manager.Request_EditBankAmount(true,f.Token);Check(f.manager.Request_BankAmount(edit,"100",Id()).Accepted,"second transfer queued");var late=f.Transfer(700,800);f.Loaded(700);Check(f.View.bankTransferRequest==null&&!f.manager.Notify_BankTransfer(late).Accepted,"absolute currency update releases stale transaction without double applying it");
  var denied=new Fixture();notice=denied.Notice(1);notice.approved=false;before=State(denied.store.data);Check(!denied.manager.Notify_BankBalance(notice).Accepted&&State(denied.store.data)==before,"denied bank query does not invent balance");
  var cancel=new Fixture();string token=cancel.Token;Check(!cancel.manager.Request_CancelBankClient("wrong")&&cancel.View.currencyRequest!=null,"another token cannot cancel query");Check(cancel.manager.Request_CancelBankClient(token)&&cancel.View.currencyRequest==null,"closing own window cancels only its pending query");
  Check(cancel.service.RequestCurrencyState(Id()).Accepted,"unrelated monetary query");var unrelated=cancel.View.currencyRequest.id;Check(!cancel.manager.Request_CancelBankClient(token)&&cancel.View.currencyRequest.id==unrelated,"old bank close cannot cancel another query");
  var logout=new Fixture();notice=logout.Notice(500);logout.client.Logout();Check(!logout.manager.Notify_BankBalance(notice).Accepted,"old94 after logout is ignored");
  var authority=new Fixture(false);authority.manager.CurrencyAuthority=(request,v)=>new CurrencyNotice{id=Id(),request=request.id,character=request.character,session=request.session,expectedRevision=request.revision,approved=true,bank=321};Check(authority.manager.Request_BankBalance(authority.Token).Accepted&&authority.manager.BankBalanceReady(authority.Token)&&authority.View.record.bank==321,"existing explicit currency authority can answer bank query");
  var reentrant=new Fixture(false);reentrant.manager.CurrencyAuthority=(request,v)=>{reentrant.client.Logout();return null;};Check(!reentrant.manager.Request_BankBalance(reentrant.Token).Accepted,"logout inside query callback is safe");
  var reconnect=new Fixture(false);reconnect.manager.CurrencyAuthority=(request,v)=>{reconnect.client.Logout();reconnect.client.OpenLocalProfile();reconnect.service.BeginWorld(reconnect.definition);reconnect.service.RequestCurrencyState(Id());return new CurrencyNotice{id=Id(),request=request.id,character=request.character,session=request.session,expectedRevision=request.revision,approved=true,bank=999};};Check(!reconnect.manager.Request_BankBalance(reconnect.Token).Accepted&&reconnect.View.currencyRequest!=null&&!reconnect.View.record.bankKnown,"old callback cannot apply or clear new session's query");
  var wireClose=new Fixture(false);wireClose.manager.Notify_BankPacket+=_=>wireClose.service.CloseWorldFacility();wireClose.manager.CurrencyAuthority=(request,v)=>new CurrencyNotice{id=Id(),request=request.id,character=request.character,session=request.session,expectedRevision=request.revision,approved=true,bank=999};Check(!wireClose.manager.Request_BankBalance(wireClose.Token).Accepted&&!wireClose.View.record.bankKnown,"wire callback closing window cannot apply a later balance authority result");
  var transferLogout=new Fixture();transferLogout.Loaded();transferLogout.manager.BankTransferAuthority=(request,v)=>{transferLogout.client.Logout();return null;};edit=transferLogout.manager.Request_EditBankAmount(true,transferLogout.Token);Check(!transferLogout.manager.Request_BankAmount(edit,"100",Id()).Accepted,"logout inside transfer authority is safe");
  var unconfigured=new Fixture(true,false);unconfigured.Loaded();edit=unconfigured.manager.Request_EditBankAmount(false,unconfigured.Token);before=State(unconfigured.store.data);count=unconfigured.sends;
  Check(unconfigured.manager.Request_BankAmount(edit,"10",Id()).Accepted&&unconfigured.sends==count+1&&unconfigured.View.bankTransferRequest==null&&unconfigured.View.failure==WorldFailure.None&&State(unconfigured.store.data)==before,"native request with unknown wallet sends without invented denial or financial update");
  var overdrawn=new Fixture();overdrawn.Loaded();edit=overdrawn.manager.Request_EditBankAmount(false,overdrawn.Token);before=State(overdrawn.store.data);
  Check(overdrawn.manager.Request_BankAmount(edit,"10000",Id()).Accepted&&overdrawn.View.bankTransferRequest==null&&overdrawn.View.failure==WorldFailure.None&&State(overdrawn.store.data)==before,"unconfigured native sender does not invent a server overdraft decision");
  var interleaved=new Fixture();Check(interleaved.manager.Notify_Currency(new CurrencyNotice{id=Id(),character=interleaved.View.characterId,session=interleaved.View.currencySession,expectedRevision=0,approved=true,gold=777}).Accepted&&interleaved.View.currencyRequest==null,"new wallet push supersedes pending monetary read");
  Check(interleaved.manager.Notify_BankBalance(interleaved.Notice(500)).Accepted&&interleaved.manager.BankBalanceReady(interleaved.Token)&&interleaved.View.record.gold==777&&interleaved.View.record.bank==500,"first94 after wallet push still loads bank without erasing wallet or waiting on removed query");
  Console.WriteLine(JsonSerializer.Serialize(new{assertions=checks,unityPlay=false,scope="Production bank/currency/account/manager integration; original packet/defined atoi comparison, ownership/nonce/revision guards, explicit approval/save failure/replay and reentrant logout isolation. Unity component dispatcher and native MFC/CRT/edit/transport boundaries isolated. No initial money, fee, limit, approval or server-policy inference."}));
 }
}
