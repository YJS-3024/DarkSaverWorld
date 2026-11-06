
/* void __cdecl OnRButtonDown(int,int) */

void __cdecl OnRButtonDown(int param_1,int param_2)

{
                    /* 0x2eb0  63  ?OnRButtonDown@@YAXHH@Z */
  if (*(int *)(DAT_10093864 + 0x1c) != 0) {
    FUN_10014220(DAT_10093864);
  }
  if (*DAT_1006a708 != 0) {
    FUN_10008ca0(DAT_1006a708);
    return;
  }
  return;
}


/* incoming refs
10018a40 DATA
Entry Point EXTERNAL
*/
