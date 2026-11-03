using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;
internal static class NativeClassAdjustmentChecks
{
    static int count;static readonly JsonSerializerOptions Json=new JsonSerializerOptions{IncludeFields=true};
    static string Id()=>Guid.NewGuid().ToString("N");static string State(object value)=>JsonSerializer.Serialize(value,Json);
    static void Check(bool value,string label){count++;if(!value)throw new Exception(label);}
    static string Hex(byte[] value)=>value==null?null:Convert.ToHexString(value).ToLowerInvariant();
    static void Packet(byte[] actual,JsonElement expected){Check(expected.GetArrayLength()==0?actual==null:Hex(actual)==expected[0].GetString(),"actual native packet bytes "+expected.GetRawText());}
    static void Main(string[] args)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(args[1]));
        foreach(var row in doc.RootElement.GetProperty("simple").EnumerateArray())
        {
            int record=row.GetProperty("record").GetInt32(),points=row.GetProperty("points").GetInt32();string kind=row.GetProperty("kind").GetString();
            Packet(kind=="costs"?NativeClassAdjustmentPackets.Costs(record):kind=="options"?NativeClassAdjustmentPackets.Options(record,points):NativeClassAdjustmentPackets.Close(record,kind=="confirm"),row.GetProperty("expected"));
        }
        foreach(var row in doc.RootElement.GetProperty("stats").EnumerateArray())
        {
            int record=row.GetProperty("record").GetInt32(),stat=row.GetProperty("stat").GetInt32(),direction=row.GetProperty("direction").GetInt32();var expected=row.GetProperty("expected");
            Packet(NativeClassAdjustmentPackets.Stat(record,stat,direction),expected.GetProperty("packets"));
            var draft=new StatAdjustmentView{initial=Enumerable.Range(0,6).Select(_=>new AttributeTriplet{after=10}).ToArray(),values=Enumerable.Range(0,6).Select(_=>new AttributeTriplet{after=11}).ToArray(),costs=Enumerable.Repeat(1,6).ToArray(),points=10};
            Check(draft.TryStep(stat,direction,out var values,out int remaining)&&remaining==expected.GetProperty("points").GetInt32()&&values[stat].after==expected.GetProperty("value").GetInt32(),"source ordinary private stat prediction");
        }
        foreach(var row in doc.RootElement.GetProperty("magic").EnumerateArray())
        {
            int record=row.GetProperty("record").GetInt32(),magic=row.GetProperty("magic").GetInt32(),mode=row.GetProperty("mode").GetInt32();var expected=row.GetProperty("expected");
            if(row.GetProperty("ready").GetInt32()!=0)Packet(NativeClassAdjustmentPackets.Magic(record,magic,mode==2),expected.GetProperty("packets"));
            else Check(expected.GetProperty("packets").GetArrayLength()==0,"source zero magic points suppresses a duplicate request");
        }
        foreach(var row in doc.RootElement.GetProperty("drags").EnumerateArray())
        {
            int record=row.GetProperty("record").GetInt32(),list=row.GetProperty("list").GetInt32(),index=row.GetProperty("index").GetInt32(),size=row.GetProperty("count").GetInt32();var expected=row.GetProperty("expected");
            var state=new NativeClassMagicState{available=list==0?Enumerable.Range(101,size).ToArray():Array.Empty<int>(),owned=list==1?Enumerable.Range(101,size).ToArray():Array.Empty<int>(),points=0};
            Check(state.CanDrag(list,index),"source drop capacity independent of zero magic points");Packet(NativeClassAdjustmentPackets.DragMagic(record,state.Spell(list,index),list==0),expected.GetProperty("packets"));state.PredictDrag(list,index);
            Check((list==0?state.available:state.owned).SequenceEqual(expected.GetProperty("source").EnumerateArray().Select(v=>v.GetInt32())),"actual83/85 source count and compaction prediction");
        }
        foreach(int list in new[]{0,1})foreach(int y in new[]{46,47,78,79,80,111,112,120,121,152,153})foreach(int x in new[]{207,208,239,240,241,471,472})
        {
            bool inside=false;for(int i=0;i<(list==0?8:16);i++){int left=208+i%8*33,top=(list==0?121:47)+i/8*33;inside|=x>=left&&x<left+32&&y>=top&&y<top+32;}
            Check(NativeClassMagicDrop.Contains(list,x,y)==inside,"opposite native32px drop cells exclude one-pixel gaps");
        }
        foreach(var row in doc.RootElement.GetProperty("change").EnumerateArray())
        {
            var expected=row.GetProperty("expected");var packets=expected.GetProperty("packets");int points=row.GetProperty("points").GetInt32();
            if(points!=0&&row.GetProperty("loaded").GetBoolean())
            {
                Check(Hex(NativeClassAdjustmentPackets.Change(row.GetProperty("record").GetInt32(),row.GetProperty("job").GetInt32()))==packets[0].GetString(),"actual81 target job");
                Check(expected.GetProperty("points").GetInt32()==points-1&&expected.GetProperty("loaded").GetInt32()==0,"source private class point decrement and options invalidation");
                Check(points==1?packets.GetArrayLength()==1:Hex(NativeClassAdjustmentPackets.Options(row.GetProperty("record").GetInt32(),points-1))==packets[1].GetString(),"source86 refresh only if class points remain");
            }
            else Check(packets.GetArrayLength()==0,"source unready/no-point class change sends nothing");
        }
        foreach(var row in doc.RootElement.GetProperty("receivers").EnumerateArray())
        {
            var bytes=Convert.FromHexString(row.GetProperty("packet").GetString());var expected=row.GetProperty("expected");
            if(bytes[0]==0x8b){Check(NativeClassAdjustmentPackets.TryReadCosts(bytes,out var costs)&&costs.SequenceEqual(expected.GetProperty("costs").EnumerateArray().Select(v=>v.GetInt32())),"actual8B six signed costs");}
            else
            {
                Check(NativeClassAdjustmentPackets.TryReadOptions(bytes,out var options),"actual87 accepted source frame");
                Check(options.right==expected.GetProperty("right").GetInt32()&&options.left==expected.GetProperty("left").GetInt32()&&options.choices.SequenceEqual(expected.GetProperty("choices").EnumerateArray().Take(expected.GetProperty("count").GetInt32()).Select(v=>v.GetInt32())),"source87 right/left/list order");
                Check(options.DefaultSlot==expected.GetProperty("slot").GetInt32()&&options.opaqueHeader==(bytes[1]|bytes[2]<<8),"source87 default selected slot and preserved ignored WORD");
            }
            Check(!NativeClassAdjustmentPackets.TryReadCosts(bytes.Concat(new byte[]{0}).ToArray(),out _)&&!NativeClassAdjustmentPackets.TryReadOptions(bytes.Concat(new byte[]{0}).ToArray(),out _),"strict framed length rejects trailing bytes");
        }
        foreach(int? record in new int?[]{null,-1,65535,65536})Check(NativeClassAdjustmentPackets.Costs(record)==null&&NativeClassAdjustmentPackets.Close(record,true)==null&&NativeClassAdjustmentPackets.Stat(record,0,1)==null&&NativeClassAdjustmentPackets.Magic(record,101,true)==null,"unknown native record cannot become a local index");
        Check(NativeClassAdjustmentPackets.Stat(1,-1,1)==null&&NativeClassAdjustmentPackets.Stat(1,6,1)==null&&NativeClassAdjustmentPackets.Stat(1,0,0)==null&&NativeClassAdjustmentPackets.Magic(1,0,true)==null,"invalid projected fields are not truncated into wire values");
        Manager(args[0]);Console.WriteLine(JsonSerializer.Serialize(new{assertions=count,mismatches=0}));
    }
    static void Manager(string root)
    {
        var f=CommanderAdjustmentChecks.CreateFixture(root);f.Bind(1);var manager=new LocalServerManager(f.service);var packets=new List<string>();manager.Notify_ClassAdjustmentPacket+=b=>packets.Add(Hex(b));
        string before=State(f.store.data);Check(manager.Request_OpenClassClient().Accepted&&packets.SequenceEqual(new[]{"8a0100"}),"source cost query uses explicit secondary record");
        Check(manager.Request_OpenClassClient().Accepted&&packets.Count==1&&State(f.store.data)==before,"repaint/open query is not duplicated or persisted");
        string token=manager.ClassClientView.token;
        Check(manager.Notify_ClassClientPacket(token,Convert.FromHexString("8b010002000300040005000600")).Accepted&&manager.ClassClientView.costs.SequenceEqual(new[]{1,2,3,4,5,6})&&State(f.store.data)==before,"raw costs are a window-private read-only view");
        var received=Convert.FromHexString("8700007800640002006e006f00");
        Check(manager.Notify_ClassClientPacket(token,received).Accepted&&manager.ClassClientView.options.right==120&&manager.ClassClientView.options.left==100&&manager.ClassClientView.options.DefaultSlot==1&&State(f.store.data)==before,"raw87 previews source candidates without awarding class points");
        received[3]=0;var exposed=manager.ClassClientView;exposed.options.choices[0]=999;exposed.costs[0]=999;Check(manager.ClassClientView.options.right==120&&manager.ClassClientView.options.choices[0]==110&&manager.ClassClientView.costs[0]==1,"raw source and getter mutation cannot change the private view");
        string raw=State(manager.ClassClientView);Check(manager.Notify_ClassClientPacket("old-token",Convert.FromHexString("8b010002000300040005000600")).Failure==WorldFailure.StaleRequest&&manager.Notify_ClassClientPacket(token,new byte[]{0x87,0,0,0,0,0,0,11,0}).Failure==WorldFailure.InvalidInput&&State(manager.ClassClientView)==raw,"wrong window and unsafe list frame cannot replace a current view");
        manager.StatAdjustmentAuthority=(r,v)=>{Check(r.nativeRecord==1&&StatAdjustmentRequest.Same(r,r.Copy()),"captured stat native identity");var forged=r.Copy();forged.nativeRecord=99;Check(!StatAdjustmentRequest.Same(r,forged),"notice identity cannot change native record");return f.StatNotice(r);};
        Check(manager.Request_StatAdjustmentState().Accepted&&packets.Count==1,"state bootstrap reuses current8A query");
        Check(manager.Request_AdjustStat(0,1,Id()).Accepted&&packets.Last()=="82010000000100"&&f.View.statAdjustment.values[0].after==11&&State(f.store.data)==before,"82 updates only the approved transient draft");
        f.store.fail=true;Check(manager.Request_ConfirmStatAllocation(Id()).Failure==WorldFailure.StorageUnavailable&&State(f.store.data)==before&&packets.Last()=="880100","source88 send is distinct from failed local persistence");f.store.fail=false;
        Check(manager.Request_ConfirmStatAllocation(Id()).Accepted&&f.Target.statAllocation.values[0].after==11&&f.View.record.hp==17&&f.Target.hp==23,"approved confirmation persists selected owner without healing");
        before=State(f.store.data);var learning=new NativeClassMagicState{available=new[]{101,301,201},owned=new[]{401,501},points=2};
        Check(manager.Notify_ClassMagicState(token,learning).Accepted&&State(f.store.data)==before,"learning/owned lists are window state, not approved ownership");
        learning.available[0]=999;var copy=manager.ClassClientView;copy.magic.owned[0]=999;Check(manager.ClassClientView.magic.available[0]==101&&manager.ClassClientView.magic.owned[0]==401,"learning state and snapshots clone arrays");
        long rev=manager.ClassClientView.magicRevision;string request=Id();
        Check(manager.Request_ClassMagicButton(2,0,0,token,rev,request).Accepted&&packets.Last()=="84020001006500"&&manager.ClassClientView.magic.points==0&&State(f.store.data)==before,"84 consumes only private points pending approved response");
        Check(!manager.Request_ClassMagicButton(2,0,0,token,rev,Id()).Accepted&&!manager.Request_ClassMagicButton(2,0,0,token,manager.ClassClientView.magicRevision,request).Accepted,"old revision and replay rejected");
        Check(!manager.Request_ClassMagicButton(1,1,0,token,manager.ClassClientView.magicRevision,Id()).Accepted,"zero magic points blocks buttons");
        rev=manager.ClassClientView.magicRevision;
        Check(manager.Request_ClassMagicDrag(0,1,token,rev,Id()).Accepted&&packets.Last()=="8301002d01"&&manager.ClassClientView.magic.available.SequenceEqual(new[]{101,301})&&manager.ClassClientView.magic.owned.SequenceEqual(new[]{401,501})&&State(f.store.data)==before,"83 at zero points decrements source count without teaching magic");
        Check(manager.Request_ClassMagicDrag(1,0,token,manager.ClassClientView.magicRevision,Id()).Accepted&&packets.Last()=="8501009101"&&manager.ClassClientView.magic.owned.SequenceEqual(new[]{501})&&State(f.store.data)==before,"85 compacts only private owned projection");
        Check(!manager.Notify_ClassMagicState("old",learning).Accepted&&!manager.Notify_ClassMagicState(token,new NativeClassMagicState{available=new int[17],owned=Array.Empty<int>()}).Accepted,"unsafe or old learning state rejected");
        Check(manager.Notify_ClassMagicState(token,new NativeClassMagicState{available=new[]{101},owned=Enumerable.Range(201,8).ToArray(),points=null}).Accepted&&!manager.Request_ClassMagicDrag(0,0,token,manager.ClassClientView.magicRevision,Id()).Accepted,"full owned capacity and unknown points are not fabricated");
        Check(!manager.Request_ClassMagicButton(2,0,0,token,manager.ClassClientView.magicRevision,Id()).Accepted&&State(f.store.data)==before,"unknown point policy cannot learn or spend");
        manager.ClassOptionsAuthority=(r,v)=>new ClassOptionsNotice{id=Id(),approved=true,request=r.Copy(),classPoints=1,right=new ClassCandidate{job=120},options=Array.Empty<ClassCandidate>()};
        Check(manager.Request_ClassOptions().Accepted&&packets.Last()=="860100","86 only after explicit class-point availability");
        manager.ClassChangeAuthority=(r,v)=>f.ClassNotice();
        Check(manager.Request_ChangeClass(1,0,f.View.classOptions.id,Id()).Accepted&&packets.Skip(packets.Count-2).SequenceEqual(new[]{"8101007800","860100"})&&f.Target.growth.job==120,"81 record/job and explicit refreshed options");
        before=State(f.store.data);Check(manager.Request_CloseClassClient(true).Accepted&&packets.Last()=="880100"&&manager.Request_CloseClassClient(false).Accepted&&packets.Last()=="890100"&&State(f.store.data)==before,"X/confirm88 versus cancel89 are outgoing commands, not local rewards");
        manager.Request_CancelClassClient();Check(manager.Request_OpenClassClient().Accepted&&packets.Last()=="8a0100"&&manager.ClassClientView.token!=token&&manager.Notify_ClassClientPacket(token,Convert.FromHexString("8b010002000300040005000600")).Failure==WorldFailure.StaleRequest,"a new window queries again and cannot accept the old token");
        var unknown=CommanderAdjustmentChecks.CreateFixture(root);var state=unknown.View.commanders.Copy();state.rows[0].nativeRecord=null;Check(unknown.service.ReceiveCommanderRoster(new CommanderRosterPush{id=Id(),approved=true,character=unknown.View.characterId,session=unknown.View.commanderSession,expectedRevision=unknown.View.commanders.revision,state=state}).Accepted,"explicit unknown native primary record");var missing=new LocalServerManager(unknown.service);int sends=0;missing.Notify_ClassAdjustmentPacket+=_=>sends++;Check(!missing.Request_OpenClassClient().Accepted&&!missing.Request_CloseClassClient(true).Accepted&&sends==0,"unknown native record never silently becomes primary0");
        foreach(string action in new[]{"logout","cancel","disable","close-client"})
        {
            var stale=CommanderAdjustmentChecks.CreateFixture(root);stale.Bind(1);stale.Stats(StatAdjustmentOperation.State);var bridge=new LocalServerManager(stale.service);int approvals=0;bridge.StatAdjustmentAuthority=(r,v)=>{approvals++;return stale.StatNotice(r);};string saved=State(stale.store.data);Check(bridge.Request_OpenClassClient().Accepted,"explicit window before invalidated step");
            bridge.Notify_ClassAdjustmentPacket+=_=>{if(action=="logout")stale.client.Logout();else if(action=="cancel")stale.service.CancelStatAdjustment();else if(action=="close-client")bridge.Request_CancelClassClient();else bridge.isActiveAndEnabled=false;};
            Check(!bridge.Request_AdjustStat(0,1,Id()).Accepted&&approvals==0&&State(stale.store.data)==saved,"synchronous "+action+" cannot approve/persist old82");
        }
        foreach(string action in new[]{"logout","disable","close-client"})
        {
            var fixture=CommanderAdjustmentChecks.CreateFixture(root);fixture.Bind(1);var bridge=new LocalServerManager(fixture.service);Check(bridge.Request_OpenClassClient().Accepted,"magic lifecycle window");var v=bridge.ClassClientView;string saved=State(fixture.store.data);int typed=0;bridge.Notify_ClassMagicRequested+=_=>typed++;
            Check(bridge.Notify_ClassMagicState(v.token,new NativeClassMagicState{available=new[]{101},owned=Array.Empty<int>(),points=1}).Accepted,"explicit learnable fixture");v=bridge.ClassClientView;
            bridge.Notify_ClassAdjustmentPacket+=_=>{if(action=="logout")fixture.client.Logout();else if(action=="disable")bridge.isActiveAndEnabled=false;else bridge.Request_CancelClassClient();};
            Check(!bridge.Request_ClassMagicButton(2,0,0,v.token,v.magicRevision,Id()).Accepted&&typed==0&&State(fixture.store.data)==saved,"synchronous "+action+" blocks later typed callback and never grants magic");
        }
        var dropped=CommanderAdjustmentChecks.CreateFixture(root);dropped.Bind(1);var control=new LocalServerManager(dropped.service);control.ClassOptionsAuthority=(r,v)=>new ClassOptionsNotice{id=Id(),request=r.Copy(),approved=true,classPoints=1,right=new ClassCandidate{job=120},options=Array.Empty<ClassCandidate>()};before=State(dropped.store.data);Check(control.Request_OpenClassClient().Accepted,"explicit window before invalidated86");control.Notify_ClassAdjustmentPacket+=_=>dropped.client.Logout();Check(!control.Request_ClassOptions().Accepted&&State(dropped.store.data)==before,"logout on86 after typed bootstrap cannot apply stale options or throw");
    }
}
