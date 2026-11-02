using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using DarkSaver.Original.World;
internal static class NativeUnitFramesDifferential
{
    private static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
    public sealed class Case
    {
        public string mode;
        public int width,height,left,top,columns,rows,size,direction,nativeDirection,phase,appearance,attribute,kind,part,attackCounter,numberCounter,deathCounter;
        public uint state;
        public bool moving,attack,number,bounded,active=true,corpseAvailable=true;
        public double clock;
        public int[] attributes;
        public TrainingUnit[] units;
    }
    public static void Main(string[] args)
    {
        var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[0]),Options);
        var output=cases.Select(c=>
        {
            if(c.mode=="plan")return (object)NativeBodyOrder.Plan(c.width,c.height,(x,y)=>x>=0&&y>=0&&x<c.width&&y<c.height?c.attributes[y*c.width+x]:-1,
                c.units.Where(u=>u.nativeActive!=false&&u.nativeEnabled!=false).Select(u=>new NativeBodyOccupant{id=u.id,x=u.x,y=u.y,appearance=u.job,movementType=u.movementType}),c.left,c.top,c.columns,c.rows,includePostPasses:true);
            var actor=c.units[0];double x=actor.x*32,y=actor.y*32;
            if(c.moving){int[] dx={0,-1,1,0,0,-1,-1,1,1},dy={0,0,0,-1,1,-1,1,-1,1};int step=c.nativeDirection>=5?10:16;x+=dx[c.nativeDirection]*c.phase*step;y+=dy[c.nativeDirection]*c.phase*step;}
            var pose=new NativeUnitPresentation{unit=actor.id,life=actor.life,active=c.active,corpseAvailable=c.corpseAvailable,
                tileX=actor.x*32,tileY=actor.y*32,renderX=x,renderY=y,clock=c.clock,textureWidth=c.size*3,textureHeight=c.size*4,
                direction=c.attack&&!c.moving?2:c.direction,moving=c.moving,appearance=c.appearance,attribute=c.attribute,movementType=c.kind,state=c.state};
            if(c.bounded)pose.viewport=new NativeViewport(c.left,c.top,c.columns,c.rows);
            if(c.attack){pose.attack=BattleAttackEffect.Create(actor,c.units[1],-1)[0];pose.attack.age=c.attackCounter*40;}
            if(c.number)pose.number=new BattleNumberEffect{unit=actor.id,life=actor.life,kind=BattleFeedbackKind.Health,hp=75,maxHp=actor.maxHp,delta=-25,age=c.numberCounter*40};
            if((c.state&128)!=0)pose.death=new BattleDeathEffect{unit=actor.id,life=actor.life,job=c.appearance,age=c.deathCounter*40};
            return NativeUnitFrames.Compose(pose,c.part).Select(f=>new{surface=(int)f.surface,x=(int)f.x,y=(int)f.y,f.atlasX,f.atlasY,f.width,f.height}).ToArray();
        }).ToArray();
        // Cross-life effects never draw or hold a new corpse/body; preparing frames is pure.
        int assertions=0;void Check(bool b){assertions++;if(!b)throw new Exception("Native unit presentation contract "+assertions);}
        var p=new NativeUnitPresentation{unit=0,life=2,textureWidth=0,textureHeight=0,active=true};
        p.attack=new BattleAttackEffect{unit=0,life=1};p.number=new BattleNumberEffect{unit=0,life=1,kind=BattleFeedbackKind.Health,age=40,hp=100};
        Check(NativeUnitFrames.Compose(p).Length==0&&NativeUnitFrames.Numbers(p).Length==0&&NativeUnitFrames.State(p)==0);
        p.state=0x80;p.death=new BattleDeathEffect{unit=0,life=2,job=100,age=320};Check(NativeUnitFrames.Corpse(p)&&NativeUnitFrames.Compose(p).Length==2);
        p.death.life=1;Check(NativeUnitFrames.Compose(p).Length==0);p.death.life=2;p.corpseAvailable=false;Check(NativeUnitFrames.Compose(p).Length==0);
        p.number.life=2;Check(!NativeUnitFrames.Corpse(p)&&NativeUnitFrames.Compose(p).All(f=>f.surface==BattleFeedbackSurface.Board));
        p.active=false;Check(NativeUnitFrames.Compose(p).Length==0&&NativeUnitFrames.Numbers(p).Length==0);
        var whole=NativeViewport.FromPixels(32,64,640,480);Check(whole.left==1&&whole.top==2&&whole.columns==20&&whole.rows==15);
        var partial=NativeViewport.FromPixels(16,48,640,480);Check(partial.left==0&&partial.top==1&&partial.columns==21&&partial.rows==16);
        Check(partial.Contains(0,1)&&partial.Contains(20,16)&&!partial.Contains(21,16)&&!partial.Contains(20,17));
        var empty=NativeViewport.FromPixels(16,48,0,0);Check(!empty.Contains(0,1));
        p.active=true;p.viewport=new NativeViewport(7,11,20,15);p.tileX=6*32;p.tileY=11*32;p.renderX=7*32;p.renderY=11*32;
        Check(!NativeUnitFrames.Visible(p)&&NativeUnitFrames.Compose(p).Length==0&&NativeUnitFrames.Numbers(p).Length==0);
        p.tileX=7*32;p.tileY=20*32;Check(NativeUnitFrames.Visible(p)&&p.viewport.Value.RelativeRow((int)p.tileY/32)==9);
        File.WriteAllText(args[1],JsonSerializer.Serialize(new{output,assertions},Options));
    }
}
