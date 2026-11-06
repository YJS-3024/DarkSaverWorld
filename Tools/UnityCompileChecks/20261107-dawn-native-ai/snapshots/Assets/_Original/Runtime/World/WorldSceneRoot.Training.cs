using System;
using System.Linq;
using DarkSaver.Original.UI;
using UnityEngine;
using UnityEngine.UI;
namespace DarkSaver.Original.World
{
    public sealed partial class WorldSceneRoot
    {
        private void ToggleTrainingStatus()
        {
            if(!BattleMap()||CampaignCutscene||notice!=null)return;
            if(battleDialogs.ContainsKey(1))CloseBattleDialog(1);else{OpenBattleDialog(1);CloseBattleDialog(9);}
        }
        private void PaintTrainingStatus()=>PaintBattleDialogs();
        private BattleDialogUnit BuildBattleDialogUnit(BattleDialog p,int id,bool full,bool compact,int row)
        {
            var u=new BattleDialogUnit{Id=id,FollowsSelection=full||compact,Troop=!full&&!compact,Width=full?80:48,Root=Group(p.Full,"Native unit fields"+id,new Rect(0,0,p.Spec.width,p.Spec.height))};
            int bodyX=full?44:compact?7:8,bodyY=full?79:compact?9:row+2;
            u.Body=Picture(u.Root,"Native front unit"+id,new Rect(bodyX,bodyY,32,32),null);
            int gaugeX=full?30:compact?67:65,gaugeY=full?143:compact?8:row,width=full?80:48;
            u.ApY=gaugeY;u.Ap=Picture(u.Root,"Native AP"+id,new Rect(gaugeX,gaugeY,width,9),art.battle.board);
            u.Hp=Picture(u.Root,"Native HP"+id,new Rect(gaugeX,gaugeY+NativeBattleStatusFields.HpOffset(u.Troop),width,9),art.battle.board);
            u.Mp=Picture(u.Root,"Native MP"+id,new Rect(gaugeX,gaugeY+NativeBattleStatusFields.MpOffset(u.Troop),width,9),art.battle.board);
            u.HpDigits=BattleDigits(u.Root,"HP"+id,gaugeX+4,gaugeY+NativeBattleStatusFields.HpOffset(u.Troop)+(compact?0:1),21);
            u.MpDigits=BattleDigits(u.Root,"MP"+id,gaugeX+4,gaugeY+NativeBattleStatusFields.MpOffset(u.Troop)+(compact?0:1),21);
            if(full)
            {
                u.Name=BattleText(u.Root,"Native unit name",new Rect(9,7,67,16));
                u.Class=BattleText(u.Root,"Native class",new Rect(12,26,96,16));
                u.Level=BattleText(u.Root,"Native level",new Rect(12,41,96,16));
                u.Experience=Picture(u.Root,"Native XP",new Rect(30,182,80,9),art.battle.board);
                u.ExperienceDigits=BattleDigits(u.Root,"XP",34,183,21);
                u.AttackDigits=BattleDigits(u.Root,"Attack",32,197,11);u.DefenseDigits=BattleDigits(u.Root,"Defense",87,197,11);
                u.Conditions=Enumerable.Range(0,NativeBattleStatusFields.ConditionCount).Select(i=>Picture(u.Root,"Native received condition"+i,new Rect(23,59,16,16),art.battle.board,new Rect(NativeBattleStatusFields.ConditionSourceX(i),520,16,16))).ToArray();
            }
            return u;
        }
        private RawImage[] BattleDigits(RectTransform root,string name,int x,int y,int capacity)
        {
            var group=Group(root,name,new Rect(x,y,capacity*5,7));group.gameObject.AddComponent<RectMask2D>();
            return Enumerable.Range(0,capacity).Select(i=>Picture(group,"Native glyph"+i,new Rect(i*5,0,5,7),art.battle.board)).ToArray();
        }
        private void BattleNumber(RawImage[] glyphs,string text)
        {
            if(glyphs==null)return;text=text??"";
            for(int i=0;i<glyphs.Length;i++)
            {
                glyphs[i].gameObject.SetActive(i<text.Length);if(i>=text.Length)continue;
                glyphs[i].uvRect=Entry.NativeEntryLayout.Uv(art.battle.board,new Rect(NativeBattleStatusFields.GlyphX(text[i]),342,5,7));
            }
        }
        private void BattleGauge(RawImage image,int value,int maximum,int sourceY,int width)
        {
            if(image==null)return;
            int filled=NativeBattleStatusFields.SignedWidth(value,maximum,width);PaintBattleGaugeWidth(image,filled,sourceY);
        }
        private void PaintBattleGaugeWidth(RawImage image,int filled,int sourceY,int height=9)
        {
            if(image==null)return;image.gameObject.SetActive(filled>0);
            ((RectTransform)image.transform).sizeDelta=new Vector2(filled,height);
            image.uvRect=Entry.NativeEntryLayout.Uv(art.battle.board,new Rect(2,sourceY,filled,height));
        }
        private void PaintBattleDialogUnit(BattleDialogUnit display,int phase)
        {
            int id=display.FollowsSelection?NativeBattleStatusSelection.Display(battleStatusUnit,0):display.Id;
            var unit=view.battle?.Unit(id);bool own=id==0;
            var growth=own?view.record.growth:null;var stats=unit?.effectiveStats??(own?growth?.stats:null);var received=unit?.nativeInfo;
            var texture=unit==null?(own?CommanderPicture:null):CombatBodyPicture(unit);display.Body.texture=texture;display.Body.enabled=texture!=null;
            int appearance=unit==null?CurrentCommanderJob:OriginalBattleLifecycle.AppearanceKey(unit);int cell=appearance/100==7?64:32;
            var bodyRect=(RectTransform)display.Body.transform;bodyRect.sizeDelta=new Vector2(display.Width==80&&appearance/100==7?31:32,32);if(texture!=null)display.Body.uvRect=Entry.NativeEntryLayout.Uv(texture,new Rect(cell,cell,cell,cell));
            bool fallen=display.Troop&&unit!=null&&unit.hp==0;
            if(fallen){EnsureBattleWindowArt();display.Body.texture=battleWindowArt?.markers;display.Body.enabled=display.Body.texture!=null;if(display.Body.texture!=null)display.Body.uvRect=Entry.NativeEntryLayout.Uv(display.Body.texture,new Rect(224,0,32,32));}
            if(display.Name!=null)display.Name.text=received!=null?NativeUnitText(OriginalBattleTeamNames.DisplayName(received,view.battleTeams)):own?CommanderName:"";
            if(display.Class!=null)display.Class.text=received!=null?NativeUnitText(received.className):own?CommanderClass:definition.combatCatalog?.Job(unit?.job??-1)?.name??definition.growthCatalog?.Job(unit?.job??-1)?.name??"";
            int? level=received?.Level??(own?view.record.level:null);
            if(display.Level!=null)display.Level.text=level.HasValue&&level!=0?"[ Lv. "+level.Value+" ]":"";
            bool health=unit!=null||own&&view.record.healthKnown;int hp=unit?.hp??view.record.hp,maxHp=unit?.maxHp??view.record.maxHp,mp=unit?.mp??view.record.mp,maxMp=unit?.maxMp??view.record.maxMp;
            if(received!=null){hp=received.Hp;maxHp=received.MaxHp;mp=received.Mp;maxMp=received.MaxMp;}
            bool ready=unit!=null&&unit.maxAp!=0&&unit.ap>=unit.maxAp;
            var apRect=(RectTransform)display.Ap.transform;apRect.anchoredPosition=new Vector2(apRect.anchoredPosition.x,-display.ApY-NativeBattleStatusFields.ApOffset(display.Troop,ready));
            PaintBattleGaugeWidth(display.Ap,NativeBattleStatusFields.SignedWidth(fallen?0:unit?.ap??0,unit?.maxAp??0,display.Width),370+(ready?phase*12:0),NativeBattleStatusFields.ApHeight(display.Width==80,ready));
            BattleGauge(display.Hp,health&&!fallen?hp:0,health?maxHp:0,382,display.Width);BattleGauge(display.Mp,health&&!fallen?mp:0,health?maxMp:0,394,display.Width);
            BattleNumber(display.HpDigits,health&&!fallen?hp+"/"+maxHp:"");BattleNumber(display.MpDigits,health&&!fallen?mp+"/"+maxMp:"");
            BattleNumber(display.AttackDigits,stats?.attack.ToString(System.Globalization.CultureInfo.InvariantCulture));BattleNumber(display.DefenseDigits,stats?.defense.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if(display.Conditions!=null)
            {
                int slot=0;for(int i=0;i<display.Conditions.Length;i++)
                {
                    bool active=unit?.nativeStatusFlags.HasValue==true&&(unit.nativeStatusFlags.Value&NativeBattleStatusFields.ConditionMask(i))!=0;display.Conditions[i].gameObject.SetActive(active);
                    if(active){Place((RectTransform)display.Conditions[i].transform,new Rect(NativeBattleStatusFields.ConditionX(slot),NativeBattleStatusFields.ConditionY(slot),16,16));slot++;}
                }
            }
            uint? next=received!=null?(uint?)received.NextExperience:growth?.nextExperience;ulong value=received!=null?received.Experience:growth?.experience??0;bool xp=next>0;
            PaintBattleGaugeWidth(display.Experience,xp?NativeBattleStatusFields.ExperienceWidth(value,next.Value,display.Width):0,406);
            BattleNumber(display.ExperienceDigits,xp?NativeBattleStatusFields.Experience(value,next.Value):"");
        }
        private string NativeUnitText(byte[] bytes)
        {if(bytes==null||worldTextCodec==null)return"";return worldTextCodec.TryCString(bytes,0,bytes.Length,out string text)?text:"";}
    }
}
