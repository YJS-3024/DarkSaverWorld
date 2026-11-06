using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class EntryAccountService
    {
        internal int? TrainingTestCommanderJob=>world?.CommanderJob;
        internal bool TrainingTestLayoutSize(int map,int width,int height)
        {var grid=world?.Map(map)?.map;return grid!=null&&grid.width==width&&grid.height==height;}
        internal int? TrainingTestCommandRange=>world?.Definition.combatCatalog?.Job(world.CommanderJob)?.rawCommandRange;
        internal TrainingUnit TrainingTestUnit(int id,int job,int x,int y)
        {
            var row=world?.Definition.combatCatalog?.Job(job);var stats=row?.baseStats;
            if(stats?.Valid!=true)return null;
            return new TrainingUnit {id=id,job=job,x=x,y=y,life=1,hp=stats.hp,maxHp=stats.hp,mp=Math.Max(0,stats.mp),maxMp=Math.Max(0,stats.mp),
                ap=LocalServerManager.TrainingApMaximum,maxAp=LocalServerManager.TrainingApMaximum,nativeState=0,nativeActive=true,nativeEnabled=true,canMove=stats.movement>0,canAttack=true,canRest=true,movementBudget=stats.movement,attackRange=1,
                nativePalette=id>=400?1:0,baseStats=stats.Copy(),effectiveStats=stats.CombatStats()};
        }
        internal int TrainingTestMoveDirection(TrainingActionRequest request,TrainingUnit actor)
        {
            var path=request.movePath??OriginalMemberCommands.Route(world.Map(world.Record.map),world.Definition.combatCatalog,world.Combat.Units,actor.id,request.x,request.y)?.ToArray();
            if(path==null||path.Length==0)return actor.direction;
            var before=path.Length>1?path[path.Length-2]:actor.Tile;var last=path[path.Length-1];
            return DarkSaver.Original.Tutorial.TutorialGeometry.FacingRow(last.X-before.X,last.Y-before.Y);
        }
        internal int? TrainingTestExperience(int job)=>world?.Definition.combatCatalog?.Job(job)?.baseExperience;
        internal WorldReply TrainingTestEnsureGrowth()
        {
            if(world?.Record.growth!=null)return WorldResult(WorldFailure.None);
            // New local characters have no received growth state yet. Do not infer a veteran's class level.
            if(world==null||world.Record.level>1||world.Definition.growthCatalog?.Row(world.CommanderJob,1)==null)return WorldResult(WorldFailure.RulesUnavailable);
            string id=Guid.NewGuid().ToString("N");
            return ReceiveCharacterGrowth(new CharacterGrowthNotice {approved=true,id=id,character=worldCharacter,session=world.GrowthSession,expectedRevision=0,
                operation=CharacterGrowthOperation.Snapshot,state=new CharacterGrowthRecord {job=world.CommanderJob,classLevel=1,totalLevel=1,revision=1,lastNotice=id}});
        }
        internal bool TrainingTestCanPlace(TrainingUnit unit,TrainingUnit[] occupied)
        {
            var map=world.Map(world.Record.map);int? type=OriginalMemberCommands.MovementType(unit,world.Definition.combatCatalog);
            return type.HasValue&&map.map.Contains(unit.Tile)&&map.map.Terrain(unit.Tile,type.Value*3+2)>=0&&map.map.Terrain(unit.Tile,type.Value*3+2)!=99
                &&!occupied.Any(u=>u.x==unit.x&&u.y==unit.y)&&!map.npcs.Any(n=>n.x==unit.x&&n.y==unit.y)&&!map.portals.Any(p=>p.cells.Any(c=>c.Tile==unit.Tile));
        }
    }

    public sealed partial class LocalServerManager
    {
        // Explicit local test policy, not recovered original server combat formulas.
        [SerializeField] private bool trainingCombatTest = true;
        [Serializable] private sealed class TestPlacement { public int unit=0,job=0,x=0,y=0; }
        [Serializable] private sealed class TestLayout { public int version=0,width=0,height=0; public string mapKey=null;public TestPlacement[] units=null; }
        private readonly Dictionary<int,double> trainingTestDeaths = new Dictionary<int,double>();
        private TrainingUnit[] trainingTestSpawns;
        private string trainingTestSession;
        private int trainingTestMap;
        private double trainingTestSeconds,trainingTestPulse,trainingTestApElapsed;
        private Func<TrainingActionRequest,TrainingView,TrainingUnitsNotice> trainingTestAuthority;

        private TrainingUnitsNotice TrainingTestNotice(WorldView v, TrainingUnit[] units, string request=null)
            => new TrainingUnitsNotice {approved=true,id=Guid.NewGuid().ToString("N"),character=v.characterId,
                session=v.campaignSession,expectedRevision=v.training?.revision??0,units=units,request=request};
        // Local stat-based physical resolution, not a recovered original server formula.
        // Effective received stats take precedence; the factory supplies source base stats when unknown.
        internal static int? TrainingPhysicalDamage(TrainingUnit source,TrainingUnit target)
        {
            if(source?.effectiveStats?.Valid!=true||target?.effectiveStats?.Valid!=true)return null;
            return Math.Max(0,source.effectiveStats.attack-target.effectiveStats.defense);
        }

        private void BeginTrainingTest(WorldView v)
        {
            if(v==null)return;
            if(trainingTestAuthority!=null&&TrainingAuthority==trainingTestAuthority
                &&(v.training==null||v.campaignSession!=trainingTestSession||v.record.map!=trainingTestMap))ClearTrainingTest();
            if(v.training!=null)return;
            if(!trainingCombatTest||TrainingAuthority!=null||BattleAuthority!=null||NativeTrainingAuthority!=null||NativeBattleAuthority!=null)return;
            var asset=Resources.Load<TrainingTestLayoutCatalog>("TrainingTestLayoutCatalog")?.Find(v.record.map);
            if(asset==null){Debug.LogWarning("수련장 "+v.record.map+": 게임용 편집 배치가 없습니다. 편집툴에서 해당 수련장 배치를 저장하거나 초기 유닛 응답을 연결하세요.");return;}
            TestLayout layout;
            try{layout=JsonUtility.FromJson<TestLayout>(asset.text);}
            catch(ArgumentException e){Debug.LogWarning("수련장 "+v.record.map+": 배치 JSON을 읽지 못했습니다. 편집툴에서 다시 저장하세요. "+e.Message);return;}
            if(layout?.version!=1||!int.TryParse(layout.mapKey,out int layoutMap)||layoutMap!=v.record.map||!service.TrainingTestLayoutSize(v.record.map,layout.width,layout.height)
                ||layout.units==null||layout.units.Length==0||layout.units.Any(u=>u==null||u.unit<400||u.unit>999||u.x<0||u.y<0||u.x>=layout.width||u.y>=layout.height)
                ||layout.units.Select(u=>u.unit).Distinct().Count()!=layout.units.Length||layout.units.Select(u=>u.x+","+u.y).Distinct().Count()!=layout.units.Length)
            {Debug.LogWarning("수련장 "+v.record.map+": 배치의 맵 번호·크기·유닛 번호·좌표를 확인하고 편집툴에서 다시 저장하세요.");return;}
            if(v.record.ghost||v.record.healthKnown&&v.record.hp<=0){Debug.LogWarning("수련장 테스트: 사망한 지휘관은 먼저 부활해야 합니다.");return;}
            var job=service.TrainingTestCommanderJob;
            if(!job.HasValue)return;
            var commander=service.TrainingTestUnit(0,job.Value,v.record.x,v.record.y);
            if(commander==null){Debug.LogWarning("수련장 테스트: 해당 클래스의 기본 스탯 테이블이 필요합니다. 직업="+job.Value);return;}
            commander.direction=v.record.direction;
            if(v.record.healthKnown){commander.hp=v.record.hp;commander.maxHp=v.record.maxHp;commander.mp=v.record.mp;commander.maxMp=v.record.maxMp;}
            if(v.record.growth?.stats!=null)commander.effectiveStats=v.record.growth.stats.Copy();
            var monsters=layout.units.Select(u=>service.TrainingTestUnit(u.unit,u.job,u.x,u.y)).ToArray();
            if(monsters.Any(u=>u==null)){Debug.LogWarning("수련장 테스트: 배치된 몬스터/용병 직업의 기본 스탯이 없습니다.");return;}
            // Use only actually hired soldiers. Do not invent ownership, hire costs or extra units.
            if(v.record.hiredCount>9){Debug.LogWarning("수련장 테스트: 보유 용병 수가 지휘관 부대 슬롯 1~9 범위를 초과합니다.");return;}
            var members=new List<TrainingUnit>();
            for(int id=1;id<=v.record.hiredCount;id++)
            {
                var member=service.TrainingTestUnit(id,v.record.hiredJob,0,0);
                if(member==null){Debug.LogWarning("수련장 테스트: 보유 용병 기본 스탯이 없습니다.");return;}
                bool placed=false;
                var candidates=Enumerable.Range(1,layout.width-2).SelectMany(x=>Enumerable.Range(1,layout.height-2).Select(y=>(x,y)))
                    .OrderBy(p=>Math.Abs(p.x-commander.x)+Math.Abs(p.y-commander.y)).ThenBy(p=>p.y).ThenBy(p=>p.x);
                foreach(var tile in candidates){member.x=tile.x;member.y=tile.y;
                    if(service.TrainingTestCanPlace(member,new[]{commander}.Concat(monsters).Concat(members).ToArray())){placed=true;break;}}
                if(!placed){Debug.LogWarning("수련장 테스트: 보유 용병을 놓을 수 있는 칸이 없습니다.");return;}
                member.attackTargets=monsters.Select(u=>u.id).ToArray();members.Add(member);
            }
            commander.canCommand=true;commander.commandRange=service.TrainingTestCommandRange;
            commander.troopOrder=1;
            if(monsters.Any(u=>u.x==commander.x&&u.y==commander.y)){Debug.LogWarning("수련장 테스트: 진입점과 몬스터 배치가 겹칩니다.");return;}
            if(!service.TrainingTestCanPlace(commander,monsters.Concat(members).ToArray()))
            {Debug.LogWarning("수련장 테스트: 직업 "+job.Value+"의 병종으로 진입 좌표 ("+commander.x+","+commander.y+")에 배치할 수 없습니다. 편집툴의 진입점과 원본 지형을 확인하세요.");return;}
            var growth=service.TrainingTestEnsureGrowth();
            if(!growth.Accepted){Debug.LogWarning("수련장 경험치 기록 준비 거부: "+growth.Failure);return;}
            v=WorldSnapshot;
            var reply=Notify_TrainingUnits(TrainingTestNotice(v,new[]{commander}.Concat(members).Concat(monsters).ToArray()));
            if(!reply.Accepted){Debug.LogWarning("수련장 테스트 초기화 거부: "+reply.Failure);return;}
            var current=WorldSnapshot;
            if(current?.training==null||current.characterId!=v.characterId||current.campaignSession!=v.campaignSession||current.record.map!=v.record.map
                ||current.training.revision!=reply.View.training.revision||TrainingAuthority!=null||BattleAuthority!=null||NativeTrainingAuthority!=null||NativeBattleAuthority!=null)return;
            trainingTestSpawns=monsters.Select(u=>u.Copy()).ToArray();trainingTestSession=v.campaignSession;trainingTestMap=v.record.map;
            trainingTestDeaths.Clear();trainingTestSeconds=trainingTestPulse=trainingTestApElapsed=0;
            trainingTestAuthority=ResolveTrainingTest;TrainingAuthority=trainingTestAuthority;
            Debug.Log("수련장 전투: 캐릭터·몬스터·용병 원본 기본표 사용 / 로컬 물리 피해 = 공격력 - 방어력 (0 하한) / 사망 후 30초 리스폰 / 처치 경험치는 원본 획득경 지급 / 금전·아이템 보상 없음");
        }
        private TrainingUnitsNotice ResolveTrainingTest(TrainingActionRequest request,TrainingView battle)
        {
            var v=WorldSnapshot;if(v==null||v.campaignSession!=trainingTestSession||v.record.map!=trainingTestMap)return null;
            var units=battle.units.Select(u=>u.Copy()).ToArray();var actor=units.First(u=>u.id==request.source);
            var notice=TrainingTestNotice(v,units,request.id);
            switch(request.action)
            {
                case TrainingAction.Move:
                case TrainingAction.Regroup:
                case TrainingAction.SoldierMove:
                    actor.direction=service.TrainingTestMoveDirection(request,actor);actor.x=request.x;actor.y=request.y;
                    if(request.action==TrainingAction.SoldierMove)actor.nativeState=request.aiStateAfter;
                    break;
                case TrainingAction.Attack:
                    var target=units.First(u=>u.id==request.target);
                    int? damage=TrainingPhysicalDamage(actor,target);if(!damage.HasValue)return null;
                    int before=target.hp;target.hp=Math.Max(0,target.hp-damage.Value);
                    var pair=BattleAttackEffect.Create(actor,target,0);actor.direction=pair[0].direction;target.direction=pair[1].direction;
                    notice.feedbackEvents=new[]{new BattleFeedbackEvent {kind=BattleFeedbackKind.Attack,unit=actor.id,life=actor.life,target=target.id,targetLife=target.life,value=0},TrainingHealthEvent(target)};
                    if(before>0&&target.hp==0){target.nativeState=(target.nativeState??0)|0x80u;notice.lifecycleEvents=new[]{TrainingDeathEvent(target)};}
                    if(target.id>=400&&before>0&&target.hp==0)
                    {
                        int? reward=service.TrainingTestExperience(target.job);
                        if(!reward.HasValue)return null;
                        if(reward.Value>0)
                        {
                            var growth=v.record.growth;
                            if(growth==null||growth.revision==long.MaxValue||ulong.MaxValue-growth.experience<(ulong)reward.Value)return null;
                            // Death and absolute owner XP use the same revision-checked durable notice.
                            notice.experience=growth.experience+(ulong)reward.Value;notice.expectedGrowthRevision=growth.revision;
                        }
                    }
                    break;
                case TrainingAction.Rest: actor.ap=actor.maxAp;break;
                case TrainingAction.TroopOrder: units.First(u=>u.id==0).troopOrder=request.nativeOrder;break;
                default:return null;
            }
            if(request.automatic)
            {
                actor.nativeState=request.aiStateAfter;
                // Verified SoldierAIAction outcome resets AP; manual action costs remain separate.
                if(request.action!=TrainingAction.SoldierMove)actor.ap=0;
            }
            return notice;
        }
        private void ClearTrainingTest()
        {
            if(trainingTestAuthority!=null&&TrainingAuthority==trainingTestAuthority)TrainingAuthority=null;
            trainingTestAuthority=null;trainingTestSpawns=null;trainingTestSession=null;trainingTestDeaths.Clear();
            trainingTestSeconds=trainingTestPulse=trainingTestApElapsed=0;
        }
        private static BattleFeedbackEvent TrainingHealthEvent(TrainingUnit unit)
            =>new BattleFeedbackEvent {kind=BattleFeedbackKind.Health,unit=unit.id,life=unit.life,value=unit.hp};
        private static BattleLifecycleEvent TrainingDeathEvent(TrainingUnit unit)
            =>new BattleLifecycleEvent {kind=BattleLifecycleKind.Death,unit=unit.id,life=unit.life};
        private bool OwnsTrainingTest(WorldView v)
            =>trainingTestSpawns!=null&&trainingTestAuthority!=null&&TrainingAuthority==trainingTestAuthority&&v?.training!=null
                &&v.campaignSession==trainingTestSession&&v.record.map==trainingTestMap&&BattleAuthority==null&&NativeTrainingAuthority==null&&NativeBattleAuthority==null;
        private void PumpTrainingAp(double seconds,WorldView v)
        {
            if(v.training.pending!=null)return; // A pulse must not cancel an outstanding action request.
            if(v.training.units.All(u=>u.nativeActive!=true||u.nativeEnabled!=true||u.ap>=u.maxAp)){trainingTestApElapsed=0;return;}
            trainingTestApElapsed+=seconds;
            int pulses=(int)Math.Min(TrainingApMaximum,Math.Floor(trainingTestApElapsed*TrainingApRecoveryPerSecond));
            for(int i=0;i<pulses;i++)
            {
                var current=WorldSnapshot;
                if(current?.training==null||current.campaignSession!=trainingTestSession||current.record.map!=trainingTestMap||current.training.pending!=null)return;
                var reply=Notify_TrainingTicks(new BattleTickNotice {approved=true,id=Guid.NewGuid().ToString("N"),character=current.characterId,
                    session=current.campaignSession,expectedRevision=current.training.revision,packet=OriginalBattleTicks.PulsePacket()});
                var after=WorldSnapshot;
                if(!reply.Accepted||!OwnsTrainingTest(after)||after.campaignSession!=current.campaignSession)return;
                trainingTestApElapsed-=1.0/TrainingApRecoveryPerSecond;
                if(reply.View.training.units.All(u=>u.nativeActive!=true||u.nativeEnabled!=true||u.ap>=u.maxAp)){trainingTestApElapsed=0;break;}
            }
        }
        private void PumpTrainingTest(double seconds)
        {
            if(trainingTestSpawns==null)return;
            var v=WorldSnapshot;
            if(!trainingCombatTest||!OwnsTrainingTest(v)){ClearTrainingTest();return;}
            if(double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<0)return;
            trainingTestSeconds+=seconds;
            string frameSession=v.campaignSession;
            PumpTrainingAp(seconds,v);v=WorldSnapshot;
            if(v?.campaignSession!=frameSession)return;
            if(!OwnsTrainingTest(v)){ClearTrainingTest();return;}
            foreach(var spawn in trainingTestSpawns)
            {var unit=v.training.Unit(spawn.id);if(unit!=null&&unit.hp==0&&!trainingTestDeaths.ContainsKey(unit.id))trainingTestDeaths[unit.id]=trainingTestSeconds;}
            if(v.training.pending!=null||v.moving)return;
            bool aiTick=trainingTestSeconds>=trainingTestPulse+1;
            if(aiTick)trainingTestPulse=trainingTestSeconds;
            var units=v.training.units.Select(u=>u.Copy()).ToArray();bool changed=false;
            foreach(var spawn in trainingTestSpawns)
            {
                var unit=units.FirstOrDefault(u=>u.id==spawn.id);
                if(unit==null)continue;
                if(unit.hp==0&&trainingTestDeaths.TryGetValue(unit.id,out var death)&&trainingTestSeconds-death>=TrainingMonsterRespawnDelaySeconds
                    &&!units.Any(u=>u.hp>0&&u.x==spawn.x&&u.y==spawn.y))
                {var fresh=spawn.Copy();fresh.life=checked(unit.life+1);units[Array.IndexOf(units,unit)]=fresh;changed=true;}
            }
            if(changed)
            {
                if(!Notify_TrainingUnits(TrainingTestNotice(v,units)).Accepted||!OwnsTrainingTest(WorldSnapshot)||WorldSnapshot.campaignSession!=frameSession)return;
                foreach(var u in units.Where(u=>u.hp>0))trainingTestDeaths.Remove(u.id);
            }
            if(!aiTick)return;
            var current=WorldSnapshot;var leader=current.training.Commander;
            var attackers=current.training.units.Where(u=>u.id>=400&&OriginalCommandRange.Living(u)&&Math.Abs(u.x-leader.x)+Math.Abs(u.y-leader.y)==1).Select(u=>u.id).ToArray();
            // Separate ordered notices preserve native single-target feedback and durable HP revisions.
            foreach(int id in attackers)
            {
                current=WorldSnapshot;if(!OwnsTrainingTest(current)||current.campaignSession!=frameSession||current.training.pending!=null||current.moving)return;
                units=current.training.units.Select(u=>u.Copy()).ToArray();var actor=units.First(u=>u.id==0);var enemy=units.FirstOrDefault(u=>u.id==id);
                if(actor.hp==0)return;
                if(!OriginalCommandRange.Living(enemy)||Math.Abs(enemy.x-actor.x)+Math.Abs(enemy.y-actor.y)!=1)continue;
                int? damage=TrainingPhysicalDamage(enemy,actor);if(!damage.HasValue)continue;
                var pair=BattleAttackEffect.Create(enemy,actor,0);enemy.direction=pair[0].direction;actor.direction=pair[1].direction;
                actor.hp=Math.Max(0,actor.hp-damage.Value);
                var notice=TrainingTestNotice(current,units);
                notice.feedbackEvents=new[]{new BattleFeedbackEvent {kind=BattleFeedbackKind.Attack,unit=enemy.id,life=enemy.life,target=actor.id,targetLife=actor.life,value=0},TrainingHealthEvent(actor)};
                if(actor.hp==0){actor.nativeState=(actor.nativeState??0)|0x80u;notice.lifecycleEvents=new[]{TrainingDeathEvent(actor)};}
                if(!Notify_TrainingUnits(notice).Accepted)return;
            }
        }
    }
}
