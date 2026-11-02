using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;
using DarkSaver.Original.Campaign;

internal static class BattleMagicChecks
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
        public Memory store=new Memory();public EntryAccountService server;public WorldDefinition definition;public bool scenario;
        public WorldView View=>server.WorldSnapshot();
        public Fixture(bool scenario=false,bool loadout=true,int[] selected=null)
        {
            this.scenario=scenario;definition=Read<WorldDefinition>("Data/World/KaosiaWorld.json");definition.growthCatalog=Read<CharacterGrowthCatalog>("Data/Growth/CharacterGrowth.json");definition.combatCatalog=Read<OriginalCombatCatalog>("Data/Combat/OriginalCombat.json");definition.magicMenu=Read<MagicMenuCatalog>("Data/Combat/MagicMenu.json");definition.magicProjectiles=Read<MagicProjectileCatalog>("Data/Combat/MagicProjectiles.json");
            if(scenario)
            {
                var campaign=Read<CampaignDefinition>("Data/Campaign/CampaignCatalog.json");var map=campaign.Map(26);var spawn=map.entries.First(e=>e.unit==0);
                using var manifest=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"Tools/OriginalWorldChecks/campaign-assets.json")));var scripts=manifest.RootElement.GetProperty("scripts").EnumerateArray().Select(p=>p.GetString()).ToArray();
                map.intro=map.introIndex<0?"":File.ReadAllText(Path.Combine(root,scripts[map.introIndex]));map.outro=map.outroIndex<0?"":File.ReadAllText(Path.Combine(root,scripts[map.outroIndex]));campaign.connections=new[]{new CampaignConnection{fromMap=1000,toMap=26,x=spawn.x,y=spawn.y,evidence="isolated cast fixture only",cells=new[]{new WorldPoint{x=21,y=21}}}};definition=campaign.Merge(definition);
            }
            server=new EntryAccountService(store);var client=new EntryClient(server);client.OpenLocalProfile();client.Create(100,"시전검사");var c=store.data.accounts.Single().character;c.tutorialCompleted=true;c.world=new WorldRecord{healthKnown=true,hp=200,maxHp=200,mp=45,maxMp=65};client.OpenLocalProfile();Check(server.BeginWorld(definition).Accepted,"isolated world");string id=Id();
            Check(server.ReceiveCharacterGrowth(new CharacterGrowthNotice{id=id,approved=true,character=View.characterId,session=View.growthSession,state=new CharacterGrowthRecord{job=100,classLevel=1,totalLevel=1,revision=1,lastNotice=id,experience=123,magic=new[]{101,301},skills=new[]{10101}}}).Accepted,"explicit owned magic");
            if(loadout)
            {
                var choices=selected??new[]{101};Check(server.ExecuteMagicLoadout(MagicLoadoutOperation.State,-1,Id(),(r,v)=>new MagicLoadoutNotice{id=Id(),request=r.Copy(),approved=true,learned=new[]{101,301},selected=choices}).Accepted,"explicit editor snapshot");
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
            Check(server.ReceiveBattleUnits(Notice(new[]{Unit(0,100,x,y),Unit(400,302,targetX,y)})).Accepted,"explicit roster/permissions/costs, no default enemies");
        }
        public TrainingUnit Unit(int id,int job,int x,int y)=>new TrainingUnit{id=id,job=job,x=x,y=y,life=1,hp=id==0?200:50,maxHp=id==0?200:50,mp=id==0?45:0,maxMp=id==0?65:0,ap=20,maxAp=20,canMove=true,canAttack=true,canRest=true,movementBudget=5,attackRange=1,
            magicRules=id==0?new[]{new BattleMagicRule{magic=101,enabled=true,mpCost=5,apCost=2,targets=new[]{400}}}:null};
        public TrainingUnitsNotice Notice(TrainingUnit[] units=null)=>new TrainingUnitsNotice{id=Id(),character=View.characterId,session=View.campaignSession,approved=true,expectedRevision=View.battle?.revision??0,units=units??View.battle.units.Select(u=>u.Copy()).ToArray()};
        public TrainingUnitsNotice Result(TrainingActionRequest r=null,bool kill=false,bool omit=false)
        {
            r=r??View.battle.pending;var n=Notice();n.request=r.id;var caster=n.units.First(u=>u.id==0);caster.mp-=r.mpCost;caster.ap-=r.apCost;caster.direction=DarkSaver.Original.Tutorial.TutorialGeometry.FacingRow(r.x-caster.x,r.y-caster.y);
            var enemy=n.units.First(u=>u.id==r.target);enemy.hp=kill?0:Math.Max(0,enemy.hp-10);if(omit)n.units=n.units.Where(u=>u.id!=r.target).ToArray();return n;
        }
        public void Queue()=>Check(server.RequestBattleMagic(101,400,Id()).Accepted,"valid cast queued");
        public void Rule(Action<BattleMagicRule> change)
        {var n=Notice();change(n.units[0].magicRules[0]);Check(server.ReceiveBattleUnits(n).Accepted,"explicit updated cast rule");}
        public void Cast()=>Check(server.ExecuteBattleAction(TrainingAction.CastMagic,400,0,0,(r,v)=>Result(r),101).Accepted,"local explicit cast approval");
    }
    private static void Main(string[] args)
    {
        root=args[0];var f=new Fixture();var before=State(f.View.record);var battle=State(f.View.battle.units);
        Check(f.server.ExecuteBattleAction(TrainingAction.CastMagic,400,0,0,null,101).Failure==WorldFailure.RulesUnavailable&&f.View.battle.pending==null,"absent authority does not consume or lock");
        Check(new Fixture(loadout:false).server.RequestBattleMagic(101,400,Id()).Failure==WorldFailure.RulesUnavailable,"no confirmed battle selection means no cast");
        var unselected=new Fixture(selected:new[]{301});Check(unselected.server.RequestBattleMagic(101,400,Id()).Failure==WorldFailure.NotOwned,"owned but unselected magic cannot cast");
        Check(f.server.RequestBattleMagic(10101,400,Id()).Failure==WorldFailure.RulesUnavailable,"unsupported magic never reuses fireball effect");
        Check(f.server.RequestBattleMagic(101,0,Id()).Failure==WorldFailure.InvalidInput&&f.server.RequestBattleMagic(101,999,Id()).Failure==WorldFailure.InvalidInput,"fireball rejects self/absent target");
        f.Queue();var queued=f.View.battle.pending;Check(queued.magic==101&&queued.mpCost==5&&queued.apCost==2&&queued.targetLife==1&&queued.sourceLife==1,"explicit cost, selected spell and target life captured");
        queued.magic=301;Check(f.View.battle.pending.magic==101,"pending request is copied");Check(State(f.View.record)==before&&State(f.View.battle.units)==battle&&f.View.battle.magicEffects.Length==0,"request alone changes no MP/AP/HP or animation");
        Check(f.server.RequestBattleMagic(101,400,Id()).Failure==WorldFailure.Busy,"one pending combat action");
        var notice=f.Result();f.store.fail=true;Check(f.server.ReceiveBattleUnits(notice).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==before&&f.View.battle.pending!=null&&f.View.battle.magicEffects.Length==0,"failed save preserves resources/HP and emits no effect");
        f.store.fail=false;Check(f.server.ReceiveBattleUnits(notice).Accepted,"same approved cast retries");
        Check(f.View.record.mp==40&&f.View.battle.Commander.ap==18&&f.View.battle.Unit(400).hp==40&&f.View.record.growth.experience==123&&f.View.record.growth.magic.SequenceEqual(new[]{101,301}),"only explicit result/costs applied; no XP or unlearning formula");
        Check(f.View.battle.magicEffects.Length==1&&!f.View.battle.magicEffects[0].Visible&&f.View.battle.impact==null,"one approved fireball, no duplicated physical contact effect");
        before=State(f.View.record);Check(!f.server.ReceiveBattleUnits(notice).Accepted&&State(f.View.record)==before&&f.View.battle.magicEffects.Length==1,"duplicate cast cannot spend or animate twice");
        var rules=f.View.battle.Commander.magicRules;rules[0].targets[0]=999;Check(f.View.battle.Commander.magicRules[0].targets[0]==400,"nested authority rules copied");
        f.server.AdvanceWorld(40);var effect=f.View.battle.magicEffects[0];Check(effect.Visible&&!effect.Impact&&effect.Counter==1,"native40ms presentation phase");
        f.server.AdvanceWorld(360);effect=f.View.battle.magicEffects[0];Check(effect.Impact&&Math.Abs(effect.PixelX-effect.targetX*32)<.001&&Math.Abs(effect.PixelY-effect.targetY*32)<.001,"flight reaches frozen endpoint before impact frames");
        f.server.AdvanceWorld(800);Check(f.View.battle.magicEffects.Length==0,"expired presentation released");
        foreach(string rule in new[]{"disabled","missing-mp","missing-ap","missing-targets","denied-target","mp","ap"})
        {
            var bad=new Fixture();bad.Rule(r=>{switch(rule){case "disabled":r.enabled=false;break;case "missing-mp":r.mpCost=null;break;case "missing-ap":r.apCost=null;break;case "missing-targets":r.targets=null;break;case "denied-target":r.targets=Array.Empty<int>();break;case "mp":r.mpCost=46;break;case "ap":r.apCost=21;break;}});
            var state=State(bad.View.record);bool called=false;var reply=bad.server.ExecuteBattleAction(TrainingAction.CastMagic,400,0,0,(r,v)=>{called=true;return bad.Result(r);},101);
            Check(!reply.Accepted&&!called&&bad.View.battle.pending==null&&State(bad.View.record)==state,"withheld permission/cost/short resource rejected before authority: "+rule);
        }
        var zero=new Fixture();zero.Rule(r=>{r.mpCost=0;r.apCost=0;});zero.Cast();Check(zero.View.record.mp==45&&zero.View.battle.Commander.ap==20&&zero.View.battle.Unit(400).hp==40,"explicit zero costs distinct from missing costs");
        var far=new Fixture();var outOfRange=far.Notice();outOfRange.units[1].x=far.View.record.x+3;outOfRange.units[1].y=far.View.record.y+1;Check(far.server.ReceiveBattleUnits(outOfRange).Accepted,"explicit diagonal distant target");Check(far.server.RequestBattleMagic(101,400,Id()).Failure==WorldFailure.InvalidInput,"range3 uses native diamond, not square");
        var dead=new Fixture();var deadState=dead.Notice();deadState.units[1].hp=0;Check(dead.server.ReceiveBattleUnits(deadState).Accepted&&dead.server.RequestBattleMagic(101,400,Id()).Failure==WorldFailure.InvalidInput,"dead enemy cannot be targeted with fireball");
        foreach(string field in new[]{"mp","ap","max-mp","max-ap","source-life","target-life","request","id","revision","negative-rule"})
        {
            var bad=new Fixture();bad.Queue();var n=bad.Result();var state=State(bad.View.record);
            switch(field)
            {case "mp":n.units[0].mp++;break;case "ap":n.units[0].ap++;break;case "max-mp":n.units[0].maxMp++;break;case "max-ap":n.units[0].maxAp++;break;case "source-life":n.units[0].life++;break;case "target-life":n.units[1].life++;break;case "request":n.request=Id();break;case "id":n.id="bad";break;case "revision":n.expectedRevision++;break;case "negative-rule":n.units[0].magicRules[0].mpCost=-1;break;}
            Check(!bad.server.ReceiveBattleUnits(n).Accepted&&State(bad.View.record)==state&&bad.View.battle.magicEffects.Length==0,"malformed cast approval preserves resources/presentation: "+field);
        }
        foreach(bool omit in new[]{false,true})
        {
            var kill=new Fixture();kill.Queue();var n=kill.Result(kill:true,omit:omit);var target=kill.View.battle.Unit(400);Check(kill.server.ReceiveBattleUnits(n).Accepted,"approved death/removal");kill.server.AdvanceWorld(400);var fx=kill.View.battle.magicEffects.Single();
            Check(fx.targetX==target.x&&fx.targetY==target.y&&Math.Abs(fx.PixelX-target.x*32)<.001&&Math.Abs(fx.PixelY-target.y*32)<.001,"dead/disappeared target does not move effect to caster");
        }
        var overlap=new Fixture();overlap.Cast();overlap.Cast();Check(overlap.View.battle.magicEffects.Length==1&&overlap.View.record.mp==35&&overlap.View.battle.Commander.ap==16,"new approved magic replaces the same target slot effect while both approved costs persist");
        var update=new Fixture();update.Queue();var old=update.Result();Check(update.server.ReceiveBattleUnits(update.Notice()).Accepted,"unsolicited new roster snapshot");Check(!update.server.ReceiveBattleUnits(old).Accepted&&update.View.record.mp==45&&update.View.battle.magicEffects.Length==0,"late cast from older battle revision cannot spend or animate");
        foreach(string mode in new[]{"null","throw","wrong-request","denied"})
        {
            var provider=new Fixture();var state=State(provider.View.record);var reply=provider.server.ExecuteBattleAction(TrainingAction.CastMagic,400,0,0,(r,v)=>{if(mode=="null")return null;if(mode=="throw")throw new Exception("fixture");var n=provider.Result(r);if(mode=="wrong-request")n.request=Id();else n.approved=false;return n;},101);
            Check(!reply.Accepted&&provider.View.battle.pending==null&&State(provider.View.record)==state&&provider.View.battle.magicEffects.Length==0,"authority failure unlocks with no speculative cast: "+mode);
        }
        var scenario=new Fixture(scenario:true);scenario.Cast();Check(scenario.View.record.scenarioBattle.units[0].mp==40&&scenario.View.record.scenarioBattle.units[0].ap==18&&scenario.View.record.campaignPhase=="explore","scenario approved resources persist without automatic clear");
        var restored=new EntryAccountService(scenario.store);new EntryClient(restored).OpenLocalProfile();Check(restored.BeginWorld(scenario.definition).Accepted,"scenario reconnect");var restoredView=restored.WorldSnapshot();
        Check(restoredView.battle.Commander.mp==40&&restoredView.battle.Commander.ap==18&&restoredView.battle.Commander.magicRules[0].mpCost==5&&restoredView.battle.magicEffects.Length==0,"approved combat/rules restore; transient effect never replays on login");
        var path=Path.Combine(Path.GetTempPath(),"ds-battle-magic-"+Id()+".json");
        try
        {var disk=new LocalAccountFileStore(path);Check(disk.TryWrite(scenario.store.data)&&disk.TryRead(out var document)&&document.accounts[0].character.world.scenarioBattle.units[0].magicRules[0].targets[0]==400,"nested cast rules actual file round trip");}
        finally{if(File.Exists(path))File.Delete(path);}
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlayExecuted=false,scope="Original fireball101 selected-magic/permission/explicit MP-AP/target-life requests, approved combat persistence and fixed-endpoint presentation; no default damage, casting policy, all-spell renderer or audio/timing measurement claim."}));
    }
}
