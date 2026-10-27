using System;
using System.IO;
using System.Linq;
using DarkSaver.Core;
using DarkSaver.Events;
using DarkSaver.Network;
namespace DarkSaver.Tests
{
    public static class LocalOnboardingChecks
    {
        public static string Source=>File.ReadAllText(Path.Combine("Assets","_Game","Resources","Tutorial","TUTORIAL.txt"));
        public static TutorialProgressRequest RequiredInput(NativeTutorialProgress p,OriginalTutorialScript script)
        {
            var moving=p.actors.FirstOrDefault(a=>a.moving);
            if(moving!=null)return new TutorialProgressRequest(p.step,"MovementCompleted",moving.id,x:moving.targetX,y:moving.targetY);
            var c=script.Current(p);int N(int i)=>int.Parse(c.Arguments[i]);
            switch(c.Name)
            {
                case "DO":return new TutorialProgressRequest(p.step,"Delay");
                case "WAITMOUSEBUTTON":return new TutorialProgressRequest(p.step,"Click");
                case "WAITKEYBOARD":return new TutorialProgressRequest(p.step,"Key",key:c.Arguments[0]);
                case "WAITMOUSEMOVE":return new TutorialProgressRequest(p.step,"Hover",N(0));
                case "WAITTOPMENU":return new TutorialProgressRequest(p.step,"TopMenu",N(0));
                case "WAITMOVEMENU":return new TutorialProgressRequest(p.step,"MoveMenu",N(0));
                case "WAITMOVEPOS":return new TutorialProgressRequest(p.step,"MovePosition",N(0),x:N(1),y:N(2));
                case "WAITATTACKMENU":return new TutorialProgressRequest(p.step,"AttackMenu",N(0));
                case "WAITATTACKCHAR":return new TutorialProgressRequest(p.step,"Attack",N(0),N(1));
                case "WAITRESTMENU":return new TutorialProgressRequest(p.step,"Rest",N(0));
                case "WAITMAGICMENU":return new TutorialProgressRequest(p.step,"Magic",N(1),target:N(0)==2?N(2):-1,stage:N(0));
                case "WAITORDERMENU":return new TutorialProgressRequest(p.step,"Order",N(1),stage:N(0));
                case "WAITALLYBUTTON":return new TutorialProgressRequest(p.step,"Ally");
                default:throw new Exception("Unexpected blocked command: "+c.Name);
            }
        }
        public static void Run(Action<bool,string> check)
        {
            string directory=Path.Combine(Path.GetTempPath(),"ds-original-onboarding-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            try
            {
                var paths=Enumerable.Range(0,10).Select(i=>Path.Combine(directory,"slot"+i+".json")).ToArray();
                var server=new CharacterServerBoundary(Path.Combine(directory,"history.json"),paths);
                check(!server.Create(new CharacterCreateRequest("Test",100,paths[0])).Accepted,"unauthenticated creation rejected");
                check(server.Register(new AccountRegisterRequest("alpha_01","onboarding-password","onboarding-password")).Accepted,"register new account");
                check(server.Login(new LoginRequest("alpha_01","onboarding-password")).Accepted,"login new account");
                check(!server.Create(new CharacterCreateRequest("Test",100,paths[0])).Accepted,"creation requires server selection");
                check(server.SelectServer(new ServerSelectRequest(1)).Accepted,"select local server");
                check(!server.Create(new CharacterCreateRequest("Test",102,paths[0])).Accepted,"unsupported starting job rejected");
                check(!server.Create(new CharacterCreateRequest("Test",100,Path.Combine(directory,"foreign.json"))).Accepted,"foreign slot rejected");
                for(int i=0;i<3;i++)
                {
                    int job=new[]{100,101,103}[i];check(server.Create(new CharacterCreateRequest("Class"+job,job,paths[i])).Accepted,"create class "+job);
                    check(new NativeSaveStore(paths[i]).TryLoad(out var data,out _) && data.character.tutorial.version==2 && data.character.tutorial.step==0,"new original tutorial progress "+job);
                    check(!data.hasInventory && !data.hasVitals,"no guessed permanent grants "+job);
                }
                var bytes=File.ReadAllBytes(paths[0]);
                check(!server.Create(new CharacterCreateRequest("Overwrite",100,paths[0])).Accepted && bytes.SequenceEqual(File.ReadAllBytes(paths[0])),"occupied slot preserved");
                File.WriteAllText(paths[4]+".bak","preserve backup");
                check(!server.Create(new CharacterCreateRequest("Backup",100,paths[4])).Accepted,"backup-only slot preserved");
                var entry=(CharacterEntryResponse)server.Select(new CharacterSelectRequest(paths[0]));
                check(entry.Accepted && entry.RequiresTutorial && entry.AccountId=="alpha_01" && entry.ServerId==1,"entry requires original tutorial");
                check(!server.Select(new CharacterSelectRequest(paths[0])).Accepted,"duplicate scene entry rejected");
                var detached=entry.CaptureState();detached.character.name="Modified";
                check(entry.CaptureState().character.name=="Class100","defensive entry snapshot");
                check(NativeSaveStore.QueueResume(entry.CaptureState(),entry.SavePath) && NativeSaveStore.TakeResume(out _,out var path) && path==paths[0],"bootstrap transfer");
                var store=new NativeSaveStore(paths[0]);store.TryLoad(out var fixture,out _);
                fixture.hasVitals=true;fixture.hp=17;fixture.maxHp=200;fixture.mp=3;fixture.maxMp=45;fixture.hasInventory=true;fixture.inventory=new NativeInventory{gold=777};
                check(store.TrySave(fixture,out _),"preservation fixture saved");
                var service=new TutorialServerBoundary(paths[0],fixture.character,Source);
                check(service.Handle(new TutorialProgressRequest(0,false,false,true)).Accepted,"original script checkpoint prepares first input wait");
                store.TryLoad(out var saved,out _);var p=saved.character.tutorial;var script=new OriginalTutorialScript(Source,100);
                check(p.map==301 && p.actors.Length==4 && p.actors.Any(a=>a.id==0&&a.job==100) && p.actors.Any(a=>a.id==100&&a.job==125),"original map and separate commander/instructor IDs");
                check(p.actors.First(a=>a.id==0).magic==101 && p.actors.First(a=>a.id==0).skill==10101,"practice-only magic and skill");
                check(!service.Handle(new TutorialProgressRequest(p.step,false,true,false)).Accepted,"skip rejected");
                check(p.actors.First(a=>a.id==0).hp==200 && p.actors.First(a=>a.id==0).maxMp==65,"CHARABILITY copies raw base/max values");
                int inputs=0;
                while(!p.IsComplete && inputs++<400)
                {
                    var before=File.ReadAllBytes(paths[0]);
                    check(!service.Handle(new TutorialProgressRequest(p.step,"Invalid")).Accepted && before.SequenceEqual(File.ReadAllBytes(paths[0])),"wrong input preserves file at "+p.step);
                    var command=script.Current(p);
                    if(command.Name=="WAITMOVEPOS")
                    {
                        check(!service.Handle(new TutorialProgressRequest(p.step,"MovePosition",0,x:int.Parse(command.Arguments[1])+1,y:int.Parse(command.Arguments[2]))).Accepted,"wrong destination rejected");
                        check(p.actors.First(a=>a.id==0).hp==200,"movement lesson retains original HP");
                    }
                    if(command.Name=="WAITATTACKCHAR")
                        check(!service.Handle(new TutorialProgressRequest(p.step,"Attack",0,410)).Accepted,"wrong attack target rejected");
                    if(command.Name=="WAITMAGICMENU")
                        check(!service.Handle(new TutorialProgressRequest(p.step,"Magic",0,target:410,stage:99)).Accepted,"wrong magic stage rejected");
                    if(command.Name=="WAITKEYBOARD")
                        check(!service.Handle(new TutorialProgressRequest(p.step,"Key",key:"F12")).Accepted,"wrong key rejected");
                    if(p.activeCommand=="MagicTarget")check(p.actors.First(a=>a.id==0).hp==99,"rest HP is script absolute 99");
                    var request=RequiredInput(p,script);var result=(TutorialProgressResponse)service.Handle(request);
                    check(result.Accepted,"matching original wait accepted "+p.step+" "+request.Kind);
                    check(store.TryLoad(out saved,out _) && saved.hp==17 && saved.mp==3 && saved.inventory.gold==777 && !saved.character.hasClassAbilities,"practice preserves permanent vitals/economy/magic "+p.step);
                    var next=result.CaptureState().character.tutorial;
                    // A movement completion may leave the cursor at a DO; it still cannot repeat.
                    before=File.ReadAllBytes(paths[0]);
                    check(!service.Handle(request).Accepted && before.SequenceEqual(File.ReadAllBytes(paths[0])),"duplicate input rejected "+p.step);
                    p=next;service=new TutorialServerBoundary(paths[0],saved.character,Source);
                }
                check(p.unappliedLiteralCount==2 && p.lastUnappliedLiteral=="SETMP" && p.actors.First(a=>a.id==0).mp==45,"unregistered SETHP/SETMP are not invented aliases");
                check(p.IsComplete && p.step==420 && inputs<400,"all recovered commands reach END");
                check(p.actors.Any(a=>a.id==800&&a.job==805&&a.x==18&&a.y==7&&a.facing=="UP"),"original messenger ending");
                check(p.actors.Where(a=>new[]{1,2}.Contains(a.id)).All(a=>a.removed),"KILLCHAR1 removes mercenaries");
                check(p.actors.Where(a=>new[]{400,410}.Contains(a.id)).All(a=>a.dead && !a.removed),"KILLCHAR2 preserves distinct corpse state");
                check(saved.inventory.Count(398)==0,"no invented bomb/ring reward");
                check(!service.Handle(new TutorialProgressRequest(420,"Click")).Accepted,"completed script cannot repeat");
                var legacy=new NativeTutorialProgress {version=1,step=4};check(legacy.IsValid && !legacy.IsComplete,"four-page guide is not original completion");
                var legacyStore=new NativeSaveStore(paths[1]);legacyStore.TryLoad(out var legacySave,out _);legacySave.character.tutorial=legacy;legacyStore.TrySave(legacySave,out _);
                check(new TutorialServerBoundary(paths[1],legacySave.character,Source).Handle(new TutorialProgressRequest(4,false,false,true)).Accepted && legacyStore.TryLoad(out var migrated,out _) && migrated.character.tutorial.version==2 && migrated.character.tutorial.step>4,"legacy guide migrates to first original wait");
                check(!new TutorialServerBoundary(paths[2],new NativeCharacterProfile{name="Other",jobId=103},Source).Handle(new TutorialProgressRequest(0,false,false,true)).Accepted,"wrong character identity rejected");
                var preserved=File.ReadAllBytes(paths[2]);File.WriteAllText(paths[2],"{\"format\":\"DarkSaverWorld.NativeSave\",\"version\":99}");
                check(!new TutorialServerBoundary(paths[2],new NativeCharacterProfile{name="Class103",jobId=103},Source).Handle(new TutorialProgressRequest(0,false,false,true)).Accepted && File.ReadAllText(paths[2]).Contains("99"),"future save is not overwritten");
                File.WriteAllBytes(paths[2],preserved);
                var checkResponse=(TutorialProgressResponse)new TutorialServerBoundary(paths[2],new NativeCharacterProfile{name="Class103",jobId=103},Source).Handle(new TutorialProgressRequest(0,false,false,true));
                check(checkResponse.Accepted,"retry after storage recovery");
                var mutated=checkResponse.CaptureState();mutated.character.tutorial.actors[0].hp=9999;
                check(checkResponse.CaptureState().character.tutorial.actors[0].hp==180,"response actor snapshots are defensive");
                check(!File.Exists(Path.Combine(directory,"history.json")),"no automatic login history");
            }
            finally{NativeSaveStore.ClearResume();Directory.Delete(directory,true);}
        }
    }
}
