using DarkSaver.Combat;
var checks=0;
void Check(bool condition,string name){if(!condition)throw new Exception(name);checks++;}
var tutorialPresentationStart=checks;
DarkSaver.Tests.TutorialPresentationChecks.Run(Check);
Console.WriteLine("Tutorial presentation checks: "+(checks-tutorialPresentationStart)+" (offline, not Unity).");
var visitStart=checks;
DarkSaver.Tests.TrainingVisitChecks.Run(Check);
Console.WriteLine("Training visit travel checks: "+(checks-visitStart)+" (offline, not Unity).");
var trainingStart=checks;
DarkSaver.Tests.LocalTrainingProtocolChecks.Run(Check);
Console.WriteLine("Local training packet checks: "+(checks-trainingStart)+" (offline, not Unity).");
// Training resource identity never supplies monster spawns or class assignment.
for(int map=99;map<=109;map++)
{
    bool known=map>=101 && map<=107;
    Check(OriginalTrainingMaps.Contains(map)==known,"training native map range "+map);
    Check(OriginalTrainingMaps.TryGetMapId("Map_"+map+"_;",out var parsed)==known && parsed==(known?map:0),"training exact scene identity "+map);
}
foreach(var invalid in new[]{"Map_0101_;","map_101_;","Training101_Runtime","Kaosia_Map",null})
    Check(!OriginalTrainingMaps.TryGetMapId(invalid,out var unknown) && unknown==0,"no inferred training identity "+invalid);
foreach(var sample in new[]{(0,0,false),(23,0,false),(0,20,false),(19,20,false),(20,20,true),(21,20,false)})
    Check(CombatActionPoints.IsReady(sample.Item1,sample.Item2)==sample.Item3,"native readiness requires a known positive maximum "+sample);
