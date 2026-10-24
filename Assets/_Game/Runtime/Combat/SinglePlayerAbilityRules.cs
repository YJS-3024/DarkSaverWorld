using System;
using DarkSaver.Magic;
namespace DarkSaver.Combat
{
    public enum AbilityEffect {Damage,Heal,HpDrain,MpDrain,RestoreMp,Revive,AttackBuff,DefenseBuff,Quick,Poison,CurePoison,MovementReduction,Silence,Sleep,Wake,AttackDown,MagicDefenseDown,MagicDefenseUp,MagicAttackUp,Slow,Regeneration,PoisonWeapon,Accuracy,Berserk,SleepWard,TurnUndead,Hiring,Emote}
    // Native ID/MP/geometry/presentation are preserved. Magnitudes, duration and
    // AP conversion below are explicit single-player policy, not recovered server formulas.
    public sealed class SinglePlayerAbilityRule
    {
        public int Id,Mp,Range,Power,Ap;public float Seconds;public AbilityEffect Effect;public bool Physical;
        public bool Combat=>Effect!=AbilityEffect.Hiring && Effect!=AbilityEffect.Emote;
        public bool Support=>Effect==AbilityEffect.Heal || Effect==AbilityEffect.RestoreMp || Effect==AbilityEffect.Revive || Effect==AbilityEffect.AttackBuff || Effect==AbilityEffect.DefenseBuff || Effect==AbilityEffect.Quick || Effect==AbilityEffect.CurePoison || Effect==AbilityEffect.Wake || Effect==AbilityEffect.MagicDefenseUp || Effect==AbilityEffect.MagicAttackUp || Effect==AbilityEffect.Regeneration || Effect==AbilityEffect.PoisonWeapon || Effect==AbilityEffect.Accuracy || Effect==AbilityEffect.Berserk || Effect==AbilityEffect.SleepWard;
    }
    public static class SinglePlayerAbilityRules
    {
        public static bool TryCreate(MagicCatalogEntry entry,out SinglePlayerAbilityRule rule)
        {
            rule=null;if(entry==null || entry.Id<=0 || entry.Id>short.MaxValue || string.IsNullOrWhiteSpace(entry.Name))return false;
            string name=entry.Name.Trim();int bracket=name.IndexOf('(');if(bracket>=0)name=name.Substring(0,bracket);
            AbilityEffect effect;
            if(entry.Id>=20000 && entry.Id<22000 && name.EndsWith("고용",StringComparison.Ordinal))effect=AbilityEffect.Hiring;
            else if(entry.Id>=29001 && entry.Id<=29015)effect=AbilityEffect.Emote;
            else switch(name)
            {
                case "힐":case "포스힐":case "힐포션&싸구려약초":case "흔한약초":case "귀한약초":case "희귀한약초":case "닥세콜라":case "홀리워터":effect=AbilityEffect.Heal;break;
                case "HP드레인":case "뱀파이어":case "블러드_크루세이더":effect=AbilityEffect.HpDrain;break;
                case "MP드레인":effect=AbilityEffect.MpDrain;break;
                case "매직포션":effect=AbilityEffect.RestoreMp;break;
                case "리절렉션":effect=AbilityEffect.Revive;break;
                case "디어택":effect=AbilityEffect.AttackDown;break;
                case "앤티레지스트":case "포스앤티져":effect=AbilityEffect.MagicDefenseDown;break;
                case "레지스트":effect=AbilityEffect.MagicDefenseUp;break;
                case "절대마력":effect=AbilityEffect.MagicAttackUp;break;
                case "프로텍션":effect=AbilityEffect.DefenseBuff;break;
                case "어택":effect=AbilityEffect.AttackBuff;break;
                case "퀵":case "래피드":effect=AbilityEffect.Quick;break;
                case "포이즌":case "포이즌2":case "클라우드킬":effect=AbilityEffect.Poison;break;
                case "큐어":case "해독제":case "초급해독제_묶음":case "중급해독제_묶음":case "고급해독제_묶음":effect=AbilityEffect.CurePoison;break;
                case "디무브":effect=AbilityEffect.MovementReduction;break;
                case "뮤트":effect=AbilityEffect.Silence;break;
                case "슬립":effect=AbilityEffect.Sleep;break;
                case "각성제":effect=AbilityEffect.Wake;break;
                case "불침":effect=AbilityEffect.SleepWard;break;
                case "슬로우":effect=AbilityEffect.Slow;break;
                case "리제네":effect=AbilityEffect.Regeneration;break;
                case "포이즈너스":effect=AbilityEffect.PoisonWeapon;break;
                case "카리스마":effect=AbilityEffect.Accuracy;break;
                case "버서커":effect=AbilityEffect.Berserk;break;
                case "턴언데드":effect=AbilityEffect.TurnUndead;break;
                case "파이어볼":case "파이어":case "프리즈":case "블리자드":case "썬더":case "썬더스톰":case "윈드커터":case "토네이도":case "어스퀘이크":case "메테오":case "블래스트":case "애로우":case "수리검":case "불수리검":case "용암":case "1화_썬더스톰":case "레벨업웨이브":case "레드블러셤":case "블레이드_러너":case "스켈러쉬":case "윈드프리즈":case "트러스트":case "크로스트러스트":case "더블암트러스트":case "애토믹웨이브":case "플라즈마캐논":case "체크스매쉬":case "샐러맨더":case "마이크로샷":case "우뢰":case "네오우뢰":case "소닉붐":case "쇼크웨이브":case "아쿠아크래쉬":effect=AbilityEffect.Damage;break;
                default:return false;
            }
            if(entry.Mp<0 || entry.Mp>int.MaxValue || entry.Range< -6 || entry.Range>255 || entry.Area<0 || entry.Area>255 || entry.RawTick<0 || entry.RawTick>int.MaxValue || entry.GetNumericValue(9)==long.MinValue)return false;
            int power=(int)Math.Min(10000,Math.Max(1,Math.Abs(entry.GetNumericValue(9))));
            int range=entry.Range<0?255:entry.Range>0?(int)Math.Min(255,entry.Range+entry.Area*2):(int)Math.Min(255,Math.Max(1,entry.Area*3));
            rule=new SinglePlayerAbilityRule {Id=entry.Id,Mp=(int)entry.Mp,Range=range,Power=power,Effect=effect,Seconds=(float)Math.Min(90,Math.Max(5,entry.GetNumericValue(10))),Ap=(int)Math.Min(10,Math.Max(1,(entry.RawTick+4)/5)),Physical=entry.Id>=10000 && entry.Id<20000 && entry.Attribute==8};return true;
        }
        public static bool IsUndead(CombatUnit unit)
        {
            string n=unit?.Name ?? "";return n.Contains("스켈") || n.Contains("좀비") || n.Contains("구울") || n.Contains("고스트") || n.Contains("유령") || n.Contains("언데드") || n.Contains("리치");
        }
    }
}
