using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;
internal static class NativeUnitEntry{public static void Main(string[] args)=>MagicProjectileChecks.RunNativeUnitChecks(args[0]);}
internal static partial class MagicProjectileChecks
{
    private static NativeUnitPacket UnitPacket(TrainingUnit unit,int kind=0x51,uint xp=123)
    {
        var w=kind==0x50?new short[16]:Enumerable.Repeat((short)-1,45).ToArray();
        w[0]=(short)unit.id;w[1]=(short)(unit.id>=400?8:0);w[2]=(short)unit.job;w[3]=-1;
        int attack=unit.effectiveStats?.attack??60,defense=unit.effectiveStats?.defense??20;
        if(kind==0x50)
        {w[4]=0;w[5]=-123;w[6]=(short)unit.maxHp;w[7]=(short)unit.hp;w[8]=(short)unit.maxAp;w[9]=(short)unit.ap;w[10]=(short)attack;w[11]=(short)defense;w[12]=5;w[13]=1;w[14]=(short)unit.x;w[15]=(short)unit.y;}
        else
        {w[4]=3;w[5]=0;w[6]=-123;w[7]=(short)unit.maxHp;w[8]=(short)unit.hp;w[9]=(short)unit.maxMp;w[10]=(short)unit.mp;w[11]=3;w[12]=11;w[13]=12;w[14]=(short)unit.maxAp;w[15]=(short)unit.ap;w[16]=(short)attack;w[17]=(short)defense;w[18]=5;w[19]=1;w[20]=(short)unit.x;w[21]=(short)unit.y;w[22]=101;w[23]=301;w[30]=10101;w[31]=21101;}
        var flags=OriginalBattleTicks.CommandFlags(255);
        return new NativeUnitPacket{kind=kind,words=w,values=kind==0x50?new uint[]{0x80000081,flags}:new uint[]{1000,xp,0x80000081,flags},className=System.Text.Encoding.ASCII.GetBytes("Native class"),name=kind==0x50?Array.Empty<byte>():System.Text.Encoding.ASCII.GetBytes("Native name")};
    }
    private static NativeBattleUnitsNotice UnitBatch(Fixture f,params NativeUnitPacket[] packets)=>new NativeBattleUnitsNotice{id=Id(),approved=true,character=f.View.characterId,session=f.View.campaignSession,
        expectedRevision=f.View.battle?.revision??0,expectedGrowthRevision=f.View.record.growth?.revision,packets=packets.Select(p=>new NativeUnitDelivery{life=1,packet=OriginalNativeUnits.Packet(p)}).ToArray()};
    private static void CheckUnitCodec()
    {
        var unit=new TrainingUnit{id=0,job=100,x=10,y=7,hp=17,maxHp=200,mp=3,maxMp=65,ap=7,maxAp=20};
        foreach(int kind in new[]{0x50,0x51})foreach(int length in new[]{0,1,255})
        {
            var p=UnitPacket(unit,kind,uint.MaxValue);p.className=Enumerable.Repeat((byte)65,length).ToArray();if(kind==0x51)p.name=Enumerable.Repeat((byte)66,length).ToArray();
            var bytes=OriginalNativeUnits.Packet(p);Check(OriginalNativeUnits.TryDecode(bytes,out var read)&&OriginalNativeUnits.Packet(read).SequenceEqual(bytes),"native unit roundtrip preserves ignored WORDs, DWORDs and byte-counted strings");
            Check(read.State().Valid&&read.State().fields[41]==0&&read.State().fields[42]==0,"native CString handles are not copied as host pointers");
            foreach(int cut in Enumerable.Range(0,bytes.Length))Check(!OriginalNativeUnits.TryDecode(bytes.Take(cut).ToArray(),out _),"every truncated unit application message is rejected");
            Check(!OriginalNativeUnits.TryDecode(bytes.Concat(new byte[]{0}).ToArray(),out _),"native unit trailing data is not silently swallowed");
            var state=read.State();var copy=state.Copy();copy.fields[0]=999;if(copy.className.Length>0)copy.className[0]=0;
            Check(state.fields[0]==0&&(state.className.Length==0||state.className[0]==65),"decoded current unit state owns its numeric and text storage");
        }
        var slots=UnitPacket(unit);slots.words[22]=0;slots.words[23]=-1;slots.words[24]=301;
        Check(slots.State().Slots(false).SequenceEqual(new[]{0}),"original spell lists stop at -1 and retain zero, never reinterpret zero as terminator");
        var invalid=UnitPacket(unit);invalid.words[0]=-1;Check(OriginalNativeUnits.Packet(invalid)==null,"negative native array index cannot be encoded");
    }
    private static void CheckUnitIngress(bool scenario)
    {
        var f=LifecycleFixture(scenario);var original=f.View.record.Copy();var caster=UnitPacket(f.View.battle.Commander);caster.words[16]=77;caster.words[17]=44;caster.values[1]=456;
        var npc=UnitPacket(f.View.battle.Unit(400));npc.words[16]=90;npc.words[17]=42;var incoming=UnitBatch(f,npc,caster);
        var manager=new LocalServerManager(f.server);int events=0;manager.Notify_BattleChanged+=_=>events++;
        Check(manager.Notify_BattleUnitPackets(incoming).Accepted&&events==1,"manager accepts one atomic mixed unit snapshot in training/scenario "+scenario);
        Check(f.View.battle.Commander.effectiveStats.attack==77&&f.View.battle.Unit(400).effectiveStats.defense==42&&f.View.record.growth.experience==456,"native stats and current XP are read from their actual fields");
        Check(f.View.record.level==original.level&&f.View.record.growth.classLevel==original.growth.classLevel&&f.View.battle.Commander.nativeInfo.Level==3,"single battle display level never invents profile class/base levels");
        Check(f.View.record.growth.magic.SequenceEqual(original.growth.magic)&&f.View.record.growth.skills.SequenceEqual(original.growth.skills)&&f.View.battleMagic.SequenceEqual(new[]{101,301})&&f.View.battleSkills.SequenceEqual(new[]{10101,21101}),"received battle slots do not grant learned magic or skills");
        Check(f.View.battle.Commander.nativeStatusFlags==0x140&&f.View.battle.Commander.nativeAppearanceJob==100,"MagicFlag input permutation and negative appearance fallback follow original code");
        var before=State(f.View.record);Check(!manager.Notify_BattleUnitPackets(incoming).Accepted&&State(f.View.record)==before,"unit snapshot replay applies no duplicate state");
        incoming.packets[0].packet[0]=0;Check(f.View.battle.Unit(400).nativeInfo.Attack==90,"receiver owns source packet and state copies");
        var shortUnit=UnitPacket(f.View.battle.Commander,0x50);shortUnit.words[7]=16;var shortNotice=UnitBatch(f,shortUnit);
        Check(f.server.ReceiveBattleUnitPackets(shortNotice).Accepted&&f.View.record.mp==original.mp&&f.View.record.maxMp==original.maxMp&&f.View.record.growth.experience==456,"partial50 does not zero owner MP/XP/profile values");
        Check(f.View.battle.Commander.nativeInfo.Mp==0&&f.View.battle.Commander.nativeInfo.MaxMp==0&&f.View.battle.Commander.nativeInfo.Level==0&&f.View.battleMagic.Length==0,"native partial presentation retains its missing-field zeros and empty spell slots");
        var reappeared=UnitBatch(f,shortUnit);reappeared.packets[0].life=2;
        Check(f.server.ReceiveBattleUnitPackets(reappeared).Accepted&&f.View.record.mp==original.mp&&f.View.record.maxMp==original.maxMp,"partial50 new owner lifetime still cannot erase known profile MP");
        var xp=new NativeBattleNotice{id=Id(),character=f.View.characterId,session=f.View.campaignSession,approved=true,unitLife=f.View.battle.Commander.life,expectedRevision=f.View.battle.revision,expectedGrowthRevision=f.View.record.growth.revision,packet=OriginalBattleExperience.Packet(0,789)};
        Check(f.server.ReceiveBattlePacket(xp).Accepted&&f.View.battle.Commander.nativeInfo.Experience==789,"later53 XP setter updates the current unit cache");
        var otherXp=NativeNotice(f,OriginalBattleExperience.Packet(400,uint.MaxValue),400);
        Check(f.server.ReceiveBattlePacket(otherXp).Accepted&&f.View.battle.Unit(400).nativeInfo.Experience==uint.MaxValue&&f.View.record.growth.experience==789,"non-owner53 replaces its cached XP DWORD without granting owner XP");
        Check(f.server.ReceiveBattlePacket(NativeNotice(f,OriginalBattleFeedback.Packet(BattleFeedbackKind.Mana,0,2),0)).Accepted&&f.View.battle.Commander.nativeInfo.Mp==2&&f.View.battle.Commander.nativeInfo.MaxMp==0,"MP setter updates its scalar without inventing a missing maxMP");
        var full=UnitPacket(f.View.battle.Commander);var detailed=UnitBatch(f,full);detailed.packets[0].life=f.View.battle.Commander.life;
        Check(f.server.ReceiveBattleUnitPackets(detailed).Accepted&&f.View.battle.Commander.nativeInfo.MaxMp==original.maxMp,"detailed51 restores the actual received MP range");
        if(scenario)
        {var restored=new EntryAccountService(f.store);new EntryClient(restored).OpenLocalProfile();Check(restored.BeginWorld(f.definition).Accepted&&restored.WorldSnapshot().battle.Commander.nativeInfo.Valid&&restored.WorldSnapshot().battleMagic.SequenceEqual(new[]{101,301}),"scenario reconnect restores copied native unit fields and battle slots");}
    }
    private static void CheckInitialUnitDelivery(bool scenario)
    {
        var f=new Fixture(scenario:scenario,seedUnits:false);Check(f.View.battle==null,"no synthetic roster before actual unit ingress");
        int x=f.View.record.x,y=f.View.record.y;var commander=f.Unit(0,100,x,y);var enemy=f.Unit(400,302,x+1,y);
        var first=UnitBatch(f,UnitPacket(enemy));string before=State(f.View.record);
        Check(!f.server.ReceiveBattleUnitPackets(first).Accepted&&f.View.battle==null&&State(f.View.record)==before,"initial non-owner-only batch waits without a partial session");
        first=UnitBatch(f,UnitPacket(enemy),UnitPacket(commander));
        Check(f.server.ReceiveBattleUnitPackets(first).Accepted&&f.View.battle.units.Length==2&&f.View.battle.Commander.nativeState==0x30&&f.View.battle.Commander.troopOrder==1,"original unit messages create initial commander and monster atomically "+scenario);
        var p=UnitPacket(f.View.battle.Unit(400),0x50);p.words[7]=7;
        Check(f.server.ReceiveBattlePacket(NativeNotice(f,OriginalNativeUnits.Packet(p),400)).Accepted&&f.View.battle.Unit(400).hp==7,"single-message unified raw50 dispatch updates an existing monster");
        var full=UnitPacket(f.View.battle.Commander);var raw=NativeNotice(f,OriginalNativeUnits.Packet(full),0);raw.expectedGrowthRevision=f.View.record.growth.revision;
        var manager=new LocalServerManager(f.server);int events=0;manager.Notify_BattleChanged+=_=>events++;
        Check(manager.Notify_BattlePacket(raw).Accepted&&events==1,"single-message raw51 uses real manager notification path");
        if(scenario)Check(f.server.ReceiveTrainingUnitPackets(UnitBatch(f,full)).Failure==WorldFailure.InvalidStage,"training-only snapshot route rejects scenario context");
        var spellOnly=UnitBatch(f,full);var accept=f.server.ReceiveBattleUnitPackets(spellOnly);Check(accept.Accepted,"received spell slots stored without implicit cost rules");
        Check(f.server.RequestBattleMagicAt(101,enemy.x,enemy.y,Id()).Failure==WorldFailure.RulesUnavailable,"received spell slot alone cannot invent MP/AP cost or eligible targets");
    }
    private static void CheckMovingUnitSnapshot(int source)
    {
        var f=LifecycleFixture();var end=NativeDestination(f,source,2);var origin=f.View.battle.Unit(source).Tile;
        Check(f.server.ReceiveBattlePacket(MovePacket(f,source,end)).Accepted,"native movement before stat refresh "+source);f.server.AdvanceWorld(80);
        var before=f.View;double x=source==0?before.renderX:before.battle.motions.Single(m=>m.unit==source).x;
        double y=source==0?before.renderY:before.battle.motions.Single(m=>m.unit==source).y;
        var p=UnitPacket(before.battle.Unit(source));p.words[20]=(short)origin.X;p.words[21]=(short)origin.Y;p.words[16]=91;
        Check(f.server.ReceiveBattleUnitPackets(UnitBatch(f,p)).Accepted&&f.View.battle.Unit(source).Tile==end,"same-life reset retains approved live destination "+source);
        var after=f.View;Check((source==0?after.renderX:after.battle.motions.Single(m=>m.unit==source).x)==x&&(source==0?after.renderY:after.battle.motions.Single(m=>m.unit==source).y)==y,"same-life reset never snaps the moving render position "+source);
        var replacement=UnitBatch(f,p);replacement.packets[0].life=2;
        Check(f.server.ReceiveBattleUnitPackets(replacement).Accepted&&f.View.battle.Unit(source).life==2,"new entity lifetime replaces prior moving actor "+source);
        Check(source==0?!f.View.moving:f.View.battle.motions.All(m=>m.unit!=source),"old route cannot move replacement entity "+source);
        f.server.AdvanceWorld(100000);Check(f.View.battle.Unit(source).Tile==origin,"replacement remains at its received tile "+source);
        var removed=LifecycleFixture();end=NativeDestination(removed,source,2);origin=removed.View.battle.Unit(source).Tile;
        Check(removed.server.ReceiveBattlePacket(MovePacket(removed,source,end)).Accepted,"movement before runtime removal");
        Check(removed.server.ReceiveBattleLifecycle(LifecycleNotice(removed,BattleLifecycleKind.Remove,source)).Accepted,"native removal does not invent a fresh lifetime");
        p=UnitPacket(removed.View.battle.Unit(source));p.words[20]=(short)origin.X;p.words[21]=(short)origin.Y;
        Check(removed.server.ReceiveBattleUnitPackets(UnitBatch(removed,p)).Accepted&&removed.View.battle.Unit(source).Tile==origin,"removed record reset uses received tile rather than old destination");
        Check(source==0?!removed.View.moving:removed.View.battle.motions.All(m=>m.unit!=source),"inactive prior actor cannot resume its discarded native path");
    }
    private static void CheckUnitFailures()
    {
        foreach(string failure in new[]{"life","session","revision","growth","missing-growth","duplicate","outside","job","length","cache"})
        {
            var f=LifecycleFixture();var p=UnitPacket(f.View.battle.Commander);var n=UnitBatch(f,p);string before=State(f.View.record);long revision=f.View.battle.revision;
            if(failure=="life")n.packets[0].life=0;if(failure=="session")n.session=Id();if(failure=="revision")n.expectedRevision++;
            if(failure=="growth")n.expectedGrowthRevision++;if(failure=="missing-growth")n.expectedGrowthRevision=null;
            if(failure=="duplicate")n.packets=new[]{n.packets[0],n.packets[0].Copy()};
            if(failure=="outside"){p.words[20]=199;n.packets[0].packet=OriginalNativeUnits.Packet(p);}if(failure=="job"){p.words[2]=999;n.packets[0].packet=OriginalNativeUnits.Packet(p);}
            if(failure=="length")n.packets[0].packet=n.packets[0].packet.Take(20).ToArray();
            WorldReply reply;
            if(failure=="cache"){var typed=f.Notice();typed.units[0].nativeInfo=p.State();typed.units[0].nativeInfo.fields=new int[1];reply=f.server.ReceiveBattleUnits(typed);}
            else reply=f.server.ReceiveBattleUnitPackets(n);
            Check(!reply.Accepted&&State(f.View.record)==before&&f.View.battle.revision==revision,"bad native unit snapshot is atomic "+failure);
        }
        var durable=LifecycleFixture(true);var packet=UnitBatch(durable,UnitPacket(durable.View.battle.Commander));string old=State(durable.View.record);durable.store.fail=true;
        Check(durable.server.ReceiveBattleUnitPackets(packet).Failure==WorldFailure.StorageUnavailable&&State(durable.View.record)==old&&durable.View.battle.Commander.nativeInfo==null,"native unit save failure leaves no partial metadata/stats");
        durable.store.fail=false;Check(durable.server.ReceiveBattleUnitPackets(packet).Accepted,"same native unit batch retries after storage recovers");
        var moved=LifecycleFixture();var correction=UnitPacket(moved.View.battle.Commander);correction.words[20]=10;correction.words[21]=8;
        Check(moved.server.ReceiveBattleUnitPackets(UnitBatch(moved,correction)).Accepted&&moved.View.record.x==10&&moved.View.record.y==8,"authoritative stationary unit snapshot can correct commander coordinates");
    }
    private static void CheckUnitFilePersistence()
    {
        var f=LifecycleFixture(true);var p=UnitPacket(f.View.battle.Commander,0x51,uint.MaxValue);p.className=new byte[]{0xb0,0xcb,0xbb,0xe7};
        Check(f.server.ReceiveBattleUnitPackets(UnitBatch(f,p)).Accepted,"uint32 maximum and CP949 class bytes accepted before disk roundtrip");
        string folder=Path.Combine(Path.GetTempPath(),"native-unit-disk-"+Id()),path=Path.Combine(folder,"accounts.json");
        try
        {
            var disk=new LocalAccountFileStore(path);Check(disk.TryWrite(f.store.data),"write separate temporary native unit save");
            Check(new LocalAccountFileStore(path).TryRead(out var document),"read separate native unit file with a new store");
            var saved=document.accounts.Single().character.world.scenarioBattle.units.Single(u=>u.id==0);
            Check(saved.nativeInfo.Valid&&saved.nativeInfo.Experience==uint.MaxValue&&saved.nativeInfo.className.SequenceEqual(p.className)&&saved.nativeInfo.Slots(false).SequenceEqual(new[]{101,301}),"DataContract preserves unsigned DWORD, CP949 bytes and numeric slots");
            var service=new EntryAccountService(new LocalAccountFileStore(path));Check(new EntryClient(service).OpenLocalProfile()&&service.BeginWorld(f.definition).Accepted,"fresh account service restores serialized unit snapshot");
            var view=service.WorldSnapshot();Check(view.battle.Commander.nativeInfo.Experience==uint.MaxValue&&view.record.growth.experience==uint.MaxValue&&view.battleSkills.SequenceEqual(new[]{10101,21101}),"restored profile and received battle cache remain separate and consistent");
            var exposed=view.battle.Commander.nativeInfo;exposed.fields[8]=0;exposed.className[0]=0;
            Check(service.WorldSnapshot().battle.Commander.nativeInfo.Hp==200&&service.WorldSnapshot().battle.Commander.nativeInfo.className[0]==0xb0,"published view cannot mutate live or persisted native state");
        }
        finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
    }
    private static void CheckRemovedUnitReset()
    {
        foreach(bool active in new[]{false,true})foreach(bool enabled in new[]{false,true})foreach(uint state in new uint[]{0,0x10,0x80})
        {
            var f=LifecycleFixture();var prior=f.Notice();var actor=prior.units.Single(u=>u.id==1);actor.nativeActive=active;actor.nativeEnabled=enabled;actor.nativeState=state;actor.troopOrder=2;
            Check(f.server.ReceiveBattleUnits(prior).Accepted,"explicit prior runtime flags for reset");
            var p=UnitPacket(f.View.battle.Unit(1));
            Check(f.server.ReceiveBattleUnitPackets(UnitBatch(f,p)).Accepted,"reset accepts prior removed/disabled record");
            actor=f.View.battle.Unit(1);Check(actor.nativeActive==true&&actor.nativeEnabled==true&&actor.nativeState==(enabled?state:0x30u)&&actor.troopOrder==(enabled?2:1),"reset active assignment precedes enabled test, matching original prefix");
        }
    }
    private static NativeBattleNotice TeamNotice(Fixture f,params NativeBattleTeamName[] rows)=>new NativeBattleNotice{id=Id(),character=f.View.characterId,session=f.View.campaignSession,approved=true,
        expectedRevision=f.View.battle?.revision??0,expectedTeamRevision=f.View.battleTeamRevision,packet=OriginalBattleTeamNames.Packet(rows)};
    private static void CheckBattleTeamNames(bool scenario)
    {
        var f=new Fixture(scenario:scenario,seedUnits:false);string before=State(f.View.record);
        var rows=new[]{new NativeBattleTeamName{team=0,name=new byte[]{0xb0,0xcb,0xbb,0xe7}},new NativeBattleTeamName{team=8,name=System.Text.Encoding.ASCII.GetBytes("Opponent")}};
        var initial=TeamNotice(f,rows);var manager=new LocalServerManager(f.server);int events=0;manager.Notify_BattleChanged+=_=>events++;
        f.store.fail=true;Check(manager.Notify_BattlePacket(initial).Accepted&&events==1&&f.View.battle==null&&f.View.battleTeamRevision==1,"team names can arrive before units, without inventing a combat session or profile save");
        Check(State(f.View.record)==before&&f.View.battleTeams.Length==2,"team-name presentation notice never renames/grants permanent account data");
        initial.packet[0]=0;rows[0].name[0]=0;
        Check(f.View.battleTeams[0].name[0]==0xb0,"team receiver owns both wire and exposed table copies");
        var exposed=f.View.battleTeams;exposed[0].name[0]=0;Check(f.View.battleTeams[0].name[0]==0xb0,"published team table cannot mutate live state");
        f.store.fail=false;
        var header=UnitPacket(f.Unit(0,100,f.View.record.x,f.View.record.y)).State();header.fields[24]=0;
        Check(OriginalBattleTeamNames.DisplayName(header,f.View.battleTeams).SequenceEqual(new byte[]{0xb0,0xcb,0xbb,0xe7}),"received matching team name overrides per-unit fallback");
        header.fields[24]=7;Check(OriginalBattleTeamNames.DisplayName(header,f.View.battleTeams).SequenceEqual(header.name),"unlisted team retains received per-unit fallback");
        var duplicate=TeamNotice(f,new NativeBattleTeamName{team=7,name=Array.Empty<byte>()},new NativeBattleTeamName{team=7,name=new byte[]{65}});
        Check(f.server.ReceiveBattlePacket(duplicate).Accepted&&OriginalBattleTeamNames.DisplayName(header,f.View.battleTeams).Length==0,"first matching team row wins even when its name is empty");
        before=State(f.View.record);long revision=f.View.battleTeamRevision;
        Check(!f.server.ReceiveBattlePacket(duplicate).Accepted&&f.View.battleTeamRevision==revision&&State(f.View.record)==before,"team-name replay is rejected without a profile/battle revision change");
        foreach(string invalid in new[]{"context","revision","table-revision","missing-table-revision","length","approval"})
        {
            var n=TeamNotice(f);if(invalid=="context")n.session=Id();if(invalid=="revision")n.expectedRevision++;
            if(invalid=="table-revision")n.expectedTeamRevision++;if(invalid=="missing-table-revision")n.expectedTeamRevision=null;
            if(invalid=="length")n.packet=n.packet.Take(3).ToArray();if(invalid=="approval")n.approved=false;
            Check(!f.server.ReceiveBattlePacket(n).Accepted&&f.View.battleTeamRevision==revision&&State(f.View.record)==before,"malformed/stale team table has no partial effect "+invalid);
        }
        Check(f.server.ReceiveBattlePacket(TeamNotice(f)).Accepted&&f.View.battleTeams.Length==0,"empty team table explicitly replaces previous received names");
        if(scenario)
        {var restored=new EntryAccountService(f.store);new EntryClient(restored).OpenLocalProfile();Check(restored.BeginWorld(f.definition).Accepted&&restored.WorldSnapshot().battleTeams.Length==0&&restored.WorldSnapshot().battleTeamRevision==0,"session-only team names are not fabricated from saved account/party ownership");}
        var live=LifecycleFixture(scenario);Check(live.server.RequestBattleAction(TrainingAction.Rest,-1,0,0,Id()).Accepted,"pending action before display-only roster update");
        string pending=live.View.battle.pending.id;long battleRevision=live.View.battle.revision;
        Check(live.server.ReceiveBattlePacket(TeamNotice(live,new NativeBattleTeamName{team=0,name=new byte[]{65}})).Accepted&&live.View.battle.pending.id==pending&&live.View.battle.revision==battleRevision,"team name update does not cancel an action or simulate battle outcomes");
        if(!scenario)
        {
            var nested=LifecycleFixture();var host=new LocalServerManager(nested.server);bool again=true;int trainingEvents=0;
            host.Notify_BattleChanged+=_=>{if(again){again=false;host.Notify_BattlePacket(TeamNotice(nested,new NativeBattleTeamName{team=0,name=new byte[]{66}}));}};
            host.Notify_TrainingChanged+=_=>trainingEvents++;
            Check(host.Notify_BattlePacket(TeamNotice(nested,new NativeBattleTeamName{team=0,name=new byte[]{65}})).Accepted&&nested.View.battleTeamRevision==2&&trainingEvents==1,"nested new name snapshot suppresses stale outer training notification");
        }
    }
    private static void CheckTeamCodec()
    {
        foreach(int count in new[]{0,1,9,10,21})
        {
            var rows=Enumerable.Range(0,count).Select(i=>new NativeBattleTeamName{team=i-10,name=Enumerable.Repeat((byte)(65+i%26),i==0?39:i%5).ToArray()}).ToArray();
            var packet=OriginalBattleTeamNames.Packet(rows);
            Check(OriginalBattleTeamNames.TryDecode(packet,out var read)&&OriginalBattleTeamNames.Packet(read).SequenceEqual(packet),"native team table roundtrip retains signed IDs and byte-counted names");
            foreach(int cut in Enumerable.Range(0,packet.Length))Check(!OriginalBattleTeamNames.TryDecode(packet.Take(cut).ToArray(),out _),"truncated variable team list rejects all partial rows");
            Check(!OriginalBattleTeamNames.TryDecode(packet.Concat(new byte[]{0}).ToArray(),out _),"team table trailing bytes are not consumed silently");
        }
        Check(OriginalBattleTeamNames.Packet(new[]{new NativeBattleTeamName{team=0,name=new byte[40]}})==null,"team names never reproduce original fixed-buffer overflow");
        Check(!OriginalBattleTeamNames.TryDecode(new byte[]{0x5c,0x60,255,255},out _)&&!OriginalBattleTeamNames.TryDecode(new byte[]{0x5c,0x60,22,0},out _),"negative/oversized team count rejected");
    }
    private static void CheckUnitMagicReset()
    {
        foreach(int kind in new[]{0x50,0x51})foreach(int reset in new[]{0,1,400})
        {
            var f=LifecycleFixture(true);Check(f.server.ReceiveBattlePacket(MagicPacket(f,400,601,1)).Accepted,"explicit native spell creates independent caster/target slots before reset");
            var effects=f.View.battle.magicEffects;Check(effects.Length==2,"one received caster slot and one target slot");
            var p=UnitPacket(f.View.battle.Unit(reset),kind);var notice=UnitBatch(f,p);string state=State(f.View.record);f.store.fail=true;
            Check(f.server.ReceiveBattleUnitPackets(notice).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==state&&f.View.battle.magicEffects.Select(e=>e.id).SequenceEqual(effects.Select(e=>e.id)),"failed unit reset cannot partially remove live magic slots");
            f.store.fail=false;Check(f.server.ReceiveBattleUnitPackets(notice).Accepted,"same native unit reset retries after durable failure");
            var remaining=f.View.battle.magicEffects;
            Check(reset==0?remaining.Length==2:reset==400?remaining.Length==1&&remaining[0].role!=BattleMagicEffectRole.ChargeOnly&&remaining[0].targetUnit==1:remaining.Length==1&&remaining[0].role==BattleMagicEffectRole.ChargeOnly&&remaining[0].sourceUnit==400,"raw reset clears only the updated unit's native caster or target slot "+reset);
        }
    }
    private static void CheckUnknownOwnerHealth()
    {
        var f=new Fixture(scenario:true,seedUnits:false);f.store.data.accounts.Single().character.world.healthKnown=false;
        f.server=new EntryAccountService(f.store);Check(new EntryClient(f.server).OpenLocalProfile()&&f.server.BeginWorld(f.definition).Accepted&&f.View.battle==null&&!f.View.record.healthKnown,"restore explicit unknown owner health without creating a synthetic battle");
        var unit=f.Unit(0,100,f.View.record.x,f.View.record.y);string before=State(f.View.record);
        Check(f.server.ReceiveBattleUnitPackets(UnitBatch(f,UnitPacket(unit,0x50))).Failure==WorldFailure.RulesUnavailable&&State(f.View.record)==before&&f.View.battle==null,"initial partial50 cannot turn absent owner MP information into confirmed zero");
        Check(f.server.ReceiveBattleUnitPackets(UnitBatch(f,UnitPacket(unit,0x51))).Accepted&&f.View.record.healthKnown&&f.View.record.mp==45&&f.View.record.maxMp==65,"detailed51 supplies the missing explicit owner HP/MP range");
    }
    private static void CheckFullPresentationReset()
    {
        foreach(int kind in new[]{0x50,0x51})foreach(string operation in new[]{"hp","mp","miss","attack","death","hp-death"})
        {
            var f=LifecycleFixture(true);
            if(operation=="hp"||operation=="hp-death")Check(f.server.ReceiveBattlePacket(NativeNotice(f,OriginalBattleFeedback.Packet(BattleFeedbackKind.Health,0,operation=="hp-death"?0:17),0)).Accepted,"explicit numeric event before native unit reset");
            if(operation=="mp")Check(f.server.ReceiveBattlePacket(NativeNotice(f,OriginalBattleFeedback.Packet(BattleFeedbackKind.Mana,0,3),0)).Accepted,"explicit MP event before native unit reset");
            if(operation=="miss")Check(f.server.ReceiveBattlePacket(NativeNotice(f,OriginalBattleFeedback.Packet(BattleFeedbackKind.Miss,0,0),0)).Accepted,"explicit MISS event before native unit reset");
            if(operation=="attack")Check(f.server.ReceiveBattlePacket(NativeNotice(f,OriginalBattleFeedback.Packet(BattleFeedbackKind.Attack,0,7,400),0,400)).Accepted,"explicit attack pair before native unit reset");
            if(operation=="death"||operation=="hp-death")Check(f.server.ReceiveBattleLifecycle(LifecycleNotice(f,BattleLifecycleKind.Death,0)).Accepted,"explicit death before native unit reset");
            f.server.AdvanceWorld(280);
            var before=f.View;string state=State(before.record),numbers=JsonSerializer.Serialize(before.battle.numberEffects),attacks=JsonSerializer.Serialize(before.battle.attackEffects),deaths=JsonSerializer.Serialize(before.battle.deaths);
            var notice=UnitBatch(f,UnitPacket(before.battle.Commander,kind));f.store.fail=true;
            Check(f.server.ReceiveBattleUnitPackets(notice).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==state
                &&JsonSerializer.Serialize(f.View.battle.numberEffects)==numbers&&JsonSerializer.Serialize(f.View.battle.attackEffects)==attacks&&JsonSerializer.Serialize(f.View.battle.deaths)==deaths,"failed raw snapshot preserves every transient counter "+operation);
            f.store.fail=false;Check(f.server.ReceiveBattleUnitPackets(notice).Accepted,"raw snapshot reset retries after storage recovers");
            var view=f.View;
            if(operation=="hp"||operation=="mp"||operation=="miss"||operation=="hp-death")
                Check(view.battle.numberEffects.Single(e=>e.unit==0).kind==BattleFeedbackKind.Health&&view.battle.numberEffects.Single(e=>e.unit==0).delta==0&&view.battle.numberEffects.Single(e=>e.unit==0).Counter==0,"surviving numeric slot becomes native HP-kind0/delta0/counter0 "+operation);
            if(operation=="attack")
            {
                var contact=view.battle.attackEffects.Single(e=>e.unit==0);Check(contact.Counter==0&&contact.partner==0&&contact.flag==0&&contact.offsetX==0&&contact.offsetY==0&&!contact.ContactVisible,"snapshot resets only the selected attack marker slot");
                Check(JsonSerializer.Serialize(view.battle.attackEffects.Single(e=>e.unit==400))==JsonSerializer.Serialize(before.battle.attackEffects.Single(e=>e.unit==400)),"other actor's attack presentation is unchanged by one unit snapshot");
                Check(f.server.ReceiveBattlePacket(NativeNotice(f,OriginalBattleFeedback.Packet(BattleFeedbackKind.Attack,400,1,0),400,0)).Accepted&&f.View.battle.attackEffects.Single(e=>e.unit==0).flag==0,"later attack does not reuse the reset target's old294 extra-hit flag");
            }
            if(operation=="death"||operation=="hp-death")Check(view.battle.deaths.Single(e=>e.unit==0).Counter==0&&view.battle.deaths.Single(e=>e.unit==0).held==(operation=="hp-death"),"snapshot restarts surviving death counter and numeric hold");
            string resetState=JsonSerializer.Serialize(view.battle.numberEffects)+JsonSerializer.Serialize(view.battle.deaths);
            Check(!f.server.ReceiveBattleUnitPackets(notice).Accepted&&JsonSerializer.Serialize(f.View.battle.numberEffects)+JsonSerializer.Serialize(f.View.battle.deaths)==resetState,"duplicate snapshot cannot restart reset effects twice");
        }
    }
    private static void CheckNativeAiHintRefresh()
    {
        foreach(bool scenario in new[]{false,true})
        {
            var f=Soldiers(scenario,mode:2,hp:40,state:48);var initial=f.Notice();var actor=initial.units.Single(u=>u.id==1);
            actor.nativeActive=actor.nativeEnabled=true;actor.attackTargets=Array.Empty<int>();actor.movementBudget=0;
            Check(f.server.ReceiveBattleUnits(initial).Accepted,"native rest-hint reset setup");
            Check(f.server.AdvanceBattleAutomation((r,v)=>throw new Exception("no packet"),n=>0,0)==null,"native attack-move derives forced-rest hint without a packet");
            var world=(WorldSession)typeof(EntryAccountService).GetField("world",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(f.server);
            Check(world.Combat.AiHints[1].hint==99,"source-derived1FC hint is present");
            Check(f.server.ReceiveBattleUnits(f.Notice()).Accepted&&world.Combat.AiHints[1].hint==99,"ordinary typed notice does not pretend to execute native B0A0");
            var batch=UnitBatch(f,UnitPacket(f.View.battle.Unit(1)));f.store.fail=true;
            Check(f.server.ReceiveBattleUnitPackets(batch).Failure==WorldFailure.StorageUnavailable&&world.Combat.AiHints[1].hint==99,"failed native snapshot retains client hint until commit");f.store.fail=false;
            Check(f.server.ReceiveBattleUnitPackets(batch).Accepted&&!world.Combat.AiHints.ContainsKey(1),"actual51 refresh clears1FC even for the same enabled unit/life");
        }
    }
    private static void CheckReceivedTeamSoldierControl()
    {
        foreach(bool scenario in new[]{false,true})foreach(bool contradictory in new[]{false,true})
        {
            var f=Soldiers(scenario);var packets=f.View.battle.units.Select(u=>
            {var p=UnitPacket(u);p.values[3]=OriginalBattleTicks.CommandFlags(0);return p;}).ToArray();
            Check(f.server.ReceiveBattleUnitPackets(UnitBatch(f,packets)).Accepted,"actual51 supplies AI teams and disabled manual menu bits");
            var n=f.Notice();n.units.First(u=>u.id==0).troopOrder=2;
            foreach(var u in n.units)u.attackTargets=contradictory?Array.Empty<int>():null;
            n.units.First(u=>u.id==1).nativeState=16;
            Check(f.server.ReceiveBattleUnits(n).Accepted,"explicit original auto mode without a fabricated enemy list");
            Check(f.View.battle.Unit(1).canAttack==false&&f.View.battle.Unit(0).canCommand==false,"native menu really disables manual buttons");
            string before=State(f.View.record);
            Check(f.server.RequestBattleSoldierAction(1,Id(),count=>0,5000).Accepted,"actual51 teams determine enemy when legacy list is missing or contradictory");
            var request=f.View.battle.pending;
            Check(request.action==TrainingAction.Attack&&request.target==400&&request.NativePacket[1]==0x82&&State(f.View.record)==before,"received-team AI generates82 without predicting damage or XP");
            Check(f.server.CancelBattleAction(request.id).Accepted,"cancel received-team probe without changing save");
            var same=UnitPacket(f.View.battle.Unit(400));same.words[1]=0;same.values[3]=OriginalBattleTicks.CommandFlags(0);
            Check(f.server.ReceiveBattleUnitPackets(UnitBatch(f,same)).Accepted,"actual51 same-team metadata overrides contradictory enemy IDs");
            Check(!f.server.RequestBattleSoldierAction(1,Id(),count=>0,5000).Accepted,"same-team target is not an enemy because its ID is400");
            var peer=UnitPacket(f.View.battle.Unit(400));peer.words[1]=1;peer.values[3]=OriginalBattleTicks.CommandFlags(0);
            Check(f.server.ReceiveBattleUnitPackets(UnitBatch(f,peer)).Accepted,"actual51 different player team without received alliance context");
            n=f.Notice();foreach(var u in n.units)u.attackTargets=null;Check(f.server.ReceiveBattleUnits(n).Accepted,"no legacy authority supplies missing peer alliance");before=State(f.View.record);
            Check(!f.server.RequestBattleSoldierAction(1,Id(),count=>0,5000).Accepted&&State(f.View.record)==before,"missing alliance stays unavailable instead of enemy or forced rest");
        }
    }
    internal static void RunNativeUnitChecks(string path)
    {root=path;CheckReceivedTeamSoldierControl();CheckNativeAiHintRefresh();CheckUnitCodec();CheckUnitIngress(false);CheckUnitIngress(true);CheckUnitFailures();CheckInitialUnitDelivery(false);CheckInitialUnitDelivery(true);foreach(int source in new[]{0,1,400})CheckMovingUnitSnapshot(source);CheckUnitFilePersistence();CheckRemovedUnitReset();CheckTeamCodec();CheckBattleTeamNames(false);CheckBattleTeamNames(true);CheckUnitMagicReset();CheckUnknownOwnerHealth();CheckFullPresentationReset();Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlayExecuted=false,scope="Actual50/51/60 decoder, atomic unit/service/manager ingress, initial roster, partial versus detailed presentation, profile separation, slots/team names, memory/file persistence, movement/lifetime/numeric/attack/death/effect-slot resets and failure rollback; no original server XP/growth/roster rules."}));}
}