foreach(var sample in new[]{(65,55,false),(65,55,true),(0,999,false),(10,-20,false),(int.MaxValue,0,true)})
{
    bool rejected=false;try{UnresolvedCombatDamage.Calculate(sample.Item1,sample.Item2,sample.Item3);}catch(NotSupportedException){rejected=true;}
    Check(rejected,"missing server verdict never invents damage "+sample);
}
// Explicit supplied AP state must not depend on removed single-player faction caps.
var costWorld=new CombatWorld();var costActor=new CombatUnit {Id=100,Hp=20,MaxHp=20};var costEnemy=new CombatUnit {Id=800,Hp=30,MaxHp=30,Enemy=true};
costWorld.Add(costActor);costWorld.Add(costEnemy);var costFlow=new CombatFlow(costWorld,new CombatServerBoundary(),true);
Check(!costFlow.ConfigureAttackApCost(100,2),"unknown maximum does not permit an explicit cost");
costActor.ActionPoints.ConfigureMaximum(10);costEnemy.ActionPoints.ConfigureMaximum(20);
Check(costFlow.ConfigureActionPointCosts(2,6) && costFlow.GetAttackApCost(100)==2,"explicit native costs accepted without invented faction cap");
Check(!costFlow.ConfigureAttackApCost(100,11) && costFlow.GetAttackApCost(100)==2,"cost above received unit maximum preserves previous cost");
Check(!costFlow.ConfigureActionPointCosts(0,6) && !costFlow.ConfigureActionPointCosts(2,short.MaxValue+1) && costFlow.GetAttackApCost(100)==2,"invalid cost payload rejected atomically");
Check(!costFlow.CanStartAction(new CombatRequest(100,800,CombatActionType.Attack),out _),"explicit costs do not grant starting AP");
costActor.ActionPoints.ApplyAbsolute(10);
Check(costFlow.CanStartAction(new CombatRequest(100,800,CombatActionType.Attack),out _) && costActor.Hp==20 && costEnemy.Hp==30,"received AP allows request validation without local damage");
// Group copies retain each received maximum, including unknown maxima, without aliasing.
foreach(var maximum in new[]{0,10,30})
{
    var copyWorld=new CombatWorld();var source=new CombatUnit {Id=200,Hp=20,MaxHp=20};
    if(maximum>0)source.ActionPoints.ConfigureMaximum(maximum);
    source.ActionPoints.ApplyAbsolute(maximum>0?maximum-1:23);
    Check(copyWorld.TryRegisterGroup(new[]{source}),"group accepts explicit AP fixture "+maximum);
    var copied=copyWorld.Get(200);
    Check(copied.ActionPoints.Current==source.ActionPoints.Current && copied.ActionPoints.Maximum==maximum
        && copied.ActionPoints.HasConfirmedMaximum==(maximum>0),"received AP and maximum survive registration "+maximum);
    source.ActionPoints.ApplyAbsolute(1);
    Check(copied.ActionPoints.Current==(maximum>0?maximum-1:23),"group copy owns detached AP "+maximum);
}
// Native removal/experience updates are independent authoritative observations.
var nativeWorld=new CombatWorld();
var nativeActor=new CombatUnit {Id=100,Hp=17,MaxHp=120,Mp=3,MaxMp=30,Experience=11};
var nativeEnemy=new CombatUnit {Id=800,Enemy=true,Hp=75,MaxHp=75};
nativeWorld.Add(nativeActor);nativeWorld.Add(nativeEnemy);
var nativeFlow=new CombatFlow(nativeWorld,new CombatServerBoundary(),true);
Check(!DarkSaver.GameConstants.BattleUseTemporaryLocalRules,"production excludes provisional battle simulation");
Check(nativeFlow.TryApplyDeath(800,true,preserveReceivedHp:true) && nativeEnemy.Hp==75,"corpse preserves independently received HP");
Check(nativeFlow.TryApplyDeath(800,false,preserveReceivedHp:true) && nativeEnemy.Hp==75,"removal preserves independently received HP");
Check(nativeActor.Experience==11 && nativeActor.Mp==3 && nativeActor.Hp==17,"death cannot invent XP or recovery");
Check(nativeFlow.TryApplyHp(800,0) && nativeEnemy.Hp==0,"separate HP0 response completes native state");
Check(nativeFlow.TryApplyExperience(new CombatExperienceResult(100,1234)) && nativeActor.Experience==1234,"native XP response replaces absolute value");
Check(nativeFlow.TryApplyExperience(new CombatExperienceResult(100,1234)) && nativeActor.Experience==1234,"duplicate XP response does not add reward");
Check(!nativeFlow.AdvanceActionPoints(10) && nativeActor.Hp==17 && nativeActor.Mp==3 && nativeActor.Experience==1234,"native clock does not heal or grant XP");
var ticks=new CombatActionPoints();
Check(ticks.Current==0 && !ticks.HasConfirmedMaximum && ticks.Maximum==0,"no invented initial AP or faction maximum");
Check(ticks.ApplyAbsolute(23) && ticks.Current==23,"incoming tick is retained without an invented faction cap");
Check(!ticks.Advance(1000) && ticks.Current==23,"no local AP regeneration timer");
Check(!ticks.TrySpend(4) && ticks.Current==23,"unknown maximum cannot authorize an action");
Check(ticks.ConfigureMaximum(30) && ticks.Maximum==30,"explicit maximum fixture applied");
Check(ticks.ApplyAbsolute(30) && ticks.TrySpend(4) && ticks.Current==26,"explicit confirmed action fixture only");
Check(!ticks.ConfigureMaximum(-1) && ticks.Maximum==30 && ticks.Current==26,"invalid maximum preserves state");
var boundary=new CombatServerBoundary();boundary.TemporaryRulesEnabled=true;
Check(!boundary.TemporaryRulesEnabled,"removed simulator cannot be re-enabled");
try {boundary.Resolve(nativeWorld,new CombatRequest(100,800,CombatActionType.Attack));throw new Exception("invented combat result");}
catch(NotSupportedException){checks++;}
DarkSaver.Tests.OriginalTutorialRangeChecks.Run(Check);
DarkSaver.Tests.OriginalTutorialMovementChecks.Run(Check);
DarkSaver.Tests.OriginalTutorialMapRouteChecks.Run(Check,File.ReadAllText("Assets/_Game/Resources/Tutorial/TUTORIAL.txt"),File.ReadAllBytes("분석/Saver_1.2/extracted/Saver_Files/Saver/MAP/301.mrc"));
Console.WriteLine($"PASS {checks} original client state checks; no Unity execution.");
