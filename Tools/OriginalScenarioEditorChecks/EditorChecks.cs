using System;
using DarkSaver.Original.Maps;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Text;
using DarkSaver.Original.Editor.Scenarios;

internal static class EditorChecks
{
    private static int assertions;
    private static void Check(bool result,string name){assertions++;if(!result)throw new Exception(name);}
    private static void Reject(Action action,string name)
    {bool rejected=false;try{action();}catch(InvalidDataException){rejected=true;}catch(IOException){rejected=true;}Check(rejected,name);}
    private static string Read(string path)=>File.Exists(path)?File.ReadAllText(path):null;
    private static ScenarioSource Source(string root,string key)
    {
        var map=File.ReadAllBytes(Path.Combine(root,"Assets/_Original/SourceData/"+(key=="1000"?"KaosiaLegacy/":"Native/map/")+key+".mrc.bytes"));var size=NativeMapDecoder.Dimensions(map);
        string path=Path.Combine(root,"Assets/_Original/SourceData/Decoded/map/"+key+"set.arc/"+key);
        return ScenarioLayoutData.ReadSource(key,size.width,size.height,map,Read(path+".set.txt"),Read(path+".pos.txt"),Read(path+".evt.txt"),null);
    }
    private static void Main(string[] args)
    {
        string root=args[0],work=args[1];var sourceRoot=Path.Combine(root,"Assets/_Original/SourceData/Native/map");
        var rows=ScenarioLayoutData.Csv(File.ReadAllText(Path.Combine(root,"Assets/_Original/Data/Tables/units_all.csv"))).ToArray();
        int jobColumn=Array.IndexOf(rows[0],"직업");var jobs=new HashSet<int>(rows.Skip(1).Select(r=>int.Parse(r[jobColumn])));
        Check(jobs.Count==542,"actual unit catalog parsed including quoted descriptions");
        using var gatewayMaps=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"Assets/_Original/Data/Campaign/GatewayMaps.json")));
        var keys=gatewayMaps.RootElement.GetProperty("maps").EnumerateArray().Select(m=>m.GetProperty("key").GetString()).OrderBy(int.Parse).ToArray();
        var sources=keys.ToDictionary(k=>k,k=>Source(root,k));
        foreach(var source in sources.Values)ScenarioLayoutData.Validate(source.layout,source.layout,jobs);
        Check(keys.Length==151&&Enumerable.Range(101,7).Concat(Enumerable.Range(201,7)).All(i=>sources.ContainsKey(i.ToString())),"scenario/training/duel source library");
        using var literal=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"Assets/_Original/Data/Placements/scenario_monster_placements.json")));
        int units=0;
        foreach(var map in literal.RootElement.GetProperty("scenarios").EnumerateArray())
        {
            string key=map.GetProperty("scenario").GetString();var actual=sources[key].layout;
            Check(actual.units.Length==map.GetProperty("count").GetInt32(),"exact original roster count "+key);
            foreach(var u in map.GetProperty("units").EnumerateArray())
            {
                var placed=actual.units.Single(p=>p.unit==u.GetProperty("unit").GetInt32());
                Check(placed.job==u.GetProperty("job").GetInt32()&&placed.x==u.GetProperty("x").GetInt32()&&placed.y==u.GetProperty("y").GetInt32()
                    &&placed.setLine==u.GetProperty("setLine").GetInt32()&&placed.posLine==u.GetProperty("posLine").GetInt32(),"literal source join/provenance "+key+"/"+placed.unit);units++;
            }
        }
        Check(units==610&&sources.Values.Sum(s=>s.layout.units.Length)==610,"no fabricated fallback spawns anywhere");
        var rosterBytes=File.ReadAllBytes(Path.Combine(root,"Assets/_Original/Data/Placements/scenario_monster_placements.json"));
        int restoredFromCatalog=0;
        foreach(var item in literal.RootElement.GetProperty("scenarios").EnumerateArray())
        {
            string key=item.GetProperty("scenario").GetString();var original=sources[key].layout;
            var map=File.ReadAllBytes(Path.Combine(sourceRoot,key+".mrc.bytes"));
            var restored=ScenarioLayoutData.ReadSourceWithCatalog(key,original.width,original.height,map,null,null,null,null,rosterBytes,jobs);
            Check(restored.layout.source=="original-catalog"&&restored.layout.catalogHash==ScenarioLayoutData.Hash(rosterBytes)
                &&ScenarioLayoutData.SameUnits(restored.layout,original),"missing SET/POS restored from verified catalog without invented units "+key);
            Check(restored.layout.units.Zip(original.units,(a,b)=>a.setLine==b.setLine&&a.posLine==b.posLine).All(v=>v),"catalog preserves original row provenance "+key);
            restoredFromCatalog+=restored.layout.units.Length;
        }
        Check(restoredFromCatalog==610,"all16 catalog rosters restore exactly610 native units");
        var reference26=sources["26"].layout;var bytes26=File.ReadAllBytes(Path.Combine(sourceRoot,"26.mrc.bytes"));
        string path26=Path.Combine(root,"Assets/_Original/SourceData/Decoded/map/26set.arc/26");
        ScenarioSource FromCatalog(string set,string pos,byte[] data)=>ScenarioLayoutData.ReadSourceWithCatalog("26",reference26.width,reference26.height,bytes26,set,pos,null,null,data,jobs);
        var raw26=FromCatalog(Read(path26+".set.txt"),Read(path26+".pos.txt"),null);
        Check(raw26.layout.source=="original-set-pos"&&raw26.layout.catalogHash==null,"available text retains existing source identity and hashes");
        var setOnly=FromCatalog(Read(path26+".set.txt"),null,rosterBytes);
        Check(setOnly.layout.units.Length==65&&setOnly.layout.posHash==""&&setOnly.layout.setHash==reference26.setHash,"SET-only source recovers positions and retains the actual available hash");
        var lfOnly=FromCatalog(Read(path26+".set.txt").Replace("\r\n","\n"),null,rosterBytes);
        Check(lfOnly.layout.units.Length==65&&lfOnly.layout.catalogHash==setOnly.layout.catalogHash,"line-ending conversion does not falsely reject the same source text");
        var posOnly=FromCatalog(null,Read(path26+".pos.txt"),rosterBytes);
        Check(posOnly.layout.units.Length==65&&posOnly.layout.setHash==""&&posOnly.entries.Length==sources["26"].entries.Length,"POS-only source recovers jobs and preserves friendly entry markers");
        string wrongSet=string.Join("\n",reference26.units.Select((u,i)=>u.unit+" "+(i==0?401:u.job)));
        Reject(()=>FromCatalog(wrongSet,null,rosterBytes),"conflicting remaining SET is not mixed with reference data");
        string wrongPos=string.Join("\n",reference26.units.Select((u,i)=>u.unit+" "+(u.x+(i==0?1:0))+" "+u.y));
        Reject(()=>FromCatalog(null,wrongPos,rosterBytes),"conflicting remaining POS is not mixed with reference data");
        Reject(()=>FromCatalog(null,Read(path26+".pos.txt")+"\n999 1 1",rosterBytes),"extra POS units cannot silently disappear under a reference fallback");
        var referenceNode=System.Text.Json.Nodes.JsonNode.Parse(rosterBytes);
        referenceNode["scenarios"][0]["count"]=64;
        Reject(()=>FromCatalog(null,null,Encoding.UTF8.GetBytes(referenceNode.ToJsonString())),"invalid reference count is rejected");
        var absent=sources["01"].layout;var missing=ScenarioLayoutData.ReadSourceWithCatalog("01",absent.width,absent.height,File.ReadAllBytes(Path.Combine(sourceRoot,"01.mrc.bytes")),null,null,null,null,rosterBytes,jobs);
        Check(missing.layout.source=="original-missing"&&missing.layout.units.Length==0,"catalog fallback does not invent the missing01 roster");
        var emptyLayout=sources["324"].layout;var emptyCatalog=ScenarioLayoutData.ReadSourceWithCatalog("324",emptyLayout.width,emptyLayout.height,File.ReadAllBytes(Path.Combine(sourceRoot,"324.mrc.bytes")),null,null,null,null,rosterBytes,jobs);
        Check(emptyCatalog.layout.source=="original-catalog-empty"&&emptyCatalog.layout.units.Length==0,"documented empty324 remains distinct from unavailable data");
        var catalog26=FromCatalog(null,null,rosterBytes).layout;
        var changedReference=catalog26.Copy();changedReference.catalogHash="changed";
        Reject(()=>ScenarioLayoutData.Validate(catalog26,changedReference,jobs),"changed reference hash invalidates a saved fallback layout");
        var formerMissing=ScenarioLayoutData.ReadSource("26",reference26.width,reference26.height,bytes26,null,null,null,null).layout;
        var preview=ScenarioLayoutData.LoadForPreview(ScenarioLayoutData.Encode(formerMissing),catalog26,jobs,out bool heldReference,out string referenceNotice);
        Check(heldReference&&preview.units.Length==65,"old empty missing-source layout cannot hide the recovered native roster");
        Check(sources["01"].layout.source=="original-missing"&&sources["101"].layout.units.Length==0,"missing scenario/training roster remains empty");
        Check(new[]{"324","325","326"}.All(k=>sources[k].layout.source=="original-empty"),"literal empty distinguished from missing");
        Check(sources["324"].entries.Length==3&&sources["324"].entries[1].line==2&&sources["324"].entries[2].line==3,"duplicate friendly POS reference lines preserved without inventing a battle unit");
        var native26=sources["26"].layout;
        var fresh=ScenarioLayoutData.LoadForPreview(null,native26,jobs,out bool protectedView,out string previewNotice);
        Check(!protectedView&&fresh.units.Length==65&&!ReferenceEquals(fresh.units[0],native26.units[0]),"first preview uses an independent copy of all native26 units");
        byte[] corrupt=Encoding.UTF8.GetBytes("not valid json");var unchanged=(byte[])corrupt.Clone();
        var recovered=ScenarioLayoutData.LoadForPreview(corrupt,native26,jobs,out protectedView,out previewNotice);
        Check(protectedView&&previewNotice.Length>0&&recovered.units.Length==65&&corrupt.SequenceEqual(unchanged),"corrupt override shows65 native units read-only and preserves saved bytes");
        var wrongVersion=native26.Copy();wrongVersion.mapHash="old version";
        recovered=ScenarioLayoutData.LoadForPreview(ScenarioLayoutData.Encode(wrongVersion),native26,jobs,out protectedView,out previewNotice);
        Check(protectedView&&recovered.units.Length==65&&recovered.mapHash==native26.mapHash,"old map hash does not hide a readable native roster");
        var deliberatelyEmpty=native26.Copy();deliberatelyEmpty.units=Array.Empty<ScenarioPlacement>();
        var honored=ScenarioLayoutData.LoadForPreview(ScenarioLayoutData.Encode(deliberatelyEmpty),native26,jobs,out protectedView,out previewNotice);
        Check(!protectedView&&honored.units.Length==0&&native26.units.Length==65,"valid authored empty layout is preserved until explicit original reload");
        Check(sources["01"].events.Any(m=>m.kind=="chest"&&m.x==5&&m.y==15&&m.file=="01.evt"),"chest anchored to CHANGESPRITE original position");
        Check(sources["01"].events.Any(m=>m.kind=="event"&&m.x==6&&m.y==15),"event condition kept separate from chest image");
        Check(ScenarioLayoutData.ReadEvents("EVENT 1\nGETITEM 398\nEND","test").Length==0,"reward without explicit location is not invented on map");
        string clip="LOADSCENE 01\nCHARPOS 401 400 2 3\nCHARPOS 402 400 4 5\nCHARMOVE 400 6 7\nKILLCHAR2 400\nLOADSCENE 02\nCHARPOS 701 700 8 9";
        Check(ScenarioLayoutData.Cutscene(clip,"test","01",1).Length==0,"cutscene begins without battle actors");
        var actors=ScenarioLayoutData.Cutscene(clip,"test","01",3);
        Check(actors.Length==1&&actors[0].job=="402"&&actors[0].x==4,"reused script id represented at selected line, not simultaneous copies");
        Check(ScenarioLayoutData.Cutscene(clip,"test","01",4).Single().x==6,"CHARMOVE endpoint snapshot");
        Check(ScenarioLayoutData.Cutscene(clip,"test","01",5).Length==0,"KILLCHAR2 removes previous placement");
        Check(ScenarioLayoutData.Cutscene(clip,"test","01",7).Length==0,"foreign LOADSCENE placement not overlaid on current map");
        Check(ScenarioLayoutData.Cutscene("CHARPOS COM0 0 2 3","test","01",1).Single().job=="COM0","commander alias not assigned an invented class");
        var folder=Path.Combine(work,"save-"+Guid.NewGuid().ToString("N"));
        try
        {
            string catalogFolder=Path.Combine(folder,"catalog");var catalogEdit=catalog26.Copy();catalogEdit.units[0].x++;
            string catalogSavedHash=ScenarioLayoutData.Save(catalogFolder,catalogEdit,catalog26,jobs,null);
            var catalogRoundtrip=ScenarioLayoutData.Decode(File.ReadAllBytes(Path.Combine(catalogFolder,"26.json")));
            Check(catalogRoundtrip.catalogHash==catalog26.catalogHash&&catalogRoundtrip.units[0].x==catalogEdit.units[0].x,"catalog-backed edits preserve reference hash through real file save/reopen");
            Reject(()=>ScenarioLayoutData.Save(catalogFolder,catalogRoundtrip,changedReference,jobs,catalogSavedHash),"changing reference versions cannot overwrite a saved catalog-backed edit");
            var original=sources["26"].layout;var edit=original.Copy();edit.units[0].x++;
            string hash=ScenarioLayoutData.Save(folder,edit,original,jobs,null),file=Path.Combine(folder,"26.json");
            var loaded=ScenarioLayoutData.Decode(File.ReadAllBytes(file));Check(loaded.edited&&loaded.units[0].x==edit.units[0].x&&original.units[0].x!=edit.units[0].x,"save/reload changes override only");
            loaded.units=loaded.units.Skip(1).ToArray();ScenarioLayoutData.Save(folder,loaded,original,jobs,hash);
            Check(ScenarioLayoutData.Decode(File.ReadAllBytes(file)).units.Length==64,"deletion persists across reopen");
            byte[] before=File.ReadAllBytes(file);Reject(()=>ScenarioLayoutData.Save(folder,edit,original,jobs,hash),"external/stale file cannot be overwritten");
            Check(File.ReadAllBytes(file).SequenceEqual(before),"stale save preserves bytes");
            var invalid=edit.Copy();invalid.units[1].unit=invalid.units[0].unit;
            Reject(()=>ScenarioLayoutData.Save(folder,invalid,original,jobs,ScenarioLayoutData.Hash(before)),"duplicate unit rejected before replacing valid file");
            Check(File.ReadAllBytes(file).SequenceEqual(before),"invalid save preserves bytes");
            invalid=edit.Copy();invalid.units[0].x=original.width;Reject(()=>ScenarioLayoutData.Validate(invalid,original,jobs),"out of grid bounds rejected");
            invalid=edit.Copy();invalid.units[0].job=-1;Reject(()=>ScenarioLayoutData.Validate(invalid,original,jobs),"missing job rejected");
            invalid=edit.Copy();invalid.mapHash="wrong";Reject(()=>ScenarioLayoutData.Validate(invalid,original,jobs),"changed source map requires review");
            invalid=edit.Copy();invalid.mapKey="../26";Reject(()=>ScenarioLayoutData.Validate(invalid,original,jobs),"path traversal blocked");
            invalid=edit.Copy();invalid.version=2;Reject(()=>ScenarioLayoutData.Validate(invalid,original,jobs),"unknown schema rejected");
            Check(!ScenarioLayoutData.IsKey("26/other")&&!ScenarioLayoutData.IsKey("26.json")&&ScenarioLayoutData.IsKey("01"),"map filename whitelist");
            var empty=sources["101"].layout.Copy();empty.units=new[]{new ScenarioPlacement{unit=400,job=401,x=2,y=3}};
            string trainingHash=ScenarioLayoutData.Save(folder,empty,sources["101"].layout,jobs,null);
            Check(ScenarioLayoutData.Decode(File.ReadAllBytes(Path.Combine(folder,"101.json"))).edited&&sources["101"].layout.units.Length==0,"authored training monsters do not become original source");
            empty.units=Array.Empty<ScenarioPlacement>();ScenarioLayoutData.Save(folder,empty,sources["101"].layout,jobs,trainingHash);
            Check(!ScenarioLayoutData.Decode(File.ReadAllBytes(Path.Combine(folder,"101.json"))).edited,"reset to original remains explicit override");
            Check(Directory.GetFiles(folder,"*.tmp").Length==0,"atomic save leaves no temporary files");
        }
        finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
        Check(NativeMapDecoder.Grid(-100,0,1,40)==0&&NativeMapDecoder.Grid(99999,0,1,40)==39,"drag clamps to actual grid");
        Check(NativeMapDecoder.Grid(99,3,1.5,40)==2,"pixel/pan/zoom conversion uses native32px tiles");
        Check(NativeMapDecoder.Zoom(4,-30)==4&&NativeMapDecoder.Zoom(.125,30)==.125,"wheel zoom limits");
        Check(ScenarioLayoutData.NextUnit(sources["26"].layout)==405,"added slot fills free id without rearranging original groups");
        var rendered=new List<object>();var sample=new HashSet<string>(new[]{"01","02","26","101","201","314","1010"});
        foreach(var key in keys)
        {
            var mapFolder=key=="1000"?Path.Combine(root,"Assets/_Original/SourceData/KaosiaLegacy"):sourceRoot;
            var map=NativeMapDecoder.Render(File.ReadAllBytes(Path.Combine(mapFolder,key+".mrc.bytes")),id=>File.ReadAllBytes(Path.Combine(mapFolder,"mdsr"+id+".grs.bytes")));
            Check(map.width==sources[key].layout.width*32&&map.height==sources[key].layout.height*32,"full native render "+key);
            rendered.Add(new{key,map.width,map.height,sha256=ScenarioLayoutData.Hash(map.rgba)});
            if(sample.Contains(key))File.WriteAllBytes(Path.Combine(work,key+".rgba"),map.rgba);
        }
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,maps=keys.Length,originalUnits=units,catalogFallbackUnits=restoredFromCatalog,catalogFallbackMaps=literal.RootElement.GetProperty("scenarios").GetArrayLength(),originalRosterMaps=sources.Values.Count(s=>s.layout.source=="original-set-pos"),emptyRosterMaps=sources.Values.Count(s=>s.layout.source=="original-empty"),missingRosterMaps=sources.Values.Count(s=>s.layout.source=="original-missing"),rendered,unityGuiExecuted=false}));
    }
}
