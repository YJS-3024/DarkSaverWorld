using System;
using System.Linq;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;

internal static partial class MagicProjectileChecks
{
    private static TrainingUnitsNotice MagicResult(Fixture f,int[] affected=null)
    {
        var r=f.View.battle.pending;var n=f.Notice();n.request=r.id;
        n.units[0].mp-=r.mpCost;n.units[0].ap-=r.apCost;
        if(r.action==TrainingAction.UseItem||r.action==TrainingAction.UseSkill||r.magicRange>0||r.magicUnit==8)n.magicTargets=affected??r.magicTargets.Select(t=>t.id).ToArray();
        return n;
    }
    private static int CheckAllMagic(MagicMenuCatalog metadata,MagicProjectileCatalog catalog)
    {
        var spells=metadata.entries.Where(e=>e.id<10000).ToArray();Check(spells.Length==289&&spells.All(catalog.Supports),"entire original general-magic table supported, separate from special menu");
        foreach(var entry in spells)
        {
            var f=new Fixture(entry.id);int x=f.View.record.x,y=f.View.record.y;
            var n=f.Notice();n.units=n.units.Concat(new[]{f.Unit(1,100,x-1,y),f.Unit(401,302,x+1,y+1),f.Unit(409,302,x+2,y)}).ToArray();
            n.units[0].magicRules[0].targets=new[]{0,1,400,401,409};Check(f.server.ReceiveBattleUnits(n).Accepted,"general mixed eligible roster "+entry.id);
            int target=entry.distance==0?0:400;
            var before=State(f.View.record);Check(f.server.RequestBattleMagic(entry.id,target,Id()).Accepted,"general spell request "+entry.id);
            var request=f.View.battle.pending;Check(State(f.View.record)==before&&request.magic==entry.id&&request.pattern==entry.pattern,"no speculative general outcome "+entry.id);
            if(entry.distance==0)Check(request.x==x&&request.y==y,"zero-distance spell is caster-centered "+entry.id);
            if(entry.unit==8)Check(request.x==-1&&request.y==-1&&request.magicTargets.Select(t=>t.id).SequenceEqual(new[]{400,401}),"group precedence over range, no slot9 "+entry.id);
            var result=MagicResult(f);Check(f.server.ReceiveBattleUnits(result).Accepted,"entire general magic approval "+entry.id);
            Check(f.View.record.mp==40&&f.View.battle.Commander.ap==18,"one approved cost per spell "+entry.id);
            var audio=new MagicSoundPlayback();bool drawn=false;int steps=0;int type=catalog.Find(entry.pattern).type;
            if(type==0)Check(f.View.battle.magicEffects.Length==0,"native default no-effect does not fabricate art "+entry.id);
            while(f.View.battle.magicEffects.Length>0&&steps++<1024)
            {
                f.server.AdvanceWorld(40);var effects=f.View.battle.magicEffects;
                foreach(var frame in BattleMagicPresentation.Frames(effects).Select(p=>p.frame))
                {
                    drawn=true;
                    if(frame.surface>=2){Check(type==18,"only native level-up flash redraws world/caster");continue;}
                    int w=frame.sourceWidth>0?frame.sourceWidth:frame.width,h=frame.sourceHeight>0?frame.sourceHeight:frame.height;
                    Check(w>0&&h>0&&frame.width>0&&frame.height>0&&frame.atlasX>=0&&frame.atlasY>=0&&frame.atlasX+w<=999&&frame.atlasY+h<=(frame.surface==1?999:1098),"every native frame uses correct atlas bounds "+entry.id+"/"+steps);
                    Check(!double.IsNaN(frame.x)&&!double.IsNaN(frame.y),"finite native geometry "+entry.id);
                }
                Check(audio.Update(effects).All(c=>c.sound>0),"sound0/-1 never played "+entry.id);
                Check(audio.Update(effects).Length==0,"same paint does not replay audio "+entry.id);
            }
            Check(steps<1024&&f.View.battle.magicEffects.Length==0&&(type==0||drawn),"all render families terminate with no stuck slots "+entry.id);
            Check(!f.server.ReceiveBattleUnits(result).Accepted&&f.View.record.mp==40,"no repeated approved cast or duplicate resource spend "+entry.id);
        }
        CheckRecoveryAndStatus();CheckServerMagicEvents(metadata,catalog);CheckGeneralGeometry(catalog);CheckSoundCues(catalog);
        return spells.Length;
    }
    private static void CheckRecoveryAndStatus()
    {
        // Healing and support use permitted allies, not an id>=400 inference.
        var heal=new Fixture(2401);int x=heal.View.record.x,y=heal.View.record.y;
        var seed=heal.Notice();var ally=heal.Unit(1,100,x-1,y);ally.hp=10;seed.units=seed.units.Concat(new[]{ally}).ToArray();seed.units[0].magicRules[0].targets=new[]{0,1};seed.units[0].hp=100;
        Check(heal.server.ReceiveBattleUnits(seed).Accepted&&heal.server.RequestBattleMagic(2401,1,Id()).Accepted,"friendly group healing and self slot permitted explicitly");
        var n=MagicResult(heal);n.units[0].hp=145;n.units.Single(u=>u.id==1).hp=40;
        Check(heal.server.ReceiveBattleUnits(n).Accepted&&heal.View.record.hp==145&&heal.View.battle.Unit(1).hp==40,"approved healing persists both caster and ally without local formula");
        var hp=new Fixture(1201);seed=hp.Notice();seed.units[0].hp=17;Check(hp.server.ReceiveBattleUnits(seed).Accepted,"injured HP drain caster");hp.Queue(1201);n=MagicResult(hp);n.units[0].hp=57;n.units[1].hp=10;
        Check(hp.server.ReceiveBattleUnits(n).Accepted&&hp.View.record.hp==57&&hp.View.battle.Unit(400).hp==10,"explicit HP absorption, no damage-to-heal conversion inferred");
        var mp=new Fixture(1301);mp.Queue(1301);n=MagicResult(mp);n.units[0].mp=52;n.magicRestoreMp=12;n.units[1].maxMp=20;n.units[1].mp=8;
        Check(mp.server.ReceiveBattleUnits(n).Accepted&&mp.View.record.mp==52&&mp.View.battle.Unit(400).mp==8,"explicit MP restoration may exceed cast cost without bypassing net resource check");
        foreach(int? refund in new int?[]{null,-1,13,int.MaxValue})
        {var f=new Fixture(1301);f.Queue(1301);n=MagicResult(f);n.units[0].mp=52;n.magicRestoreMp=refund;string saved=State(f.View.record);Check(!f.server.ReceiveBattleUnits(n).Accepted&&State(f.View.record)==saved&&f.View.battle.magicEffects.Length==0,"invalid or absent MP refund does not grant resources "+refund);}
        foreach(int magic in new[]{1501,1601,1701,1801,1901,2001,2101,2201,2301,3601,3801,6901})
        {
            var f=new Fixture(magic);f.Queue(magic);n=MagicResult(f);var u=n.units.Single(v=>v.id==400);
            u.nativeState=0x10203040;u.effectiveStats=new CharacterGrowthStats{attack=31,defense=27,magicAttack=21,magicDefense=19};u.canMove=false;u.canAttack=false;u.canRest=false;u.movementBudget=0;u.ap=3;
            Check(f.server.ReceiveBattleUnits(n).Accepted,"approved support/debuff state "+magic);u=f.View.battle.Unit(400);
            Check(u.nativeState==0x10203040&&u.effectiveStats.attack==31&&!u.canMove&&!u.canAttack&&u.movementBudget==0&&u.ap==3,"all authoritative status fields reflected "+magic);
            f.server.AdvanceWorld(100000);Check(f.View.battle.Unit(400).nativeState==0x10203040,"no invented elapsed status expiry "+magic);
            n=f.Notice();n.units[1].nativeState=0;n.units[1].effectiveStats=null;n.units[1].canMove=true;n.units[1].movementBudget=5;
            Check(f.server.ReceiveBattleUnits(n).Accepted&&f.View.battle.Unit(400).nativeState==0&&f.View.battle.Unit(400).canMove,"explicit removal/restoration "+magic);
        }
        foreach(bool incrementLife in new[]{false,true})
        {
            var f=new Fixture(3701);seed=f.Notice();seed.units[1].hp=0;seed.units[1].ghost=true;Check(f.server.ReceiveBattleUnits(seed).Accepted,"fallen target seed");
            Check(f.server.RequestBattleMagic(3701,400,Id()).Failure==WorldFailure.InvalidInput,"dead targets need explicit resurrection permission");
            f.Rule(r=>r.allowDeadTargets=true);f.Queue(3701);n=MagicResult(f);n.units[1].hp=23;n.units[1].ghost=false;if(incrementLife)n.units[1].life++;
            var saved=State(f.View.record);Check(!f.server.ReceiveBattleUnits(n).Accepted&&State(f.View.record)==saved,"revival must be named, not a respawn side effect");
            n.revivedTargets=new[]{400};Check(f.server.ReceiveBattleUnits(n).Accepted&&f.View.battle.Unit(400).hp==23&&!f.View.battle.Unit(400).ghost,"explicit resurrection with optional new life identity");
        }
        var persist=new Fixture(2101,scenario:true);persist.Queue(2101);n=MagicResult(persist);n.units[1].nativeState=8;n.units[1].effectiveStats=new CharacterGrowthStats{attack=9};Check(persist.server.ReceiveBattleUnits(n).Accepted,"scenario status state saved");
        var restored=new EntryAccountService(persist.store);new EntryClient(restored).OpenLocalProfile();Check(restored.BeginWorld(persist.definition).Accepted&&restored.WorldSnapshot().battle.Unit(400).nativeState==8&&restored.WorldSnapshot().battle.Unit(400).effectiveStats.attack==9&&restored.WorldSnapshot().battle.magicEffects.Length==0,"status restored, no transient effect/audio replay");
    }
    private static void CheckServerMagicEvents(MagicMenuCatalog metadata,MagicProjectileCatalog catalog)
    {
        var f=new Fixture();
        foreach(var entry in metadata.entries.Where(e=>e.id<10000))
        {
            var n=f.Notice();n.magicEvents=new[]{new BattleMagicEvent{magic=entry.id,source=400,sourceLife=1,x=entry.unit==8?-1:f.View.record.x,y=entry.unit==8?-1:f.View.record.y,targets=new[]{0}}};
            Check(f.server.ReceiveBattleUnits(n).Accepted,"monster/item/event effects use approved event without changing owned magic "+entry.id);
            Check(f.View.record.growth.magic.SequenceEqual(new[]{101})&&f.View.record.mp==45,"authoritative event does not invent learning or player cost "+entry.id);
            f.server.AdvanceWorld(100000);Check(f.View.battle.magicEffects.Length==0,"event cleanup "+entry.id);
        }
        foreach(string bad in new[]{"source","source-life","target","duplicates","skill","coordinates"})
        {
            var n=f.Notice();var e=new BattleMagicEvent{magic=101,source=400,sourceLife=1,x=f.View.record.x,y=f.View.record.y,targets=new[]{0}};n.magicEvents=new[]{e};
            switch(bad){case "source":e.source=999;break;case "source-life":e.sourceLife++;break;case "target":e.targets=new[]{999};break;case "duplicates":e.targets=new[]{0,0};break;case "skill":e.magic=20101;break;case "coordinates":e.x=-1;break;}
            string before=State(f.View.record);Check(!f.server.ReceiveBattleUnits(n).Accepted&&State(f.View.record)==before,"invalid event atomic rejection "+bad);
        }
        var failed=new Fixture();var notice=failed.Notice();notice.magicEvents=new[]{new BattleMagicEvent{magic=101,source=400,sourceLife=1,x=failed.View.record.x,y=failed.View.record.y,targets=new[]{0}}};failed.store.fail=true;
        Check(failed.server.ReceiveBattleUnits(notice).Failure==WorldFailure.StorageUnavailable&&failed.View.battle.magicEffects.Length==0,"failed event save produces no visual or audio side effect");failed.store.fail=false;
        Check(failed.server.ReceiveBattleUnits(notice).Accepted&&!failed.server.ReceiveBattleUnits(notice).Accepted,"event retry commits once");
        var dead=failed.Notice();dead.units[0].hp=0;dead.units[0].ghost=true;Check(failed.server.ReceiveBattleUnits(dead).Accepted,"fallen commander authoritative state");
        var revived=failed.Notice();revived.units[0].hp=20;revived.units[0].ghost=false;revived.units[0].life++;revived.revivedTargets=new[]{0};revived.magicEvents=new[]{new BattleMagicEvent{magic=3701,source=400,sourceLife=1,x=-1,y=-1,targets=new[]{0}}};
        Check(failed.server.ReceiveBattleUnits(revived).Accepted&&failed.View.record.hp==20&&!failed.View.record.ghost,"approved other caster can resurrect commander");
    }
    private static BattleMagicEffect Effect(MagicProjectileCatalog catalog,int pattern,BattleMagicEffectRole role=BattleMagicEffectRole.TargetOnly)=>new BattleMagicEffect{id="geometry",pattern=catalog.Find(pattern).Copy(),role=role,sourceX=3,sourceY=4,targetX=5,targetY=4,areaX=5,areaY=4};
    private static void CheckGeneralGeometry(MagicProjectileCatalog catalog)
    {
        var shrink=Effect(catalog,18);for(int c=0;c<5;c++){shrink.Advance(40);var f=shrink.Frames.Single();Check(f.width==(7-c)*16&&f.height==(7-c)*16&&f.sourceWidth==shrink.pattern.impact.width,"shrinking target source size remains original");}shrink.Advance(40);Check(shrink.Expired,"five step shrink clears");
        var slow=Effect(catalog,50);for(int c=0;c<10;c++){slow.Advance(40);Check(slow.Frames.Single().surface==1,"slow uses Fx2 not Fx");}slow.Advance(40);Check(slow.Expired,"slow ends after two source cycles");
        var hold=Effect(catalog,48);for(int c=0;c<10;c++){hold.Advance(40);Check(hold.Frames.Single().width==(c<5?112:(12-c)*16),"held debuff size sequence");}
        var blast=Effect(catalog,14);blast.Advance(11*40);Check(blast.Frames.Length==0,"blast native counter10 is sound-only");blast.Advance(40);Check(blast.Frames.Length==1,"blast first hit counter11");blast.Advance(14*40);Check(blast.Expired,"blast clears25");
        var lava=Effect(catalog,28);Check(lava.Frames.Length==1,"lava includes counter0");lava.Advance(400);Check(lava.Frames.Length==0,"lava10 no invented impact0");lava.Advance(40);Check(lava.Frames.Single().atlasX==lava.pattern.impact.x+lava.pattern.impact.stepX,"lava11 impact1");
        var arrow=Effect(catalog,26);Check(arrow.Frames.Length==0,"arrow starts after0");for(int c=1;c<10;c++){arrow.Advance(40);Check(arrow.Frames.Single().surface==1,"arrow eight directions use Fx2");}arrow.Advance(40);Check(arrow.Frames.Single().surface==0&&arrow.Frames.Single().atlasX==arrow.pattern.impact.x,"arrow10 Fx impact0");
        var dirs=new[]{(0,-1),(1,-1),(1,0),(1,1),(0,1),(-1,1),(-1,0),(-1,-1)};for(int i=0;i<8;i++)Check(GeneralMagicFrames.Direction(dirs[i].Item1,dirs[i].Item2)==i,"eight native arrow direction cells "+i);
        var earth=Effect(catalog,9);earth.Advance(40);var coord=BattleMagicPresentation.Frames(new[]{earth}).Single(p=>p.key.StartsWith("coordinate/")).frame;Check(coord.width==224&&coord.height==224&&coord.x==5*32-96&&coord.y==4*32-96,"earthquake full224 square on clicked center");earth.Advance(400);Check(earth.Frames.Length==0&&earth.PostCounter==0&&GeneralMagicFrames.Coordinate(earth).Length==1,"earthquake target-clear coordinate0 pass");
        var circle=Effect(catalog,45,BattleMagicEffectRole.ChargeOnly);circle.Advance(40);Check(circle.Frames.Length==0,"level circle counter0 empty");circle.Advance(40);Check(circle.Frames.Count(fr=>fr.surface==1)==1,"level circle first piece");circle.Advance(11*40);Check(circle.Frames.Count(fr=>fr.surface==1)==12,"level circle accumulated12 pieces");circle.Advance(2*40);Check(BattleMagicPresentation.Frames(new[]{circle}).Select(v=>v.frame.surface).SequenceEqual(new[]{2,3}),"native white flash redraws caster only");circle.Advance(8*40);Check(circle.Frames.Any(fr=>fr.surface==2),"final flash22 retained");circle.Advance(40);Check(circle.Expired,"level source cleanup after last white pass");
        var meteor=Effect(catalog,10);meteor.Advance(40);int post=meteor.PostCounter;var mf=GeneralMagicFrames.Coordinate(meteor);Check(post==0?mf.Length==0:mf.Length==(11-post)%7+1,"meteor count derives updated randomized counter");
    }
    private static void CheckSoundCues(MagicProjectileCatalog catalog)
    {
        var p=Effect(catalog,3);var audio=new MagicSoundPlayback();Check(audio.Update(new[]{p}).Length==0,"no projectile sound before first display");p.Advance(40);var c=audio.Update(new[]{p});Check(c.Length==1&&c[0].sound==p.pattern.sound2&&!c[0].stop,"native flight sound2 starts once");p.Advance(360);c=audio.Update(new[]{p});Check(c.Length==2&&c[0].stop&&c[0].sound==p.pattern.sound2&&!c[1].stop&&c[1].sound==p.pattern.sound1,"flight stop then impact sound at10");
        p=Effect(catalog,45);audio.Clear();p.Advance(400);Check(audio.Update(new[]{p}).Length==0,"delayed impact silent until old counter10");p.Advance(40);Check(audio.Update(new[]{p}).Single().sound==p.pattern.sound1,"blast sound precedes first visible frame");
        p=Effect(catalog,9);audio.Clear();p.Advance(40);Check(audio.Update(new[]{p}).Single().sound==9,"earth target starts sound1");p.Advance(360);Check(audio.Update(new[]{p}).Single().stop,"coordinate10 stops sound1");p.Advance(40);Check(!audio.Update(new[]{p}).Single().stop,"final coordinate0 restarts sound1");
        p=Effect(catalog,3);p.pattern.sound1=0;p.pattern.sound2=-1;audio.Clear();p.Advance(400);Check(audio.Update(new[]{p}).Length==0,"0 and negative IDs remain silent");
        p=Effect(catalog,3);audio.Clear();p.Advance(40);audio.Update(new[]{p});Check(audio.Update(Array.Empty<BattleMagicEffect>()).Single().stop,"replacement/clear stops flight audio");
    }
}
