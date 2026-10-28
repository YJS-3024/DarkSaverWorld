namespace DarkSaver.Events
{
    public static class OriginalTutorialMinimap
    {
        // GameCtl late DlgDraw/0x10006150:100px per map dimension,3px marker at top-left.
        public static bool TryPoint(int x,int y,int width,int height,out int px,out int py)
        {
            px=py=0;if(width<=0 || height<=0 || x<0 || y<0 || x>=width || y>=height)return false;
            px=x*100/width;py=y*100/height;return true;
        }
        // Tutorial owns group0, instructor group1 uses SETALLYINFO, practice enemies group4.
        public static int Marker(int unitId,bool instructorAllied)
        {
            if(unitId<0 || unitId>=1000)return -1;int group=unitId/100;
            return group==0?0:group==1?(instructorAllied?3:2):group>=4?group:2;
        }
    }
}
