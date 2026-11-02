using System;
using System.Collections.Generic;
using System.Linq;

namespace DarkSaver.Original.World
{
    public sealed class NativeBattleResponse
    {
        // One local action response, not a recovered TCP frame or original server batching rule.
        public string id,request;
        public bool approved;
        public byte[][] packets;
        public NativeBattleResponse Copy()=>new NativeBattleResponse{id=id,request=request,approved=approved,packets=packets?.Select(p=>p==null?null:(byte[])p.Clone()).ToArray()};
    }
    public static class OriginalBattleResponses
    {
        public const int MaximumPackets=32;
        public static bool Supports(TrainingAction action)=>action==TrainingAction.Attack||action==TrainingAction.Rest;
        public static TrainingUnitsNotice Prepare(TrainingActionRequest request,TrainingView view,NativeBattleResponse response)
        {
            if(request==null||view==null||response==null||!Supports(request.action)||request.NativePacket==null
                ||view.pending?.id!=request.id||request.revision!=view.revision||response.request!=request.id
                ||!Guid.TryParseExact(response.id,"N",out _)||view.units==null||view.units.Any(u=>u==null)
                ||view.units.Select(u=>u.id).Distinct().Count()!=view.units.Length||response.packets?.Length>MaximumPackets
                ||response.packets?.Any(p=>p==null||p.Length>8)==true)return null;
            var reply=response.Copy();
            var notice=new TrainingUnitsNotice{id=reply.id,request=request.id,character=request.character,session=request.session,expectedRevision=request.revision,approved=reply.approved,nativeResponse=reply};
            if(!reply.approved)return reply.packets?.Length>0?null:notice;
            if(reply.packets==null||reply.packets.Length<1||reply.packets.Length>MaximumPackets)return null;
            var units=view.units.Select(u=>u.Copy()).ToArray();var source=units.FirstOrDefault(u=>u.id==request.source);
            if(source==null||source.life!=request.sourceLife)return null;
            if(request.action==TrainingAction.Attack&&!units.Any(u=>u.id==request.target&&u.life==request.targetLife))return null;
            var numbers=new Dictionary<int,BattleFeedbackEvent>();var attacks=new List<BattleFeedbackEvent>();var deaths=new Dictionary<int,BattleLifecycleEvent>();
            var seen=new HashSet<(int kind,int unit)>();
            foreach(var bytes in reply.packets)
            {
                if(bytes==null||bytes.Length<2||bytes[0]!=0x5c)return null;
                int id;
                if(OriginalBattleTicks.TryDecode(bytes,out var tick))
                {
                    // Global pulses and unrelated actors stay on the independent notice route.
                    if(tick.pulse||tick.unit!=request.source||!seen.Add((0x52,tick.unit)))return null;
                    id=tick.unit;var before=units.First(u=>u.id==id);
                    if(!OriginalBattleTicks.TryMenu(before,tick.ap,tick.menu,out var after))return null;
                    units[Array.IndexOf(units,before)]=after;
                }
                else if(OriginalBattleFeedback.TryDecode(bytes,out var feedback))
                {
                    id=feedback.unit;if(id!=request.source&&(request.action!=TrainingAction.Attack||id!=request.target))return null;
                    var actor=units.FirstOrDefault(u=>u.id==id);if(actor==null)return null;
                    if(!seen.Add((bytes[1],id)))return null;
                    if(feedback.kind==BattleFeedbackKind.Attack)
                    {
                        if(request.action!=TrainingAction.Attack||id!=request.source||feedback.target!=request.target)return null;
                        var target=units.First(u=>u.id==feedback.target);var pair=BattleAttackEffect.Create(actor,target,feedback.value);
                        actor.direction=pair[0].direction;target.direction=pair[1].direction;
                        attacks.Add(new BattleFeedbackEvent{kind=feedback.kind,unit=id,life=actor.life,target=target.id,targetLife=target.life,value=feedback.value});
                    }
                    else
                    {
                        if(feedback.kind==BattleFeedbackKind.Health)
                        {if(feedback.value<0||feedback.value>actor.maxHp)return null;actor.hp=feedback.value;}
                        if(feedback.kind==BattleFeedbackKind.Mana)
                        {if(feedback.value<0||feedback.value>actor.maxMp)return null;actor.mp=feedback.value;}
                        // Each field occurs at most once. HP->MP and MP->HP preserve native
                        // shared-slot order; repeated same-field packets need separate deliveries.
                        numbers[id]=new BattleFeedbackEvent{kind=feedback.kind,unit=id,life=actor.life,value=feedback.value};
                    }
                }
                else if(OriginalBattleLifecycle.TryDecode(bytes,out var lifecycle))
                {
                    id=lifecycle.unit;
                    if(request.action!=TrainingAction.Attack||lifecycle.kind!=BattleLifecycleKind.Death||id!=request.target||!seen.Add((7,id)))return null;
                    var before=units.FirstOrDefault(u=>u.id==id);
                    if(!OriginalBattleLifecycle.TryApply(before,lifecycle.kind,out var after))return null;
                    units[Array.IndexOf(units,before)]=after;deaths[id]=new BattleLifecycleEvent{kind=BattleLifecycleKind.Death,unit=id,life=after.life};
                }
                else return null;
            }
            notice.units=units;notice.feedbackEvents=attacks.Concat(numbers.Values.OrderBy(e=>e.unit)).ToArray();notice.lifecycleEvents=deaths.Values.OrderBy(e=>e.unit).ToArray();return notice;
        }
        public static bool Matches(TrainingUnitsNotice notice,TrainingUnitsNotice expected,bool allowMagicEvents=false)
        {
            if(notice==null||expected==null||notice.id!=expected.id||notice.request!=expected.request||notice.character!=expected.character
                ||notice.session!=expected.session||notice.expectedRevision!=expected.expectedRevision||notice.approved!=expected.approved
                ||notice.experience.HasValue||notice.expectedGrowthRevision!=0||notice.magicTargets!=null||!allowMagicEvents&&notice.magicEvents!=null||notice.magicRestoreMp.HasValue
                ||notice.revivedTargets!=null||notice.itemConsumed.HasValue||notice.itemRemainingUses.HasValue)return false;
            if(!notice.approved)return true;
            if(allowMagicEvents)
            {
                var actual=notice.magicEvents??Array.Empty<BattleMagicEvent>();var wanted=expected.magicEvents??Array.Empty<BattleMagicEvent>();
                if(actual.Length!=wanted.Length)return false;
                for(int i=0;i<actual.Length;i++)if(actual[i]==null||actual[i].magic!=wanted[i].magic||actual[i].source!=wanted[i].source||actual[i].sourceLife!=wanted[i].sourceLife
                    ||actual[i].x!=wanted[i].x||actual[i].y!=wanted[i].y||actual[i].targets==null||!actual[i].targets.SequenceEqual(wanted[i].targets))return false;
            }
            if(notice.units==null||notice.units.Length!=expected.units.Length||notice.units.Any(u=>u==null)||notice.units.Select(u=>u.id).Distinct().Count()!=notice.units.Length)return false;
            foreach(var unit in expected.units)
            {var actual=notice.units.SingleOrDefault(u=>u.id==unit.id);if(actual==null||!OriginalMemberCommands.Same(unit,actual))return false;}
            var a=notice.feedbackEvents??Array.Empty<BattleFeedbackEvent>();var b=expected.feedbackEvents;
            if(a.Length!=b.Length||a.Any(e=>e==null))return false;
            for(int i=0;i<a.Length;i++)if(a[i].kind!=b[i].kind||a[i].unit!=b[i].unit||a[i].life!=b[i].life||a[i].target!=b[i].target||a[i].targetLife!=b[i].targetLife||a[i].value!=b[i].value)return false;
            var c=notice.lifecycleEvents??Array.Empty<BattleLifecycleEvent>();var d=expected.lifecycleEvents;
            if(c.Length!=d.Length||c.Any(e=>e==null))return false;
            for(int i=0;i<c.Length;i++)if(c[i].kind!=d[i].kind||c[i].unit!=d[i].unit||c[i].life!=d[i].life)return false;
            return true;
        }
    }
}
