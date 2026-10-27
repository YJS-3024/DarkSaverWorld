using DarkSaver.Combat;
var checks=0;
void Check(bool condition,string name){if(!condition)throw new Exception(name);checks++;}
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
Console.WriteLine($"PASS {checks} original client state checks; no Unity execution.");
