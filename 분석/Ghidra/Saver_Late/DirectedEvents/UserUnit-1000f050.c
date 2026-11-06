
/* void __cdecl dmKillChar2(int) */

void __cdecl dmKillChar2(int param_1)

{
                    /* 0xf050  91  ?dmKillChar2@@YAXH@Z */
  if (-1 < param_1) {
    *(uint *)&g_RuntimeCharacters[param_1].field_0x100 =
         *(uint *)&g_RuntimeCharacters[param_1].field_0x100 | 0x80;
  }
  return;
}


/* incoming refs
10014150 DATA
Entry Point EXTERNAL
*/
