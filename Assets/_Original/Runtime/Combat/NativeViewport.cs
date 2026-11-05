using System;

namespace DarkSaver.Original.World
{
    // UserUnit F900/F940 use an integer tile origin and exclusive right/bottom.
    public readonly struct NativeViewport
    {
        public readonly int left,top,columns,rows;
        public NativeViewport(int left,int top,int columns,int rows)
        {
            if(columns<0||rows<0)throw new ArgumentOutOfRangeException();
            this.left=left;this.top=top;this.columns=columns;this.rows=rows;
        }
        public bool Contains(int x,int y)=>x>=left&&y>=top&&(long)x<(long)left+columns&&(long)y<(long)top+rows;
        public int RelativeRow(int y)=>y-top;
        // GameCtl10003D10: integer tile centering, also used before magic4001.
        public static NativeViewport Centered(int x,int y,int width,int height,int columns=20,int rows=15)
        {
            if(width<0||height<0||columns<0||rows<0)throw new ArgumentOutOfRangeException();
            return new NativeViewport((int)Math.Min(Math.Max(0L,(long)x-columns/2),Math.Max(0,width-columns)),
                (int)Math.Min(Math.Max(0L,(long)y-rows/2),Math.Max(0,height-rows)),columns,rows);
        }
        // Host adapter for the existing pixel-scrolled Unity view. Keep partially
        // visible tiles at both edges; do not change camera centering or movement.
        public static NativeViewport FromPixels(double x,double y,int width,int height)
        {
            if(double.IsNaN(x)||double.IsNaN(y)||double.IsInfinity(x)||double.IsInfinity(y)||width<0||height<0)throw new ArgumentOutOfRangeException();
            int left=(int)Math.Floor(x/32),top=(int)Math.Floor(y/32);
            return new NativeViewport(left,top,width==0?0:(int)Math.Ceiling((x+width)/32)-left,height==0?0:(int)Math.Ceiling((y+height)/32)-top);
        }
    }
}
