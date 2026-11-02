using System;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;

internal static class BattleFeedbackEntry {public static void Main(string[] args)=>MagicProjectileChecks.RunBattleFeedbackChecks(args[0]);}
internal static partial class MagicProjectileChecks
{
    private static BattleFeedbackNotice FeedbackNotice(Fixture f,BattleFeedbackKind kind,int unit,int value,int target=-1)
        =>new BattleFeedbackNotice{id=Id(),character=f.View.characterId,session=f.View.campaignSession,expectedRevision=f.View.battle.revision,unitLife=f.View.battle.Unit(unit)?.life??0,targetLife=target<0?0:f.View.battle.Unit(target)?.life??0,approved=true,packet=OriginalBattleFeedback.Packet(kind,unit,value,target)};
    private static void CheckFeedbackCodec()
    {
        foreach(var kind in new[]{BattleFeedbackKind.Health,BattleFeedbackKind.Mana,BattleFeedbackKind.Attack})
        foreach(int unit in new[]{0,1,999,2000,2999})foreach(int value in new[]{-32768,-1,0,1,32767})
        {
            int target=kind==BattleFeedbackKind.Attack?400:-1;var bytes=OriginalBattleFeedback.Packet(kind,unit,value,target);
            Check(OriginalBattleFeedback.TryDecode(bytes,out var p)&&p.kind==kind&&p.unit==unit&&p.target==target&&p.value==value,"signed native feedback WORD roundtrip");
            Check(bytes.Length==(kind==BattleFeedbackKind.Attack?8:6),"strict application-packet length");
        }
        var attack=OriginalBattleFeedback.Packet(BattleFeedbackKind.Attack,1,123,400);
        Check(attack.SequenceEqual(new byte[]{0x5c,0x82,0x90,1,1,0,123,0}),"attack WORD order is target/attacker/raw flag");
        foreach(var bytes in new[]{Array.Empty<byte>(),new byte[]{0x5c},new byte[]{0x5c,5,0,0,0},new byte[]{0x5c,6,0,0,0,0,0},new byte[]{0x5c,5,255,255,0,0},new byte[]{0x5c,0x82,0,0,0,0,0,0},new byte[]{0x5c,0x52,0,0,0,0}})
            Check(!OriginalBattleFeedback.TryDecode(bytes,out _),"truncated/extra/wrong-scope/invalid ID packets refused");
        foreach(int unit in new[]{0,1,999,2999})Check(OriginalBattleFeedback.TryDecode(OriginalBattleFeedback.Packet(BattleFeedbackKind.Miss,unit,0),out var miss)&&miss.kind==BattleFeedbackKind.Miss&&miss.unit==unit,"native5C0F signed unit WORD to SetFail");
        Check(OriginalBattleFeedback.Packet(BattleFeedbackKind.Miss,0,1)==null&&OriginalBattleFeedback.Packet(BattleFeedbackKind.Health,3000,0)==null&&OriginalBattleFeedback.Packet(BattleFeedbackKind.Mana,0,32768)==null,"MISS has no damage field and WORD values are not silently truncated");
    }
    private static void CheckFeedbackFrames()
    {
        int[] bounce={0,-4,-5,-4,0,1,4,10,4,1,0,1,4,10};
        foreach(var kind in new[]{BattleFeedbackKind.Health,BattleFeedbackKind.Mana,BattleFeedbackKind.Miss})
        foreach(int delta in new[]{-30,0,30})for(int counter=0;counter<=28;counter++)
        {
            var e=new BattleNumberEffect{unit=1,life=1,kind=kind,hp=50,maxHp=100,delta=delta,age=counter*40};var frames=BattleFeedbackFrames.Numbers(e,100,200);
            bool floating=counter>=1&&counter<=13,health=kind==BattleFeedbackKind.Health&&counter>=1&&counter<=26;
            Check(e.DeltaVisible==floating&&e.HealthVisible==health&&e.Expired==(counter>=27),"native floating1..13, HP readout1..26, update27 clears");
            if(!floating&&!health)Check(frames.Length==0,"silent initial/expired numeric phases");
            if(floating)
            {
                var moving=frames.Last();Check(moving.y==200+bounce[counter]+10&&moving.height==(kind==BattleFeedbackKind.Miss?11:14),"native common numeric/MISS bounce and size");
                if(kind==BattleFeedbackKind.Miss)Check(moving.atlasX==1&&moving.atlasY==447&&moving.width==36,"single native MISS image");
                else Check(moving.atlasY==418+e.DeltaRow*18,"signed HP or MP row, no critical-color invention");
            }
            if(health)Check(e.DisplayHealth==50-delta*(20-Math.Max(10,Math.Min(20,counter)))/10,"native old-to-new absolute HP number interpolation");
        }
        foreach(int hp in new[]{29,30,70,71})
        {var e=new BattleNumberEffect{kind=BattleFeedbackKind.Health,hp=hp,maxHp=100,age=40};Check(e.HealthRow==(hp<30?0:hp>70?2:1),"HP color boundaries preserve equality");}
        Check(OriginalBattleFeedback.DigitCell('0')==10&&OriginalBattleFeedback.DigitCell('+')==11&&OriginalBattleFeedback.DigitCell('-')==12&&OriginalBattleFeedback.DigitCell('9')==9,"native digit/sign atlas cells");
        for(int dx=-3;dx<=3;dx++)for(int dy=-3;dy<=3;dy++)
        {
            if(dx==0&&dy==0)continue;var a=new TrainingUnit{id=0,life=1,x=5,y=5,direction=1};var b=new TrainingUnit{id=400,life=2,x=5+dx,y=5+dy,direction=2};var pair=BattleAttackEffect.Create(a,b,1);bool close=Math.Abs(dx)+Math.Abs(dy)<3;
            Check(pair[0].offsetX==(close?dx*16:0)&&pair[1].offsetX==-pair[0].offsetX&&pair[1].offsetY==-pair[0].offsetY,"native opposed half-tile contact offsets only below distance3");
            Check(pair[0].ContactVisible==close&&pair[1].ContactVisible,"far attacker only marker, target contact remains");
            for(int counter=0;counter<=49;counter++)
            {
                foreach(var e in pair){e.age=counter*40;var frames=BattleFeedbackFrames.Attack(e,160,160);Check(e.Frame==Math.Min(49,counter)/2%4&&e.Expired==(counter>=49),"native four contact frames and49-update termination");if(counter>=49)Check(frames.Length==0,"expired contact no orphan frame");}
            }
        }
    }
    private static void CheckFeedbackIngress(bool scenario)
    {
        var f=Members(scenario);var state=State(f.View.record);var attack=FeedbackNotice(f,BattleFeedbackKind.Attack,1,2,400);var hp=f.View.battle.units.Select(u=>u.hp).ToArray();
        Check(f.server.ReceiveBattleFeedback(attack).Accepted&&f.View.battle.units.Select(u=>u.hp).SequenceEqual(hp),"native attack display causes no guessed damage, MP/AP/XP");
        Check(f.View.battle.attackEffects.Length==2&&f.View.battle.numberEffects.Length==0,"two native actor slots, no inferred MISS/HP event");
        var original=f.View.battle.attackEffects[0].offsetX;f.View.battle.attackEffects[0].offsetX=999;Check(f.View.battle.attackEffects[0].offsetX==original,"attack presentation snapshots are isolated");
        Check(!f.server.ReceiveBattleFeedback(attack).Accepted,"duplicate native display cannot restart counters");
        f.server.AdvanceWorld(80);Check(f.View.battle.attackEffects.All(e=>e.Counter==2),"contact advances without health mutation");
        var receive=FeedbackNotice(f,BattleFeedbackKind.Health,0,175);Check(f.server.ReceiveBattleFeedback(receive).Accepted&&f.View.record.hp==175,"absolute HP packet updates health and atomic record");
        var effect=f.View.battle.numberEffects.Single();Check(effect.delta==-25&&effect.Counter==0&&!effect.HealthVisible,"HP difference from previous value, no count0 picture");
        f.server.AdvanceWorld(40);Check(f.View.battle.numberEffects.Single().DisplayHealth==200,"absolute number starts at previous HP");
        Check(f.server.ReceiveBattleFeedback(FeedbackNotice(f,BattleFeedbackKind.Mana,0,40)).Accepted&&f.View.record.mp==40,"absolute MP packet");
        Check(f.View.battle.numberEffects.Single().kind==BattleFeedbackKind.Mana&&f.View.battle.numberEffects.Single().delta==-5,"MP overwrites the shared numeric slot instead of stacking");
        Check(f.server.ReceiveBattleFeedback(FeedbackNotice(f,BattleFeedbackKind.Health,0,175)).Accepted&&f.View.battle.numberEffects.Single().delta==0,"explicit equal HP still resets native numeric slot; do not invent a hit");
        var silent=f.Notice();silent.units[0].hp=170;f.server.AdvanceWorld(40);var prior=f.View.battle.numberEffects.Single().Counter;
        Check(f.server.ReceiveBattleUnits(silent).Accepted&&f.View.battle.numberEffects.Single().Counter==prior,"ordinary full snapshot does not implicitly make display events");
        var miss=f.Notice();miss.feedbackEvents=new[]{new BattleFeedbackEvent{kind=BattleFeedbackKind.Miss,unit=0,life=f.View.battle.Commander.life}};
        Check(f.server.ReceiveBattleUnits(miss).Accepted&&f.View.battle.numberEffects.Single().kind==BattleFeedbackKind.Miss&&f.View.record.hp==170,"only explicit MISS displays MISS, no invented packet or health change");
        f.server.AdvanceWorld(2000);Check(f.View.battle.numberEffects.Length==0&&f.View.battle.attackEffects.Length==0,"large time step removes expired feedback");
        Check(f.server.ReceiveBattleFeedback(FeedbackNotice(f,BattleFeedbackKind.Attack,400,0,1)).Accepted&&f.View.battle.attackEffects.Single(e=>e.unit==1).flag==2,"native target294 retains its previous source flag across expiry");
        if(scenario)Check(f.server.ReceiveTrainingFeedback(FeedbackNotice(f,BattleFeedbackKind.Health,0,160)).Failure==WorldFailure.InvalidStage,"training-only feedback ingress guard");
    }
    private static void CheckFeedbackFailures()
    {
        foreach(string problem in new[]{"session","revision","character","life","target-life","id","decline","bad-packet","negative-hp","over-max"})
        {
            var f=Members();var n=FeedbackNotice(f,BattleFeedbackKind.Attack,1,1,400);
            switch(problem){case "session":n.session=Id();break;case "revision":n.expectedRevision--;break;case "character":n.character="wrong";break;case "life":n.unitLife++;break;case "target-life":n.targetLife++;break;case "id":n.id="bad";break;case "decline":n.approved=false;break;case "bad-packet":n.packet=new byte[]{0x5c,5};break;case "negative-hp":n=FeedbackNotice(f,BattleFeedbackKind.Health,0,-1);break;case "over-max":n=FeedbackNotice(f,BattleFeedbackKind.Health,0,201);break;}
            string before=State(f.View.record);Check(!f.server.ReceiveBattleFeedback(n).Accepted&&State(f.View.record)==before&&f.View.battle.attackEffects.Length==0&&f.View.battle.numberEffects.Length==0,"bad packet/event has no partial state or presentation "+problem);
        }
        foreach(string problem in new[]{"null","enum","life","target","value","duplicate","attack-life","attack-flag","overlap"})
        {
            var f=Members();var n=f.Notice();var e=new BattleFeedbackEvent{kind=BattleFeedbackKind.Health,unit=0,life=f.View.battle.Commander.life,value=200};n.feedbackEvents=new[]{e};
            switch(problem){case "null":n.feedbackEvents=new BattleFeedbackEvent[]{null};break;case "enum":e.kind=(BattleFeedbackKind)99;break;case "life":e.life++;break;case "target":e.target=400;break;case "value":e.value--;break;case "duplicate":n.feedbackEvents=new[]{e,e.Copy()};break;default:e.kind=BattleFeedbackKind.Attack;e.target=400;e.targetLife=1;e.value=0;if(problem=="attack-life")e.targetLife++;if(problem=="attack-flag")e.value=32768;if(problem=="overlap")n.feedbackEvents=new[]{e,e.Copy()};break;}
            string before=State(f.View.record);Check(!f.server.ReceiveBattleUnits(n).Accepted&&State(f.View.record)==before&&f.View.battle.numberEffects.Length==0,"malformed explicit feedback snapshot "+problem);
        }
        var save=Members(true);var approval=FeedbackNotice(save,BattleFeedbackKind.Health,0,150);string state=State(save.View.record);save.store.fail=true;
        Check(save.server.ReceiveBattleFeedback(approval).Failure==WorldFailure.StorageUnavailable&&State(save.View.record)==state&&save.View.battle.numberEffects.Length==0,"failed disk commit leaves health and presentation untouched");save.store.fail=false;
        Check(save.server.ReceiveBattleFeedback(approval).Accepted,"exact packet retry after storage recovery");var restored=new EntryAccountService(save.store);new EntryClient(restored).OpenLocalProfile();Check(restored.BeginWorld(save.definition).Accepted&&restored.WorldSnapshot().record.hp==150&&restored.WorldSnapshot().battle.numberEffects.Length==0&&restored.WorldSnapshot().battle.attackEffects.Length==0,"reconnect restores approved health without replaying effects");
        var removed=Members();Check(removed.server.ReceiveBattleFeedback(FeedbackNotice(removed,BattleFeedbackKind.Attack,1,0,400)).Accepted,"prepare removal effect");var n2=removed.Notice();n2.units=n2.units.Where(u=>u.id!=400).ToArray();Check(removed.server.ReceiveBattleUnits(n2).Accepted&&!removed.View.battle.attackEffects.Any(e=>e.unit==400),"removed actor drops its own attack slot");
    }
    internal static void RunBattleFeedbackChecks(string path)
    {
        root=path;CheckFeedbackCodec();CheckFeedbackFrames();CheckFeedbackIngress(false);CheckFeedbackIngress(true);CheckFeedbackFailures();
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlayExecuted=false,scope="Original5C05/06 HP/MP and5C82 attack packet ingress; per-unit shared numeric and opposed attack slots, original glyph crops/bounce/counter boundaries, explicit MISS, no inferred damage or full-snapshot effects, atomic storage/replay/lifetime safeguards."}));
    }
}
