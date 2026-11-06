using System;
using System.Collections.Generic;

namespace DarkSaver.Original.World
{
    public sealed class NativeUnitPresentation
    {
        public int unit,appearance,direction,textureWidth,textureHeight,attribute;
        public int? movementType;
        public uint? state;
        public NativeViewport? viewport;
        public long life;
        public double tileX,tileY,renderX,renderY,clock;
        public bool moving,active=true,corpseAvailable=true;
        public BattleAttackEffect attack;
        public BattleNumberEffect number;
        public BattleDeathEffect death;
    }
    // UserUnit1000FCD0: live body -> moving attack marker -> tile-anchored
    // contact/extra -> numbers, or corpse+spark when80 without numeric40.
    public static class NativeUnitFrames
    {
        private static bool Attack(NativeUnitPresentation p)=>p.attack!=null&&p.attack.unit==p.unit&&p.attack.life==p.life&&!p.attack.Expired;
        private static bool Number(NativeUnitPresentation p)=>p.number!=null&&p.number.unit==p.unit&&p.number.life==p.life&&!p.number.Expired;
        public static bool Visible(NativeUnitPresentation p)=>p.active&&(!p.viewport.HasValue||p.viewport.Value.Contains((int)(p.tileX/32),(int)(p.tileY/32)));
        public static uint State(NativeUnitPresentation p)
        {
            // Snapshot nativeState is not the live display slot. Explicit display
            // events own4/40 until their counters expire; do not mutate/save the unit.
            uint state=(p.state??0)&~0x44u;
            if(Attack(p))state|=4;if(Number(p))state|=0x40;
            return state;
        }
        public static bool Corpse(NativeUnitPresentation p)=>(State(p)&0xc0)==0x80;
        public static BattleFeedbackFrame[] Numbers(NativeUnitPresentation p)
            =>Visible(p)&&Number(p)?BattleFeedbackFrames.Numbers(p.number,p.tileX,p.tileY):Array.Empty<BattleFeedbackFrame>();
        public static BattleFeedbackFrame[] Compose(NativeUnitPresentation p,int part=0)
        {
            if(!Visible(p))return Array.Empty<BattleFeedbackFrame>();
            if(Corpse(p))return p.corpseAvailable&&p.death!=null&&p.death.unit==p.unit&&p.death.life==p.life
                ?p.death.Frames(p.tileX,p.tileY):Array.Empty<BattleFeedbackFrame>();
            int row=p.viewport.HasValue?p.viewport.Value.RelativeRow((int)(p.tileY/32)):-1;
            var frames=new List<BattleFeedbackFrame>(NativeBodyFrames.Compose(p.textureWidth,p.textureHeight,p.direction,p.clock,p.moving,p.renderX,p.renderY,p.appearance,p.attribute,p.movementType,State(p),part,row));
            if(Attack(p))
            {
                // BD00 returns the movement offset after restoring part offsets.
                // Key999 returns before calculating it, so its marker stays on the tile.
                double dx=p.moving&&p.appearance!=999?p.renderX-p.tileX:0,dy=p.moving&&p.appearance!=999?p.renderY-p.tileY:0;
                frames.AddRange(BattleFeedbackFrames.Attack(p.attack,p.tileX,p.tileY,dx,dy));
            }
            frames.AddRange(Numbers(p));return frames.ToArray();
        }
    }
}
