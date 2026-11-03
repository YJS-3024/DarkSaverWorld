using System;
using System.Collections.Generic;
using System.Linq;
namespace DarkSaver.Original.World
{
    [Serializable] public sealed class NativeHireSkill
    {public int id,maximum;public int[] jobs;}
    [Serializable] public sealed class NativeHireEquipment
    {public int id,skill,boost;}
    [Serializable] public sealed class NativeHireUnit
    {public int job,picture,hp,attack,defense;public long price;public string name;}
    [Serializable] public sealed class NativeMercenaryChoice
    {public int job,maximum;}
    [Serializable] public sealed class NativeMercenaryCatalog
    {
        public NativeHireSkill[] skills;
        public NativeHireEquipment[] equipment;
        public NativeHireUnit[] units;
        //setunit.GetSoldierList10002850: first8 skills, first7 worn slots,
        //equipment family/tier adjustments, then first-appearance job union/max.
        public NativeMercenaryChoice[] Resolve(int[] worn,int[] learned)
        {
            var known=(skills??Array.Empty<NativeHireSkill>()).ToDictionary(s=>s.id);
            var items=(equipment??Array.Empty<NativeHireEquipment>()).ToDictionary(s=>s.id);
            var selected=new List<int>();
            foreach(int skill in (learned??Array.Empty<int>()).Take(8))
                if(skill>19999&&known.ContainsKey(skill)&&!selected.Contains(skill))selected.Add(skill);
            foreach(int id in (worn??Array.Empty<int>()).Take(7))
            {
                if(id<=0||!items.TryGetValue(id,out var item)||item.boost>=0||item.skill<=19999)continue;
                int index=selected.FindIndex(s=>s/100==item.skill/100);
                if(index<0)selected.Add(item.skill);else if(selected[index]%100<item.skill%100)selected[index]=item.skill;
            }
            foreach(int id in (worn??Array.Empty<int>()).Take(7))
            {
                if(!items.TryGetValue(id,out var item)||item.boost==0||item.skill<=19999)continue;
                int index=selected.FindIndex(s=>s/100==item.skill/100);if(index<0)continue;
                int boost=item.boost<0?unchecked(-item.boost):item.boost,value=unchecked(selected[index]+boost);
                for(int attempt=0;attempt<8&&!known.ContainsKey(value);attempt++)value=unchecked(value-1);
                selected[index]=value;
            }
            var result=new List<NativeMercenaryChoice>();
            foreach(int id in selected)
            {
                if(!known.TryGetValue(id,out var skill))continue;
                foreach(int job in skill.jobs??Array.Empty<int>())
                {var row=result.FirstOrDefault(r=>r.job==job);if(row==null)result.Add(new NativeMercenaryChoice{job=job,maximum=skill.maximum});else if(row.maximum<skill.maximum)row.maximum=skill.maximum;}
            }
            return result.ToArray();
        }
        public WorldHireOffer[] Offers(WorldRecord record)
        {
            if(record?.growth?.skills==null)return Array.Empty<WorldHireOffer>();
            var worn=Enumerable.Range(0,7).Select(slot=>record.Equipment.FirstOrDefault(item=>item.slot==slot)?.item??-1).ToArray();
            var result=new List<WorldHireOffer>();
            foreach(var row in Resolve(worn,record.growth.skills))
            {
                var unit=units?.FirstOrDefault(item=>item.job==row.job);if(unit==null)continue;
                result.Add(new WorldHireOffer{commander=record.growth.job,job=row.job,limit=row.maximum,picture=unit.picture,price=unit.price,name=unit.name});
            }
            return result.ToArray();
        }
    }
}
