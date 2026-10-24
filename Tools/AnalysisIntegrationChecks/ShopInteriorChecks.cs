using DarkSaver.Core;

internal static class ShopInteriorChecks
{
    public static void Run(Action<bool,string> check)
    {
        check(ShopInteriorCatalog.All.Count==2,"two independently addressable shop interiors");
        var routes=ShopInteriorCatalog.RoutesFrom("Kaosia").Concat(ShopInteriorCatalog.All.SelectMany(s=>ShopInteriorCatalog.RoutesFrom(s.Key))).ToArray();
        check(routes.Length==4,"two reciprocal exterior/interior doorway pairs");
        var directory=Path.Combine(Path.GetTempPath(),"ds-shops-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try
        {
            foreach(var shop in ShopInteriorCatalog.All)
            {
                check(DarkSaver.Rendering.OriginalRenderRules.IsNativeMapScene(shop.Region.MapScene),"shop alias preserves native foreground rules");
                check(WorldRouteContent.Resolve(shop.Key,out var region) && region.MapScene==shop.Region.MapScene,"shop destination resolves");
                check(NativeSaveStore.IsSupportedMap(region.MapScene,region.RuntimeScene),"indoor scene pair is persistable");
                var outward=routes.Single(r=>r.fromKey=="Kaosia" && r.toKey==shop.Key);
                var inward=routes.Single(r=>r.fromKey==shop.Key && r.toKey=="Kaosia");
                check(outward.targetSpawnId==inward.spawnId && inward.targetSpawnId==outward.spawnId,"shop exit returns to its own door");
                check((outward.portalX-outward.spawnX)*(outward.portalX-outward.spawnX)+(outward.portalY-outward.spawnY)*(outward.portalY-outward.spawnY)>=9,"exterior return cannot immediately retrigger entry");
                check((inward.portalX-inward.spawnX)*(inward.portalX-inward.spawnX)+(inward.portalY-inward.spawnY)*(inward.portalY-inward.spawnY)>=9,"interior entry cannot immediately trigger exit");
                using var mapJson=System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine("Assets/_Game/Resources/Interiors",region.NativeMap,"Map.json")));
                var attributes=mapJson.RootElement.GetProperty("attributes").EnumerateArray().Select(a=>a.GetInt32()).ToArray();
                bool Blocked(int x,int y)=>x<0 || y<0 || x>=region.Width || y>=region.Height || attributes[(region.Height-1-y)*region.Width+x]==9;
                var start=((int)inward.spawnX,(int)inward.spawnY);var target=((int)inward.portalX,(int)inward.portalY);
                var visited=new HashSet<(int,int)>{start};var queue=new Queue<(int,int)>();queue.Enqueue(start);
                while(queue.Count>0)
                {
                    var (x,y)=queue.Dequeue();
                    foreach(var p in new[]{(x-1,y),(x+1,y),(x,y-1),(x,y+1)})
                        if(!Blocked(p.Item1,p.Item2) && visited.Add(p))queue.Enqueue(p);
                }
                check(visited.Contains(target) && visited.Contains((10,9)),"entry reaches exit and counter through converted native collision data");
                var store=new NativeSaveStore(Path.Combine(directory,shop.Key+".json"));
                var save=new NativeSaveData {format=NativeSaveStore.Format,version=NativeSaveStore.Version,mapScene=region.MapScene,runtimeScene=region.RuntimeScene,
                    worldX=6.5f,worldY=5.5f,hasCharacter=true,character=new NativeCharacterProfile {name="실내검증",jobId=100},clearedScenarios=Array.Empty<string>()};
                check(store.TrySave(save,out _),"shop position saves to disk");
                check(store.TryLoad(out var loaded,out _) && loaded.mapScene==region.MapScene && loaded.runtimeScene==region.RuntimeScene && loaded.worldX==6.5f && loaded.worldY==5.5f,"shop position reloads exactly");
                var history=Path.Combine(directory,"login.json");
                check(NativeLoginHistory.TryRemember(history,store.FilePath,out _) && NativeLoginHistory.TryResolve(history,new[]{store.FilePath},out var selected,out bool needsSelection,out _) && selected==store.FilePath && !needsSelection,"login can resume inside shop");
                var other=ShopInteriorCatalog.All.Single(s=>s.Key!=shop.Key);
                check(!NativeSaveStore.IsSupportedMap(region.MapScene,other.Region.RuntimeScene),"mixed shop map/runtime is rejected");
                for(int position=1;position<=10;position++)
                    check(shop.OffersPosition(position)==(shop.Weapons?(position==1 || position==2):(position==3 || position==4 || position==5 || position==8)),"shop category excludes unrelated equipment position");
            }
            check(NativeInteriorCatalog.All.Count==22,"all reviewed native interiors registered");
            foreach(var region in NativeInteriorCatalog.All)
            {
                check(WorldRouteContent.Resolve(region.Key,out var resolved) && resolved.NativeMap==region.NativeMap,"native interior destination resolves");
                check(NativeSaveStore.IsSupportedMap(region.MapScene,region.RuntimeScene),"native interior save pair supported");
                check(DarkSaver.Rendering.OriginalRenderRules.IsNativeMapScene(region.MapScene),"native interior preserves foreground rules");
                var other=NativeInteriorCatalog.All.First(r=>r.Key!=region.Key);
                check(!NativeSaveStore.IsSupportedMap(region.MapScene,other.RuntimeScene),"mixed native interior pair rejected");
            }
            check(KaosiaFacilityCatalog.All.Count==5,"five additional Kaosia indoor facilities");
            var facilityRoutes=KaosiaFacilityCatalog.RoutesFrom("Kaosia").Concat(KaosiaFacilityCatalog.All.SelectMany(r=>KaosiaFacilityCatalog.RoutesFrom(r.Region.Key))).ToArray();
            check(facilityRoutes.Length==10,"five reciprocal facility routes");
            foreach(var row in KaosiaFacilityCatalog.All)
            {
                var region=row.Region;
                check(WorldRouteContent.Resolve(region.Key,out var found) && found.NativeMap==region.NativeMap,"facility destination resolves original interior");
                check(NativeSaveStore.IsSupportedMap(region.MapScene,region.RuntimeScene),"facility pair persists");
                check(DarkSaver.Rendering.OriginalRenderRules.IsNativeMapScene(region.MapScene),"facility preserves native render order");
                var outward=facilityRoutes.Single(r=>r.fromKey=="Kaosia" && r.toKey==region.Key);
                var inward=facilityRoutes.Single(r=>r.fromKey==region.Key && r.toKey=="Kaosia");
                check(outward.targetSpawnId==inward.spawnId && inward.targetSpawnId==outward.spawnId,"facility returns to its own exterior");
                check((outward.portalX-outward.spawnX)*(outward.portalX-outward.spawnX)+(outward.portalY-outward.spawnY)*(outward.portalY-outward.spawnY)>=4,"facility return cannot immediately reenter");
                check(!NativeSaveStore.IsSupportedMap(region.MapScene,ShopInteriorCatalog.All[0].Region.RuntimeScene),"mixed facility scene pair rejected");
                var store=new NativeSaveStore(Path.Combine(directory,region.Key+".json"));
                var save=new NativeSaveData {format=NativeSaveStore.Format,version=NativeSaveStore.Version,mapScene=region.MapScene,runtimeScene=region.RuntimeScene,
                    worldX=10.5f,worldY=3.5f,hasCharacter=true,character=new NativeCharacterProfile {name="시설검증",jobId=100},clearedScenarios=Array.Empty<string>()};
                check(store.TrySave(save,out _) && store.TryLoad(out var loaded,out _) && loaded.mapScene==region.MapScene && loaded.worldX==10.5f && loaded.worldY==3.5f,"facility position survives disk save and reload");
            }
            check(ShopInteriorCatalog.ShopsWithFacilities.Count()==4,"four interactive interior shops");
            foreach(var shop in ShopInteriorCatalog.ShopsWithFacilities)
                for(int position=0;position<=9;position++)
                    check(shop.OffersPosition(position)==(shop.AdditionalPosition!=0?position==shop.AdditionalPosition:shop.Weapons?(position==1 || position==2):(position==3 || position==4 || position==5 || position==8)),"four shops reject unrelated positions");
            foreach(var position in new[]{6,7})
            {
                var values=new long[DarkSaver.Items.ItemCatalogEntry.NumericColumnCount];values[0]=2000+position;values[3]=position;values[4]=0;values[16]=500;
                var item=new DarkSaver.Items.ItemCatalogEntry(values,"500","","검증품","","검증");
                check(DarkSaver.Items.KaosiaShopRules.TryCreateAdditionalOffer(item,position,out var offer) && offer.buyPrice==500 && offer.sellPrice==-1,"additional shop quotes use positive native price with buyback disabled");
                check(!DarkSaver.Items.KaosiaShopRules.TryCreateAdditionalOffer(item,position==6?7:6,out _),"additional shop rejects wrong category");
                values[16]=0;item=new DarkSaver.Items.ItemCatalogEntry(values,"0","","검증품","","검증");
                check(!DarkSaver.Items.KaosiaShopRules.TryCreateAdditionalOffer(item,position,out _),"additional shop excludes zero price rewards");
            }
            check(!ShopInteriorCatalog.TryGet("KaosiaUnknownShop",out _),"unknown interior cannot be entered");
        }
        finally{Directory.Delete(directory,true);}
    }
}
