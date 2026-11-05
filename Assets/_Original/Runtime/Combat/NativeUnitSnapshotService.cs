using System;
using System.Linq;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class EntryAccountService
    {
        private WorldReply ReceiveBattleTeamNames(NativeBattleNotice notice)
        {
            var ready=ReadyBattle(true);if(ready!=WorldFailure.None)return WorldResult(ready);
            var input=notice?.Copy();
            if(input?.approved!=true||!OriginalBattleTeamNames.TryDecode(input.packet,out var rows))return WorldResult(WorldFailure.InvalidInput);
            if(!input.expectedTeamRevision.HasValue)return WorldResult(WorldFailure.RulesUnavailable);
            bool current=world.NativeTeamSession==world.CampaignSession;
            if(input.character!=worldCharacter||input.session!=world.CampaignSession||!Guid.TryParseExact(input.id,"N",out _)
                ||input.expectedRevision!=(world.Combat?.Revision??0)||input.expectedTeamRevision!=world.CurrentNativeTeamRevision
                ||world.CurrentNativeTeamRevision==long.MaxValue||current&&world.NativeTeamNotices.Contains(input.id))return WorldResult(WorldFailure.StaleRequest);
            // Native5C60 updates a presentation table, never a permanent account name,
            // party membership, battle action, XP, resource amount or saved profile.
            if(!current){world.NativeTeamNotices.Clear();world.NativeTeamRevision=0;}
            world.NativeTeams=rows.Select(r=>r.Copy()).ToArray();world.NativeTeamSession=world.CampaignSession;
            world.NativeTeamRevision++;world.NativeTeamNotices.Add(input.id);return WorldResult(WorldFailure.None);
        }
        public WorldReply ReceiveBattleUnitPackets(NativeBattleUnitsNotice delivery)
        {
            var ready=ReadyBattle(true);if(ready!=WorldFailure.None)return WorldResult(ready);
            var input=delivery?.Copy();var combat=world.Combat??new TrainingCombatSession();
            if(input?.approved!=true||input.packets==null||input.packets.Length==0||input.packets.Length>1000||input.packets.Any(p=>p?.packet==null||p.life<1))return WorldResult(WorldFailure.InvalidInput);
            if(input.character!=worldCharacter||input.session!=world.CampaignSession||!Guid.TryParseExact(input.id,"N",out _)
                ||input.expectedRevision!=combat.Revision||combat.Notices.Contains(input.id))return WorldResult(WorldFailure.StaleRequest);
            if(world.Definition.combatCatalog==null)return WorldResult(WorldFailure.RulesUnavailable);
            var units=combat.Units.Select(u=>u.Copy()).ToList();var seen=new System.Collections.Generic.HashSet<int>();var stopMotions=new System.Collections.Generic.List<int>();ulong? experience=null;
            foreach(var message in input.packets)
            {
                if(!OriginalNativeUnits.TryDecode(message.packet,out var packet)||!seen.Add(packet.Unit))return WorldResult(WorldFailure.InvalidInput);
                var info=packet.State();var f=info.fields;var previous=combat.Units.FirstOrDefault(u=>u.id==packet.Unit);
                var row=world.Definition.combatCatalog.Job(f[1]);
                if(row==null||f[0]>999||f[11]<0||f[12]<0)return WorldResult(WorldFailure.InvalidInput);
                if(packet.kind==0x50&&packet.Unit==0&&(previous==null||previous.life!=message.life)&&!world.Record.healthKnown)
                    return WorldResult(WorldFailure.RulesUnavailable); // A partial message cannot confirm an unknown owner MP range.
                var unit=previous!=null&&previous.life==message.life?previous.Copy():new TrainingUnit{id=f[0],life=message.life,direction=1};
                unit.job=f[1];unit.nativeInfo=info;unit.nativeAppearanceJob=f[2]<0?f[1]:f[2];
                unit.x=f[18];unit.y=f[19];unit.hp=f[8];unit.maxHp=f[7];
                if(packet.kind==0x51){unit.mp=f[10];unit.maxMp=f[9];}
                else if((previous==null||previous.life!=message.life)&&unit.id==0&&world.Record.healthKnown){unit.mp=world.Record.mp;unit.maxMp=world.Record.maxMp;}
                unit.maxAp=f[22];unit.ap=f[23];unit.movementType=f[6];unit.movementBudget=f[13];unit.commandRange=f[17];unit.attackRange=f[43]>0?(int?)f[43]:null;
                int menu=OriginalNativeUnits.MenuMask(unchecked((uint)f[21]));unit.nativeMenuMask=menu;
                unit.canMove=(menu&16)!=0;unit.canAttack=(menu&1)!=0&&unit.attackRange.HasValue;unit.canRest=(menu&8)!=0;unit.canCommand=(menu&64)!=0;
                // resetcharacter writes active=1 before testing enabled. A removed
                // but still enabled record retains its prior state/order.
                if(previous==null||previous.life!=message.life||previous.nativeEnabled!=true){unit.nativeState=0x30;unit.troopOrder=1;}
                unit.nativeActive=unit.nativeEnabled=true;unit.nativeStatusFlags=OriginalNativeUnits.StatusFlags(unchecked((uint)f[20]));unit.nativeExperience=info.Experience;
                unit.baseStats=unit.baseStats??row.baseStats?.Copy();
                unit.effectiveStats=unit.effectiveStats??(unit.id==0?world.Record.growth?.stats?.Copy():null)??row.baseStats?.CombatStats();
                if(unit.effectiveStats!=null){unit.effectiveStats.attack=f[11];unit.effectiveStats.defense=f[12];unit.effectiveStats.nativeValues=null;}
                var motion=Movement(combat,unit.id);
                // Original resetcharacter preserves a live path while replacing its stats.
                if(previous!=null&&previous.life==message.life&&previous.nativeActive==true&&motion?.Moving==true){unit.x=previous.x;unit.y=previous.y;}
                else if(motion?.Moving==true)stopMotions.Add(unit.id);
                if(unit.id==0)
                {
                    unit.ghost=world.Record.ghost;
                    if(packet.kind==0x51&&world.Record.growth!=null)
                    {if(!input.expectedGrowthRevision.HasValue)return WorldResult(WorldFailure.RulesUnavailable);experience=info.Experience;}
                }
                units.RemoveAll(u=>u.id==unit.id);units.Add(unit);
            }
            return ReceiveBattleUnits(new TrainingUnitsNotice{approved=true,id=input.id,character=input.character,session=input.session,expectedRevision=input.expectedRevision,
                units=units.OrderBy(u=>u.id).ToArray(),experience=experience,expectedGrowthRevision=input.expectedGrowthRevision??0},true,stopMotions.ToArray(),seen.ToArray());
        }
        public WorldReply ReceiveTrainingUnitPackets(NativeBattleUnitsNotice notice)
        {var ready=ReadyTraining(true);return ready==WorldFailure.None?ReceiveBattleUnitPackets(notice):WorldResult(ready);}
        private static void ResetNativeUnitPresentation(TrainingCombatSession combat,TrainingUnit[] units,int[] ids)
        {
            if(ids==null)return;
            combat.MagicEffects.RemoveAll(e=>ids.Contains(e.role==BattleMagicEffectRole.ChargeOnly?e.sourceUnit:e.targetUnit));
            foreach(int id in ids)
            {
                var before=combat.Units.FirstOrDefault(u=>u.id==id);var after=units.First(u=>u.id==id);
                bool preserves=before!=null&&before.life==after.life&&before.nativeEnabled==true;
                uint flags=before?.nativeState??0;
                bool numeric=combat.NumberEffects.TryGetValue(id,out var number)&&!number.Expired||(flags&0x40u)!=0;
                bool attack=combat.AttackEffects.TryGetValue(id,out var contact)&&!contact.Expired||(flags&4u)!=0;
                // B0A0 clears15C/160/2AC: a still-set40 flag resumes as HP-kind0,
                // delta0/counter0. It does not manufacture another HP/MP result.
                if(preserves&&numeric)combat.NumberEffects[id]=new BattleNumberEffect{unit=id,life=after.life,kind=BattleFeedbackKind.Health,hp=after.hp,maxHp=after.maxHp};
                else combat.NumberEffects.Remove(id);
                //158/280/284/288/294 are cleared. The surviving4 flag draws only
                // its marker, with no old partner offset or extra hit graphic.
                //148 is retained: use the unit's current facing, not the old hit.
                if(preserves&&attack)combat.AttackEffects[id]=new BattleAttackEffect{unit=id,life=after.life,partner=0,partnerLife=units.First(u=>u.id==0).life,
                    direction=after.direction,attacker=true};
                else combat.AttackEffects.Remove(id);
                combat.AttackFlags.Remove(id);
                //164 restarts an explicit surviving80 corpse; disabled/new lives
                // were reset to30 by the snapshot decoder and lose the old corpse.
                if(preserves&&(after.nativeState.GetValueOrDefault()&0x80u)!=0)
                    combat.Deaths[id]=new BattleDeathEffect{unit=id,life=after.life,job=OriginalBattleLifecycle.AppearanceKey(after),held=preserves&&numeric};
                else combat.Deaths.Remove(id);
            }
        }
        private static void SyncNativeUnitState(TrainingUnit unit,TrainingUnit previous,TrainingUnitsNotice notice)
        {
            var info=unit.nativeInfo;if(info==null||previous==null)return;var f=info.fields;
            if(unit.hp!=previous.hp||notice.feedbackEvents?.Any(e=>e.unit==unit.id&&e.kind==BattleFeedbackKind.Health)==true)f[8]=unit.hp;
            if(unit.maxHp!=previous.maxHp)f[7]=unit.maxHp;
            if(unit.mp!=previous.mp||notice.feedbackEvents?.Any(e=>e.unit==unit.id&&e.kind==BattleFeedbackKind.Mana)==true)f[10]=unit.mp;
            if(unit.maxMp!=previous.maxMp)f[9]=unit.maxMp;
            if(unit.effectiveStats!=null&&(previous.effectiveStats==null||unit.effectiveStats.attack!=previous.effectiveStats.attack||unit.effectiveStats.defense!=previous.effectiveStats.defense))
            {f[11]=unit.effectiveStats.attack;f[12]=unit.effectiveStats.defense;}
        }
    }
}
