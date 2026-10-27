namespace DarkSaver.Network
{
    // Local practice input contract; original wire opcode/packing has not been recovered.
    public sealed class TutorialProgressRequest:ClientPacket
    {
        public int Step {get;} public bool Moved {get;} public bool Skip {get;} public bool Checkpoint {get;}
        public string Kind {get;} public int Actor {get;} public int Target {get;} public int X {get;} public int Y {get;} public string Key {get;} public int Stage {get;}
        public TutorialProgressRequest(int step,bool moved,bool skip,bool checkpoint)
            :this(step,checkpoint?"Checkpoint":"Click",skip:skip){Moved=moved;Checkpoint=checkpoint;}
        public TutorialProgressRequest(int step,string kind,int actor=-1,int target=-1,int x=-1,int y=-1,string key=null,int stage=-1,bool skip=false)
        {Step=step;Kind=kind;Actor=actor;Target=target;X=x;Y=y;Key=key;Stage=stage;Skip=skip;Checkpoint=kind=="Checkpoint";}
    }
}
