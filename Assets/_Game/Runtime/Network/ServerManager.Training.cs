using System;

namespace DarkSaver.Network
{
    public sealed partial class ServerManager
    {
        private TrainingProtocolBinding trainingBinding;
        private TrainingTravelService trainingTravel;
        public int LocalTrainingSessionId => trainingTravel?.SessionId ?? 0;
        public int LocalTrainingMapId => trainingTravel?.MapId ?? 0;
        public bool TryPrepareTrainingResume(DarkSaver.Core.NativeSaveData source,out DarkSaver.Core.NativeSaveData state)
        {
            state=null;
            return trainingTravel!=null && source!=null && source.mapScene=="Map_"+trainingTravel.MapId+"_;"
                && source.runtimeScene=="Training"+trainingTravel.MapId+"_Runtime"
                && trainingTravel.TryGetResumePosition(out var x,out var y)
                && DarkSaver.Core.NativeTrainingResume.TryRedirect(source,x,y,out state);
        }
        internal void AbortUncommittedTrainingEntry(int id)=>trainingTravel?.AbortUncommittedEntry(id);
        public void BindLocalTrainingTravelService(Func<TrainingTravelContext> context)
        {
            if(trainingTravel!=null)return;
            if(trainingBinding!=null && trainingBinding.Authority!=null)
                throw new InvalidOperationException("A training authority is already bound.");
            var source=UnityEngine.Resources.Load<UnityEngine.TextAsset>("World/TrainingTravel");
            var settings=source==null?null:UnityEngine.JsonUtility.FromJson<TrainingTravelSettings>(source.text);
            var service=new TrainingTravelService(settings,context);
            UnbindLocalTrainingService();BindLocalTrainingService(service);trainingTravel=service;
        }
        // With no authority, requests receive typed rejections, never fake success.
        // Training messages are separate from the building MapEnter handler owner.
        public void BindLocalTrainingService(ITrainingServerAuthority authority = null)
        {
            if (trainingBinding != null)
            {
                if (!ReferenceEquals(trainingBinding.Authority, authority))
                    throw new InvalidOperationException("A training authority is already bound. Unbind it before replacement.");
                return;
            }
            trainingBinding = new TrainingProtocolBinding(LocalServer, authority);
        }
        public void UnbindLocalTrainingService()
        { trainingBinding?.Dispose(); trainingBinding = null; trainingTravel = null; }
    }
}
