using DarkSaver.Core;
using DarkSaver.Events;

internal static class TownQuestChecks
{
    public static void Run(Action<bool,string> check)
    {
        check(TownCatalog.All.Count == 4, "four distinct towns are registered");
        var townFolder=Path.Combine(Path.GetTempPath(),"ds-four-town-save-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(townFolder);
        try
        {
            foreach(var town in TownCatalog.All)
            {
                check(TownCatalog.TryGet(town.Key,out var found) && found==town, "town key resolves without guessed map arithmetic");
                check(DarkSaver.Rendering.OriginalRenderRules.IsNativeMapScene(town.MapScene),"town map preserves native foreground pass");
                foreach(var other in TownCatalog.All)
                    check(NativeSaveStore.IsSupportedMap(town.MapScene,other.RuntimeScene)==(town==other),"town save rejects mismatched map/runtime pairs");
                var save=new NativeSaveData {format=NativeSaveStore.Format,version=NativeSaveStore.Version,
                    mapScene=town.MapScene,runtimeScene=town.RuntimeScene,worldX=10.5f,worldY=11.5f,
                    clearedScenarios=Array.Empty<string>(),hasCharacter=true,character=new NativeCharacterProfile {name="마을",jobId=100},
                    hasInventory=true,inventory=new NativeInventory {gold=777}};
                var townStore=new NativeSaveStore(Path.Combine(townFolder,town.Key+".json"));
                check(townStore.TrySave(save,out _) && townStore.TryLoad(out var restored,out _) && restored.mapScene==town.MapScene
                    && restored.runtimeScene==town.RuntimeScene && restored.worldX==10.5f && restored.inventory.gold==777,"all four town saves round-trip on disk");
            }
        }
        finally {Directory.Delete(townFolder,true);}
        check(FieldCatalog.All.Count==18,"all18 ordinary world layouts are registered");
        var network=UnityEngine.JsonUtility.FromJson<WorldRouteContent>(File.ReadAllText("Assets/_Game/Resources/World/Routes.json"));
        check(network.IsValid && network.routes.Length==42,"reciprocal network connects18 fields and4 towns");
        var badNetwork=UnityEngine.JsonUtility.FromJson<WorldRouteContent>(File.ReadAllText("Assets/_Game/Resources/World/Routes.json"));
        badNetwork.routes=badNetwork.routes.Skip(1).ToArray();check(!badNetwork.IsValid,"one-way route is rejected");
        badNetwork=UnityEngine.JsonUtility.FromJson<WorldRouteContent>(File.ReadAllText("Assets/_Game/Resources/World/Routes.json"));
        badNetwork.routes[0].spawnX=badNetwork.routes[0].portalX;badNetwork.routes[0].spawnY=badNetwork.routes[0].portalY;
        check(!badNetwork.IsValid,"arrival inside its return gate is rejected");
        var fieldFolder=Path.Combine(Path.GetTempPath(),"ds-world-fields-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(fieldFolder);
        try
        {
            foreach(var field in FieldCatalog.All)
            {
                check(FieldCatalog.TryGet(field.Key,out var resolved) && resolved==field,"field key resolves");
                check(DarkSaver.Rendering.OriginalRenderRules.IsNativeMapScene(field.MapScene),"field preserves source foreground order");
                foreach(var other in FieldCatalog.All)check(NativeSaveStore.IsSupportedMap(field.MapScene,other.RuntimeScene)==(field==other),"field map/runtime pair is isolated");
                foreach(var town in TownCatalog.All)check(!NativeSaveStore.IsSupportedMap(field.MapScene,town.RuntimeScene),"field cannot restore a town runtime");
                var save=new NativeSaveData {format=NativeSaveStore.Format,version=NativeSaveStore.Version,mapScene=field.MapScene,runtimeScene=field.RuntimeScene,
                    worldX=5.5f,worldY=6.5f,clearedScenarios=Array.Empty<string>(),hasCharacter=true,character=new NativeCharacterProfile {name="필드",jobId=100},hasInventory=true,inventory=new NativeInventory {gold=999}};
                var fieldStore=new NativeSaveStore(Path.Combine(fieldFolder,field.Key+".json"));
                check(fieldStore.TrySave(save,out _) && fieldStore.TryLoad(out var loaded,out _) && loaded.mapScene==field.MapScene && loaded.runtimeScene==field.RuntimeScene && loaded.worldX==5.5f && loaded.inventory.gold==999,"all18 field locations round-trip on disk");
            }
        }
        finally {Directory.Delete(fieldFolder,true);}
        var sourceItems = DarkSaver.Editor.ItemCatalogConverter.Parse(File.ReadAllText("분석/Saver_1.2/catalogs/items.csv"));
        var offers = sourceItems.Where(i => DarkSaver.Items.KaosiaShopRules.TryCreateOffer(i,out _)).ToArray();
        check(offers.Length == 202, "user tier1-3 scope reconstructs202 priced weapon/armor candidates");
        foreach (var item in offers)
        {
            check(DarkSaver.Items.KaosiaShopRules.TryCreateOffer(item,out var offer) && offer.buyPrice == item.GetNumericValue(16) && offer.sellPrice == offer.buyPrice / 2,
                "Kaosia purchase uses exact source price and uses user-confirmed half-price buyback");
        }
        foreach(int id in new[]{0,98,99,174,303,929})
            check(!DarkSaver.Items.KaosiaShopRules.TryCreateOffer(sourceItems.Single(i=>i.Id==id),out _),"zero-priced/special/higher-tier/non-equipment stock excluded");
        DarkSaver.Items.ItemCatalogEntry BoundaryItem(long required, long price=100, long position=2, long permission=1)
        {
            var values=new long[41]; values[0]=999; values[3]=position;values[4]=required;values[6]=permission;values[16]=price;
            return new DarkSaver.Items.ItemCatalogEntry(values,price.ToString(),"","boundary","","weapon");
        }
        check(DarkSaver.Items.KaosiaShopRules.TryCreateOffer(BoundaryItem(30),out _),"tier3 boundary level30 is included");
        check(!DarkSaver.Items.KaosiaShopRules.TryCreateOffer(BoundaryItem(31),out _),"tier4 boundary level31 is excluded");
        check(!DarkSaver.Items.KaosiaShopRules.TryCreateOffer(BoundaryItem(-1),out _),"unknown required level is excluded");
        check(!DarkSaver.Items.KaosiaShopRules.TryCreateOffer(BoundaryItem(1,0),out _),"zero price cannot become a free shop reward");
        check(!DarkSaver.Items.KaosiaShopRules.TryCreateOffer(BoundaryItem(1,100,6),out _),"accessory is outside confirmed weapons and armor scope");
        check(!DarkSaver.Items.KaosiaShopRules.TryCreateOffer(BoundaryItem(1,100,2,0),out _),"unwearable item is excluded");
        check(!DarkSaver.Items.KaosiaShopRules.TryCreateOffer(BoundaryItem(1,100,2,2),out _),"unknown permission is not treated as allowed");
        var bought = new NativeInventory {gold=20000};
        var dagger = sourceItems.Single(i=>i.Id==1);
        DarkSaver.Items.KaosiaShopRules.TryCreateOffer(dagger,out var quote);
        check(bought.TryTrade(1,2,quote.buyPrice,true,out var purchased) && purchased.gold==4000 && purchased.Count(1)==2,
            "reconstructed source dagger quote reaches production quantity trade");
        check(bought.gold==20000 && bought.Count(1)==0,"purchase does not mutate inventory baseline");
        check(purchased.TryTrade(1,1,quote.sellPrice,false,out var sold) && sold.gold==8000 && sold.Count(1)==1,"half-price sale returns 4000 and removes one dagger");
        check(!purchased.TryTrade(1,1,quote.buyPrice,true,out _),"insufficient gold cannot buy an additional weapon");
        var definition = UnityEngine.JsonUtility.FromJson<Scenario01Definition>(File.ReadAllText("Assets/_Game/Resources/Scenarios/01/Content.json"));
        var content = UnityEngine.JsonUtility.FromJson<TownQuestContent>(File.ReadAllText("Assets/_Game/Resources/Town/Quests.json"));
        check(content.IsValid, "source town quest definitions validate");
        var state = Scenario01Progress.Create(definition);
        var items = new NativeInventory();
        var temp = Path.Combine(Path.GetTempPath(), "ds-town-quests-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(temp);
        var store = new NativeSaveStore(Path.Combine(temp,"hero.json"));
        void Checkpoint()
        {
            var save = new NativeSaveData { format=NativeSaveStore.Format, version=NativeSaveStore.ScenarioVersion,
                mapScene="Kaosia_Map",runtimeScene="Town_Runtime",worldX=21,worldY=21,hasCharacter=true,
                character=new NativeCharacterProfile {name="의뢰검증",jobId=100},hasInventory=true,inventory=items,
                hasScenario01=true,scenario01=state,clearedScenarios=Array.Empty<string>() };
            check(store.TrySave(save,out _), "town quest checkpoint saved");
            check(store.TryLoad(out var loaded,out _), "town quest checkpoint restored");
            state=loaded.scenario01;items=loaded.inventory;
        }
        void Apply(TownQuestDefinition quest,bool claim)
        {
            check(TownQuestRules.TryApply(definition,state,items,quest,claim,out var next,out var inventory),"town quest transaction prepared");
            check(!ReferenceEquals(next,state) && !ReferenceEquals(items,inventory),"town quest candidate is independent");
            state=next;items=inventory;Checkpoint();
        }
        void RunEvent()
        {
            int budget=0;
            while(state.activeEvent!=0 && budget++<1000)
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
            check(budget<1000,"source quest event terminates");
        }
        try
        {
            foreach(var quest in content.rows)
            {
                check(!TownQuestRules.TryApply(definition,state,items,quest,true,out _,out _),"quest reward cannot precede objective");
                Apply(quest,false);
                check(TownQuestRules.Stage(state,quest.id)==1,"town acceptance persists source step1");
                check(!TownQuestRules.TryApply(definition,state,items,quest,false,out _,out _),"quest acceptance cannot duplicate");
            }
            // Test fixture bypasses intro already covered by Scenario01Checks. All
            // objective and dialogue processing below uses the actual source rules.
            state.phase=Scenario01Progress.Playing;
            check(Scenario01Rules.TryActivate(definition,state,items,18,22,out var next,out var inventory),"accepted Cain quest activates actual source EVT52");
            state=next;items=inventory;check(state.activeEvent==52,"correct source quest event selected");RunEvent();
            check(TownQuestRules.Stage(state,1)==2,"source EVT52 unlocks town report");
            check(items.Count(929)==0 && items.gold==0,"narrative necklace is not an invented inventory reward");
            Apply(content.rows.Single(q=>q.id==1),true);
            check(items.gold==20000,"source Cain reward is exactly 20000 gold");
            foreach(var unit in definition.units.OrderBy(u=>u.wave).ThenBy(u=>u.id))
            {
                state.pendingBattle=unit.id;
                check(Scenario01Rules.RecordBattleReturn(definition,state,unit.id,true),"battle-return fixture records confirmed death");
                if(unit.id==420)break;
            }
            bool orbEvent = false;
            for (int events = 0; TownQuestRules.Stage(state,2) < 2 && events < 30; events++)
            {
                check(Scenario01Rules.TryActivate(definition,state,items,0,0,out next,out inventory),"source death events activate in original order");
                state=next; items=inventory; orbEvent |= state.activeEvent==12; RunEvent();
            }
            check(orbEvent && TownQuestRules.Stage(state,2)==2,"source EVT12 unlocks Taran reward after420 death");
            Apply(content.rows.Single(q=>q.id==2),true);
            check(items.Count(929)==1 && items.gold==20000,"exact source-name bracelet reward granted once");
            foreach(var quest in content.rows)check(!TownQuestRules.TryApply(definition,state,items,quest,true,out _,out _),"saved claim receipt prevents duplicate rewards");
            var fresh=Scenario01Progress.Create(definition,true);
            check(!TownQuestRules.TryApply(definition,fresh,items,content.rows[0],false,out _,out _),"completed legacy scenario cannot accept an impossible quest");
            var unsafeState=Scenario01Progress.Create(definition);unsafeState.dutySteps.Add("1:1");unsafeState.dutySteps.Add("1:2");
            var rich=new NativeInventory {gold=long.MaxValue};
            check(!TownQuestRules.TryApply(definition,unsafeState,rich,content.rows[0],true,out _,out _),"reward overflow is rejected before mutation");
        }
        finally {Directory.Delete(temp,true);}
    }
}
