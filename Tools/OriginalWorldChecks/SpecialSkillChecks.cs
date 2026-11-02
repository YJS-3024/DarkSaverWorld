using System;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;
using DarkSaver.Original.Tutorial;

internal static class SpecialSkillEntry
{public static void Main(string[] args)=>MagicProjectileChecks.RunSkillChecks(args[0]);}
internal static partial class MagicProjectileChecks
{
    private static Fixture Skill(int id,bool scenario=false)
    {
        var f=new Fixture(scenario:scenario,skills:new[]{id});var n=f.Notice();int x=n.units[0].x,y=n.units[0].y;
        if(scenario)
        {
            n.units[0].magicRules=new[]{new BattleMagicRule{magic=id,enabled=true,mpCost=6,apCost=3,targets=new[]{0,400}}};
            Check(f.server.ReceiveBattleUnits(n).Accepted,"scenario skill permissions");return f;
        }
        var list=new System.Collections.Generic.List<TrainingUnit>{n.units[0],f.Unit(400,302,x+1,y),f.Unit(1,100,x-1,y)};int unit=401;
        for(int dx=-3;dx<=3;dx++)for(int dy=-3;dy<=3;dy++)
            if((dx!=0||dy!=0)&&(dx!=1||dy!=0)&&(dx!=-1||dy!=0)&&x+dx>=0&&y+dy>=0)list.Add(f.Unit(unit++,302,x+dx,y+dy));
        n.units=list.ToArray();n.units[0].magicRules=new[]{new BattleMagicRule{magic=id,enabled=true,mpCost=6,apCost=3,targets=list.Select(u=>u.id).ToArray()}};
        Check(f.server.ReceiveBattleUnits(n).Accepted,"explicit skill roster/cost/permissions "+id);return f;
    }
    private static WorldReply QueueSkill(Fixture f,int id,int direction=-1)
    {
        var m=f.definition.magicMenu.Find(id);var c=f.View.battle.Commander;
        if(BattleSkillArea.Directional(m.distance))return f.server.RequestBattleSkillAt(id,direction,-1,Id());
        if(m.distance==-2||m.distance==-5)return f.server.RequestBattleSkillAt(id,c.x,c.y,Id());
        if(m.distance==-6)return f.server.RequestBattleSkillAt(id,c.x+2,c.y+1,Id());
        return f.server.RequestBattleSkill(id,m.distance==0?0:400,Id());
    }
    internal static void RunSkillChecks(string path)
    {
        root=path;var metadata=Read<MagicMenuCatalog>("Data/Combat/MagicMenu.json");var catalog=Read<MagicProjectileCatalog>("Data/Combat/MagicProjectiles.json");
        var rows=metadata.entries.Where(e=>e.id>=10000&&e.id<20000||e.id>=30000&&e.id<32000).ToArray();
        Check(rows.Length==86&&rows.All(catalog.SupportsSkill)&&rows.All(e=>!catalog.Supports(e)),"86 native menu IDs, not magic-loadout/hire/emotion aliases");
        int casts=0;
        foreach(var entry in rows)foreach(int direction in BattleSkillArea.Directional(entry.distance)?new[]{-1,-2,-3,-4}:new[]{-1})
        {
            var f=Skill(entry.id);string before=State(f.View.record);
            Check(QueueSkill(f,entry.id,direction).Accepted,"skill request "+entry.id+" dir"+direction);casts++;
            var r=f.View.battle.pending;Check(r.action==TrainingAction.UseSkill&&r.magic==entry.id&&State(f.View.record)==before,"no speculative skill effect/cost "+entry.id);
            if(BattleSkillArea.Directional(entry.distance))Check(r.x==direction&&r.y==-1,"direction sentinel preserved");
            var n=MagicResult(f);foreach(int id in n.magicTargets)n.units.Single(u=>u.id==id).nativeState=0x12340000+(uint)id;
            Check(f.server.ReceiveBattleUnits(n).Accepted,"skill outcome "+entry.id);Check(f.View.record.mp==39&&f.View.battle.Commander.ap==17,"single approved cost for all targets "+entry.id);
            if(catalog.Find(entry.pattern).type==16)Check(f.View.battle.magicEffects.All(e=>e.role==BattleMagicEffectRole.ChargeOnly),"cross-trust does not invent target bursts");
            var audio=new MagicSoundPlayback();int steps=0;bool drawn=false;
            while(f.View.battle.magicEffects.Length>0&&steps++<1024)
            {
                f.server.AdvanceWorld(40);var effects=f.View.battle.magicEffects;
                foreach(var frame in BattleMagicPresentation.Frames(effects).Select(p=>p.frame))
                {
                    drawn=true;int w=frame.sourceWidth>0?frame.sourceWidth:frame.width,h=frame.sourceHeight>0?frame.sourceHeight:frame.height;
                    Check(frame.surface>=0&&frame.surface<=1&&w>0&&h>0&&frame.width>0&&frame.height>0&&frame.atlasX>=0&&frame.atlasY>=0&&frame.atlasX+w<=999&&frame.atlasY+h<=(frame.surface==1?999:1098),"skill atlas/size bounds "+entry.id+"/"+direction+"/"+steps);
                }
                Check(audio.Update(effects).All(c=>c.sound>0),"positive sound IDs only");Check(audio.Update(effects).Length==0,"no per-paint sound replay");
            }
            Check(drawn&&steps<1024&&f.View.battle.magicEffects.Length==0,"all skill families terminate "+entry.id);
            Check(!f.server.ReceiveBattleUnits(n).Accepted&&f.View.record.mp==39,"duplicate skill response rejected");
        }
        CheckSkillFailures();CheckSkillGeometry(catalog);CheckSkillEvents(rows);CheckSkillSave();
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,specialSkills=rows.Length,casts,renderFamilies=rows.Select(e=>catalog.Find(e.pattern).type).Distinct().Count(),excludedHireDefinitions=50,excludedEmotionDefinitions=15,unityPlayExecuted=false,scope="All86 native special-menu IDs, four-direction masks and source/target renderers, explicit status/resource authority and persistence. Hire50/emotion15 kept separate. No server formulas or Play/listening/wall-clock claim."}));
    }
    private static void CheckSkillFailures()
    {
        foreach(string bad in new[]{"unowned","no-cost","disabled","mp","ap","direction","direction-y","loadout","missing-list","outside","duplicate","refund","invalid-pattern"})
        {
            int skill=bad=="loadout"?10901:10101;var f=Skill(bad=="unowned"?10901:skill);var n=f.Notice();
            switch(bad)
            {
                case "unowned":n.units[0].magicRules[0].magic=skill;Check(f.server.ReceiveBattleUnits(n).Accepted,"explicit rule does not grant ownership");break;
                case "no-cost":n.units[0].magicRules[0].mpCost=null;Check(f.server.ReceiveBattleUnits(n).Accepted,"missing cost");break;
                case "disabled":n.units[0].magicRules[0].enabled=false;Check(f.server.ReceiveBattleUnits(n).Accepted,"disabled");break;
                case "mp":n.units[0].magicRules[0].mpCost=46;Check(f.server.ReceiveBattleUnits(n).Accepted,"insufficient MP rule");break;
                case "ap":n.units[0].magicRules[0].apCost=21;Check(f.server.ReceiveBattleUnits(n).Accepted,"insufficient AP rule");break;
            }
            string before=State(f.View.record);
            if(bad=="unowned"||bad=="no-cost"||bad=="disabled"||bad=="mp"||bad=="ap")Check(!QueueSkill(f,skill).Accepted&&State(f.View.record)==before,"skill request rejection atomic "+bad);
            else if(bad=="direction"||bad=="direction-y")Check(!f.server.RequestBattleSkillAt(skill,bad=="direction"?-5:-1,bad=="direction"?-1:0,Id()).Accepted&&State(f.View.record)==before,"no invalid directional encoding");
            else if(bad=="loadout")Check(f.server.RequestBattleMagic(skill,0,Id()).Failure==WorldFailure.RulesUnavailable&&QueueSkill(f,skill).Accepted,"skill owns separate action, no magic loadout required");
            else
            {
                Check(QueueSkill(f,skill).Accepted,"invalid response queued");n=MagicResult(f);
                switch(bad){case "missing-list":n.magicTargets=null;break;case "outside":n.magicTargets=new[]{999};break;case "duplicate":n.magicTargets=new[]{400,400};break;case "refund":n.magicRestoreMp=1;break;case "invalid-pattern":f.definition.magicMenu.Find(skill).range++;break;}
                Check(!f.server.ReceiveBattleUnits(n).Accepted&&State(f.View.record)==before&&f.View.battle.magicEffects.Length==0,"invalid skill approval atomic "+bad);
            }
        }
        var empty=Skill(10201);var notice=empty.Notice();notice.units[0].magicRules[0].targets=Array.Empty<int>();Check(empty.server.ReceiveBattleUnits(notice).Accepted&&QueueSkill(empty,10201).Accepted,"zero-target caster effect accepted explicitly");notice=MagicResult(empty);Check(empty.server.ReceiveBattleUnits(notice).Accepted&&empty.View.battle.magicEffects.Length==1,"cross-trust source remains without targets");
        var longBeam=Skill(10106);Check(QueueSkill(longBeam,10106).Accepted&&longBeam.server.ReceiveBattleUnits(MagicResult(longBeam)).Accepted,"long source beam approval");longBeam.server.AdvanceWorld(26*40);
        Check(longBeam.View.battle.magicEffects.Length==1&&longBeam.View.battle.magicEffects[0].role==BattleMagicEffectRole.ChargeOnly,"source35 survives target25 cleanup");longBeam.server.AdvanceWorld(10*40);Check(longBeam.View.battle.magicEffects.Length==0,"source35 independently clears");
        var cancelled=Skill(10501);string saved=State(cancelled.View.record);Check(QueueSkill(cancelled,10501,-3).Accepted,"skill cancellation setup");string request=cancelled.View.battle.pending.id;
        Check(cancelled.server.CancelBattleAction(request).Accepted&&cancelled.View.battle.pending==null&&State(cancelled.View.record)==saved&&cancelled.View.battle.magicEffects.Length==0,"cancel skill without resource or visual side effect");
        foreach(int id in new[]{20101,21705,29001,29015}){var f=Skill(10101);Check(f.server.RequestBattleSkill(id,400,Id()).Failure==WorldFailure.RulesUnavailable,"noncombat special-table entry cannot be cast "+id);}
    }
    private static BattleMagicEffect SkillEffect(MagicProjectileCatalog cat,int magic,int type,int area,int x=-1)
        =>new BattleMagicEffect{id="skill-geometry",magic=magic,pattern=cat.patterns.First(p=>p.type==type).Copy(),role=BattleMagicEffectRole.ChargeOnly,radius=area,sourceX=10,sourceY=10,areaX=x,areaY=-1,mapWidth=30,mapHeight=30};
    private static void CheckSkillGeometry(MagicProjectileCatalog c)
    {
        var origin=new TutorialTile(10,10);var m=new MagicMenuEntry{distance=-1,range=3};
        Check(BattleSkillArea.Weight(m,origin,-1,-1,11,10)==3&&BattleSkillArea.Weight(m,origin,-1,-1,13,10)==1&&BattleSkillArea.Weight(m,origin,-1,-1,14,10)==0,"native thrust positive weights");
        m.distance=-3;Check(BattleSkillArea.Weight(m,origin,-1,-1,11,10)==0&&BattleSkillArea.Weight(m,origin,-1,-1,11,9)==3&&BattleSkillArea.Weight(m,origin,-1,-1,11,11)==3,"double arm excludes center line");
        m.distance=-4;Check(BattleSkillArea.Weight(m,origin,-1,-1,11,10)==3&&BattleSkillArea.Weight(m,origin,-1,-1,11,11)==2,"plasma side length one less");
        m.distance=-2;Check(BattleSkillArea.Weight(m,origin,10,10,10,10)==3&&BattleSkillArea.Weight(m,origin,10,10,11,11)==0,"cross axes include own center, no diagonals");
        m.distance=-5;m.range=2;Check(BattleSkillArea.Weight(m,origin,10,10,10,10)==1&&BattleSkillArea.Weight(m,origin,10,10,11,10)==0&&BattleSkillArea.Weight(m,origin,10,10,11,11)==1,"check-smash parity cells");
        m.distance=-6;Check(BattleSkillArea.Weight(m,origin,13,11,11,10)==1&&BattleSkillArea.Weight(m,origin,13,11,12,11)==1&&BattleSkillArea.Weight(m,origin,13,11,11,11)==0,"native integer100 DDA, not generic line rasterization");
        var single=SkillEffect(c,10101,6,2);single.Advance(40);Check(single.Frames.Length==3&&single.Frames[1].width==34,"thrust first34px body");single.Advance(200);Check(single.Frames[1].width==68,"thrust grows34px each5 counters");
        var doubleArm=SkillEffect(c,10301,15,2);doubleArm.Advance(40);Check(doubleArm.Frames.Length==0,"native right double-arm inverted source stays empty");doubleArm.areaX=-2;Check(doubleArm.Frames.Length==6&&Math.Abs(doubleArm.Frames[0].y-doubleArm.Frames[3].y)==64,"double-arm separation64px");
        var plasma=SkillEffect(c,10501,19,2);plasma.Advance(40);Check(plasma.Frames.Length==3,"right plasma only center3");plasma.areaX=-3;Check(plasma.Frames.Length==9,"other plasma direction has9 pieces");
        var cross=SkillEffect(c,10201,16,2);cross.Advance(40);Check(cross.Frames.Length==2,"cross first two horizontal trails");cross.Advance(120);Check(cross.Frames.Length==14,"cross maximal14 trails");
        var grow=SkillEffect(c,10406,14,5);grow.pattern=c.Find(59).Copy();grow.Advance(40);Check(grow.Frames.Single().width==10&&grow.Frames.Single().height==26,"odd skull frame truncation before doubling");grow.Advance(40);Check(grow.Frames.Single().width==18&&grow.Frames.Single().height==34,"independent grow axes");
        var salamander=SkillEffect(c,10701,20,3,15);salamander.areaY=12;salamander.Advance(40);Check(salamander.Frames.Last().surface==1&&salamander.Frames.Last().atlasX==618,"salamander right-facing head, Fx2");salamander.areaX=10;salamander.areaY=15;Check(salamander.Frames.Last().atlasX==756,"native downward head shares left pair");
        var linear=new BattleMagicEffect{id="linear",pattern=c.Find(58).Copy(),sourceX=2,sourceY=2,targetX=5,targetY=2};linear.Advance(40);Check(linear.Frames.Length==0,"linear first counter0 empty");var sound=new MagicSoundPlayback();Check(sound.Update(new[]{linear}).Single().sound==18,"linear flight sound starts at old0 even without a picture");linear.Advance(40);Check(linear.Frames.Single().x==2*32+19,"MulDiv96*2/10 rounds19");linear.Advance(160);Check(linear.Frames.Single().surface==0&&linear.Frames.Single().atlasX==250,"linear6th tick impact0");var cues=sound.Update(new[]{linear});Check(cues.Length==2&&cues[0].stop&&cues[0].sound==18&&!cues[1].stop&&cues[1].sound==7,"linear counter10 transition is6th tick, sound stop before hit");linear.Advance(160);Check(linear.Expired,"linear14 termination");
    }
    private static void CheckSkillEvents(MagicMenuEntry[] rows)
    {
        var f=Skill(10101);
        foreach(var e in rows)
        {
            var n=f.Notice();var u=n.units.First(v=>v.id==400);int x=e.distance<0?BattleSkillArea.Directional(e.distance)?-2:e.distance==-6?u.x-2:u.x:e.unit==8?-1:u.x,y=e.distance<0?BattleSkillArea.Directional(e.distance)?-1:u.y:e.unit==8?-1:u.y;
            n.magicEvents=new[]{new BattleMagicEvent{magic=e.id,source=400,sourceLife=u.life,x=x,y=y,targets=new[]{0}}};
            Check(f.server.ReceiveBattleUnits(n).Accepted,"all skill event notice "+e.id);f.server.AdvanceWorld(100000);Check(f.View.battle.magicEffects.Length==0&&f.View.record.mp==45,"event neither spends nor teaches "+e.id);
        }
    }
    private static void CheckSkillSave()
    {
        var f=Skill(10406,scenario:true);Check(QueueSkill(f,10406).Accepted,"scenario skill request");var n=MagicResult(f);n.units.Single(u=>u.id==400).nativeState=9;n.units[0].effectiveStats=new CharacterGrowthStats{attack=123};f.store.fail=true;
        string before=State(f.View.record);Check(f.server.ReceiveBattleUnits(n).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==before&&f.View.battle.magicEffects.Length==0,"failed save no skill side effect");f.store.fail=false;Check(f.server.ReceiveBattleUnits(n).Accepted,"same approval retry");
        var restored=new EntryAccountService(f.store);new EntryClient(restored).OpenLocalProfile();Check(restored.BeginWorld(f.definition).Accepted&&restored.WorldSnapshot().record.mp==39&&restored.WorldSnapshot().record.growth.skills.SequenceEqual(new[]{10406})&&restored.WorldSnapshot().battle.Unit(400).nativeState==9&&restored.WorldSnapshot().battle.Commander.effectiveStats.attack==123&&restored.WorldSnapshot().battle.magicEffects.Length==0,"scenario skill state survives, transient effects do not replay");
    }
}
