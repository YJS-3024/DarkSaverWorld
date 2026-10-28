namespace DarkSaver.Combat
{
    public sealed partial class BattleSceneRoot
    {
        public int TrainingMapId { get; private set; }
        public bool IsPersistentTraining => OriginalTrainingMaps.Contains(TrainingMapId);

        // Local visit receives the existing character's vitals; no enemy is invented.
        public bool InitializeTrainingVisitor(int jobId,string name,int hp,int maxHp,int mp,int maxMp,uint experience)
        {
            EnsureInitialized();
            if(!IsPersistentTraining || GridConfigured || jobId<=0 || maxHp<=0 || hp<0 || hp>maxHp || maxMp<0 || mp<0 || mp>maxMp)return false;
            world.Remove(EnemyId);
            var actor=world.Get(PlayerId);actor.JobId=jobId;actor.Name=name;actor.Hp=hp;actor.MaxHp=maxHp;
            actor.Mp=mp;actor.MaxMp=maxMp;actor.Experience=experience;PlayerJobId=jobId;
            return true;
        }

        // Identifies a received/bound native map, without choosing a monster or
        // interpreting EVT TICK/REGEN operands as local seconds/counts.
        public bool ConfigureTrainingMap(int mapId)
        {
            if (!OriginalTrainingMaps.Contains(mapId)) return false;
            TrainingMapId = mapId;
            return true;
        }
    }
}
