using System;
using System.IO;
using System.Linq;
using DarkSaver.Core;
using DarkSaver.Network;
namespace DarkSaver.Tests
{
    public static class TrainingResumeChecks
    {
        private static NativeSaveData Sample(int id)=>new NativeSaveData {
            format=NativeSaveStore.Format,version=NativeSaveStore.Version,mapScene="Map_"+id+"_;",runtimeScene="Training"+id+"_Runtime",
            worldX=10.5f,worldY=7.5f,experience=123,hasVitals=true,hp=17,maxHp=200,mp=3,maxMp=45,lifeState=0,
            clearedScenarios=new[]{"01"},hasCharacter=true,character=new NativeCharacterProfile{name="훈련입구",jobId=100},
            tutorialPresenceVersion=1,hasTutorial=false,hasInventory=true,
            inventory=new NativeInventory {gold=321,items=new[]{new NativeItemStack{itemId=398,quantity=1}}}};
        public static void Run(Action<bool,string> check)
        {
            for(int id=101;id<=107;id++)
            {
                var source=Sample(id);check(NativeSaveStore.IsValid(source),"valid training resume fixture "+id);
                check(NativeTrainingResume.TryRedirect(source,8.5f,21.5f,out var town),"training save redirects "+id);
                check(town.mapScene=="Kaosia_Map" && town.runtimeScene=="Town_Runtime" && town.worldX==8.5f && town.worldY==21.5f,"resume uses entrance instead of training center "+id);
                check(town.hp==17 && town.maxHp==200 && town.mp==3 && town.maxMp==45 && town.experience==123 && town.lifeState==0,"redirect preserves vitals and XP "+id);
                check(town.inventory.gold==321 && town.inventory.Count(398)==1 && town.clearedScenarios.SequenceEqual(source.clearedScenarios),"redirect preserves economy and progress "+id);
                check(source.mapScene=="Map_"+id+"_;" && source.worldX==10.5f && source.worldY==7.5f,"redirect never mutates source location "+id);
                town.inventory.gold=0;town.character.name="변경";
                check(source.inventory.gold==321 && source.character.name=="훈련입구","redirected nested data are detached "+id);
            }
            var dead=Sample(101);dead.hp=0;dead.lifeState=1;
            check(NativeTrainingResume.TryRedirect(dead,8.5f,21.5f,out var ghost) && ghost.hp==0 && ghost.mp==3 && ghost.lifeState==1,"return checkpoint never resurrects or refills MP");
            var other=Sample(101);other.mapScene="Kaosia_Map";other.runtimeScene="Town_Runtime";
            check(!NativeTrainingResume.TryRedirect(other,8.5f,21.5f,out _),"ordinary town saves are not redirected");
            other=Sample(101);other.runtimeScene="Training102_Runtime";
            check(!NativeTrainingResume.TryRedirect(other,8.5f,21.5f,out _),"wrong training scene pair rejected");
            foreach(float x in new[]{float.NaN,float.PositiveInfinity,-1f,43f})
                check(!NativeTrainingResume.TryRedirect(Sample(101),x,21.5f,out _),"invalid entrance point rejected "+x);
            string directory=Path.Combine(Path.GetTempPath(),"ds-training-return-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            try
            {
                var paths=Enumerable.Range(0,10).Select(i=>Path.Combine(directory,"slot"+i+".json")).ToArray();
                var store=new NativeSaveStore(paths[0]);check(store.TryCreateNew(Sample(101),out _),"legacy training save fixture created");
                var before=File.ReadAllBytes(paths[0]);
                var server=new CharacterServerBoundary(Path.Combine(directory,"history.json"),paths,
                    source=>!NativeTrainingResume.IsTraining(source)?source:NativeTrainingResume.TryRedirect(source,8.5f,21.5f,out var mapped)?mapped:null);
                check(server.Register(new AccountRegisterRequest("return_01","training-password","training-password")).Accepted,"isolated resume account registers");
                check(server.Login(new LoginRequest("return_01","training-password")).Accepted && server.SelectServer(new ServerSelectRequest(1)).Accepted,"resume account authenticates and selects local server");
                var entry=server.Select(new CharacterSelectRequest(paths[0])) as CharacterEntryResponse;
                check(entry!=null && entry.Accepted,"authenticated old training entry accepted");
                var restored=entry.CaptureState();
                check(restored.mapScene=="Kaosia_Map" && restored.worldX==8.5f && restored.worldY==21.5f && restored.hp==17 && restored.mp==3 && restored.inventory.gold==321,"authenticated entry applies location policy only");
                check(before.SequenceEqual(File.ReadAllBytes(paths[0])),"reading old training save never rewrites preserved source file");
                var checkpoint=new NativeSaveStore(paths[1]);
                check(checkpoint.TryCreateNew(restored,out _) && checkpoint.TryLoad(out var disk,out _) && disk.mapScene=="Kaosia_Map" && disk.worldY==21.5f,"entrance checkpoint survives disk roundtrip");
                var deniedPath=Path.Combine(directory,"future.json");File.WriteAllText(deniedPath,"{\"format\":\"DarkSaverWorld.NativeSave.Checked\",\"version\":99}");
                var deniedBefore=File.ReadAllBytes(deniedPath);
                check(!new NativeSaveStore(deniedPath).TrySave(restored,out _) && deniedBefore.SequenceEqual(File.ReadAllBytes(deniedPath)),"failed checkpoint write preserves future save");
            }
            finally {Directory.Delete(directory,true);}
        }
    }
}
