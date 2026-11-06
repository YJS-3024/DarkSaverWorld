
/* struct newmagicrecord const * __cdecl GetMagicTbl2(int) */

newmagicrecord * __cdecl GetMagicTbl2(int param_1)

{
  int *piVar1;
  int iVar2;
  
                    /* 0x25d0  7  ?GetMagicTbl2@@YAPBUnewmagicrecord@@H@Z */
  if (DAT_10269850 == 0) {
    LoadMagicDefinitionTables();
  }
  iVar2 = 0;
  piVar1 = &DAT_1002dbe0;
  do {
    if (*piVar1 == param_1) {
      if (iVar2 < 0) {
        return (newmagicrecord *)0x0;
      }
      return (newmagicrecord *)(&DAT_1002dbe0 + iVar2 * 0x50);
    }
    piVar1 = piVar1 + 0x50;
    iVar2 = iVar2 + 1;
  } while ((int)piVar1 < 0x1007bca1);
  return (newmagicrecord *)0x0;
}


/* incoming refs
100045b0 DATA
Entry Point EXTERNAL
*/
