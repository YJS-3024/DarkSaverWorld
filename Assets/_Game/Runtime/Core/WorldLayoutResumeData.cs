using System;

namespace DarkSaver.Core
{
    [Serializable] public sealed class WorldLayoutResumeMap
    {
        public string mapScene;
        public int width,height;
        public int[] safeCells;
        public bool IsValid {
            get {
                if(string.IsNullOrWhiteSpace(mapScene) || width<1 || width>512 || height<1 || height>512 || safeCells==null || safeCells.Length==0)return false;
                for(int i=0;i<safeCells.Length;i++)if(safeCells[i]<0 || safeCells[i]>=width*height || i>0 && safeCells[i]<=safeCells[i-1])return false;
                return true;
            }
        }
        // Preserve safe saved coordinates exactly; only a blocked legacy location is moved.
        public bool TryResolve(float x,float y,out float resolvedX,out float resolvedY)
        {
            resolvedX=x;resolvedY=y;
            if(!IsValid || float.IsNaN(x)||float.IsInfinity(x)||float.IsNaN(y)||float.IsInfinity(y) || x<0 || y<0 || x>=width || y>=height)return false;
            int cellX=(int)Math.Floor(x),sourceY=height-1-(int)Math.Floor(y);
            if(cellX>=0 && cellX<width && sourceY>=0 && sourceY<height && Array.BinarySearch(safeCells,sourceY*width+cellX)>=0)return true;
            double best=double.MaxValue;int chosen=-1;
            foreach(int cell in safeCells) {
                double dx=cell%width+.5-x,dy=height-cell/width-.5-y,distance=dx*dx+dy*dy;
                if(distance<best){best=distance;chosen=cell;}
            }
            resolvedX=chosen%width+.5f;resolvedY=height-chosen/width-.5f;return true;
        }
    }
    [Serializable] public sealed class WorldLayoutResumeData
    { public int version;public WorldLayoutResumeMap[] maps; }

}
