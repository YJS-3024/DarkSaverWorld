namespace DarkSaver.Original.UI
{
    //433BE0 captures a window-local press offset;433EC0 moves without clamping
    //the window body to640x480. Save/restoration has its separate title test.
    public sealed class NativeWindowDrag
    {
        private int offsetX,offsetY;public bool Active{get;private set;}
        public bool Press(int windowX,int windowY,int pointerX,int pointerY,int left,int top,int right,int bottom,bool enabled=true)
        {
            Cancel();if(!enabled)return false;int x=unchecked(pointerX-windowX),y=unchecked(pointerY-windowY);
            if(x<left||x>=right||y<top||y>=bottom)return false;offsetX=x;offsetY=y;Active=true;return true;
        }
        public NativeWindowPosition Move(int windowX,int windowY,int pointerX,int pointerY,bool enabled=true,bool locked=false)
            =>Active&&enabled&&!locked?new NativeWindowPosition(unchecked(pointerX-offsetX),unchecked(pointerY-offsetY)):new NativeWindowPosition(windowX,windowY);
        public void Cancel(){Active=false;}
    }
}
