using System.Linq;
using UnityEngine;

namespace DarkSaver.Original.World
{
    public sealed partial class WorldSceneRoot
    {
        private int trainingActionUnit,previousRangeSource=-1;
        private void MaintainTrainingSelection()
        {
            if(trainingActionUnit==0)
            {
                if(view.battle?.Commander!=null&&!OriginalCommandRange.Living(view.battle.Commander)){showingActions=false;trainingMode=null;CancelBattleMagicSelection();}
                return;
            }
            if(!OriginalMemberCommands.Controlled(view.battle?.units,trainingActionUnit)||view.battle.motions?.Any(m=>m.unit==trainingActionUnit)==true)
            {trainingActionUnit=0;showingActions=false;trainingMode=null;}
        }
        private bool MemberActionAvailable(int icon)
        {
            var battle=view?.battle;var unit=battle?.Unit(trainingActionUnit);
            if(!OriginalBattleTicks.AllowsIcon(unit,icon))return false;
            if(battle==null||!OriginalMemberCommands.Controlled(battle.units,trainingActionUnit)||view.moving||battle.pending!=null
                ||battle.motions?.Any(m=>m.unit==trainingActionUnit)==true||!server.HasBattleAuthorityFor(DisplayMap(view.record.map).training,icon==0?TrainingAction.Move:icon==1?TrainingAction.Attack:TrainingAction.Rest))return false;
            if(icon==0)return unit.canMove&&unit.movementBudget.HasValue&&OriginalMemberCommands.MovementType(unit,definition.combatCatalog).HasValue;
            if(icon==1)return unit.canAttack&&unit.attackRange.HasValue&&unit.attackTargets!=null;
            return icon==8&&unit.canRest;
        }
        private void OpenTrainingActions(int unit)
        {
            AcknowledgeDirectedPointer();
            if(view.facility!=null||view.moving||view.battle?.pending!=null||notice!=null)return;
            if(unit!=0&&!OriginalMemberCommands.Controlled(view.battle?.units,unit))return;
            bool wasOpen=showingActions&&trainingActionUnit==unit;
            trainingMode=null;CancelBattleMagicSelection();trainingActionUnit=unit;showingActions=!wasOpen;Paint();
        }
        private void PositionTrainingActions(float cx,float cy)
        {
            double x=view.renderX,y=view.renderY;
            if(trainingActionUnit!=0)
            {
                var unit=view.battle?.Unit(trainingActionUnit);var motion=view.battle?.motions?.FirstOrDefault(m=>m.unit==trainingActionUnit);
                if(unit==null){showingActions=false;actions.gameObject.SetActive(false);return;}x=motion?.x??unit.x;y=motion?.y??unit.y;
            }
            Place(actions,new Rect((float)x*32-cx-32,(float)y*32-cy-32,96,96));
        }
        private void MemberMenuAction(int icon)
        {
            if(!MemberActionAvailable(icon))return;
            showingActions=false;CancelBattleMagicSelection();trainingMode=null;
            if(icon==0)trainingMode=TrainingAction.Move;
            else if(icon==1)trainingMode=TrainingAction.Attack;
            else if(icon==8)Handle(DisplayMap(view.record.map).training?server.Request_TrainingMemberRest(trainingActionUnit):server.Request_BattleMemberRest(trainingActionUnit));
            Paint();
        }
    }
}
