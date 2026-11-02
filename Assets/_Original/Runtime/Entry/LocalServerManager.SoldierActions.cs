using System;
using UnityEngine;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        // Native code samples rand()%count. A selector can be injected for reproducible checks.
        public Func<int,int> SoldierTargetChoice { get; set; }
        public WorldReply Request_BattleSoldierAction(int member)=>BattleResult(s=>s.ExecuteBattleSoldierAction(member,AutomationAuthority(false),SoldierTargetChoice,Time.realtimeSinceStartupAsDouble*1000.0));
        public WorldReply Request_TrainingSoldierAction(int member)=>TrainingResult(s=>s.ExecuteTrainingSoldierAction(member,AutomationAuthority(true),SoldierTargetChoice,Time.realtimeSinceStartupAsDouble*1000.0));
    }
}
