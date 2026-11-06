
/* void __cdecl OnRButtonUp(int,int) */

void __cdecl OnRButtonUp(int param_1,int param_2)

{
                    /* 0x2ee0  65  ?OnRButtonUp@@YAXHH@Z */
  if (DAT_1006c118 == 0) {
    if ((DAT_1006a70c != 0) && ((DAT_10093864 == 0 || (*(int *)(DAT_10093864 + 4) != 2)))) {
      FUN_1000a6e0(DAT_1006a70c);
    }
  }
  else if (DAT_1006c114 != 0) {
    FUN_1000a6e0(DAT_1006a70c);
    return;
  }
  return;
}


/* incoming refs
10018a48 DATA
Entry Point EXTERNAL
*/
