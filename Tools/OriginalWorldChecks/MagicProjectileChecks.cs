using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;
using DarkSaver.Original.Campaign;

internal static partial class MagicProjectileChecks
{
    private static int assertions;private static string root;
    private static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
    private static string Id()=>Guid.NewGuid().ToString("N");
    private static void Check(bool ok,string label){assertions++;if(!ok)throw new Exception(label);}
    private static string State(object value)=>JsonSerializer.Serialize(value,Options);
    private static T Read<T>(string path)=>JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(root,"Assets/_Original",path)),Options);
    private sealed class Memory:IEntryAccountStore
    {
        public LocalAccountDocument data=new LocalAccountDocument();public bool fail;
        public bool TryRead(out LocalAccountDocument value){value=data.Copy();return true;}
        public bool TryWrite(LocalAccountDocument value){if(fail)return false;data=value.Copy();return true;}
    }
    private sealed class Fixture
    {
        public Memory store=new Memory();public EntryAccountService server;public WorldDefinition definition;public bool scenario;public int[] spells;
        public WorldView View=>server.WorldSnapshot();
        public Fixture(int magic=101,bool scenario=false,int[] spells=null,int[] skills=null,WorldOwnedItem[] inventory=null,bool seedUnits=true)
        {
            this.scenario=scenario;this.spells=spells??new[]{magic};definition=Read<WorldDefinition>("Data/World/KaosiaWorld.json");definition.growthCatalog=Read<CharacterGrowthCatalog>("Data/Growth/CharacterGrowth.json");definition.combatCatalog=Read<OriginalCombatCatalog>("Data/Combat/OriginalCombat.json");definition.magicMenu=Read<MagicMenuCatalog>("Data/Combat/MagicMenu.json");definition.magicProjectiles=Read<MagicProjectileCatalog>("Data/Combat/MagicProjectiles.json");
            if(scenario)
            {
                var campaign=Read<CampaignDefinition>("Data/Campaign/CampaignCatalog.json");var map=campaign.Map(26);var spawn=map.entries.First(e=>e.unit==0);
                using var manifest=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"Tools/OriginalWorldChecks/campaign-assets.json")));var scripts=manifest.RootElement.GetProperty("scripts").EnumerateArray().Select(p=>p.GetString()).ToArray();
                map.intro=map.introIndex<0?"":File.ReadAllText(Path.Combine(root,scripts[map.introIndex]));map.outro=map.outroIndex<0?"":File.ReadAllText(Path.Combine(root,scripts[map.outroIndex]));campaign.connections=new[]{new CampaignConnection{fromMap=1000,toMap=26,x=spawn.x,y=spawn.y,evidence="isolated cast fixture only",cells=new[]{new WorldPoint{x=21,y=21}}}};definition=campaign.Merge(definition);
            }
            server=new EntryAccountService(store);var client=new EntryClient(server);client.OpenLocalProfile();client.Create(100,"시전검사");var c=store.data.accounts.Single().character;c.tutorialCompleted=true;c.world=new WorldRecord{healthKnown=true,hp=200,maxHp=200,mp=45,maxMp=65,inventory=inventory??Array.Empty<WorldOwnedItem>()};client.OpenLocalProfile();Check(server.BeginWorld(definition).Accepted,"isolated world");string id=Id();
            Check(server.ReceiveCharacterGrowth(new CharacterGrowthNotice{id=id,approved=true,character=View.characterId,session=View.growthSession,state=new CharacterGrowthRecord{job=100,classLevel=1,totalLevel=1,revision=1,lastNotice=id,experience=123,magic=this.spells,skills=skills??new[]{10101}}}).Accepted,"explicit owned magic");
            if(true)
            {
                var choices=this.spells;Check(server.ExecuteMagicLoadout(MagicLoadoutOperation.State,-1,Id(),(r,v)=>new MagicLoadoutNotice{id=Id(),request=r.Copy(),approved=true,learned=this.spells,selected=choices}).Accepted,"explicit editor snapshot");
                Check(server.ExecuteMagicLoadout(MagicLoadoutOperation.Confirm,-1,Id(),(r,v)=>new MagicLoadoutNotice{id=Id(),request=r.Copy(),approved=true,selected=(int[])r.selected.Clone()}).Accepted,"confirmed battle-use magic");
            }
            if(scenario)
            {
                Check(server.EnterCampaignMap(26).Accepted,"scenario entry");for(int i=0;i<10000&&View.presentation?.completed==false;i++){if(View.presentation.waitingMessage)server.ContinueCampaignPresentation();server.AdvanceWorld(1000);}server.AdvanceWorld(0);Check(View.record.campaignPhase=="explore","original intro precedes battle");
            }
            else
            {
                var gate=definition.Map(1000).portals.First(p=>p.targetMap==101);Check(server.MoveWorld(gate.cells[0].x,gate.cells[0].y).Accepted,"training entrance path");server.AdvanceWorld(100000);Check(server.EnterWorldPortal(gate.id,true).Accepted,"actual training entrance");
            }
            int x=View.record.x,y=View.record.y;var targetX=x+1<definition.Map(View.record.map).map.width?x+1:x-1;
            if(seedUnits)Check(server.ReceiveBattleUnits(Notice(new[]{Unit(0,100,x,y),Unit(400,302,targetX,y)})).Accepted,"explicit roster/permissions/costs, no default enemies");
        }
        public TrainingUnit Unit(int id,int job,int x,int y)=>new TrainingUnit{id=id,job=job,x=x,y=y,life=1,hp=id==0?200:50,maxHp=id==0?200:50,mp=id==0?45:0,maxMp=id==0?65:0,ap=20,maxAp=20,canMove=true,canAttack=true,canRest=true,movementBudget=5,attackRange=1,
            magicRules=id==0?spells.Select(s=>new BattleMagicRule{magic=s,enabled=true,mpCost=5,apCost=2,targets=new[]{400}}).ToArray():null};
        public TrainingUnitsNotice Notice(TrainingUnit[] units=null)=>new TrainingUnitsNotice{id=Id(),character=View.characterId,session=View.campaignSession,approved=true,expectedRevision=View.battle?.revision??0,units=units??View.battle.units.Select(u=>u.Copy()).ToArray()};
        public TrainingUnitsNotice Result(TrainingActionRequest r=null,bool kill=false,bool omit=false)
        {
            r=r??View.battle.pending;var n=Notice();n.request=r.id;var caster=n.units.First(u=>u.id==0);caster.mp-=r.mpCost;caster.ap-=r.apCost;caster.direction=DarkSaver.Original.Tutorial.TutorialGeometry.FacingRow(r.x-caster.x,r.y-caster.y);
            var enemy=n.units.First(u=>u.id==r.target);enemy.hp=kill?0:Math.Max(0,enemy.hp-10);if(omit)n.units=n.units.Where(u=>u.id!=r.target).ToArray();return n;
        }
        public void Queue(int magic=101)=>Check(server.RequestBattleMagic(magic,400,Id()).Accepted,"valid cast queued");
        public void Rule(Action<BattleMagicRule> change)
        {var n=Notice();change(n.units.Single(u=>u.id==0).magicRules[0]);Check(server.ReceiveBattleUnits(n).Accepted,"explicit updated cast rule");}
        public void Cast(int magic=101,int target=400)=>Check(server.ExecuteBattleAction(TrainingAction.CastMagic,target,0,0,(r,v)=>Result(r),magic).Accepted,"local explicit cast approval");
    }
    private static void AddExtraTargets(Fixture f,int count)
    {
        var n=f.Notice();int x=f.View.record.x,y=f.View.record.y;
        n.units=n.units.Concat(Enumerable.Range(1,count).Select(i=>f.Unit(400+i,302,x+1+i,y))).ToArray();
        foreach(var rule in n.units[0].magicRules)rule.targets=Enumerable.Range(400,count+1).ToArray();
        Check(f.server.ReceiveBattleUnits(n).Accepted,"explicit distinct target roster for simultaneous effect families");
    }
    private static (int x,int y) SeedArea(Fixture f)
    {
        int x=f.View.record.x+2,y=f.View.record.y;var c=f.View.battle.Commander.Copy();
        foreach(var rule in c.magicRules)rule.targets=new[]{400,401,402,403,404};
        var dead=f.Unit(406,302,x,y-1);dead.hp=0;
        Check(f.server.ReceiveBattleUnits(f.Notice(new[]{c,f.Unit(400,302,x,y),f.Unit(401,302,x-1,y),f.Unit(402,302,x,y+1),f.Unit(403,302,x,y-2),f.Unit(404,302,x+2,y+1),f.Unit(405,302,x+1,y),dead})).Accepted,"explicit isolated area roster and target permissions");
        return (x,y);
    }
    private static TrainingUnitsNotice AreaResult(Fixture f,int[] affected=null)
    {
        var r=f.View.battle.pending;var n=f.Notice();n.request=r.id;n.magicTargets=affected??r.magicTargets.Select(t=>t.id).ToArray();
        var c=n.units.Single(u=>u.id==0);c.mp-=r.mpCost;c.ap-=r.apCost;
        foreach(var u in n.units.Where(u=>n.magicTargets.Contains(u.id)))u.hp=Math.Max(0,u.hp-(u.id-399));
        return n;
    }
    private static int CheckAreaMagic(MagicMenuCatalog metadata,MagicProjectileCatalog catalog)
    {
        var spells=metadata.entries.Where(e=>catalog.Supports(e)&&(e.pattern==1||e.pattern==4)&&e.range>0).Select(e=>e.id).ToArray();
        Check(spells.Length==20&&spells.Contains(201)&&spells.Contains(4204)&&spells.Contains(401)&&spells.Contains(4404),"native fire and blizzard twenty area spells only");
        for(int radius=0;radius<=6;radius++)
        {
            int count=0;for(int x=-8;x<=8;x++)for(int y=-8;y<=8;y++)if(BattleMagicArea.Weight(0,0,x,y,radius)>0)count++;
            Check(count==1+2*radius*(radius+1),"native positive diamond cell count radius"+radius);
            Check(BattleMagicArea.Weight(0,0,radius,0,radius)==1&&BattleMagicArea.Weight(0,0,radius+1,0,radius)==0,"last positive boundary then zero ring radius"+radius);
        }
        Check(BattleMagicArea.Weight(int.MinValue,0,int.MaxValue,0,6)==0&&BattleMagicArea.Weight(0,0,0,0,-1)==0,"area weight rejects negative and extreme distances without overflow");
        foreach(int magic in spells)
        {
            var f=new Fixture(magic);var center=SeedArea(f);var entry=metadata.Find(magic);var before=State(f.View.record);
            Check(f.server.RequestBattleMagicAt(magic,center.x,center.y,Id()).Accepted,"area coordinate request "+magic);var r=f.View.battle.pending;
            var expected=entry.range==2?new[]{401,403,400,402}:new[]{401,403,400,402,404};
            Check(r.target==-1&&r.x==center.x&&r.y==center.y&&r.magicTargets.Select(t=>t.id).SequenceEqual(expected)&&r.magicRange==entry.range&&r.magicDistance==entry.distance,"positive-mask X outer/Y inner candidate capture "+magic);
            Check(State(f.View.record)==before&&f.View.battle.magicEffects.Length==0,"area request predicts no resources/damage/effects "+magic);
            var snapshot=f.View.battle.pending;snapshot.magicTargets[0].x=999;snapshot.magicTargets[0].life=999;
            Check(f.View.battle.pending.magicTargets[0].x!=999&&f.View.battle.pending.magicTargets[0].life==1,"deep request candidate copy "+magic);
            var approval=AreaResult(f);Check(f.server.ReceiveBattleUnits(approval).Accepted,"atomic multi-target area result "+magic);
            Check(f.View.record.mp==40&&f.View.battle.Commander.ap==18&&expected.All(id=>f.View.battle.Unit(id).hp==50-(id-399))&&f.View.battle.Unit(405).hp==50&&f.View.battle.Unit(406).hp==0,"single approved cost, explicit per-target HP, denied/dead excluded "+magic);
            Check(f.View.battle.magicEffects.Length==expected.Length&&f.View.battle.magicEffects.Select(e=>e.id).Distinct().Count()==expected.Length&&f.View.battle.magicEffects.All(e=>e.pattern.id==entry.pattern),"one distinct original-pattern effect per approved target "+magic);
            f.server.AdvanceWorld(400);
            foreach(var target in r.magicTargets)
            {
                var fx=f.View.battle.magicEffects.Single(e=>e.id.EndsWith("/"+target.id));var p=catalog.Find(entry.pattern);
                Check(fx.DrawX==target.x*32-(p.impact.width-32)/2&&fx.DrawY==target.y*32-(p.impact.height-32)/2&&fx.Frame==0,"area hit remains at each captured target, not click center "+magic+"/"+target.id);
            }
            f.server.AdvanceWorld(240);Check(f.View.battle.magicEffects.Length==0&&!f.server.ReceiveBattleUnits(approval).Accepted&&f.View.record.mp==40,"area termination and duplicate response no replay "+magic);
        }
        var emptyCenter=new Fixture(201);var point=SeedArea(emptyCenter);Check(emptyCenter.server.RequestBattleMagicAt(201,point.x-1,point.y+1,Id()).Accepted&&emptyCenter.View.battle.pending.magicTargets.All(t=>t.id!=405),"empty clicked tile can select surrounding permitted targets");
        var empty=new Fixture(201);point=SeedArea(empty);var state=State(empty.View.record);Check(empty.server.RequestBattleMagicAt(201,point.x-4,point.y,Id()).Failure==WorldFailure.InvalidInput&&empty.View.battle.pending==null&&State(empty.View.record)==state,"empty positive mask has no request or resource change");
        Check(empty.server.RequestBattleMagicAt(201,-1,point.y,Id()).Failure==WorldFailure.InvalidInput,"outside map area center rejected");
        Check(empty.server.RequestBattleMagicAt(201,point.x+2,point.y,Id()).Failure==WorldFailure.InvalidInput,"area center outside cast distance rejected even if some targets are close");
        var clicked=new Fixture(401);point=SeedArea(clicked);Check(clicked.server.RequestBattleMagic(401,400,Id()).Accepted&&clicked.View.battle.pending.magicTargets.Length==4,"unit click routes same blizzard area selection");
        var subset=new Fixture(201);point=SeedArea(subset);Check(subset.server.RequestBattleMagicAt(201,point.x,point.y,Id()).Accepted,"subset area queued");var subsetNotice=AreaResult(subset,new[]{402,400});
        Check(subset.server.ReceiveBattleUnits(subsetNotice).Accepted&&subset.View.battle.magicEffects.Length==2&&subset.View.battle.Unit(401).hp==50&&subset.View.battle.Unit(403).hp==50,"explicit affected subset only, no candidate-wide inferred hit");
        var zero=new Fixture(401);point=SeedArea(zero);Check(zero.server.RequestBattleMagicAt(401,point.x,point.y,Id()).Accepted,"zero affected area queued");Check(zero.server.ReceiveBattleUnits(AreaResult(zero,Array.Empty<int>())).Accepted&&zero.View.record.mp==40&&zero.View.battle.magicEffects.Length==0,"explicit empty affected list differs from missing result");
        foreach(string problem in new[]{"missing-list","duplicate","outside-mask","denied","new-life","mp-cost","ap-cost","scope-change"})
        {
            var f=new Fixture(201);point=SeedArea(f);Check(f.server.RequestBattleMagicAt(201,point.x,point.y,Id()).Accepted,"invalid area fixture queued "+problem);var n=AreaResult(f);state=State(f.View.record);
            switch(problem)
            {
                case "missing-list":n.magicTargets=null;break;
                case "duplicate":n.magicTargets=new[]{400,400};break;
                case "outside-mask":n.magicTargets=new[]{404};break;
                case "denied":n.magicTargets=new[]{405};break;
                case "new-life":n.units.Single(u=>u.id==403).life++;break;
                case "mp-cost":n.units[0].mp--;break;
                case "ap-cost":n.units[0].ap--;break;
                case "scope-change":f.definition.magicMenu.Find(201).range++;break;
            }
            Check(!f.server.ReceiveBattleUnits(n).Accepted&&State(f.View.record)==state&&f.View.battle.magicEffects.Length==0,"area validation failure never partially commits "+problem);
        }
        var failed=new Fixture(401);point=SeedArea(failed);Check(failed.server.RequestBattleMagicAt(401,point.x,point.y,Id()).Accepted,"save failure area queued");var result=AreaResult(failed);state=State(failed.View.record);failed.store.fail=true;
        Check(failed.server.ReceiveBattleUnits(result).Failure==WorldFailure.StorageUnavailable&&State(failed.View.record)==state&&failed.View.battle.magicEffects.Length==0,"multi-target save failure no partial HP or resource/effect changes");failed.store.fail=false;Check(failed.server.ReceiveBattleUnits(result).Accepted&&failed.View.battle.magicEffects.Length==4,"same area approval retries atomically");
        var removed=new Fixture(201);point=SeedArea(removed);Check(removed.server.RequestBattleMagicAt(201,point.x,point.y,Id()).Accepted,"removed area target queued");var disappear=AreaResult(removed,new[]{400});disappear.units=disappear.units.Where(u=>u.id!=400).ToArray();Check(removed.server.ReceiveBattleUnits(disappear).Accepted,"approved removed area target");removed.server.AdvanceWorld(400);Check(removed.View.battle.magicEffects.Single().DrawX==point.x*32&&removed.View.battle.magicEffects.Single().DrawY==point.y*32,"removed area target retains captured endpoint");
        var stale=new Fixture(201);point=SeedArea(stale);Check(stale.server.RequestBattleMagicAt(201,point.x,point.y,Id()).Accepted,"stale area queued");var old=AreaResult(stale);Check(stale.server.ReceiveBattleUnits(stale.Notice()).Accepted&&!stale.server.ReceiveBattleUnits(old).Accepted&&stale.View.battle.magicEffects.Length==0,"new roster invalidates captured old multi-target cast");
        var standalone=new Fixture(201);var unsolicited=standalone.Notice();unsolicited.magicTargets=new[]{400};Check(standalone.server.ReceiveBattleUnits(unsolicited).Failure==WorldFailure.InvalidInput,"affected list cannot attach to unsolicited roster");
        var normal=new Fixture(101);Check(normal.server.RequestBattleMagicAt(101,normal.View.battle.Unit(400).x,normal.View.battle.Unit(400).y,Id()).Failure==WorldFailure.InvalidInput,"coordinate path does not invent a single-target unit selection");
        var scenario=new Fixture(401,scenario:true);point=SeedArea(scenario);Check(scenario.server.RequestBattleMagicAt(401,point.x,point.y,Id()).Accepted&&scenario.server.ReceiveBattleUnits(AreaResult(scenario)).Accepted,"scenario multi-target result");var reconnect=new EntryAccountService(scenario.store);new EntryClient(reconnect).OpenLocalProfile();Check(reconnect.BeginWorld(scenario.definition).Accepted&&reconnect.WorldSnapshot().record.mp==40&&reconnect.WorldSnapshot().battle.Unit(403).hp==46&&reconnect.WorldSnapshot().battle.magicEffects.Length==0,"area HP/resources restore, no target snapshots or visual replay persisted");
        return spells.Length;
    }
    private static int CheckThunderstorm(MagicMenuCatalog metadata,MagicProjectileCatalog catalog)
    {
        var spells=metadata.entries.Where(e=>catalog.Supports(e)&&e.pattern==6&&e.range>0).Select(e=>e.id).ToArray();
        Check(spells.SequenceEqual(new[]{601,602,603,604,605,606,4601,4602,4603,4604}),"ten original thunderstorm levels and no unrelated pattern skills");
        Check(State(catalog.Find(6).travel)==State(catalog.Find(5).travel)&&State(catalog.Find(6).impact)==State(catalog.Find(5).impact)&&State(catalog.Find(6).charge)==State(catalog.Find(5).charge),"native patterns5/6 share target strips and one source charge, not projectile art");
        foreach(int magic in spells)
        {
            var f=new Fixture(magic);var center=SeedArea(f);var before=State(f.View.record);
            Check(f.server.RequestBattleMagicAt(magic,center.x,center.y,Id()).Accepted&&State(f.View.record)==before,"thunderstorm request without predictive cost "+magic);var request=f.View.battle.pending;
            int count=request.magicTargets.Length;Check(count==(metadata.Find(magic).range==2?4:5),"native thunderstorm area membership "+magic);
            var approval=AreaResult(f);approval.id=magic.ToString("x32");Check(f.server.ReceiveBattleUnits(approval).Accepted,"thunderstorm approved multi-target state "+magic);
            Check(f.View.record.mp==40&&f.View.battle.Commander.ap==18&&f.View.battle.magicEffects.Count(e=>e.role==BattleMagicEffectRole.ChargeOnly)==1&&f.View.battle.magicEffects.Count(e=>e.role==BattleMagicEffectRole.TargetOnly)==count,"one charge plus individual target slots, one resource charge "+magic);
            f.server.AdvanceWorld(40);var effects=f.View.battle.magicEffects;
            Check(effects.Sum(e=>e.Frames.Length)==count*2+1&&effects.SelectMany(e=>e.Frames).Count(p=>p.part==2)==1,"source charge rendered once for all storm targets "+magic);
            foreach(var target in request.magicTargets)
            {
                var effect=effects.Single(e=>e.targetUnit==target.id);var frames=effect.Frames;var bolt=frames.Single(p=>p.part==0);var ground=frames.Single(p=>p.part==1);
                Check(effect.pattern.id==6&&frames.Length==2&&bolt.width==50&&bolt.height==350&&bolt.x==target.x*32-9&&bolt.y==(target.y+1)*32-350&&ground.x==target.x*32&&ground.y==target.y*32,"per-target original bolt/ground rectangles and frozen coordinates "+magic+"/"+target.id);
            }
            var source=effects.Single(e=>e.role==BattleMagicEffectRole.ChargeOnly);var charge=source.Frames.Single();
            Check(charge.width==40&&charge.height==40&&charge.x==f.View.record.x*32-4&&charge.y==f.View.record.y*32-4&&charge.atlasX==0&&charge.atlasY==128,"storm charge anchored at caster, not target or click center "+magic);
            source.pattern.charge.width=999;Check(f.View.battle.magicEffects.Single(e=>e.role==BattleMagicEffectRole.ChargeOnly).pattern.charge.width==40,"storm charge metadata snapshot is isolated "+magic);
            f.server.AdvanceWorld(360);Check(f.View.battle.magicEffects.Single(e=>e.role==BattleMagicEffectRole.ChargeOnly).Frames.Single().atlasX==160,"last source charge frame9 "+magic);
            f.server.AdvanceWorld(40);Check(f.View.battle.magicEffects.Length==count&&f.View.battle.magicEffects.All(e=>e.role==BattleMagicEffectRole.TargetOnly&&e.Frames.Length==2),"source charge ends while all target bolts continue "+magic);
            bool independent=false;int ticks=0;
            while(f.View.battle.magicEffects.Length>0&&ticks++<256)
            {f.server.AdvanceWorld(40);int left=f.View.battle.magicEffects.Length;if(left>0&&left<count)independent=true;}
            Check(ticks<256&&independent&&f.View.battle.magicEffects.Length==0&&f.View.record.mp==40&&f.View.battle.Commander.ap==18,"target counters expire independently without repeated costs "+magic);
            Check(!f.server.ReceiveBattleUnits(approval).Accepted&&f.View.battle.magicEffects.Length==0,"old storm approval cannot restart charge "+magic);
        }
        var empty=new Fixture(601);var point=SeedArea(empty);Check(empty.server.RequestBattleMagicAt(601,point.x,point.y,Id()).Accepted,"zero-hit storm queued");
        Check(empty.server.ReceiveBattleUnits(AreaResult(empty,Array.Empty<int>())).Accepted&&empty.View.battle.magicEffects.Length==1&&empty.View.battle.magicEffects[0].role==BattleMagicEffectRole.ChargeOnly,"explicit zero-hit result still has native caster setup before target loop");
        empty.server.AdvanceWorld(40);Check(empty.View.battle.magicEffects.Single().Frames.Length==1&&empty.View.battle.magicEffects.Single().Frames[0].part==2&&empty.View.battle.Unit(400).hp==50,"zero-hit charge has no target bolt, inferred damage or hit");empty.server.AdvanceWorld(400);Check(empty.View.battle.magicEffects.Length==0&&empty.View.record.mp==40,"zero-hit source charge ends and cost remains once");
        var subset=new Fixture(601);point=SeedArea(subset);Check(subset.server.RequestBattleMagicAt(601,point.x,point.y,Id()).Accepted&&subset.server.ReceiveBattleUnits(AreaResult(subset,new[]{402,400})).Accepted,"storm approved target subset");subset.server.AdvanceWorld(40);Check(subset.View.battle.magicEffects.SelectMany(e=>e.Frames).Count()==5&&subset.View.battle.magicEffects.Count(e=>e.targetUnit==401)==0,"partial storm produces two bolts/grounds and only one charge");
        var failed=new Fixture(spells:new[]{501,601,201});point=SeedArea(failed);failed.Cast(501);failed.server.AdvanceWorld(80);var oldId=failed.View.battle.magicEffects.Single().id;
        Check(failed.server.RequestBattleMagicAt(601,point.x,point.y,Id()).Accepted,"storm replacing existing caster queued");var replacement=AreaResult(failed);var saved=State(failed.View.record);
        var previousFrames=State(failed.View.battle.magicEffects.Select(e=>new{e.id,e.targetUnit,e.role,frames=e.Frames}));failed.store.fail=true;
        Check(failed.server.ReceiveBattleUnits(replacement).Failure==WorldFailure.StorageUnavailable&&State(failed.View.record)==saved&&State(failed.View.battle.magicEffects.Select(e=>new{e.id,e.targetUnit,e.role,frames=e.Frames}))==previousFrames,"save failure does not erase prior charge/target or partly show storm");failed.store.fail=false;
        Check(failed.server.ReceiveBattleUnits(replacement).Accepted&&failed.View.battle.magicEffects.All(e=>e.id!=oldId)&&failed.View.battle.magicEffects.Count(e=>e.role==BattleMagicEffectRole.ChargeOnly)==1&&failed.View.record.mp==35,"successful retry replaces same target and caster slots atomically");failed.server.AdvanceWorld(40);
        var retained=failed.View.battle.magicEffects.Where(e=>e.targetUnit!=400&&e.role==BattleMagicEffectRole.TargetOnly).Select(e=>e.id).ToArray();
        Check(failed.server.RequestBattleMagicAt(201,point.x,point.y,Id()).Accepted&&failed.server.ReceiveBattleUnits(AreaResult(failed,new[]{400})).Accepted,"later fire replaces source charge and one targeted storm effect");
        Check(failed.View.battle.magicEffects.All(e=>e.role!=BattleMagicEffectRole.ChargeOnly)&&failed.View.battle.magicEffects.Single(e=>e.targetUnit==400).magic==201&&retained.All(id=>failed.View.battle.magicEffects.Any(e=>e.id==id)),"untouched target slots survive while selected target and caster are replaced");
        var repeated=new Fixture(spells:new[]{601,501});point=SeedArea(repeated);Check(repeated.server.RequestBattleMagicAt(601,point.x,point.y,Id()).Accepted&&repeated.server.ReceiveBattleUnits(AreaResult(repeated)).Accepted,"first repeated storm");repeated.server.AdvanceWorld(80);
        Check(repeated.server.RequestBattleMagicAt(601,point.x,point.y,Id()).Accepted&&repeated.server.ReceiveBattleUnits(AreaResult(repeated,new[]{401,403})).Accepted,"second partial repeated storm");repeated.server.AdvanceWorld(40);
        Check(repeated.View.battle.magicEffects.Where(e=>e.targetUnit>=0).GroupBy(e=>e.targetUnit).All(g=>g.Count()==1)&&repeated.View.battle.magicEffects.SelectMany(e=>e.Frames).Count(p=>p.part==2)==1,"repeated storms do not stack source charge or same-target effects");
        repeated.Cast(501,400);repeated.server.AdvanceWorld(40);Check(repeated.View.battle.magicEffects.All(e=>e.role!=BattleMagicEffectRole.ChargeOnly)&&repeated.View.battle.magicEffects.SelectMany(e=>e.Frames).Count(p=>p.part==2)==1,"single thunder supersedes storm caster charge without losing unrelated targets");
        foreach(string problem in new[]{"missing-list","duplicate","denied","new-life","cost","charge-data","range"})
        {
            var f=new Fixture(601);point=SeedArea(f);Check(f.server.RequestBattleMagicAt(601,point.x,point.y,Id()).Accepted,"invalid storm queued "+problem);var n=AreaResult(f);saved=State(f.View.record);
            switch(problem)
            {case "missing-list":n.magicTargets=null;break;case "duplicate":n.magicTargets=new[]{400,400};break;case "denied":n.magicTargets=new[]{405};break;case "new-life":n.units.Single(u=>u.id==403).life++;break;case "cost":n.units[0].mp--;break;case "charge-data":f.definition.magicProjectiles.Find(6).charge=null;break;case "range":f.definition.magicMenu.Find(601).range++;break;}
            Check(!f.server.ReceiveBattleUnits(n).Accepted&&State(f.View.record)==saved&&f.View.battle.magicEffects.Length==0,"invalid storm commits no charge/target/resources "+problem);
        }
        var scene=new Fixture(601,scenario:true);point=SeedArea(scene);Check(scene.server.RequestBattleMagicAt(601,point.x,point.y,Id()).Accepted&&scene.server.ReceiveBattleUnits(AreaResult(scene)).Accepted,"scenario storm approved");var restored=new EntryAccountService(scene.store);new EntryClient(restored).OpenLocalProfile();Check(restored.BeginWorld(scene.definition).Accepted&&restored.WorldSnapshot().record.mp==40&&restored.WorldSnapshot().battle.Unit(403).hp==46&&restored.WorldSnapshot().battle.magicEffects.Length==0,"scenario storm results persist while caster/target presentation never replays on reconnect");
        return spells.Length;
    }
    private static void SeedGroup(Fixture f,bool all=false)
    {
        int x=f.View.record.x,y=f.View.record.y,dy=y+3<f.definition.Map(f.View.record.map).map.height?1:-1;var c=f.View.battle.Commander.Copy();
        foreach(var rule in c.magicRules)rule.targets=Enumerable.Range(400,11).Where(id=>all||id!=403).ToArray();
        var dead=f.Unit(402,302,x+2,y+dy);if(!all)dead.hp=0;
        var units=new[]{c,f.Unit(400,302,x+7,y+dy),f.Unit(401,302,x+2,y),dead,f.Unit(403,302,x+1,y+dy),f.Unit(404,302,x+3,y+dy),f.Unit(405,302,x-2,y+dy),f.Unit(406,302,x-1,y+2*dy),f.Unit(407,302,x+1,y),f.Unit(408,302,x,y+3*dy),f.Unit(409,302,x+1,y+3*dy),f.Unit(410,302,x+1,y-dy)};
        Check(f.server.ReceiveBattleUnits(f.Notice(units.Reverse().ToArray())).Accepted,"explicit scattered Unit8 fixture, unordered roster, no default spawn");
    }
    private static int CheckWindGroup(MagicMenuCatalog metadata,MagicProjectileCatalog catalog)
    {
        var spells=metadata.entries.Where(e=>catalog.Supports(e)&&e.pattern==7&&e.unit==8).Select(e=>e.id).ToArray();
        Check(spells.SequenceEqual(new[]{701,702,703,704,705,706,4701,4702,4703,4704}),"ten original windcutter levels, Unit8 not a generic area spell");
        var pattern=catalog.Find(7);Check(pattern.type==1&&pattern.travel.x==280&&pattern.travel.y==128&&pattern.travel.width==40&&pattern.travel.height==40&&pattern.travel.count==2&&pattern.impact.x==250&&pattern.impact.y==240&&pattern.impact.width==32&&pattern.impact.count==4,"literal windcutter travel/impact differ from fireball and freeze");
        foreach(int magic in spells)
        {
            var f=new Fixture(magic);SeedGroup(f);var before=State(f.View.record);
            Check(f.server.RequestBattleMagic(magic,407,Id()).Accepted,"Unit8 member click "+magic);var r=f.View.battle.pending;var ids=new[]{400,401,404,405,406,407,408};
            Check(r.magicUnit==8&&r.magicRange==0&&r.x==-1&&r.y==-1&&r.magicTargets.Select(t=>t.id).SequenceEqual(ids)&&State(f.View.record)==before,"native group slots in ID order, sentinel XY, dead/denied/slot9/next-group excluded "+magic);
            var far=r.magicTargets.Single(t=>t.id==400);Check(Math.Abs(far.x-f.View.record.x)+Math.Abs(far.y-f.View.record.y)>metadata.Find(magic).distance,"group member distance does not shrink selected native group "+magic);
            var copied=f.View.battle.pending;copied.magicTargets[0].x=999;Check(f.View.battle.pending.magicTargets[0].x!=999,"Unit8 captured target coordinate copy "+magic);
            var n=AreaResult(f);Check(f.server.ReceiveBattleUnits(n).Accepted&&f.View.record.mp==40&&f.View.battle.Commander.ap==18,"one approved group resource cost "+magic);
            Check(f.View.battle.magicEffects.Length==7&&f.View.battle.magicEffects.Select(e=>e.id).Distinct().Count()==7&&f.View.battle.magicEffects.All(e=>e.pattern.id==7&&e.role==BattleMagicEffectRole.TargetOnly)&&f.View.battle.Unit(409).hp==50&&f.View.battle.Unit(410).hp==50&&f.View.battle.Unit(403).hp==50,"individual group effects have unique IDs, no extra slots or inferred hits "+magic);
            f.server.AdvanceWorld(40);
            Check(f.View.battle.magicEffects.All(e=>e.Frames.Length==1&&e.Width==40&&e.Height==40&&e.Frame>=0&&e.Frame<2&&e.AtlasX==280+e.Frame*40&&e.AtlasY==128),"own two-frame40px wind projectile strip "+magic);
            f.server.AdvanceWorld(360);
            foreach(var target in r.magicTargets)
            {
                var effect=f.View.battle.magicEffects.Single(e=>e.targetUnit==target.id);
                Check(effect.Impact&&effect.Frame==0&&effect.Width==32&&effect.AtlasX==250&&effect.AtlasY==240&&effect.DrawX==target.x*32&&effect.DrawY==target.y*32,"wind impact uses captured32px target position, never sentinel or caster "+magic+"/"+target.id);
            }
            f.server.AdvanceWorld(120);Check(f.View.battle.magicEffects.All(e=>e.Frame==3&&e.Visible),"last wind impact frame "+magic);f.server.AdvanceWorld(40);
            Check(f.View.battle.magicEffects.Length==0&&!f.server.ReceiveBattleUnits(n).Accepted&&f.View.record.mp==40,"wind counter14 termination and duplicate no replay "+magic);
        }
        var full=new Fixture(4704);SeedGroup(full,true);Check(full.server.RequestBattleMagic(4704,407,Id()).Accepted&&full.View.battle.pending.magicTargets.Select(t=>t.id).SequenceEqual(Enumerable.Range(400,9)),"all nine native slots supported without old three/five-unit assumption");
        var ninth=new Fixture(4704);SeedGroup(ninth);ninth.Rule(r=>r.targets=new[]{409});Check(ninth.server.RequestBattleMagic(4704,409,Id()).Accepted&&ninth.View.battle.pending.magicTargets.Length==0&&ninth.View.battle.pending.x==-1,"clicked slot9 may resolve an empty0..8 group and still return native success");
        Check(ninth.server.ReceiveBattleUnits(AreaResult(ninth,Array.Empty<int>())).Accepted&&ninth.View.record.mp==40&&ninth.View.battle.magicEffects.Length==0,"explicit empty group approval spends only supplied cost with no fabricated target");
        var partial=new Fixture(701);SeedGroup(partial);Check(partial.server.RequestBattleMagic(701,407,Id()).Accepted,"partial group queued");Check(partial.server.ReceiveBattleUnits(AreaResult(partial,new[]{408,400})).Accepted&&partial.View.battle.magicEffects.Length==2&&partial.View.battle.Unit(401).hp==50,"explicit affected group subset only");
        var wrongCenter=new Fixture(701);SeedGroup(wrongCenter);var saved=State(wrongCenter.View.record);Check(wrongCenter.server.RequestBattleMagic(701,400,Id()).Failure==WorldFailure.InvalidInput&&State(wrongCenter.View.record)==saved,"clicked unit outside positive cast mask rejected even if peer is near");
        var clicked=wrongCenter.View.battle.Unit(407);Check(wrongCenter.server.RequestBattleMagicAt(701,clicked.x,clicked.y,Id()).Failure==WorldFailure.InvalidInput,"coordinate-only API does not invent the required clicked unit ID");
        foreach(string problem in new[]{"missing-list","slot9","next-group","dead","denied","duplicate","new-life","cost","unit-change"})
        {
            var f=new Fixture(701);SeedGroup(f);Check(f.server.RequestBattleMagic(701,407,Id()).Accepted,"invalid group fixture queued "+problem);var n=AreaResult(f);saved=State(f.View.record);
            switch(problem)
            {case "missing-list":n.magicTargets=null;break;case "slot9":n.magicTargets=new[]{409};break;case "next-group":n.magicTargets=new[]{410};break;case "dead":n.magicTargets=new[]{402};break;case "denied":n.magicTargets=new[]{403};break;case "duplicate":n.magicTargets=new[]{400,400};break;case "new-life":n.units.Single(u=>u.id==400).life++;break;case "cost":n.units.Single(u=>u.id==0).ap--;break;case "unit-change":f.definition.magicMenu.Find(701).unit=3;break;}
            Check(!f.server.ReceiveBattleUnits(n).Accepted&&State(f.View.record)==saved&&f.View.battle.magicEffects.Length==0,"invalid group never partially changes resources/HP/effects "+problem);
        }
        var failed=new Fixture(701);SeedGroup(failed);Check(failed.server.RequestBattleMagic(701,407,Id()).Accepted,"group save failure queued");var approval=AreaResult(failed);saved=State(failed.View.record);failed.store.fail=true;
        Check(failed.server.ReceiveBattleUnits(approval).Failure==WorldFailure.StorageUnavailable&&State(failed.View.record)==saved&&failed.View.battle.magicEffects.Length==0,"group multi-target save failure has no partial spend or effects");failed.store.fail=false;Check(failed.server.ReceiveBattleUnits(approval).Accepted&&failed.View.battle.magicEffects.Length==7,"same group approval retries once");
        var removed=new Fixture(701);SeedGroup(removed);Check(removed.server.RequestBattleMagic(701,407,Id()).Accepted,"removed group target queued");var old=removed.View.battle.pending.magicTargets.Single(t=>t.id==400);var result=AreaResult(removed,new[]{400});result.units=result.units.Where(u=>u.id!=400).ToArray();Check(removed.server.ReceiveBattleUnits(result).Accepted,"approved removed group target");removed.server.AdvanceWorld(400);Check(removed.View.battle.magicEffects.Single().DrawX==old.x*32&&removed.View.battle.magicEffects.Single().DrawY==old.y*32,"removed group member keeps captured position rather than sentinel XY");
        var changed=new Fixture(spells:new[]{4704,601});SeedGroup(changed);Check(changed.server.RequestBattleMagic(601,407,Id()).Accepted&&changed.server.ReceiveBattleUnits(AreaResult(changed)).Accepted,"storm precedes group cast");changed.server.AdvanceWorld(40);
        Check(changed.server.RequestBattleMagic(4704,407,Id()).Accepted&&changed.server.ReceiveBattleUnits(AreaResult(changed)).Accepted&&changed.View.battle.magicEffects.All(e=>e.role!=BattleMagicEffectRole.ChargeOnly)&&changed.View.battle.magicEffects.GroupBy(e=>e.targetUnit).All(g=>g.Count()==1),"group cast replaces previous source charge and each selected target slot");
        var scene=new Fixture(701,scenario:true);SeedGroup(scene);Check(scene.server.RequestBattleMagic(701,407,Id()).Accepted&&scene.server.ReceiveBattleUnits(AreaResult(scene)).Accepted,"scenario group cast");var reconnect=new EntryAccountService(scene.store);new EntryClient(reconnect).OpenLocalProfile();Check(reconnect.BeginWorld(scene.definition).Accepted&&reconnect.WorldSnapshot().record.mp==40&&reconnect.WorldSnapshot().battle.Unit(408).hp==41&&reconnect.WorldSnapshot().battle.magicEffects.Length==0,"group HP/resources restore without request/target effect replay");
        return spells.Length;
    }
    private static int CheckTornado(MagicMenuCatalog metadata,MagicProjectileCatalog catalog)
    {
        var spells=metadata.entries.Where(e=>catalog.Supports(e)&&catalog.Find(e.pattern).type==3).Select(e=>e.id).ToArray();
        Check(spells.SequenceEqual(new[]{801,802,803,804,805,806,4801,4802,4803,4804}),"ten native tornado levels, no implicit mapping to unused pattern57");
        var pattern=catalog.Find(8);Check(pattern.impact.x==250&&pattern.impact.y==240&&pattern.impact.width==32&&pattern.impact.count==4&&pattern.travel.x==512&&pattern.travel.y==822&&pattern.travel.width==64&&pattern.travel.height==64&&pattern.travel.count==4&&pattern.charge.count==5,"native tornado hit/vortex/caster strips");
        foreach(int magic in spells)
        {
            var f=new Fixture(magic);var center=SeedArea(f);var saved=State(f.View.record);
            Check(f.server.RequestBattleMagicAt(magic,center.x,center.y,Id()).Accepted&&State(f.View.record)==saved,"tornado queue without predicted resource change "+magic);var request=f.View.battle.pending;int count=request.magicTargets.Length;
            var approval=AreaResult(f);approval.id=(magic+10000).ToString("x32");Check(f.server.ReceiveBattleUnits(approval).Accepted,"tornado result "+magic);
            Check(f.View.battle.magicEffects.Length==count+1&&BattleMagicPresentation.Frames(f.View.battle.magicEffects).Length==0&&f.View.record.mp==40&&f.View.battle.Commander.ap==18,"one charge and approved target effects with one cost "+magic);
            for(int step=1;step<=10;step++)
            {
                f.server.AdvanceWorld(40);var effects=f.View.battle.magicEffects;var frames=BattleMagicPresentation.Frames(effects);var owners=BattleMagicPresentation.TornadoOwners(effects);
                Check(owners.Length==1&&owners[0].targetUnit==400&&owners[0].areaX==center.x&&owners[0].areaY==center.y,"one native coordinate owner by target ID, not request XY ordering "+magic+"/"+step);
                Check(frames.Count(p=>p.frame.part==2)==1&&frames.Count(p=>p.frame.part==0)==count&&frames.Count(p=>p.frame.part>=3)==(step<10?3:0),"separate hit/charge/three coordinate pieces, stored10 suppresses vortex "+magic+"/"+step);
                foreach(var target in request.magicTargets)
                {
                    var effect=effects.Single(e=>e.targetUnit==target.id);
                    Check(effect.Visible&&effect.Frame==(step-1)%4&&effect.Width==32&&effect.DrawX==target.x*32&&effect.DrawY==target.y*32&&effect.AtlasY==240,"old-counter target impact and fixed position "+magic+"/"+target.id+"/"+step);
                }
                foreach(var entry in frames.Where(p=>p.frame.part>=3))
                {
                    var v=entry.frame;double dx=v.x-(center.x*32-16),dy=v.y-((center.y+1)*32-64);double radius=Math.Sqrt(dx*dx+dy*dy);
                    Check(v.width==64&&v.height==64&&v.atlasX==512+(step%4)*64&&v.atlasY==822&&radius>=18&&radius<=71,"vortex source frame, native bottom anchor and radius bounds "+magic+"/"+step);
                }
                Check(frames.Select(p=>p.key).Distinct().Count()==frames.Length,"render parts have unique stable keys "+magic+"/"+step);
            }
            f.server.AdvanceWorld(40);var ending=BattleMagicPresentation.Frames(f.View.battle.magicEffects);
            Check(f.View.battle.magicEffects.Length==count&&ending.Length==3&&ending.All(p=>p.frame.part>=3&&p.frame.atlasX==512)&&f.View.record.mp==40&&f.View.battle.Unit(400).hp==49,"target clear then registered coordinate sees reset0 once, no replayed damage "+magic);
            f.server.AdvanceWorld(40);Check(f.View.battle.magicEffects.Length==0&&BattleMagicPresentation.Frames(f.View.battle.magicEffects).Length==0&&!f.server.ReceiveBattleUnits(approval).Accepted,"next pass removes registered coordinate and duplicate cannot replay "+magic);
        }
        var golden=new BattleMagicEffect{id="tornado-fixture",magic=801,pattern=pattern.Copy(),role=BattleMagicEffectRole.TargetOnly,targetUnit=400,areaX=10,areaY=10,targetX=4,targetY=4};
        var checks=new[]{(step:1,offsets:new[]{(-14,20),(-9,-20),(33,-37)}),(step:9,offsets:new[]{(8,67),(58,-30),(14,-29)}),(step:11,offsets:new[]{(6,-46),(-19,-14),(41,-24)})};
        foreach(var sample in checks)
        {
            golden.age=sample.step*40;var frames=BattleMagicPresentation.TornadoFrames(golden);
            for(int i=0;i<3;i++)Check(frames[i].x==304+sample.offsets[i].Item1&&frames[i].y==288+sample.offsets[i].Item2,"fixed local RNG native rounding/positive screen-Y offsets "+sample.step+"/"+i);
        }
        var a=golden.Copy();a.age=0;var b=a.Copy();a.Advance(360);for(int i=0;i<9;i++)b.Advance(40);Check(State(BattleMagicPresentation.Frames(new[]{a}))==State(BattleMagicPresentation.Frames(new[]{b})),"coordinate presentation independent of elapsed-time partitioning");
        BattleMagicEffect Owner(int i)=>new BattleMagicEffect{id="ring"+i,targetUnit=400+i,magic=801,pattern=pattern.Copy(),role=BattleMagicEffectRole.TargetOnly,areaX=i,areaY=5,targetX=i,targetY=5,age=40};
        Check(BattleMagicPresentation.TornadoOwners(Array.Empty<BattleMagicEffect>()).Length==0,"empty registry has no coordinate renderer");
        var ring=Enumerable.Range(0,20).Select(Owner).ToArray();
        Check(BattleMagicPresentation.TornadoOwners(ring.Take(14).ToArray()).Length==14,"native coordinate prefix before wrap");
        Check(BattleMagicPresentation.TornadoOwners(ring.Take(15).ToArray()).Select(e=>e.targetUnit).SequenceEqual(new[]{400}),"native modulo15 cursor also bounds render prefix, preserved wrap at15");
        Check(BattleMagicPresentation.TornadoOwners(ring.Take(16).ToArray()).Select(e=>e.targetUnit).SequenceEqual(new[]{415,401}),"native wrapped overwrite and inclusive prefix at16");
        ring[15].areaX=5;var wrapped=BattleMagicPresentation.TornadoOwners(ring);
        Check(wrapped.Length==6&&wrapped.Count(e=>e.areaX==5)==2,"native prefix-local dedup may retain two same-coordinate slots after wrap");
        var wrappedFrames=BattleMagicPresentation.Frames(ring);Check(wrappedFrames.Select(p=>p.key).Distinct().Count()==wrappedFrames.Length&&wrappedFrames.Count(p=>p.frame.part>=3)==18,"wrapped duplicate coordinates keep all distinct render parts");
        var same=Enumerable.Range(0,3).Select(Owner).ToArray();foreach(var e in same)e.areaX=5;Check(BattleMagicPresentation.TornadoOwners(same.Reverse().ToArray()).Single().targetUnit==400,"normal dedup selects lowest native unit ID independently of input order");
        var empty=new Fixture(801);var point=SeedArea(empty);Check(empty.server.RequestBattleMagicAt(801,point.x,point.y,Id()).Accepted&&empty.server.ReceiveBattleUnits(AreaResult(empty,Array.Empty<int>())).Accepted,"explicit empty tornado result");empty.server.AdvanceWorld(40);
        Check(BattleMagicPresentation.Frames(empty.View.battle.magicEffects).Length==1&&BattleMagicPresentation.TornadoOwners(empty.View.battle.magicEffects).Length==0&&empty.View.battle.Unit(400).hp==50,"zero-hit tornado has only native caster charge, no guessed coordinate registration");empty.server.AdvanceWorld(400);Check(empty.View.battle.magicEffects.Length==0,"empty tornado charge cleans up independently");
        var removed=new Fixture(801);point=SeedArea(removed);Check(removed.server.RequestBattleMagicAt(801,point.x,point.y,Id()).Accepted,"removed tornado target queued");var requestTarget=removed.View.battle.pending.magicTargets.Single(t=>t.id==401);var n=AreaResult(removed,new[]{401});n.units=n.units.Where(u=>u.id!=401).ToArray();Check(removed.server.ReceiveBattleUnits(n).Accepted,"removed tornado target result");removed.server.AdvanceWorld(40);
        var owner=BattleMagicPresentation.TornadoOwners(removed.View.battle.magicEffects).Single();Check(owner.targetX==requestTarget.x&&owner.targetY==requestTarget.y&&owner.areaX==point.x&&owner.areaY==point.y,"click coordinate and individual removed target position stay distinct");
        var failure=new Fixture(801);point=SeedArea(failure);Check(failure.server.RequestBattleMagicAt(801,point.x,point.y,Id()).Accepted,"tornado save failure queued");var pending=AreaResult(failure);var before=State(failure.View.record);failure.store.fail=true;
        Check(failure.server.ReceiveBattleUnits(pending).Failure==WorldFailure.StorageUnavailable&&State(failure.View.record)==before&&failure.View.battle.magicEffects.Length==0,"failed save creates no target/caster/coordinate pieces");failure.store.fail=false;Check(failure.server.ReceiveBattleUnits(pending).Accepted,"tornado approval retry");
        foreach(string bad in new[]{"missing-list","duplicate","denied","new-life","cost","charge-data","pattern"})
        {
            var f=new Fixture(801);point=SeedArea(f);Check(f.server.RequestBattleMagicAt(801,point.x,point.y,Id()).Accepted,"invalid tornado queued "+bad);var result=AreaResult(f);before=State(f.View.record);
            switch(bad){case "missing-list":result.magicTargets=null;break;case "duplicate":result.magicTargets=new[]{400,400};break;case "denied":result.magicTargets=new[]{405};break;case "new-life":result.units.Single(u=>u.id==400).life++;break;case "cost":result.units.Single(u=>u.id==0).mp--;break;case "charge-data":f.definition.magicProjectiles.Find(8).charge=null;break;case "pattern":f.definition.magicMenu.Find(801).pattern=4;break;}
            Check(!f.server.ReceiveBattleUnits(result).Accepted&&State(f.View.record)==before&&f.View.battle.magicEffects.Length==0,"invalid tornado has no partial approval "+bad);
        }
        var mixed=new Fixture(spells:new[]{801,802,101});point=SeedArea(mixed);Check(mixed.server.RequestBattleMagicAt(801,point.x,point.y,Id()).Accepted&&mixed.server.ReceiveBattleUnits(AreaResult(mixed)).Accepted,"first shared-coordinate tornado");mixed.server.AdvanceWorld(80);
        Check(mixed.server.RequestBattleMagicAt(802,point.x,point.y,Id()).Accepted&&mixed.server.ReceiveBattleUnits(AreaResult(mixed,new[]{404})).Accepted,"second shared-coordinate subset");mixed.server.AdvanceWorld(40);
        Check(BattleMagicPresentation.TornadoOwners(mixed.View.battle.magicEffects).Length==1&&BattleMagicPresentation.Frames(mixed.View.battle.magicEffects).Count(p=>p.frame.part>=3)==3,"different cast/level at same coordinate does not multiply vortex renderers");mixed.Cast(101,400);
        Check(BattleMagicPresentation.TornadoOwners(mixed.View.battle.magicEffects).Single().targetUnit==401&&mixed.View.battle.magicEffects.All(e=>e.role!=BattleMagicEffectRole.ChargeOnly),"replaced first target moves coordinate ownership to remaining native target ID");
        var scene=new Fixture(801,scenario:true);point=SeedArea(scene);Check(scene.server.RequestBattleMagicAt(801,point.x,point.y,Id()).Accepted&&scene.server.ReceiveBattleUnits(AreaResult(scene)).Accepted,"scenario tornado approved");var restore=new EntryAccountService(scene.store);new EntryClient(restore).OpenLocalProfile();Check(restore.BeginWorld(scene.definition).Accepted&&restore.WorldSnapshot().record.mp==40&&restore.WorldSnapshot().battle.Unit(403).hp==46&&BattleMagicPresentation.Frames(restore.WorldSnapshot().battle.magicEffects).Length==0,"scenario resources/HP restore without target/caster/coordinate replay");
        return spells.Length;
    }
    private static void Main(string[] args)
    {
        root=args[0];var metadata=Read<MagicMenuCatalog>("Data/Combat/MagicMenu.json");var catalog=Read<MagicProjectileCatalog>("Data/Combat/MagicProjectiles.json");
        var supported=metadata.entries.Where(e=>catalog.Supports(e)&&(e.pattern==2||e.pattern==3)&&e.range==0&&e.unit==3).Select(e=>e.id).ToArray();Check(supported.Length==20&&supported.Contains(101)&&supported.Contains(106)&&supported.Contains(4104)&&supported.Contains(301)&&supported.Contains(4304),"native single-target projectile membership includes both ten-level families");
        Check(catalog.Find(2).travel.width==32&&catalog.Find(2).travel.count==3&&catalog.Find(2).impact.count==5,"literal fireball pattern");
        Check(catalog.Find(3).travel.width==40&&catalog.Find(3).travel.count==5&&catalog.Find(3).impact.x==200&&catalog.Find(3).impact.count==6,"literal freeze pattern, no fireball substitution");
        foreach(int spell in supported)
        {
            var f=new Fixture(spell);var entry=f.definition.magicMenu.Find(spell);var state=f.Notice();state.units[1].x=state.units[0].x+entry.distance;Check(f.server.ReceiveBattleUnits(state).Accepted,"explicit target at native range boundary");
            var before=State(f.View.record);f.Queue(spell);var request=f.View.battle.pending;
            Check(request.magic==spell&&request.pattern==entry.pattern&&request.x-f.View.record.x==entry.distance&&State(f.View.record)==before,"exact magic/pattern/range captured without predictive changes: "+spell);
            var result=f.Result();Check(f.server.ReceiveBattleUnits(result).Accepted,"explicit expanded spell approval: "+spell);
            var effect=f.View.battle.magicEffects.Single();var pattern=catalog.Find(entry.pattern);
            Check(effect.pattern.id==entry.pattern&&effect.Width==pattern.travel.width&&effect.Height==pattern.travel.height&&effect.DurationMs==(10+pattern.impact.count)*40&&effect.magic==spell,"own source pattern and non-repeating termination: "+spell);
            for(int counter=1;counter<10;counter++)
            {
                f.server.AdvanceWorld(40);effect=f.View.battle.magicEffects.Single();
                Check(!effect.Impact&&effect.Frame>=0&&effect.Frame<pattern.travel.count&&effect.AtlasX==pattern.travel.x+effect.Frame*pattern.travel.stepX&&effect.AtlasY==pattern.travel.y+effect.Frame*pattern.travel.stepY,"flight frame within actual source strip: "+spell+"/"+counter);
            }
            f.server.AdvanceWorld(40);effect=f.View.battle.magicEffects.Single();
            Check(effect.Impact&&effect.Frame==0&&Math.Abs(effect.DrawX-(request.x*32-(pattern.impact.width-32)/2))<.001&&Math.Abs(effect.DrawY-(request.y*32-(pattern.impact.height-32)/2))<.001,"first impact uses native size/half-size placement: "+spell);
            f.server.AdvanceWorld((pattern.impact.count-1)*40);effect=f.View.battle.magicEffects.Single();Check(effect.Frame==pattern.impact.count-1&&effect.Visible,"last native impact frame: "+spell);
            f.server.AdvanceWorld(40);Check(f.View.battle.magicEffects.Length==0&&f.View.record.mp==40&&f.View.battle.Commander.ap==18&&f.View.battle.Unit(400).hp==40,"terminate exactly at10+impactCount, no resource replay: "+spell);
        }
        var slots=new[]{(0,-32),(0,32),(-32,0),(32,0),(-32,-32),(-32,32),(32,-32),(32,32),(0,0)};
        for(int i=0;i<slots.Length;i++)Check(OriginalBattleMagicMenu.OffsetX(i)==slots[i].Item1&&OriginalBattleMagicMenu.OffsetY(i)==slots[i].Item2,"original surrounding menu order slot"+i);
        var mixed=new Fixture(spells:new[]{101,301});AddExtraTargets(mixed,1);mixed.Cast(101);mixed.Cast(301,401);Check(mixed.View.battle.magicEffects.Select(e=>e.pattern.id).SequenceEqual(new[]{2,3}),"mixed simultaneous effects keep own patterns");
        var copied=mixed.View.battle.magicEffects;copied[1].pattern.travel.width=999;Check(mixed.View.battle.magicEffects[1].pattern.travel.width==40&&mixed.definition.magicProjectiles.Find(3).travel.width==40,"presentation metadata copies do not mutate live effect/catalog");
        mixed.server.AdvanceWorld(600);Check(mixed.View.battle.magicEffects.Length==1&&mixed.View.battle.magicEffects[0].magic==301&&mixed.View.battle.magicEffects[0].Frame==5,"fireball ends first while six-frame freeze remains");mixed.server.AdvanceWorld(40);Check(mixed.View.battle.magicEffects.Length==0,"freeze ends at its own frame count");
        foreach(string problem in new[]{"absent-catalog","absent-pattern","type","invalid-frame","area","target-mask"})
        {
            var f=new Fixture(301);var before=State(f.View.record);
            switch(problem)
            {case "absent-catalog":f.definition.magicProjectiles=null;break;case "absent-pattern":f.definition.magicMenu.Find(301).pattern=999;break;case "type":f.definition.magicProjectiles.Find(3).type=0;break;case "invalid-frame":f.definition.magicProjectiles.Find(3).travel.x=999;break;case "area":f.definition.magicMenu.Find(301).range=2;break;case "target-mask":f.definition.magicMenu.Find(301).unit=0;break;}
            Check(f.server.RequestBattleMagic(301,400,Id()).Failure==WorldFailure.RulesUnavailable&&State(f.View.record)==before&&f.View.battle.pending==null,"missing/unsupported patterns fail without fallback: "+problem);
        }
        var wrongDistance=new Fixture(4304);var far=wrongDistance.Notice();far.units[1].x=far.units[0].x+8;far.units[1].y++;Check(wrongDistance.server.ReceiveBattleUnits(far).Accepted&&wrongDistance.server.RequestBattleMagic(4304,400,Id()).Failure==WorldFailure.InvalidInput,"range8 remains diamond at high level");
        var frozen=new Fixture(301);frozen.Queue(301);var kill=frozen.Result(kill:true,omit:true);var requestFreeze=frozen.View.battle.pending;Check(frozen.server.ReceiveBattleUnits(kill).Accepted,"freeze supports approved target disappearance");frozen.server.AdvanceWorld(400);var freezeFx=frozen.View.battle.magicEffects.Single();Check(freezeFx.DrawX==requestFreeze.x*32-4&&freezeFx.DrawY==requestFreeze.y*32-4&&freezeFx.Width==40,"disappeared freeze target keeps original40px effect anchor");
        var changed=new Fixture(301);changed.Queue(301);var approval=changed.Result();var previous=State(changed.View.record);changed.definition.magicProjectiles.patterns=Array.Empty<MagicProjectilePattern>();Check(!changed.server.ReceiveBattleUnits(approval).Accepted&&State(changed.View.record)==previous&&changed.View.battle.magicEffects.Length==0,"pattern removal before approval does not spend or display stale art");
        var failed=new Fixture(301);failed.Queue(301);var frozenApproval=failed.Result();previous=State(failed.View.record);failed.store.fail=true;Check(failed.server.ReceiveBattleUnits(frozenApproval).Failure==WorldFailure.StorageUnavailable&&State(failed.View.record)==previous&&failed.View.battle.magicEffects.Length==0,"freeze save failure emits no wrong-pattern effect or resource spend");failed.store.fail=false;Check(failed.server.ReceiveBattleUnits(frozenApproval).Accepted&&failed.View.battle.magicEffects.Single().pattern.id==3,"same freeze approval retries with correct pattern");
        var scenario=new Fixture(301,scenario:true);scenario.Cast(301);var server=new EntryAccountService(scenario.store);new EntryClient(server).OpenLocalProfile();Check(server.BeginWorld(scenario.definition).Accepted&&server.WorldSnapshot().battle.Commander.mp==40&&server.WorldSnapshot().battle.Commander.magicRules[0].magic==301&&server.WorldSnapshot().battle.magicEffects.Length==0,"freeze scenario resources/rule restore without effect replay");
        var thunders=metadata.entries.Where(e=>catalog.Supports(e)&&catalog.Find(e.pattern).type==2&&e.range==0).Select(e=>e.id).ToArray();
        Check(thunders.SequenceEqual(new[]{501,502,503,504,505,506,4501,4502,4503,4504}),"only ten native thunder magic levels; skill mappings remain held");
        Check(!catalog.Supports(metadata.Find(12101))&&!catalog.Supports(metadata.Find(10101))&&!catalog.Supports(metadata.Find(30801)),"same-pattern skill and unrelated Unit8 skills are not silently enabled");
        Check(catalog.Find(5).travel.width==50&&catalog.Find(5).travel.height==350&&catalog.Find(5).travel.count==5&&catalog.Find(5).impact.count==4,"literal thunder tall bolt and ground strips");
        foreach(int spell in thunders)
        {
            var f=new Fixture(spell);var entry=f.definition.magicMenu.Find(spell);var n=f.Notice();n.units[1].x=n.units[0].x+entry.distance;
            Check(f.server.ReceiveBattleUnits(n).Accepted,"thunder native distance boundary "+spell);var saved=State(f.View.record);f.Queue(spell);var request=f.View.battle.pending;
            Check(request.pattern==5&&request.magic==spell&&State(f.View.record)==saved&&f.View.battle.magicEffects.Length==0,"no predicted thunder resources or effect "+spell);
            var thunderPerApproval=f.Result();Check(f.server.ReceiveBattleUnits(thunderPerApproval).Accepted,"thunder approval "+spell);Check(f.View.battle.magicEffects.Single().Frames.Length==0,"thunder waits first display tick "+spell);
            int steps=0;
            while(f.View.battle.magicEffects.Length>0&&steps<256)
            {
                f.server.AdvanceWorld(40);steps++;if(f.View.battle.magicEffects.Length==0)break;
                var effect=f.View.battle.magicEffects.Single();var frames=effect.Frames;var bolt=frames.Single(t=>t.part==0);var impact=frames.Single(t=>t.part==1);
                Check(bolt.width==50&&bolt.height==350&&bolt.x==request.x*32-9&&bolt.y==(request.y+1)*32-350&&bolt.atlasY==208&&bolt.atlasX>=0&&bolt.atlasX<=200&&bolt.atlasX%50==0,"tall thunder stays anchored to target cell bottom "+spell+"/"+steps);
                Check(impact.width==32&&impact.height==32&&impact.x==request.x*32&&impact.y==request.y*32&&impact.atlasY==208&&Enumerable.Range(0,20).Any(c=>bolt.atlasX==(c/2)%5*50&&impact.atlasX==250+c%4*32),"simultaneous ground/bolt use same previous counter "+spell+"/"+steps);
                Check(frames.Count(t=>t.part==2)==(steps<=10?1:0),"caster charge ends independently after ten display steps "+spell+"/"+steps);
                if(steps<=10)
                {
                    var charge=frames.Single(t=>t.part==2);
                    Check(charge.width==40&&charge.height==40&&charge.x==f.View.record.x*32-4&&charge.y==f.View.record.y*32-4&&charge.atlasX==(steps-1)%5*40&&charge.atlasY==128,"source charge source frame/anchor "+spell+"/"+steps);
                }
                if(steps==1)Check(bolt.atlasX==0&&impact.atlasX==250,"pre-initialization draw counter is zero "+spell);
            }
            Check(steps>=17&&steps<256&&f.View.battle.magicEffects.Length==0&&f.View.record.mp==40&&f.View.battle.Commander.ap==18&&f.View.battle.Unit(400).hp==40,"thunder expires without repeated resource or damage application "+spell);
            Check(!f.server.ReceiveBattleUnits(thunderPerApproval).Accepted&&f.View.battle.magicEffects.Length==0&&f.View.record.mp==40,"expired thunder duplicate notice does not replay "+spell);
        }
        var nativeTrace=new BattleThunderPlayback("thunder0");var counters=new[]{0,5,6,7,8,8,9,9,9,10,11,12,13,13,14,15,16,17,18,19};
        for(int step=1;step<=counters.Length;step++){nativeTrace.AdvanceTo(step);Check(!nativeTrace.Finished&&nativeTrace.DrawCounter==counters[step-1],"native pre-update/stall counter trace "+step);}
        Check(nativeTrace.StoredCounter==20&&nativeTrace.Visible,"counter19 still draws while stored20 awaits next call");nativeTrace.AdvanceTo(21);Check(nativeTrace.Finished&&!nativeTrace.Visible,"counter20 ends on next renderer call, no frame20");
        var a=new BattleMagicEffect{id="thunder0",pattern=catalog.Find(5).Copy(),sourceX=2,sourceY=3,targetX=6,targetY=3};var b=a.Copy();
        a.Advance(400);for(int tick=0;tick<10;tick++)b.Advance(40);Check(State(a.Frames)==State(b.Frames),"large/small elapsed steps share deterministic independent thunder timeline");
        var snapshot=a.Copy();snapshot.Advance(400);Check(a.Frames.Length==3&&snapshot.Frames.All(t=>t.part!=2),"snapshot advancement cannot mutate live thunder playback state");
        var edge=new BattleMagicEffect{id="thunder0",pattern=catalog.Find(5).Copy(),targetX=0,targetY=0};edge.Advance(40);Check(edge.Frames.Single(t=>t.part==0).x==-9&&edge.Frames.Single(t=>t.part==0).y==-318,"edge thunder keeps original overhang for viewport clipping, no clamping to map or caster");
        var broken=new Fixture(501);broken.definition.magicProjectiles.Find(5).charge=null;Check(broken.server.RequestBattleMagic(501,400,Id()).Failure==WorldFailure.RulesUnavailable,"missing caster data blocks thunder rather than falling back to fireball");
        var masked=new Fixture(501);masked.definition.magicProjectiles.magic=Array.Empty<int>();Check(masked.server.RequestBattleMagic(501,400,Id()).Failure==WorldFailure.RulesUnavailable,"missing verified spell membership blocks cast");
        var insufficient=new Fixture(501);insufficient.Rule(r=>r.mpCost=46);var thunderSaved=State(insufficient.View.record);Check(insufficient.server.RequestBattleMagic(501,400,Id()).Failure==WorldFailure.InsufficientResource&&State(insufficient.View.record)==thunderSaved,"thunder insufficient explicitly supplied MP holds state");
        var outOfRange=new Fixture(4504);var outside=outOfRange.Notice();outside.units[1].x=outside.units[0].x+outOfRange.definition.magicMenu.Find(4504).distance;outside.units[1].y++;Check(outOfRange.server.ReceiveBattleUnits(outside).Accepted&&outOfRange.server.RequestBattleMagic(4504,400,Id()).Failure==WorldFailure.InvalidInput,"thunder high level range remains diamond");
        var failedThunder=new Fixture(501);failedThunder.Queue(501);var thunderApproval=failedThunder.Result(kill:true,omit:true);var thunderRequest=failedThunder.View.battle.pending;thunderSaved=State(failedThunder.View.record);failedThunder.store.fail=true;
        Check(failedThunder.server.ReceiveBattleUnits(thunderApproval).Failure==WorldFailure.StorageUnavailable&&State(failedThunder.View.record)==thunderSaved&&failedThunder.View.battle.magicEffects.Length==0,"failed thunder save emits no layers or resource spend");failedThunder.store.fail=false;
        Check(failedThunder.server.ReceiveBattleUnits(thunderApproval).Accepted,"thunder save retry");failedThunder.server.AdvanceWorld(40);var removed=failedThunder.View.battle.magicEffects.Single().Frames.Single(t=>t.part==0);Check(removed.x==thunderRequest.x*32-9&&removed.y==(thunderRequest.y+1)*32-350,"removed target thunder keeps approved target anchor");
        var combination=new Fixture(spells:new[]{101,301,501});AddExtraTargets(combination,2);combination.Cast(101);combination.Cast(301,401);combination.Cast(501,402);combination.server.AdvanceWorld(40);Check(combination.View.battle.magicEffects.Length==3&&combination.View.battle.magicEffects.Sum(e=>e.Frames.Length)==5,"projectile and multi-part thunder coexist with independent frame metadata");combination.server.AdvanceWorld(600);Check(combination.View.battle.magicEffects.All(e=>e.IsThunder),"projectile expiration does not remove still-active thunder");
        var thunderScenario=new Fixture(501,scenario:true);thunderScenario.Cast(501);var restored=new EntryAccountService(thunderScenario.store);new EntryClient(restored).OpenLocalProfile();Check(restored.BeginWorld(thunderScenario.definition).Accepted&&restored.WorldSnapshot().record.mp==40&&restored.WorldSnapshot().battle.Commander.magicRules[0].magic==501&&restored.WorldSnapshot().battle.magicEffects.Length==0,"approved scenario thunder resources restore with no visual replay");
        int areaSupported=CheckAreaMagic(metadata,catalog);int storms=CheckThunderstorm(metadata,catalog);int groups=CheckWindGroup(metadata,catalog);int tornadoes=CheckTornado(metadata,catalog);
        int all=CheckAllMagic(metadata,catalog);
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlayExecuted=false,supportedSpells=all,renderFamilies=catalog.patterns.Select(p=>p.type).Distinct().Count(),generalCastAndEventSweep=all*2,legacyFamilySpells=supported.Length+thunders.Length+areaSupported+storms+groups+tornadoes,tornadoSpells=tornadoes,groupSpells=catalog.groupMagic.Length,areaSpells=catalog.areaMagic.Length,noEffectSpells=metadata.entries.Count(e=>catalog.Supports(e)&&catalog.Find(e.pattern).type==0),stormSpells=storms,scope="All289 general spells,16 native families incl no-effect12; allied/self/dead selection; explicit healing, HP/MP absorption, resurrection, statuses and event outcomes; Fx/Fx2 frames and sound cues; no inferred server formulas or actual Play/timing/listening claim."}));
    }
}
