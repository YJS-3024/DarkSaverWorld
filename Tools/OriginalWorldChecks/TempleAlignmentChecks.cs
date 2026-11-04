using System;
using System.Text.Json;
using DarkSaver.Original.World;
internal static class TempleAlignmentChecks
{
 static int checks;
 static void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
 static void Main(string[] args)
 {
  var f=CommanderNameChecks.CreateFixture(args[0]);
  f.definition.Map(1000).npcs=new[]{new WorldNpc{id="temple",name="fixture",picture=818,x=22,y=21,kind=(int)FacilityKind.Temple,counter=Array.Empty<WorldPoint>()}};
  Check(f.service.OpenWorldFacility("temple").Accepted,"open isolated temple through actual facility service");
  var v=f.View;string token=v.facility.token;int queries=0,packets=0;byte[] bytes=null;
  f.manager.Notify_TempleAlignmentRequested+=snapshot=>queries++;
  f.manager.Notify_TempleAlignmentPacket+=packet=>{packets++;bytes=packet;};
  Check(f.manager.Request_TempleAlignment(token)&&queries==1&&f.manager.TempleAlignment(token)==null,"no guessed operating fee/offer on request");
  TempleAlignmentOffer Offer(uint fee=12345)=>new TempleAlignmentOffer{id=Guid.NewGuid().ToString("N"),token=token,character=v.characterId,session=v.commanderSession,fee=fee,target=1};
  Check(!f.manager.Request_ChangeTempleAlignment(token,Guid.NewGuid().ToString("N"))&&packets==0,"no send without explicit offer");
  var q=Offer();q.fee=null;Check(!f.manager.Notify_TempleAlignment(q)&&q.Packet()==null&&q.Text=="","missing fee is not a free offer");
  q=Offer();q.target=null;Check(!f.manager.Notify_TempleAlignment(q),"missing direction is not an evil quote");
  q=Offer();q.session="old";Check(!f.manager.Notify_TempleAlignment(q),"stale session rejected");
  q=Offer();Check(f.manager.Notify_TempleAlignment(q),"explicit offer accepted");
  q.fee=99;Check(f.manager.TempleAlignment(token).fee==12345,"incoming offer copied");
  var copy=f.manager.TempleAlignment(token);copy.fee=88;Check(f.manager.TempleAlignment(token).fee==12345,"outgoing offer copied");
  string before=JsonSerializer.Serialize(f.store.data,new JsonSerializerOptions{IncludeFields=true});
  Check(f.manager.Request_ChangeTempleAlignment(token,q.id)&&packets==1&&Convert.ToHexString(bytes)=="AC39300000","actual native AC DWORD byte shape");
  Check(before==JsonSerializer.Serialize(f.store.data,new JsonSerializerOptions{IncludeFields=true}),"sending does not mutate money, identity or save");
  Check(!f.manager.Request_ChangeTempleAlignment(token,q.id)&&!f.manager.Notify_TempleAlignment(q)&&packets==1,"duplicate request and sent offer replay rejected");
  q=Offer(0);Check(f.manager.Notify_TempleAlignment(q)&&q.Text=="당신의 성향을 악으로 바꾸시겠습니까?","explicit zero fee follows original special text");
  f.manager.Request_CancelTempleAlignment(token);Check(f.manager.TempleAlignment(token)==null&&!f.manager.Notify_TempleAlignment(q)&&packets==1,"cancel closes offer without send and rejects replay");
  q=Offer();Check(f.manager.Notify_TempleAlignment(q),"next unique offer");
  f.manager.isActiveAndEnabled=false;Check(f.manager.TempleAlignment(token)==null&&!f.manager.Request_ChangeTempleAlignment(token,q.id),"disabled manager cannot send");f.manager.isActiveAndEnabled=true;
  Check(f.service.CloseWorldFacility().Accepted&&!f.manager.Request_ChangeTempleAlignment(token,q.id),"closed facility cannot send");
  Check(f.service.OpenWorldFacility("temple").Accepted&&!f.manager.Notify_TempleAlignment(q),"reopened facility token rejects old quote");
  Console.WriteLine(JsonSerializer.Serialize(new{checks,packets,queries,unityPlay=false,scope="Actual offer/manager/record contracts with memory store and component boundary. No original incoming quote format, Unity UI or server policy."}));
 }
}
