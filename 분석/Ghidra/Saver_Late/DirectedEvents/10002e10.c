
/* void __cdecl OnLButtonDown(int,int) */

void __cdecl OnLButtonDown(int param_1,int param_2)

{
                    /* 0x2e10  58  ?OnLButtonDown@@YAXHH@Z */
  if (*(int *)(DAT_10093864 + 0x1c) != 0) {
    FUN_10014220(DAT_10093864);
  }
  if (*DAT_1006a708 != 0) {
    FUN_10008ca0(DAT_1006a708);
  }
  if (DAT_1006c118 == 0) {
    if ((DAT_1006a70c != (void *)0x0) && ((DAT_10093864 == 0 || (*(int *)(DAT_10093864 + 4) != 2))))
    {
      FUN_10009890(DAT_1006a70c,param_1,param_2,0);
    }
  }
  else {
    if ((DAT_1006c104 == 0) && (DAT_1006c0f0 == 0x10)) {
      DAT_1006c104 = 1;
    }
    if (DAT_1006c114 != 0) {
      FUN_10009890(DAT_1006a70c,param_1,param_2,0);
      return;
    }
  }
  return;
}


/* incoming refs
10018a2c DATA
Entry Point EXTERNAL
*/
