using System.Linq;
using DarkSaver.Original.Entry;
using UnityEngine;
using UnityEngine.UI;

namespace DarkSaver.Original.World
{
    public sealed partial class WorldSceneRoot
    {
        private RectTransform trainingStatus;
        private bool trainingStatusVisible;
        private RawImage trainingBody, trainingHp, trainingMp;
        private RawImage[] trainingHpDigits, trainingMpDigits;
        private void ToggleTrainingStatus()
        {
            if(!BattleMap())return;
            trainingStatusVisible=!trainingStatusVisible;
            if(trainingStatus==null)
            {
                var spec=art.battle.Definition.windows.First(w=>w.key==2);
                trainingStatus=Group(worldWindows,"Original training status",new Rect(spec.x,spec.y,spec.width,spec.height));
                var blocker=trainingStatus.gameObject.AddComponent<Image>();blocker.color=Color.clear;
                int index=System.Array.FindIndex(art.battle.Definition.windows,w=>w.key==2);
                Picture(trainingStatus,"Native battle F2",new Rect(0,0,spec.width,spec.height),art.battle.windows[index]);
                trainingBody=Picture(trainingStatus,"Front-facing32px body",new Rect(44,79,32,32),art.Picture(character.job),new Rect(32,32,32,32));
                Label(trainingStatus,"Name",new Rect(9,7,110,17),character.name);
                Label(trainingStatus,"Class",new Rect(12,26,110,17),definition.Map(1000).map.abilities.First(a=>a.job==character.job).name);
                trainingHp=Picture(trainingStatus,"Native HP strip",new Rect(30,156,80,9),art.battle.board,new Rect(2,382,80,9));
                trainingMp=Picture(trainingStatus,"Native MP strip",new Rect(30,169,80,9),art.battle.board,new Rect(2,394,80,9));
                trainingHpDigits=TrainingDigits("HP",34,157);trainingMpDigits=TrainingDigits("MP",34,170);
                foreach(var b in spec.buttons)
                {var hit=Hit(trainingStatus,"Close/minimize",new Rect(b.x,b.y,b.width,b.height),()=>{trainingStatusVisible=false;PaintTrainingStatus();});var image=Picture((RectTransform)hit.transform,"Native control",new Rect(0,0,b.width,b.height),art.battle.buttons);hit.gameObject.AddComponent<NativeEntryButton>().Configure(image,art.battle.buttons,new Rect(b.sx,b.sy,b.width,b.height),new Rect(b.sx+b.width,b.sy,b.width,b.height),new Rect(b.sx+2*b.width,b.sy,b.width,b.height));}
            }
            PaintTrainingStatus();
        }
        private RawImage[] TrainingDigits(string name,int x,int y)
        {
            var result=new RawImage[15];for(int i=0;i<result.Length;i++)result[i]=Picture(trainingStatus,name+i,new Rect(x+i*5,y,5,7),art.battle.board);return result;
        }
        private void TrainingNumber(RawImage[] glyphs,string text)
        {
            for(int i=0;i<glyphs.Length;i++)
            {glyphs[i].gameObject.SetActive(i<text.Length);if(i>=text.Length)continue;int x=text[i]=='/'?375:380+(text[i]-'0')*5;glyphs[i].uvRect=NativeEntryLayout.Uv(art.battle.board,new Rect(x,342,5,7));}
        }
        private void PaintTrainingStatus()
        {
            if(trainingStatus==null)return;bool visible=BattleMap()&&trainingStatusVisible;
            trainingStatus.gameObject.SetActive(visible);if(!visible)return;
            trainingStatus.SetAsLastSibling();trainingBody.texture=art.Picture(character.job);trainingBody.uvRect=NativeEntryLayout.Uv(trainingBody.texture,new Rect(32,32,32,32));
            var r=view.record;trainingHp.gameObject.SetActive(r.healthKnown);trainingMp.gameObject.SetActive(r.healthKnown);
            float hp=r.maxHp>0?Mathf.Clamp01((float)r.hp/r.maxHp):0,mp=r.maxMp>0?Mathf.Clamp01((float)r.mp/r.maxMp):0;
            Place((RectTransform)trainingHp.transform,new Rect(30,156,80*hp,9));trainingHp.uvRect=NativeEntryLayout.Uv(art.battle.board,new Rect(2,382,80*hp,9));
            Place((RectTransform)trainingMp.transform,new Rect(30,169,80*mp,9));trainingMp.uvRect=NativeEntryLayout.Uv(art.battle.board,new Rect(2,394,80*mp,9));
            TrainingNumber(trainingHpDigits,r.healthKnown?r.hp+"/"+r.maxHp:"");TrainingNumber(trainingMpDigits,r.healthKnown?r.mp+"/"+r.maxMp:"");
        }
    }
}
