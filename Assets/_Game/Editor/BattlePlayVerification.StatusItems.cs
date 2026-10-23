using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DarkSaver.Combat;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DarkSaver.EditorTools
{
    public static partial class BattlePlayVerification
    {
        // Only initial poison/AP setup is injected. Item selection, map targeting,
        // inventory consumption and presentation use the live scene's normal path.
        static bool TickStatusItems(int step, BattleSceneRoot b, BattleMapPresentation view, BattleTransitionController session)
        {
            if(step < 15 || step > 27)return false;
            void Next(int value) => SessionState.SetInt(Key+"classToolStep",value);
            bool Ready() => b.TryGetUnit(100,out var actor) && actor.Ap==actor.MaxAp;
            void Tools() => RequireMercenary(view.TryOpenActionMenu() && view.TryChooseCommand(BattleMapCommand.Tools),"Status tools unavailable");
            void Target(short id)
            {
                RequireMercenary(b.TryGetUnit(id,out var target),"Status target unavailable");
                var ground=UnityEngine.Object.FindObjectsByType<Tilemap>().FirstOrDefault(x=>x.name=="Ground" && x.gameObject.scene.name=="Map_102_;");
                RequireMercenary(ground!=null,"Status target ground unavailable");
                RequireMercenary(view.TryHandleWorldClick(ground.GetCellCenterWorld(new Vector3Int(target.X,ground.cellBounds.yMax-1-target.Y))),"Status map click rejected: "+view.LastInputMessage);
            }
            if(step==15)
            {
                if(!Ready())return true;
                var flow=(CombatFlow)typeof(BattleSceneRoot).GetField("flow",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(b);
                var world=(CombatWorld)typeof(CombatFlow).GetField("world",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(flow);
                RequireMercenary(flow.ConfigureTemporaryStatusMagic(1601,0,5,TemporaryStatusEffect.Poison,8,20),"Poison fixture rule rejected");
                var enemy=world.Get(b.PrimaryEnemyId);enemy.ActionPoints.ApplyAbsolute(enemy.ActionPoints.Maximum);
                RequireMercenary(flow.TryExecute(new CombatRequest(enemy.Id,100,CombatActionType.Magic,1601),out _,out _),"Explicit ally poison fixture rejected");
                Tools();Next(16);return true;
            }
            if(step==16){ClickBattleMenu("tool-row-2");Next(17);return true;}
            if(step==17)
            {
                int hp=b.PlayerHp,mp=b.PlayerMp;
                RequireMercenary(view.IsToolTargeting && b.TryGetTemporaryStatus(100,TemporaryStatusEffect.Poison,out _,out _),"Cure selection or poison fixture missing");
                Target(100);
                RequireMercenary(!b.TryGetTemporaryStatus(100,TemporaryStatusEffect.Poison,out _,out _)
                    && session.OwnedItemCount(307)==0 && b.PlayerHp==hp && b.PlayerMp==mp,"Cure item state/quantity mismatch");
                Next(18);return true;
            }
            if(step==18){ScreenCapture.CaptureScreenshot(Path.Combine(output,"battle-real-cure-item.png"));Next(19);return true;}
            if(step==19)
            {
                if(!Ready())return true;
                RequireMercenary(b.ConfigureMovementBudget(b.PrimaryEnemyId,4),"Explicit movement budget fixture rejected");
                Tools();Next(20);return true;
            }
            if(step==20){ClickBattleMenu("tool-row-3");Next(21);return true;}
            if(step==21)
            {
                int mp=b.PlayerMp;Target(b.PrimaryEnemyId);
                RequireMercenary(b.TryGetTemporaryStatus(b.PrimaryEnemyId,TemporaryStatusEffect.MovementReduction,out int power,out _) && power==25
                    && session.OwnedItemCount(315)==0 && b.PlayerMp==mp,"Movement item state/quantity mismatch");
                Next(22);return true;
            }
            if(step==22){ScreenCapture.CaptureScreenshot(Path.Combine(output,"battle-real-demove-item.png"));Next(23);return true;}
            if(step==23){if(!Ready())return true;Tools();Next(24);return true;}
            if(step==24){ClickBattleMenu("tool-row-2");Next(25);return true;}
            if(step==25)
            {
                int mp=b.PlayerMp;Target(b.PrimaryEnemyId);
                RequireMercenary(b.TryGetTemporaryStatus(b.PrimaryEnemyId,TemporaryStatusEffect.Poison,out _,out _)
                    && session.OwnedItemCount(313)==0 && b.PlayerMp==mp,"Poison item state/quantity mismatch");
                SessionState.SetInt(Key+"statusEnemyHp",b.EnemyHp);
                SessionState.SetFloat(Key+"statusTickAt",Time.unscaledTime+2.1f);Next(26);return true;
            }
            if(step==26)
            {
                if(Time.unscaledTime<SessionState.GetFloat(Key+"statusTickAt",0))return true;
                RequireMercenary(b.EnemyHp<SessionState.GetInt(Key+"statusEnemyHp",0),"Live poison did not tick");
                ScreenCapture.CaptureScreenshot(Path.Combine(output,"battle-real-poison-item.png"));Next(27);return true;
            }
            Note("PASS: pointer cure307/self target removed explicit poison fixture; pointer demove315/enemy target applied movement reduction; pointer poison313/enemy target produced live periodic damage; quantities consumed once, player MP unchanged");
            Next(28);return true;
        }
    }
}
