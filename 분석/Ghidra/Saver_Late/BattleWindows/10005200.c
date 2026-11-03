
/* void __cdecl DlgButtonActionContinue(int,int) */

void __cdecl DlgButtonActionContinue(int param_1,int param_2)

{
  uint uVar1;
  
                    /* 0x5200  21  ?DlgButtonActionContinue@@YAXHH@Z */
  if (param_1 == 3) {
    if (param_2 == 6) {
      DAT_1001c4b8 = ((int)(DAT_1001c4b8 - 1) < 1) - 1 & DAT_1001c4b8 - 1;
    }
    else if ((param_2 == 7) && ((int)(DAT_1001c4b8 + 4) < (int)DAT_1006bd10)) {
      DAT_1001c4b8 = DAT_1001c4b8 + 1;
      return;
    }
  }
  else {
    if (param_1 == 7) {
      if (param_2 == 0) {
        DAT_1001c4b4 = ((int)(DAT_1001c4b4 - 1) < 0) - 1 & DAT_1001c4b4 - 1;
      }
      else {
        if (param_2 != 1) {
          return;
        }
        DAT_1001c4b4 = DAT_1001c4b4 + 1;
        if (DAT_1001c4d0 + -0x12 < (int)DAT_1001c4b4) {
          DAT_1001c4b4 = DAT_1001c4d0 + -0x12;
          return;
        }
      }
      return;
    }
    if (param_1 == 0x13) {
      if (param_2 == 1) {
        uVar1 = ((int)(DAT_1001c4bc - 1) < 1) - 1 & DAT_1001c4bc - 1;
      }
      else {
        if (param_2 != 2) {
          return;
        }
        (*DAT_1001c444)(0xffffffff,&param_2);
        uVar1 = param_2 - 0xc;
        if ((int)(DAT_1001c4bc + 1) < (int)uVar1) {
          DAT_1001c4bc = DAT_1001c4bc + 1;
          return;
        }
      }
      DAT_1001c4bc = uVar1;
      return;
    }
  }
  return;
}

