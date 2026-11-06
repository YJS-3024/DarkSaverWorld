
/* void __cdecl dmKillChar(int) */

void __cdecl dmKillChar(int param_1)

{
                    /* 0xf000  92  ?dmKillChar@@YAXH@Z */
  if (-1 < param_1) {
    if ((g_RuntimeCharacters[param_1].field_0x100 & 0x80) == 0) {
      g_RuntimeCharacters[param_1].active = 0;
      mainmapsetchar(g_RuntimeCharacters[param_1].tileX,g_RuntimeCharacters[param_1].tileY,-1);
    }
  }
  return;
}


/* incoming refs
10014154 DATA
Entry Point EXTERNAL
*/
