using DarkSaver.Core;
using DarkSaver.Events;

internal static class CampaignChecks
{
    public static void Run(Action<bool,string> check)
    {
        var definitions=ScenarioCatalog.All.Where(r=>r.Key!="01").ToDictionary(r=>r.Key,r=>UnityEngine.JsonUtility.FromJson<Scenario01Definition>(File.ReadAllText("Assets/_Game/Resources/Scenarios/"+r.Key+"/Content.json")));
        CheckSecondBridge(definitions["02"], check);
        var ten=UnityEngine.JsonUtility.FromJson<Scenario01Definition>(File.ReadAllText("Assets/_Game/Resources/Scenarios/10/Content.json"));
        ten.outro=new[]{new Scenario01Command {name="GET_GENERAL",args=new[]{"109"}},new Scenario01Command {name="END"}};
        var tenState=Scenario01Progress.Create(ten);tenState.phase=Scenario01Progress.Outro;
        check(Scenario01Rules.Step(ten,tenState,new NativeInventory(),0,out var skippedGeneral,out _)==Scenario01Rules.StepResult.Advanced
            && skippedGeneral.unlockedGenerals.Count==0,"original scenario10 underscored spelling grants no character");
        ten.outro[0].name="GETGENERAL";
        check(Scenario01Rules.Step(ten,tenState,new NativeInventory(),0,out var grantedGeneral,out _)==Scenario01Rules.StepResult.Advanced
            && grantedGeneral.unlockedGenerals.SequenceEqual(new[]{109}),"normal GETGENERAL spelling still records the source character");
        check(ScenarioCatalog.All.Count==25,"twenty-five scenario destinations are registered");
        var legacy=UnityEngine.JsonUtility.FromJson<Scenario01Definition>(File.ReadAllText("Assets/_Game/Resources/Scenarios/01/Content.json"));
        var first=Scenario01Progress.Create(legacy,true);
        var states=new Dictionary<string,Scenario01Progress>();var items=new NativeInventory();
        var directory=Path.Combine(Path.GetTempPath(),"ds-campaign-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        var store=new NativeSaveStore(Path.Combine(directory,"campaign.json"));
        try
        {
            foreach(var pair in definitions)
            {
                string key=pair.Key;var definition=pair.Value;
                check(definition.IsValid,"scenario"+key+" source definition validates");
                check(ScenarioCatalog.TryGet(key,out var metadata) && NativeSaveStore.IsSupportedMap(metadata.MapScene,metadata.RuntimeScene),"scenario"+key+" map pair is supported");
                var state=Scenario01Progress.Create(definition);state.rng=73;
                void Checkpoint()
                {
                    states[key]=state;
                    var save=new NativeSaveData {format=NativeSaveStore.Format,version=NativeSaveStore.CampaignVersion,
                        mapScene=metadata.MapScene,runtimeScene=metadata.RuntimeScene,worldX=definition.entryX+.5f,worldY=definition.height-1-definition.entryY+.5f,
                        hasCharacter=true,character=new NativeCharacterProfile {name="연속검증",jobId=100},hasInventory=true,inventory=items,
                        hasScenario01=true,scenario01=first,campaign=states.OrderBy(r=>r.Key).Select(r=>r.Value.Clone()).ToArray(),
                        clearedScenarios=new[]{"01"}.Concat(states.Where(r=>r.Value.phase==Scenario01Progress.Complete).Select(r=>r.Key)).ToArray()};
                    check(store.TrySave(save,out var reason),"campaign"+key+" checkpoint saves: "+reason);
                    check(store.TryLoad(out var loaded,out _),"campaign"+key+" checkpoint reloads");
                    states=loaded.campaign.ToDictionary(s=>s.scenarioKey);state=states[key];items=loaded.inventory;
                }
                void RunScript()
                {
                    int limit=0;
                    while((state.phase==Scenario01Progress.Intro || state.phase==Scenario01Progress.Outro || state.activeEvent!=0) && limit++<20000)
                    {
                        var commands=Scenario01Rules.CurrentCommands(definition,state);
                        string context=key+":"+(state.cursor<commands.Length?commands[state.cursor].name+" line"+commands[state.cursor].line:"end");
                        try
                        {
                            var result=Scenario01Rules.Step(definition,state,items,100,out var next,out var inventory);
                            if(result==Scenario01Rules.StepResult.Waiting)
                            {
                                if(Scenario01Rules.AdvanceDialogue(definition,state,out var page))state=page;
                                else Scenario01Rules.AdvanceTime(state,1,6);
                            }
                            else {state=next;items=inventory;}
                            Checkpoint();
                        }
                        catch(Exception ex){throw new Exception("Scenario command failed "+context,ex);}
                    }
                    check(limit<20000,"scenario"+key+" script terminates");
                }
                bool Pump(int x,int y)
                {
                    if(!Scenario01Rules.TryActivate(definition,state,items,x,y,out var next,out var inventory))return false;
                    state=next;items=inventory;Checkpoint();RunScript();return true;
                }
                Checkpoint();RunScript();check(state.phase==Scenario01Progress.Playing,"scenario"+key+" intro opens play");
                int turns=0;
                while(state.phase!=Scenario01Progress.Complete && turns++<1000)
                {
                    var enemy=definition.units.Where(u=>u.wave==state.wave && !state.defeated.Contains(u.id)).OrderBy(u=>u.id).FirstOrDefault();
                    if(enemy==null)break;
                    state.pendingBattle=enemy.id;
                    check(Scenario01Rules.RecordBattleReturn(definition,state,enemy.id,true),"scenario"+key+" confirmed battle-return fixture");Checkpoint();
                    for(int events=0;events<500 && state.phase!=Scenario01Progress.Complete;events++)if(!Pump(definition.entryX,definition.entryY))break;
                }
                if(state.phase!=Scenario01Progress.Complete)
                    foreach(var condition in definition.Event(99).conditions.Where(c=>c.name=="CHARPOS"))
                    {if(state.phase==Scenario01Progress.Complete)break;Pump(condition.Int(0),condition.Int(1));}
                check(state.phase==Scenario01Progress.Complete,"scenario"+key+" reaches source clear and ending");
                Checkpoint();check(state.Matches(definition),"scenario"+key+" complete save remains valid");
                check(!Scenario01Rules.TryActivate(definition,state,items,definition.entryX,definition.entryY,out _,out _),"scenario"+key+" completed rewards cannot replay");
            }
            check(states.Count==24 && states.Values.All(s=>s.phase==Scenario01Progress.Complete),"campaign02-17 and eight late scenarios retain every completed state");
            var entrances=UnityEngine.JsonUtility.FromJson<CampaignEntranceData>(File.ReadAllText("Assets/_Game/Resources/Scenarios/Shared/Entrances.json"));
            check(entrances.IsValid,"all25 physical scenario entrances validate");
            foreach(var key in new[]{"02","08","12"})
            {
                var definition=definitions[key];
                check(definition.endings.Length==2 && definition.defaultEnding=="A","source ending A/B variants are retained with explicit local default");
                var variant=Scenario01Progress.Create(definition);variant.phase=Scenario01Progress.Outro;variant.endingVariant="B";
                var variantItems=new NativeInventory();var variantStore=new NativeSaveStore(Path.Combine(directory,"variant-"+key+".json"));
                int limit=0;
                while(variant.phase!=Scenario01Progress.Complete && limit++<20000)
                {
                    var result=Scenario01Rules.Step(definition,variant,variantItems,100,out var next,out var economy);
                    if(result==Scenario01Rules.StepResult.Waiting)
                    {if(Scenario01Rules.AdvanceDialogue(definition,variant,out var page))variant=page;else Scenario01Rules.AdvanceTime(variant,1,6);}
                    else {variant=next;variantItems=economy;}
                    var snapshot=new NativeSaveData {format=NativeSaveStore.Format,version=NativeSaveStore.CampaignVersion,
                        mapScene="Kaosia_Map",runtimeScene="Town_Runtime",hasCharacter=true,character=new NativeCharacterProfile {name="분기",jobId=100},
                        hasInventory=true,inventory=variantItems,campaign=new[]{variant},clearedScenarios=variant.phase==Scenario01Progress.Complete?new[]{key}:Array.Empty<string>()};
                    check(variantStore.TrySave(snapshot,out _) && variantStore.TryLoad(out var loaded,out _) && loaded.campaign[0].endingVariant=="B","alternate ending cursor/choice survives real disk reload");
                }
                check(limit<20000 && variant.phase==Scenario01Progress.Complete,"source alternate endingB terminates");
                check(variant.actors.All(a=>!a.partyProxy),"single-player cinematic party proxies disappear after ending");
            }
            var randomEvent=new Scenario01Event {actions=new[]{new Scenario01Command {name="RANDOM",args=new[]{"65"}},new Scenario01Command {name="GETITEM",args=new[]{"5"}}}};
            check(Scenario01Rules.ResolveActions(randomEvent,100,true).Length==0,"implicit source RANDOM can reject pickup without TEST block");
            check(Scenario01Rules.ResolveActions(randomEvent,0,true).Any(c=>c.name=="GETITEM"),"implicit source RANDOM can allow pickup");
            var oldState=Scenario01Progress.Create(legacy);oldState.mapChanges=null;oldState.unlockedGenerals=null;oldState.deathSides=null;
            check(oldState.IsValid && oldState.Clone().mapChanges!=null && oldState.Clone().deathSides!=null,"older v3 scenario state tolerates absent new optional lists");
            var legacyWrite=new NativeSaveData {format=NativeSaveStore.Format,version=NativeSaveStore.Version,mapScene="Kaosia_Map",runtimeScene="Town_Runtime",clearedScenarios=Array.Empty<string>()};
            check(!store.TrySave(legacyWrite,out _),"legacy writer cannot erase campaign v4");
        }
        finally{Directory.Delete(directory,true);}
    }
    private static void CheckSecondBridge(Scenario01Definition definition,Action<bool,string> check)
    {
        foreach(int y in new[]{19,20,21})
        {
            var original=Scenario01Progress.Create(definition);original.phase=Scenario01Progress.Playing;
            original.activatedEvents.Add(50);
            var items=new NativeInventory {gold=899,items=new[]{new NativeItemStack {itemId=398,quantity=1}}};
            check(Scenario01Rules.TryActivate(definition,original,items,33,y,out var next,out var remaining)
                && next.activeEvent==51 && remaining.Count(398)==0 && remaining.gold==899,"bridge51 west bank consumes exactly one bomb "+y);
            check(original.activatedEvents.SequenceEqual(new[]{50}) && items.Count(398)==1,"bridge candidate leaves source progress and inventory untouched");
            check(next.mapChanges.SequenceEqual(definition.Event(51).actions.Where(c=>c.name=="CHANGESPRITE").Select(c=>c.line)),"bridge51 preserves all original terrain changes");
            next.activeEvent=0;next.cursor=0;next.page=0;
            Scenario01Rules.TryActivate(definition,next,items,33,y,out var replay,out var replayItems);
            check(replayItems.Count(398)==1 && replay.activatedEvents.Count(e=>e==51)==1 && replay.activeEvent!=51,"bridge51 replay does not consume another bomb");
        }
        foreach(var probe in new[]{(32,20),(33,18),(33,22)})
        {
            var state=Scenario01Progress.Create(definition);state.phase=Scenario01Progress.Playing;state.activatedEvents.Add(50);
            Scenario01Rules.TryActivate(definition,state,new NativeInventory {items=new[]{new NativeItemStack {itemId=398,quantity=1}}},probe.Item1,probe.Item2,out var next,out var items);
            check(!next.activatedEvents.Contains(51) && items.Count(398)==1,"remote bridge probe cannot open or consume");
        }
        foreach(bool firstOpen in new[]{false,true})
        {
            var state=Scenario01Progress.Create(definition);state.phase=Scenario01Progress.Playing;
            if(firstOpen)state.activatedEvents.Add(50);
            var items=new NativeInventory {items=firstOpen?Array.Empty<NativeItemStack>():new[]{new NativeItemStack {itemId=398,quantity=1}}};
            Scenario01Rules.TryActivate(definition,state,items,33,20,out var next,out var remaining);
            check(!next.activatedEvents.Contains(51) && remaining.Count(398)==items.Count(398),"west trigger still requires bomb and first bridge");
        }
        var live=Scenario01Progress.Create(definition);live.phase=Scenario01Progress.Playing;live.activatedEvents.Add(50);
        var stock=new NativeInventory {gold=899,items=new[]{new NativeItemStack {itemId=398,quantity=1}}};int revision=0;bool canSave=false;
        var server=new DarkSaver.Network.LocalScenarioServer(definition,()=>live,()=>stock,()=>revision,()=>100,()=>false,
            (Scenario01Progress candidate,NativeInventory inventory,out string reason)=>{
                reason=canSave?"":"write rejected";if(!canSave)return false;live=candidate.Clone();stock=inventory.Clone();revision++;return true;});
        var failed=server.Handle(new DarkSaver.Network.ScenarioCommandRequest("02",0,DarkSaver.Network.ScenarioOperation.Probe,33,20));
        check(!failed.Accepted && revision==0 && stock.Count(398)==1 && !live.activatedEvents.Contains(51),"failed bridge commit is atomic");
        canSave=true;
        var accepted=server.Handle(new DarkSaver.Network.ScenarioCommandRequest("02",0,DarkSaver.Network.ScenarioOperation.Probe,33,20));
        check(accepted.Accepted && revision==1 && stock.Count(398)==0 && live.activatedEvents.Contains(51),"bridge retry uses original local packet commit");
        check(!server.Handle(new DarkSaver.Network.ScenarioCommandRequest("02",0,DarkSaver.Network.ScenarioOperation.Probe,33,20)).Accepted
            && stock.Count(398)==0 && revision==1,"stale bridge request cannot consume twice");
    }

}
