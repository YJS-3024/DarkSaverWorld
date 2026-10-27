namespace DarkSaver.Combat
{
    public sealed partial class BattleSceneRoot
    {
        public bool IsPersistentTraining {get;private set;}
        public bool HasTrainingRespawnInterval=>false;
        public bool TrainingRespawnPending=>false;
        public double TrainingRespawnRemaining=>0;
        public bool ConfigurePersistentTraining(BattleEnemyDefinition source,float intervalSeconds)
        {
            if(source==null || source.JobId!=EnemyJobId)return false;
            IsPersistentTraining=true;return true;
        }
        public bool ConfigureTrainingRespawnInterval(float seconds)=>false;
        public bool AdvanceTrainingRespawns(float seconds)=>false;
    }
}
