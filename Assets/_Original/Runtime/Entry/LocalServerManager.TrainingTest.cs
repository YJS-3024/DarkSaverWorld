using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class EntryAccountService { internal int? TrainingTestCommanderJob=>world?.CommanderJob; }

    public sealed partial class LocalServerManager
    {
        // Explicit local test policy, not recovered original server combat formulas.
        [SerializeField] private bool trainingCombatTest = true;
        #pragma warning disable 0649 // Fields populated by JsonUtility.
        [Serializable] private sealed class TestPlacement { public int unit,job,x,y; }
        [Serializable] private sealed class TestLayout { public int version,width,height; public TestPlacement[] units; }
        #pragma warning restore 0649
        private readonly Dictionary<int,double> trainingTestDeaths = new Dictionary<int,double>();
        private TrainingUnit[] trainingTestSpawns;
        private string trainingTestSession;
        private int trainingTestMap;
        private double trainingTestSeconds,trainingTestPulse;
        private Func<TrainingActionRequest,TrainingView,TrainingUnitsNotice> trainingTestAuthority;

        private TrainingUnitsNotice TrainingTestNotice(WorldView v, TrainingUnit[] units, string request=null)
            => new TrainingUnitsNotice {approved=true,id=Guid.NewGuid().ToString("N"),character=v.characterId,
                session=v.campaignSession,expectedRevision=v.training?.revision??0,units=units,request=request};

        private void BeginTrainingTest(WorldView v)
        {
            if(!trainingCombatTest||TrainingAuthority!=null||BattleAuthority!=null||NativeTrainingAuthority!=null||NativeBattleAuthority!=null)return;
            var asset=Resources.Load<TextAsset>("TrainingTestLayouts/"+v.record.map);
            if(asset==null)return;
            var layout=JsonUtility.FromJson<TestLayout>(asset.text);
            if(layout?.version!=1||layout.units==null||layout.units.Length==0||layout.units.Any(u=>u==null||u.unit<400||u.unit>999||u.x<0||u.y<0||u.x>=layout.width||u.y>=layout.height)
                ||layout.units.Select(u=>u.unit).Distinct().Count()!=layout.units.Length||layout.units.Select(u=>u.x+","+u.y).Distinct().Count()!=layout.units.Length)return;
            if(!v.record.healthKnown||v.record.hp<=0){Debug.LogWarning("수련장 테스트: 지휘관의 생존 HP/MP 상태가 필요합니다.");return;}
            var job=service.TrainingTestCommanderJob;
            if(!job.HasValue)return;
            var commander=new TrainingUnit {id=0,job=job.Value,x=v.record.x,y=v.record.y,direction=v.record.direction,life=1,
                hp=v.record.hp,maxHp=v.record.maxHp,mp=v.record.mp,maxMp=v.record.maxMp,ap=20,maxAp=20,
                canMove=true,canAttack=true,canRest=true,movementBudget=5,attackRange=1,nativePalette=0};
            var monsters=layout.units.Select(u=>new TrainingUnit {id=u.unit,job=u.job,x=u.x,y=u.y,life=1,
                hp=1,maxHp=1,mp=1,maxMp=1,ap=1,maxAp=1,canMove=true,canAttack=true,canRest=true,
                movementBudget=1,attackRange=1,nativePalette=1,effectiveStats=new CharacterGrowthStats {attack=1,defense=1,magicAttack=1,magicDefense=1,nativeValues=Enumerable.Repeat((short)1,16).ToArray()}}).ToArray();
            if(monsters.Any(u=>u.x==commander.x&&u.y==commander.y)){Debug.LogWarning("수련장 테스트: 진입점과 몬스터 배치가 겹칩니다.");return;}
            var reply=Notify_TrainingUnits(TrainingTestNotice(v,new[]{commander}.Concat(monsters).ToArray()));
            if(!reply.Accepted){Debug.LogWarning("수련장 테스트 초기화 거부: "+reply.Failure);return;}
            trainingTestSpawns=monsters.Select(u=>u.Copy()).ToArray();trainingTestSession=v.campaignSession;trainingTestMap=v.record.map;
            trainingTestDeaths.Clear();trainingTestSeconds=trainingTestPulse=0;
            trainingTestAuthority=ResolveTrainingTest;TrainingAuthority=trainingTestAuthority;
            Debug.Log("수련장 테스트 전투: 몬스터 능력치 1 / 기본 피해 1 / 사망 후 30초 리스폰 / 경험치·보상 없음");
        }
        private TrainingUnitsNotice ResolveTrainingTest(TrainingActionRequest request,TrainingView battle)
        {
            var v=WorldSnapshot;if(v==null||v.campaignSession!=trainingTestSession||v.record.map!=trainingTestMap)return null;
            var units=battle.units.Select(u=>u.Copy()).ToArray();var actor=units.First(u=>u.id==0);
            switch(request.action)
            {
                case TrainingAction.Move: actor.x=request.x;actor.y=request.y;break;
                case TrainingAction.Attack: var target=units.First(u=>u.id==request.target);target.hp=Math.Max(0,target.hp-1);break;
                case TrainingAction.Rest: actor.ap=actor.maxAp;break;
                default:return null;
            }
            return TrainingTestNotice(v,units,request.id);
        }
        private void PumpTrainingTest(double seconds)
        {
            if(trainingTestSpawns==null)return;
            var v=WorldSnapshot;
            if(!trainingCombatTest||v?.training==null||v.campaignSession!=trainingTestSession||v.record.map!=trainingTestMap||TrainingAuthority!=trainingTestAuthority)
            {if(TrainingAuthority==trainingTestAuthority)TrainingAuthority=null;trainingTestSpawns=null;trainingTestDeaths.Clear();return;}
            trainingTestSeconds+=seconds;
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
            var actor=units.First(u=>u.id==0);
            // Minimal test AI: adjacent living monsters strike once per second for one HP.
            if(aiTick&&actor.hp>0){int hits=units.Count(u=>u.id>=400&&u.hp>0&&Math.Abs(u.x-actor.x)+Math.Abs(u.y-actor.y)==1);
                if(hits>0){actor.hp=Math.Max(0,actor.hp-hits);changed=true;}}
            if(changed&&Notify_TrainingUnits(TrainingTestNotice(v,units)).Accepted)
                foreach(var u in units.Where(u=>u.hp>0))trainingTestDeaths.Remove(u.id);
        }
    }
}
